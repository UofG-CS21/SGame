using System;
using System.Timers;
using System.IO;
using System.Net;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using SShared;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SGame.Tests")]
namespace SGame
{
    /// <summary>
    /// Command-line arguments passed to the compute node instance.
    /// </summary>
    class CmdLineOptions
    {
        /// <summary>
        /// The HTTP hostname to bind to.
        /// </summary>
        [Option('H', "host", Default = "localhost", Required = false, HelpText = "The hostname to bind the compute node to.")]
        public string Host { get; set; }

        /// <summary>
        /// The HTTP TCP port to bind to.
        /// </summary>
        [Option('P', "port", Default = 8000u, Required = false, HelpText = "The port to bind the compute node to.")]
        public uint Port { get; set; }
    }

    /// <summary>
    /// An instance of the server.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The external REST API and its state.
        /// </summary>
        Api api;

        /// <summary>
        /// Routes REST API calls to Server.
        /// </summary>
        Router<Api> router;


        private static Timer GameLoopTimer;

        /// <summary>
        /// Initializes an instance of the program.
        /// </summary>
        Program()
        {
            // FIXME: Assuming the local SGame node manages the whole universe for now
            //        (this will change when multiple nodes are connected to a SArbiter)
            var quadtree = new LocalQuadTreeNode(null, new SShared.Quad(0.0, 0.0, double.MaxValue), 0);

            api = new Api(quadtree);
            router = new Router<Api>(api);
        }

        public async Task<bool> ProcessRequest(HttpListenerContext context)
        {
            string requestUrl = context.Request.RawUrl.Substring(1);
            Console.Error.WriteLine("Got a request: {0}", requestUrl);

            var body = new StreamReader(context.Request.InputStream).ReadToEnd();
            JObject json = new JObject();
            if (body.Length > 0)
            {
                try
                {
                    json = JObject.Parse(body);
                }
                catch (JsonReaderException exc)
                {
                    // TODO: Log this and (likely) send a HTTP 500
                }
            }

#if DEBUG
            // Handle "exit" in debug mode
            if (requestUrl == "exit")
            {
                return false;
            }
#endif

            var response = new ApiResponse(context.Response);
            var data = new ApiData(json);
            await router.Dispatch(requestUrl, response, data);
            return true;
        }

        private void SetupTimer(int frequency)
        {
            GameLoopTimer = new Timer(frequency);

            GameLoopTimer.Elapsed += UpdateGameState;
            GameLoopTimer.AutoReset = true;
            GameLoopTimer.Enabled = true;
        }

        private void UpdateGameState(Object source, ElapsedEventArgs e)
        {
            api.UpdateGameState();
            //Console.WriteLine("Updated game state at {0:HH:mm:ss.fff}", e.SignalTime);
        }

        /// <summary>
        /// Runs the main loop run for the HTTP/REST server.
        /// </summary>
        /// <param name="prefixes">A list of endpoint URLs to bind the HTTP server to.</param>
        public async Task ServerLoop(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HttpListener is not supported on this platform!");
                Environment.Exit(1);
            }

            // Create a listener.
            HttpListener listener = new HttpListener();
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            // Main server loop
            listener.Start();
            SetupTimer(30);
            Console.Error.WriteLine("Listening...");

            while (true)
            {
                var task = await listener.GetContextAsync();
                bool keepGoing = await ProcessRequest(task);
                if (!keepGoing) break;
            }


            listener.Stop();
            GameLoopTimer.Stop();
            Console.Error.WriteLine("Stopped");
        }

        /// <summary>
        /// The entry point of the program.
        /// </summary>
        static async Task Main(string[] args)
        {
            CmdLineOptions options = null;
            Parser.Default.ParseArguments<CmdLineOptions>(args)
                .WithParsed<CmdLineOptions>((opts) => options = opts);
            if (options == null)
            {
                Environment.ExitCode = -1;
                return;
            }

            string[] endpoints = new string[] { "http://" + options.Host + ":" + options.Port + "/" };
            Console.WriteLine("Endpoint: {0}", endpoints[0]);

            Program P = new Program();
            await P.ServerLoop(endpoints);
        }
    }
}
