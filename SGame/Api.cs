using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SGame.Tests")]
namespace SGame
{
    /// <summary>
    /// The implementation of the externally-visible REST API.
    /// </summary>
    class Api
    {
        /// <summary>
        /// Manges the elapsed in-game time.
        /// </summary>
        GameTime gameTime;

        // start the gameTime stopwatch on API creation
        public Api()
        {
            this.gameTime = new GameTime();
        }

        /// <summary>
        /// The next free spaceship ID to use.
        /// </summary>
        int freeID = 0;

        /// <summary>
        /// The internal table of [spaceship token -> spaceship ID] for the compute node.
        /// </summary>
        Dictionary<string, int> players = new Dictionary<string, int>();

        /// <summary>
        /// The internal table of [spaceship ID -> spaceship token] for the compute node.
        /// Used to remove killed ships from players.
        /// </summary>
        Dictionary<int, string> inversePlayers = new Dictionary<int, string>();


        /// <summary>
        /// Internal game state of [spaceship ID -> Spaceship ] for the server.
        /// </summary>
        Dictionary<int, Spaceship> ships = new Dictionary<int, Spaceship>();

        /// <summary>
        /// The internal table of spaceship tokens, containing dead spaceships.
        /// </summary>
        HashSet<string> deadPlayers = new HashSet<string>();


        /// <summary>
        /// Takes data["token"] as spaceship token and looks up the spaceship ID in `players`, returning it.
        /// If token is present and valid, returns the relevant player ID.
        /// Otherwise, sends an error response, and returns null.
        /// <summary>
        Nullable<int> GetSpaceshipId(ApiResponse response, JObject data)
        {

            if (!data.ContainsKey("token"))
            {
                response.Data["error"] = "Spaceship token not in sent data.";
                response.Send(500);
                return null;
            }
            var token = (string)data["token"];

            if (players.ContainsKey(token))
            {
                return players[token];
            }

            if (deadPlayers.Contains(token))
            {

                deadPlayers.Remove(token);
                response.Data["error"] = "Your spaceship has been killed. Please reconnect.";
                response.Send(500);
                return null;
            }

            response.Data["error"] = "Ship not found for given token.";
            response.Send(500);
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
            inversePlayers[playerID] = playerToken;
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
            var maybeId = GetSpaceshipId(response, data.Json);
            if (maybeId == null)
            {
                return;
            }
            int id = maybeId.Value;
            var token = (string)data.Json["token"];

            Console.WriteLine("Disconnecting player with id " + id);
            ships.Remove(id);
            players.Remove(token);
            inversePlayers.Remove(id);
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
            var maybeId = GetSpaceshipId(response, data.Json);
            if (maybeId == null)
            {
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
            var id = GetSpaceshipId(response, data.Json);
            if (id == null)
            {
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

        /// <summary>
        /// Returns a list of ID's of ships that lie within a triangle with one vertex at pos, the center of its opposite side
        /// is at an angle of worldDeg degrees from the vertex, its two other sides are an angle scanWidth from this point, and
        /// its area will be equal to SCAN_ENERGY_SCALING_FACTOR times the energy spent
        /// </summary>

        private const int SCAN_ENERGY_SCALING_FACTOR = 2000;

        public List<int> CircleSectorScan(Vector2 pos, double worldDeg, double scanWidth, int energySpent)
        {
            // The radius of the cone will be such that the area scanned is energySpent * SCAN_ENERGY_SCALING_FACTOR
            double areaScanned = energySpent * SCAN_ENERGY_SCALING_FACTOR;

            // Convert angles to radians
            worldDeg = (Math.PI * worldDeg) / 180.0;
            scanWidth = (Math.PI * scanWidth) / 180.0;

            // We want the radius of the circle, such that a sercular sector of angle 2*scanwidth has area areaScanned
            float radius = (float)Math.Sqrt(areaScanned / (2 * scanWidth));

            // The circular sector is a triangle whose vertices are pos, and the points at an angle (worldDeg +- scanWidth) and distance radius
            // And a segment between those points on the circle centered at pos with that radius

            Vector2 leftPoint = new Vector2(radius * (float)Math.Cos(worldDeg + scanWidth), radius * (float)Math.Sin(worldDeg + scanWidth));
            Vector2 rightPoint = new Vector2(radius * (float)Math.Cos(worldDeg - scanWidth), radius * (float)Math.Sin(worldDeg - scanWidth));

            Console.WriteLine("Scanning with radius " + radius + "; In triangle " + pos.ToString() + "," + leftPoint.ToString() + "," + rightPoint.ToString());

            List<int> result = new List<int>();

            // Go through all spaceships and add those that intersect with our triangle
            foreach (int id in ships.Keys)
            {
                if (MathsUtil.CircleTriangleIntersection(ships[id].Pos, ships[id].Radius(), pos, leftPoint, rightPoint) || MathsUtil.CircleSegmentIntersection(ships[id].Pos, (float)ships[id].Radius(), pos, radius, (float)worldDeg, (float)scanWidth))
                {
                    //Console.WriteLine("Intersected");
                    result.Add(id);
                }
            }

            return result;
        }


        /// <summary>
        /// Handles a "Scan" REST request, returning a set of spaceships that are within the scan
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship, the angle of scanning, the width of scan, and the energy spent on the scan.true </param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("scan")]
        [ApiParam("token", typeof(string))]
        [ApiParam("direction", typeof(float))]
        [ApiParam("width", typeof(float))]
        [ApiParam("energy", typeof(int))]

        public void Scan(ApiResponse response, ApiData data)
        {
            UpdateGameState();

            int id = IntersectionParamCheck(response, data);

            Spaceship ship = ships[id];
            int energy = (int)data.Json["energy"];
            float direction = (float)data.Json["direction"];
            float width = (float)data.Json["width"];
            energy = (int)Math.Min(energy, Math.Floor(ship.Energy));
            ships[id].Energy -= energy;

            Console.WriteLine("Scan by " + id + ", pos = " + ships[id].Pos.ToString() + " , direction = " + direction + ", width = " + width + ", energy spent = " + energy);

            List<int> scanned = CircleSectorScan(ship.Pos, direction, width, energy);
            JArray scannedShips = new JArray();
            foreach (int scannedId in scanned)
            {
                // ignore our ship
                if (scannedId == id)
                    continue;

                JToken scannedShipInfo = new JObject();
                scannedShipInfo["id"] = scannedId;
                scannedShipInfo["area"] = ships[scannedId].Area;
                scannedShipInfo["posX"] = ships[scannedId].Pos.X;
                scannedShipInfo["posY"] = ships[scannedId].Pos.Y;
                scannedShips.Add(scannedShipInfo);
            }

            response.Data["scanned"] = scannedShips;
            response.Send();
        }

        private const double MINIMUM_AREA = 0.75;
        /// <summary>
        /// Handles a "Shoot" REST request, damaging all ships caught in its blast. pew pew.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship, the angle to shoot at, the width of the shot, the energy to expend on the shot (determines distance), and damage (scaling) </param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("shoot")]
        [ApiParam("token", typeof(string))]
        [ApiParam("direction", typeof(float))]
        [ApiParam("width", typeof(float))]
        [ApiParam("energy", typeof(int))]
        [ApiParam("damage", typeof(float))]

        public void Shoot(ApiResponse response, ApiData data)
        {
            //Check that the arguments for each parameter are valid
            int id = IntersectionParamCheck(response, data, true);
            if (id == -1)
            {
                return;
            }
            Spaceship ship = ships[id];
            float width = (float)data.Json["width"];
            float direction = (float)data.Json["direction"];
            float damageScaling = (float)data.Json["damage"];

            int energy = (int)Math.Min((int)data.Json["energy"], Math.Floor(ship.Energy / damageScaling));
            ships[id].Energy -= energy * damageScaling; //remove energy for the shot

            Console.WriteLine("Shot by " + id + ", pos = " + ships[id].Pos.ToString() + " , direction = " + direction + ", width = " + width + ", energy spent = " + energy + ", scaling = " + damageScaling);

            List<int> struck = CircleSectorScan(ship.Pos, direction, width, energy);
            JArray struckShips = new JArray();
            foreach (int struckShipId in struck)
            {
                // ignore our ship
                if (struckShipId == id)
                    continue;

                //The api doesnt have a return value for shooting, but ive left this in for now for testing purposes.
                JToken struckShipInfo = new JObject();
                struckShipInfo["id"] = struckShipId;
                struckShipInfo["area"] = ships[struckShipId].Area;
                struckShipInfo["posX"] = ships[struckShipId].Pos.X;
                struckShipInfo["posY"] = ships[struckShipId].Pos.Y;
                struckShips.Add(struckShipInfo);

                double damage = ShotDamage(energy, width, damageScaling, Vector2.Subtract(ships[id].Pos, ships[struckShipId].Pos).Length());
                // We have killed a ship, gain it's kill reward, and move struck ship to the graveyard
                if (ships[struckShipId].Area - damage < MINIMUM_AREA)
                {
                    ships[id].Area += ships[struckShipId].KillReward;
                    ships.Remove(struckShipId);
                    players.Remove(inversePlayers[struckShipId]);
                    deadPlayers.Add(inversePlayers[struckShipId]);
                    inversePlayers.Remove(struckShipId);
                }
                else // Struck ship survived - note that it's in combat
                {
                    if (ships[struckShipId].LastUpdate - ships[struckShipId].LastCombat > Spaceship.COMBAT_COOLDOWN)
                    {
                        // Reset kill reward when hit ship was not in combat
                        ships[struckShipId].KillReward = ships[struckShipId].Area;
                    }
                    ships[struckShipId].LastCombat = ships[struckShipId].LastUpdate;
                    ships[struckShipId].Area -= damage;
                }
            }

            //Ship performed combat action, lock kill reward if not in combat from before
            if (ships[id].LastUpdate - ships[id].LastCombat > Spaceship.COMBAT_COOLDOWN)
            {
                ships[id].KillReward = ships[id].Area;
            }
            ships[id].LastCombat = ships[id].LastUpdate;

            response.Data["struck"] = struckShips;
            response.Send();
        }


        /// <summary>
        /// Calculates the shotDamage applied to a ship. Shot damage drops off exponentially as distance increases, base =1.1
        /// </summary>
        private float ShotDamage(int energy, float width, float scaling, float distance)
        {
            distance = Math.Max(distance, 1);
            width = (float)(Math.PI * width) / (float)180.0;
            return (float)(energy * scaling) / (float)(Math.Max(1, Math.Pow(2, 2 * width)) * Math.Sqrt(distance));

            /* 
                A new ship shoots at another new ship, using all its 10 energy. It can oneshot the ship at
                [angle width] -> [oneshot distance]
                90  20
                45  180
                30  352
                15  800
                1   1500
            */
        }

        /// <summary>
        /// Verifies the arguments passed in an intersection based request are appropriate.
        /// </summary>
        private int IntersectionParamCheck(ApiResponse response, ApiData data, bool requireDamage = false)
        {
            UpdateGameState();
            var maybeId = GetSpaceshipId(response, data.Json);
            if (maybeId == null)
            {
                return -1;
            }

            int id = maybeId.Value;

            String[] requiredParams = new String[3] { "direction", "width", "energy" };

            for (int i = 0; i < requiredParams.Length; i++)
            {
                if (data.Json[requiredParams[i]] == null)
                {
                    response.Data["error"] = "Requires parameter: " + requiredParams[i];
                    response.Send(500);
                    return -1;
                }
            }

            float direction = (float)data.Json["direction"];

            float width = (float)data.Json["width"];
            if (width <= 0 || width >= 90)
            {
                response.Data["error"] = "Width not in interval (0,90) degrees";
                response.Send(500);
                return -1;
            }

            int energy = (int)data.Json["energy"];
            if (energy <= 0)
            {
                response.Data["error"] = "Energy spent must be positive";
                response.Send(500);
                return -1;
            }

            if (requireDamage)
            {
                if (data.Json["damage"] == null)
                {
                    response.Data["error"] = "Requires parameter: " + "damage";
                    response.Send(500);
                    return -1;
                }

                float damage = (float)data.Json["damage"];
                if (damage <= 0)
                {
                    response.Data["error"] = "Damage scaling must be positive";
                    response.Send(500);
                    return -1;
                }
            }

            return id;
        }

        private int DistanceBetweenShips(float x1, float y1, float x2, float y2)
        {
            return (int)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

#if DEBUG
        /// <summary>
        /// A function that, when invoked, sets a certain parameter of `ship` to `value`.
        /// </summary>
        /// <param name="api">The API instance.</param>
        /// <param name="ship">The ship to set the attribute on.</param>
        /// <param name="value">The new value of the parameter to set.</param>
        private delegate void AttributeSetter(Api api, Spaceship ship, JToken value);

        /// <summary>
        /// The map of `JSON key name -> AttributeSetter` used by `Sudo`.
        /// Note that the names reflect those used by `GetShipInfo`!
        /// 
        /// In the future, if setters become more complex than just setting a value, one can replace the lambdas with
        /// references to a more complex function/method.
        /// </summary>
        private static readonly Dictionary<string, AttributeSetter> SUDO_SETTER_MAP = new Dictionary<string, AttributeSetter>
        {
            { "area", (api, ship, value) => ship.Area = (double)value },
            { "energy", (api, ship, energy) => ship.Energy = (double)energy },
            { "posX", (api, ship, posX) => ship.Pos = new Vector2((float)posX, ship.Pos.Y) },
            { "posY", (api, ship, posY) => ship.Pos = new Vector2(ship.Pos.X, (float)posY) },
            { "velX", (api, ship, velX) => ship.Velocity = new Vector2((float)velX, ship.Velocity.Y) },
            { "velY", (api, ship, velY) => ship.Velocity = new Vector2(ship.Velocity.X, (float)velY) },
            { "time", (api, ship, timeMs) => api.gameTime.SetElapsedMillisecondsManually((long)timeMs) }
        };

        /// <summary>
        /// "SuperUser DO"; debug-only endpoint used to forcefully set attributes of a connected ship via REST.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("sudo")]
        [ApiParam("token", typeof(string), Optional = true)]
        public void Sudo(ApiResponse response, ApiData data)
        {
            Spaceship ship = null;

            if (data.Json.ContainsKey("token"))
            {
                var token = (string)data.Json["token"];
                if (players.ContainsKey(token))
                    ship = ships[players[token]];
                else
                {
                    response.Data["error"] = "Ship not found for given token.";
                    response.Send(500);
                    return;
                }
            }


            foreach (var kv in data.Json)
            {
                if (kv.Key == "token") continue;

                AttributeSetter setter = SUDO_SETTER_MAP.GetValueOrDefault(kv.Key, null);
                if (setter == null)
                {
                    response.Data["error"] = "Unrecognized attribute `" + kv.Key + "`";
                    response.Send(500);
                    return;
                }

                try
                {
                    setter.Invoke(this, ship, kv.Value);
                }
                catch (Exception exc)
                {
                    response.Data["error"] = "Failed to set attribute `" + kv.Key + "`: " + exc.ToString();
                    response.Send(500);
                    return;
                }
            }

            response.Send(200);
        }
#endif

    }
}




