using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly : System.Runtime.CompilerServices.InternalsVisibleTo ("SGame.Tests")]
namespace SGame {
    /// <summary>
    /// Command-line arguments passed to the compute node instance.
    /// </summary>
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
    /// An instance of the server.
    /// </summary>
    class Program {

        /// <summary>
        /// The next free spaceship ID to use.
        /// </summary>
        int freeID = 0;

        /// <summary>
        /// The internal table of [spaceship token -> spaceship ID] for the compute node.
        /// </summary>
        Dictionary<string, int> players = new Dictionary<string, int> ();

        /// <summary>
        /// Handles a "connect" REST request, connecting a player to the server.
        /// Responds with a fresh spaceship ID and player token for that spaceship.
        /// </summary>
        /// <param name="response">The HTTP response to the client.</param>
        public void ConnectPlayer (HttpListenerResponse response) {
            int playerID = freeID;
            freeID++;
            string playerToken = Guid.NewGuid ().ToString ();
            players[playerToken] = playerID;

            Console.WriteLine ("Connected player " + playerID.ToString () + " with session token " + playerToken);

            string responseString = "{ \"id\": " + playerID + ", \"token\" : \"" + playerToken + "\" }";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes (responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write (buffer, 0, buffer.Length);
            output.Close ();
        }

        /// <summary>
        /// Handles a "disconnect" REST request, disconnecting a player from the server.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to disconnect.</param>
        /// <param name="response">The HTTP response to the client.</param>
        public void DisconnectPlayer (HttpListenerResponse response, JObject data) {
            string responseString = null, error = null;
            if (!data.ContainsKey ("token")) {
                error = "Missing token in disconnect request";
            } else {
                string token = (string) data["token"];
                if (players.ContainsKey (token)) {
                    Console.WriteLine ("Disconnecting player with session token " + token);
                    players.Remove (token);
                    responseString = "ACK";
                } else {
                    error = "Invalid spaceship token";
                }
            }

            if (error != null) {
                responseString = "{ \"error\": \"" + error + "\" }";
            }

            // Respond to the request
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes (responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write (buffer, 0, buffer.Length);
            output.Close ();
        }

        /// <summary>
        /// Runs the main loop run for the HTTP/REST server.
        /// </summary>
        /// <param name="prefixes">A list of endpoint URLs to bind the HTTP server to.</param>
        public void ServerLoop (string[] prefixes) {
            if (!HttpListener.IsSupported) {
                Console.WriteLine ("HttpListener is not supported on this platform!");
                Environment.Exit (1);
            }

            // Create a listener.
            HttpListener listener = new HttpListener ();
            foreach (string s in prefixes) {
                listener.Prefixes.Add (s);
            }

            // Main server loop
            listener.Start ();
            Console.Error.WriteLine ("Listening...");
            while (true) {
                // Note: The GetContext method blocks while waiting for a request. 
                HttpListenerContext context = listener.GetContext ();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.

                string requestUrl = request.RawUrl.Substring (1);
                Console.Error.WriteLine ("Got a request: {0}", requestUrl);
                if (requestUrl == "exit") {
                    break;
                } else if (requestUrl == "connect") {
                    ConnectPlayer (response);
                } else if (requestUrl == "disconnect") {
                    JObject JSONdata;
                    var body = new StreamReader (context.Request.InputStream).ReadToEnd ();
                    //Console.WriteLine(body);
                    JSONdata = JObject.Parse (body);
                    //Console.Write(JSONdata);
                    DisconnectPlayer (response, JSONdata);
                }
                // TODO: Respond to invalid commands!
            }

            listener.Stop ();
            Console.Error.WriteLine ("Stopped");
        }

        /// <summary>
        /// The entry point of the program.
        /// </summary>
        static void Main (string[] args) {
            Parser.Default.ParseArguments<CmdLineOptions> (args)
                .WithParsed<CmdLineOptions> (opts => {
                    string[] endpoints = new string[] { "http://" + opts.Host + ":" + opts.Port + "/" };
                    Console.WriteLine ("Endpoint: {0}", endpoints[0]);

                    Program P = new Program ();
                    P.ServerLoop (endpoints);
                });
        }
    }
}