using System;
using CommandLine;
using SGame;
using Xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGame.Tests
{


    public class RequestResponseTest
    {
        [Theory]
        [InlineData("Json dict placeholder", "Request Url Placeholder")]
        public void ResponseDataTest(string body, string requestUrl)
        {
            //JObject json = JObject.Parse(body);

        }
    }

    /// <summary>
    /// Tests that cmd line options are correctly parsed
    /// </summary>
    public class CmdLineOptions_ParseTester
    {
        private readonly string[] args;
        private readonly string Hostname = "TestHost";
        private readonly uint Port = 6000;

        public CmdLineOptions_ParseTester()
        {
            this.args = new string[] { "-H", Hostname, "-P", Port.ToString() };
        }

        [Fact]
        public void ParseTest()
        {
            Parser.Default.ParseArguments<CmdLineOptions>(this.args)
                .WithParsed<CmdLineOptions>(opts =>
                {
                    Assert.Equal(opts.Host, this.Hostname);
                    Assert.Equal(opts.Port, this.Port);
                });

        }
    }
}