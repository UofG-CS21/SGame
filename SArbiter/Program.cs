using System;
using SShared;
using CommandLine;

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
        public string CientHost { get; set; }

        /// <summary>
        /// The HTTP TCP port to bind to for clients.
        /// </summary>
        [Option('P', "client-port", Default = 8000u, Required = false, HelpText = "The port to bind to to serve the REST API.")]
        public uint ClientPort { get; set; }

        /// <summary>
        /// The UDP port to use for the master event bus.
        /// </summary>
        [Option("bus-port", Default = 3000u, Required = false, HelpText = "The UDP port to use for the master event bus.")]
        public uint BusPort { get; set; }
    }


    class Program
    {
        static void Loop(CmdLineOptions opts)
        {
            using (NetNode busMaster = new NetNode(null, (int)opts.BusPort))
            {
                Console.WriteLine("Listening...");
                while (true)
                {
                    busMaster.Update();
                }
            }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CmdLineOptions>(args)
                .WithParsed<CmdLineOptions>(opts => Loop(opts));
        }
    }
}
