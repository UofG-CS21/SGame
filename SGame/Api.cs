using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Numerics;
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
        /// Internal game state of [spaceship ID -> Spaceship ] for the server.
        /// </summary>
        Dictionary<int, Spaceship> ships = new Dictionary<int, Spaceship>();

        /// <summary>
        /// Handles a "connect" REST request, connecting a player to the server.
        /// Responds with a fresh spaceship ID and player token for that spaceship.
        /// </summary>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("connect")]

        Nullable<int> GetSpaceshipId(JObject data)
        {
            if (!data.ContainsKey("token"))
            {
                return null;
            }
            var token = (string)data["token"];

            if (players.ContainsKey(token))
            {
                return players[token];
            }
            return null;
        }

        public void ConnectPlayer(ApiResponse response, ApiData data)
        {
            int playerID = freeID;
            freeID++;
            string playerToken = Guid.NewGuid().ToString();
            players[playerToken] = playerID;
            ships[playerID] = new Spaceship(playerID);

            Console.WriteLine("Connected player " + playerID.ToString() + " with session token " + playerToken);

            response.Data["id"] = playerID;
            response.Data["token"] = playerToken;
            response.Send();
        }

        /// <summary>
        /// Handles a "disconnect" REST request, disconnecting a player from the server.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to disconnect.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("disconnect")]
        [ApiParam("token", typeof(string))]
        public void DisconnectPlayer(ApiResponse response, ApiData data)
        {
            if (!data.Json.ContainsKey("token"))
            {
                response.Data["error"] = "Missing token in disconnect request";
                response.Send(500);
                return;
            }

            string token = (string)data.Json["token"];
            if (players.ContainsKey(token))
            {
                Console.WriteLine("Disconnecting player with session token " + token);
                ships.Remove(players[token]);
                players.Remove(token);
                response.Send(200);
            }
            else
            {
                response.Data["error"] = "Invalid spaceship token";
                response.Send(500);
            }

        }

        /// <summary>
        /// Handles a "accelerate" REST request, .
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to accelerate.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("accelerate")]
        [ApiParam("token", typeof(string))]
        [ApiParam("x", typeof(float))]
        [ApiParam("y", typeof(float))]
        public void AcceleratePlayer(ApiResponse response, ApiData data)
        {
            if (!data.Json.ContainsKey("token"))
            {
                response.Data["error"] = "Missing token in disconnect request";
                response.Send(500);
                return;
            }

            string token = (string)data.Json["token"];
            if (players.ContainsKey(token))
            {
                Console.WriteLine("Accelerating player with session token " + token);
                int id = players[token];
                float x = (float)data.Json["x"];
                float y = (float)data.Json["y"];
                ships[id].Velocity = new Vector2(x, y);
                response.Send(200);
            }
            else
            {
                response.Data["error"] = "Invalid spaceship token";
                response.Send(500);
            }
        }


        /// <summary>
        /// Handles a "ShipInfo" REST request, returning the player's spaceship info from the server.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to disconnect.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("ShipInfo")]
        [ApiParam("token", typeof(string))]

        public void ShipInfo(ApiResponse response, ApiData data)
        {
            var id = GetSpaceshipId(data.Json);
            if (id == null)
            {
                response.Data["error"] = "Missing token in disconnect request";
                response.Send(500);
                return;
            }

            Spaceship ship = ships[id.Value];
            response.Data["area"] = ship.Area;
            response.Data["id"] = ship.Id;
            response.Data["energy"] = ship.Energy;
            response.Data["posX"] = ship.Pos.X;
            response.Data["posY"] = ship.Pos.Y;
            response.Data["velX"] = ship.Velocity.X;
            response.Data["velY"] = ship.Velocity.Y;

            response.Send();
        }
    }


}


