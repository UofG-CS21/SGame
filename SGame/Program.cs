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
        /// The hostname of the SArbiter.
        /// </summary>
        [Option("arbiter", Default = "localhost", Required = false, HelpText = "Hostname or address of the SArbiter managing this compute node.")]
        public string Arbiter { get; set; }

        /// <summary>
        /// The HTTP address to serve the SGame REST API on.
        /// </summary>
        [Option("api-url", Default = "http://127.0.0.1:9000/", Required = false,
         HelpText = "The HTTP address to serve the SGame REST API on. Must be externally accessible for multinode setups!")]
        public string ApiUrl { get; set; }


        /// <summary>
        /// The UDP port of the arbiter's master event bus.
        /// </summary>
        [Option("arbiter-bus-port", Default = 4242u, Required = false, HelpText = "Externally-visible UDP port of the arbiter's event bus.")]
        public uint ArbiterBusPort { get; set; }

        /// <summary>
        /// The UDP port of this node's master event bus.
        /// </summary>
        [Option("local-bus-port", Default = 4242u, Required = false, HelpText = "Externally-visible UDP port of this node's event bus.")]
        public uint LocalBusPort { get; set; }

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
        /// Size of the game universe.
        /// </summary>
        public const double UniverseSize = 1 << 31;

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
            this.bus = new NetNode(listenPort: (int)options.LocalBusPort);
            this.persistence = options.PersistenceUrl != null ? new Persistence(options.PersistenceUrl) : null;
            LiteNetLib.NetPeer arbiterPeer = this.bus.Connect(options.Arbiter, (int)options.ArbiterBusPort);

            // On startup, the local SGame node assumes it manages the whole universe.
            var localTree = new LocalQuadTreeNode(new SShared.Quad(0.0, 0.0, UniverseSize), 0);
            var rootNode = localTree;

            this.api = new Api(options.ApiUrl, rootNode, localTree, bus, arbiterPeer, options.LocalBusPort, persistence);
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
                listener.Prefixes.Add(options.ApiUrl);

                // Main server loop
                listener.Start();
                SetupTimer((int)options.Tickrate);
                Console.Error.WriteLine("Listening...");
                Console.Error.WriteLine("(API on {0})", options.ApiUrl);

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
