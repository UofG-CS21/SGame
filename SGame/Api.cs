using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGame
{
    /// <summary>
    /// The implementation of the externally-visible REST API.
    /// </summary>
    class Api
    {
        /// <summary>
        /// The next free spaceship ID to use.
        /// </summary>
        int freeID = 0;

        /// <summary>
        /// The internal table of [spaceship token -> spaceship ID] for the compute node.
        /// </summary>
        Dictionary<string, int> players = new Dictionary<string, int>();


        /// <summary>
        /// Handles a "connect" REST request, connecting a player to the server.
        /// Responds with a fresh spaceship ID and player token for that spaceship.
        /// </summary>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("connect")]
        public void ConnectPlayer(HttpListenerResponse response, JObject data)
        {
            int playerID = freeID;
            freeID++;
            string playerToken = Guid.NewGuid().ToString();
            players[playerToken] = playerID;

            Console.WriteLine("Connected player " + playerID.ToString() + " with session token " + playerToken);

            string responseString = "{ \"id\": " + playerID + ", \"token\" : \"" + playerToken + "\" }";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        /// <summary>
        /// Handles a "disconnect" REST request, disconnecting a player from the server.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to disconnect.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("disconnect")]
        public void DisconnectPlayer(HttpListenerResponse response, JObject data)
        {
            string responseString = null, error = null;
            if (!data.ContainsKey("token"))
            {
                error = "Missing token in disconnect request";
            }
            else
            {
                string token = (string)data["token"];
                if (players.ContainsKey(token))
                {
                    Console.WriteLine("Disconnecting player with session token " + token);
                    players.Remove(token);
                    responseString = "ACK";
                }
                else
                {
                    error = "Invalid spaceship token";
                }
            }

            if (error != null)
            {
                responseString = "{ \"error\": \"" + error + "\" }";
            }

            // Respond to the request
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

    }
}
