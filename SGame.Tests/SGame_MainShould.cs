using System;
using CommandLine;
using SGame;
using Xunit;

namespace SGame.UnitTests {
    class CmdLineOptions {
        /// <summary>
        /// The HTTP hostname to bind to.
        /// </summary>
        [Option ('H', "host", Default = "localhost", Required = false, HelpText = "The hostname to bind the compute node to.")]
        public string Host { get; set; }

        /// <summary>
        /// The HTTP TCP port to bind to.
        /// </summary>
        [Option ('P', "port", Default = 8000u, Required = false, HelpText = "The port to bind the compute node to.")]
        public uint Port { get; set; }
    }

    /// <summary>
    /// Tests that cmd line options are correctly parsed
    /// </summary>
    public class CmdLineOptions_ParseTester {
        private readonly string[] args;
        private readonly string Hostname = "TestHost";
        private readonly uint Port = 6000;

        public CmdLineOptions_ParseTester () {
            this.args = new string[] { "-H", Hostname, "-P", Port.ToString () };
        }

        [Fact]
        public void ParseTest () {
            Parser.Default.ParseArguments<CmdLineOptions> (this.args)
                .WithParsed<CmdLineOptions> (opts => {
                    Assert.Equal (opts.Host, this.Hostname);
                    Assert.Equal (opts.Port, this.Port);
                });

        }
    }
}