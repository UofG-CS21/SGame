using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        /// Initializes an instance of the program.
        /// </summary>
        Program()
        {
            api = new Api();
            router = new Router<Api>(api);
        }

        /// <summary>
        /// Runs the main loop run for the HTTP/REST server.
        /// </summary>
        /// <param name="prefixes">A list of endpoint URLs to bind the HTTP server to.</param>
        public void ServerLoop(string[] prefixes)
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
            Console.Error.WriteLine("Listening...");
            while (true)
            {
                // Note: The GetContext method blocks while waiting for a request. 
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.

                string requestUrl = request.RawUrl.Substring(1);
                Console.Error.WriteLine("Got a request: {0}", requestUrl);

                JObject data;
                var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                if(body.Length > 0)
                {
                    data = JObject.Parse(body);
                }
                else
                {
                    data = new JObject();
                }

#if DEBUG
                // Handle "exit" in debug mode
                if(requestUrl == "exit")
                {
                    break;
                }
#endif

                router.Dispatch(requestUrl, response, data);
            }

            listener.Stop();
            Console.Error.WriteLine("Stopped");
        }

        /// <summary>
        /// The entry point of the program.
        /// </summary>
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CmdLineOptions>(args)
                .WithParsed<CmdLineOptions>(opts =>
                {
                    string[] endpoints = new string[] { "http://" + opts.Host + ":" + opts.Port + "/" };
                    Console.WriteLine("Endpoint: {0}", endpoints[0]);

                    Program P = new Program();
                    P.ServerLoop(endpoints);
                });
        }
    }
}