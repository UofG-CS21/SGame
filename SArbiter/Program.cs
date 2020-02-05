using System;
using CommandLine;

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
        public uint BusPost { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CmdLineOptions>(args)
                .WithParsed<CmdLineOptions>(opts =>
                {
                });
        }
    }
}
