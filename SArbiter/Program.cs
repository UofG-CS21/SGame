using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SShared;
using System.Threading.Tasks;
using System.Text;
using System.Timers;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SArbiter.Tests")]
namespace SArbiter
{

    /// <summary>
    /// Command-line arguments passed to the arbiter.
    /// </summary>
    class CmdLineOptions
    {
        /// <summary>
        /// The HTTP hostname to bind to for clients.
        /// </summary>
        [Option('H', "client-host", Default = "localhost", Required = false, HelpText = "The hostname to serve the REST API on.")]
        public string ClientHost { get; set; }

        /// <summary>
        /// The HTTP TCP port to bind to for clients.
        /// </summary>
        [Option('P', "client-port", Default = 8000u, Required = false, HelpText = "The port to bind to to serve the REST API.")]
        public uint ClientPort { get; set; }

        /// <summary>
        /// The hostname to bind to for the master event bus.
        /// </summary>
        [Option("bus-host", Default = "localhost", Required = false, HelpText = "The hostname to bind to for the master event bus.")]
        public string BusHost { get; set; }

        /// <summary>
        /// The UDP port to use for the master event bus.
        /// </summary>
        [Option("bus-port", Default = 3000u, Required = false, HelpText = "The UDP port to use for the master event bus.")]
        public uint BusPort { get; set; }

        /// <summary>
        /// Tickrate of the sever.
        /// </summary>
        [Option("tickrate", Default = 100u, Required = false, HelpText = "Tickrate of the arbiter, i.e. how often it polls for bus events per second.")]
        public uint Tickrate { get; set; }
    }

    class Program : IDisposable
    {
        private NetNode _busMaster = null;

        private RoutingTable _routingTable = null;

        private Router<ArbiterApi> _apiRouter = null;

        private Timer _updateTimer = null;

        internal Program(CmdLineOptions options)
        {
            _busMaster = new NetNode(options.BusHost, (int)options.BusPort);
            _routingTable = new RoutingTable(_busMaster, null);
            _apiRouter = new Router<ArbiterApi>(new ArbiterApi(_routingTable));

            _updateTimer = new Timer(1000.0 / options.Tickrate);
            _updateTimer.AutoReset = true;
            _updateTimer.Elapsed += Update;
        }

        public void Dispose()
        {
            _busMaster.Dispose();
        }

        private async Task<bool> ProcessRequest(HttpListenerContext context)
        {
            string route = context.Request.RawUrl.Substring(1);
            Console.Error.WriteLine("Got a request: {0}", route);
#if DEBUG
            if (route == "exit")
            {
                return false;
            }
#endif
            ApiResponse response = new ApiResponse(context.Response);

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
                    response.Data["error"] = "Malformed request: " + exc.Message;
                    await response.Send(500);
                    return true;
                }
            }

            ApiData data = new ApiData(json);
            await _apiRouter.Dispatch(route, response, data);
            return true;
        }

        private void Update(object sender, ElapsedEventArgs e)
        {
            _busMaster.Update();
        }

        public async Task ServerLoop(SArbiter.CmdLineOptions options)
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HttpListener is not supported on this platform!");
                Environment.Exit(1);
            }

            using (HttpListener listener = new HttpListener())
            using (NetNode busMaster = new NetNode(null, (int)options.BusPort))
            {
                listener.Prefixes.Add($"http://{options.ClientHost}:{options.ClientPort}/");

                // Main server loop
                listener.Start();
                _updateTimer.Start();
                Console.Error.WriteLine("Listening...");

                bool keepGoing = true;
                do
                {
                    var task = await listener.GetContextAsync();
                    keepGoing = await ProcessRequest(task);
                }
                while (keepGoing);

                listener.Stop();
                _updateTimer.Stop();
                Console.Error.WriteLine("Stopped");
            }
        }

        /// <summary>
        /// The entry point of the program.
        /// </summary>
        static async Task Main(string[] args)
        {
            SArbiter.CmdLineOptions options = null;
            Parser.Default.ParseArguments<SArbiter.CmdLineOptions>(args)
                .WithParsed<SArbiter.CmdLineOptions>((opts) => options = opts);
            if (options == null)
            {
                Environment.Exit(-1);
            }

            using (Program P = new Program(options))
            {
                await P.ServerLoop(options);
            }
        }
    }
}
