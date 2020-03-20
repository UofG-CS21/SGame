using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Timers;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LiteNetLib;
using SShared;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SArbiter.Tests")]
namespace SArbiter
{

    /// <summary>
    /// Command-line arguments passed to the arbiter.
    /// </summary>
    class CmdLineOptions
    {
        /// <summary>
        /// The HTTP port to serve the REST API on.
        /// </summary>
        [Option("api-url", Default = "http://127.0.0.1:8000/", Required = false, HelpText = "The HTTP address to serve the REST API on.")]
        public string ApiUrl { get; set; }

        /// <summary>
        /// The UDP port to use for the master event bus.
        /// </summary>
        [Option("bus-port", Default = 4242u, Required = false, HelpText = "The UDP port to use for the master event bus.")]
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

        private uint _busPort;

        private RoutingTable _routingTable = null;

        private Router<ArbiterApi> _apiRouter = null;

        private Timer _updateTimer = null;

        internal Program(CmdLineOptions options)
        {
            _busMaster = new NetNode(listenPort: (int)options.BusPort);
            _busPort = options.BusPort;
            // TODO: Command-line flag to change universe size
            _routingTable = new RoutingTable(_busMaster, null, 1 << 31);
            _apiRouter = new Router<ArbiterApi>(new ArbiterApi(_routingTable));

            _busMaster.PeerConnectedEvent += OnSGameConnected;
            _busMaster.PeerDisconnectedEvent += OnSGameDisconnected;

            _updateTimer = new Timer(1000.0 / options.Tickrate);
            _updateTimer.AutoReset = true;
            _updateTimer.Elapsed += Update;
        }

        public void Dispose()
        {
            _updateTimer.Dispose();
            _busMaster.Dispose();
        }

        private void OnSGameConnected(NetPeer peer)
        {
            var newNodeInfoWaiter = new MessageWaiter<SShared.Messages.NodeConfig>(_busMaster, peer).Wait;
            newNodeInfoWaiter.Wait();
            var newNodeInfo = newNodeInfoWaiter.Result;

            var newNode = _routingTable.AddSGameNode(peer, newNodeInfo.BusAddress, newNodeInfo.BusPort, newNodeInfo.ApiUrl);
            Console.Error.WriteLine(">>> SGame node {0} (API: {1}) connected at {2} <<<", peer.EndPoint, newNode.ApiUrl, newNode.Path());

            // IMPORTANT: Send the whole network topology (as of now) to the new node (so that it can build a routing table for itself)
            foreach (var treeNode in _routingTable.RootNode.Traverse().Cast<ArbiterTreeNode>())
            {
                if (treeNode.Peer == peer) continue;

                var otherNodeConfig = new SShared.Messages.NodeConfig()
                {
                    BusAddress = treeNode.BusAddress,
                    BusPort = treeNode.BusPort,
                    Bounds = treeNode.Bounds,
                    Path = treeNode.Path(),
                    ApiUrl = treeNode.ApiUrl,
                };
                _busMaster.SendMessage(otherNodeConfig, peer, DeliveryMethod.ReliableOrdered);
            }

            // IMPORTANT: Broadcast that the new node is online and where it is (incl. the new node itself to tell it its path)
            var newNodeConfig = new SShared.Messages.NodeConfig()
            {
                BusAddress = newNode.BusAddress,
                BusPort = newNode.BusPort,
                Bounds = newNode.Bounds,
                Path = newNode.Path(),
                ApiUrl = newNode.ApiUrl,
            };
            _busMaster.BroadcastMessage(newNodeConfig, DeliveryMethod.ReliableOrdered);
        }

        private void OnSGameDisconnected(NetPeer peer, DisconnectInfo info)
        {
            Console.Error.WriteLine(">>> SGame node {0} disconnected ({1}) <<<", peer.EndPoint, info.Reason);
            _routingTable.RemoveSGameNode(peer).Wait();
        }

        private async Task<bool> ProcessRequest(HttpListenerContext context)
        {
            string route = context.Request.RawUrl.Substring(1);
#if DEBUG
            if (route == "exit")
            {
                Console.Error.WriteLine(">>> Exit <<<");
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
            using (NetNode busMaster = new NetNode(listenPort: (int)options.BusPort))
            {
                listener.Prefixes.Add(options.ApiUrl);

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
