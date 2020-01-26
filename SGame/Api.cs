using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
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
        /// Stopwatch measuring elapsed in-game time
        /// </summary>
        Stopwatch gameTime = new Stopwatch();

        // start the gameTime stopwatch on API creation
        public Api() => gameTime.Start();

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
        /// Takes data["token"] as spaceship token and looks up the spaceship ID in `players`, returning it.
        /// Returns null if the token is not present or is not present in `players`.
        /// <summary>
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

        /// <summary>
        /// Updates each spaceship's state (energy, position, ...) based on time it was not updated
        /// </summary>
        public void UpdateGameState()
        {
            foreach (int id in ships.Keys)
            {
                ships[id].UpdateState();
            }
        }

        /// <summary>
        /// Handles a "connect" REST request, connecting a player to the server.
        /// Responds with a fresh spaceship ID and player token for that spaceship.
        /// </summary>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("connect")]
        public void ConnectPlayer(ApiResponse response, ApiData data)
        {
            int playerID = freeID;
            freeID++;
            string playerToken = Guid.NewGuid().ToString();
            players[playerToken] = playerID;
            ships[playerID] = new Spaceship(playerID, gameTime);

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
            var maybeId = GetSpaceshipId(data.Json);
            if (maybeId == null)
            {
                response.Data["error"] = "Ship not found for given token";
                response.Send(500);
                return;
            }
            int id = maybeId.Value;
            var token = (string)data.Json["token"];

            Console.WriteLine("Disconnecting player with id " + id);
            ships.Remove(id);
            players.Remove(token);
            response.Send(200);
        }

        /// <summary>
        /// Handles an "accelerate" REST request.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to accelerate, and the vector of acceleration </param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("accelerate")]
        [ApiParam("token", typeof(string))]
        [ApiParam("x", typeof(float))]
        [ApiParam("y", typeof(float))]
        public void AcceleratePlayer(ApiResponse response, ApiData data)
        {
            UpdateGameState();
            var maybeId = GetSpaceshipId(data.Json);
            if (maybeId == null)
            {
                response.Data["error"] = "Ship not found for given token";
                response.Send(500);
                return;
            }
            int id = maybeId.Value;
            float x = (float)data.Json["x"];
            float y = (float)data.Json["y"];

            int energyRequired = (int)Math.Ceiling(ships[id].Area * (Math.Abs(x) + Math.Abs(y)));
            int energySpent = Math.Min(energyRequired, (int)Math.Floor(ships[id].Energy));
            ships[id].Energy -= energySpent;
            float accelerationApplied = (float)energySpent / (float)energyRequired;

            ships[id].Velocity += Vector2.Multiply(new Vector2(x, y), accelerationApplied);
            response.Send(200);
        }


        /// <summary>
        /// Handles a "ShipInfo" REST request, returning the player's spaceship info from the server.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("getShipInfo")]
        [ApiParam("token", typeof(string))]

        public void GetShipInfo(ApiResponse response, ApiData data)
        {
            UpdateGameState();
            var id = GetSpaceshipId(data.Json);
            if (id == null)
            {
                response.Data["error"] = "Could not find spaceship for given token";
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


