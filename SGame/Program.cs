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

        /// <summary>
        /// The hostname of the SArbiter.
        /// </summary>
        [Option("arbiter", Default = "localhost", Required = false, HelpText = "Hostname of the SArbiter managing this compute node.")]
        public string Arbiter { get; set; }

        /// <summary>
        /// The UDP port of the master event bus.
        /// </summary>
        [Option("bus-port", Default = 4242u, Required = false, HelpText = "UDP port of the SArbiter master event bus.")]
        public uint BusPort { get; set; }

        /// <summary>
        /// The URL of the ElasticSearch server used for persistence.
        /// </summary>
        [Option("persistence", Default = null, Required = false, HelpText = "URL of the ElasticSearch server used for persistence (optional)")]
        public string PersistenceUrl { get; set; }

        /// <summary>
        /// SGame's tickrate, i.e. the updates-per-second of the main loop.
        /// </summary>
        [Option('T', "tickrate", Default = 30u, Required = false, HelpText = "SGame tickrate (updates per second).")]
        public uint Tickrate { get; set; }
    }

    /// <summary>
    /// An instance of the server.
    /// </summary>
    class Program : IDisposable
    {
        /// <summary>
        /// SGame instance options.
        /// </summary>
        CmdLineOptions options;

        /// <summary>
        /// The external REST API and its state.
        /// </summary>
        Api api;

        /// <summary>
        /// Routes REST API calls to Server.
        /// </summary>
        Router<Api> router;

        /// <summary>
        /// The timer that periodically updates the gamestate and event bus.
        /// </summary>
        private static Timer GameLoopTimer;

        /// <summary>
        /// Connected to the SArbiter master event bus.
        /// </summary>
        NetNode bus;

        /// <summary>
        /// Connected to the ElasticSearch master used to persist ship.
        /// </summary>
        Persistence persistence;

        /// <summary>
        /// Initializes an instance of the program.
        /// </summary>
        Program(CmdLineOptions options)
        {
            this.options = options;
            this.bus = new NetNode(options.Arbiter, (int)options.BusPort);
            this.persistence = options.PersistenceUrl != null ? new Persistence(options.PersistenceUrl) : null;

            // FIXME: Assuming the local SGame node manages the whole universe for now
            //        (this will change when multiple nodes are connected to a SArbiter)
            var quadtree = new LocalQuadTreeNode(null, new SShared.Quad(0.0, 0.0, 1 << 31), 0);

            this.api = new Api(quadtree, bus, persistence);
            this.router = new Router<Api>(api);
        }

        public void Dispose()
        {
            bus.Dispose();
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

            GameLoopTimer.Elapsed += GameLoopTick;
            GameLoopTimer.AutoReset = true;
            GameLoopTimer.Enabled = true;
        }

        private void GameLoopTick(Object source, ElapsedEventArgs e)
        {
            bus.Update();
            api.UpdateGameState();
            //Console.WriteLine("Updated game state at {0:HH:mm:ss.fff}", e.SignalTime);
        }

        /// <summary>
        /// Runs the main loop run for the HTTP/REST server.
        /// </summary>
        public async Task ServerLoop()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HttpListener is not supported on this platform!");
                Environment.Exit(1);
            }

            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add($"http://{options.Host}:{options.Port}/");

                // Main server loop
                listener.Start();
                SetupTimer((int)options.Tickrate);
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

            using (Program P = new Program(options))
            {
                await P.ServerLoop();
            }
        }
    }
}
