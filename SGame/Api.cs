﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LiteNetLib;
using System.Threading.Tasks;
using SShared;
using Messages = SShared.Messages;


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
        internal GameTime gameTime;

        /// <summary>
        /// The quadtree node managed by this SGame API instance.
        /// </summary>
        public LocalQuadTreeNode QuadTreeNode { get; set; }

        /// <summary>
        /// The host that represents this SGame node on the bus.
        /// </summary>
        public NetNode Bus { get; set; }

        /// <summary>
        /// All ships who died in this node. F.
        /// </summary>
        public Dictionary<string, Spaceship> DeadShips { get; set; }

        // start the gameTime stopwatch on API creation
        public Api(LocalQuadTreeNode quadTreeNode, NetNode bus)
        {
            this.gameTime = new GameTime();
            //#if DEBUG
            //   this.gameTime.SetElapsedMillisecondsManually(0);
            //#endif
            this.QuadTreeNode = quadTreeNode;
            this.Bus = bus;
            this.DeadShips = new Dictionary<string, Spaceship>();

            this.Bus.PacketProcessor.Events<Messages.ShipConnected>().OnMessageReceived += OnShipConnected;
            this.Bus.PacketProcessor.Events<Messages.ShipDisconnected>().OnMessageReceived += OnShipDisconnected;
#if DEBUG
            this.Bus.PacketProcessor.Events<Messages.Sudo>().OnMessageReceived += OnSudo;
#endif
        }

        /// <summary>
        /// Looks up a local spaceship from data["token"]. If token is present and valid, and the spaceship is present
        /// and not dead, returns the relevant ship; otherwise sends an error response and returns null.
        /// <summary>
        async Task<LocalSpaceship> GetLocalShip(ApiResponse response, JObject data)
        {
            if (!data.ContainsKey("token"))
            {
                response.Data["error"] = "Spaceship token not in sent data.";
                await response.Send(500);
                return null;
            }
            var token = (string)data["token"];

            if (DeadShips.ContainsKey(token))
            {
                DeadShips.Remove(token); // (no need to notify the other nodes for this)

                response.Data["error"] = "Your spaceship has been killed. Please reconnect.";
                await response.Send(500);
                return null;
            }

            var ship = QuadTreeNode.ShipsByToken.GetValueOrDefault(token, null);
            if (ship == null)
            {
                response.Data["error"] = "Ship not found for given token.";
                await response.Send(500);
                return null;
            }

            return ship;
        }

        /// <summary>
        /// Updates each spaceship's state (energy, position, ...) based on time it was not updated
        /// </summary>
        internal void UpdateGameState()
        {
            foreach (var ship in QuadTreeNode.ShipsByToken.Values)
            {
                ship.UpdateState();
            }
        }

        //TODO: Finish garbage collector. Left for now to create PathString for shipTransfer message.
        internal void GarbageCollect()
        {
            foreach (var ship in QuadTreeNode.ShipsByToken.Values)
            {
                LocalQuadTreeNode currentNode = QuadTreeNode;
                QuadTreeNode<Spaceship> bestFitNode = currentNode.SmallestNodeWhichContainsShip(ship);
                if (bestFitNode == null){
                    // Messages.MoveS
                    // Bus.SendMessage()
                }
            }
        }

        public void OnShipTransferred(Messages.ShipTransferred msg, NetPeer peer)
        {

            LocalSpaceship localShip = new LocalSpaceship(msg.Ship, gameTime);
            QuadTreeNode.ShipsByToken.Add(msg.Ship.Token, localShip);
        }

        /// <summary>
        /// Handles a "ship connected" bus message.
        /// </summary>
        public void OnShipConnected(NetPeer sender, Messages.ShipConnected msg)
        {
            Console.WriteLine($"Creating ship for player (token={msg.Token})");

            LocalSpaceship ship = new LocalSpaceship(msg.Token, gameTime);
            var randomShipBounds = MathUtils.RandomQuadInQuad(QuadTreeNode.Bounds, ship.Radius());
            ship.Pos = new Vector2(randomShipBounds.CentreX, randomShipBounds.CentreY);
            QuadTreeNode.ShipsByToken.Add(msg.Token, ship);

            Bus.SendMessage(new Messages.ShipConnected() { Token = msg.Token }, Bus.FirstPeer);
        }

        /// <summary>
        /// Handles a "ship disconnected" bus message.
        /// </summary>
        public void OnShipDisconnected(NetPeer sender, Messages.ShipDisconnected msg)
        {
            Console.WriteLine($"Disconnecting player (token={msg.Token})");

            if (QuadTreeNode.ShipsByToken.Remove(msg.Token))
            {
                // TODO: Serialize ship state here?
                Bus.SendMessage(new Messages.ShipDisconnected() { Token = msg.Token }, Bus.FirstPeer);
            }
        }

        /// <summary>
        /// Handles an "accelerate" REST request.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to accelerate, and the vector of acceleration </param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("accelerate")]
        [ApiParam("token", typeof(string))]
        [ApiParam("x", typeof(double))]
        [ApiParam("y", typeof(double))]
        public async Task AcceleratePlayer(ApiResponse response, ApiData data)
        {
            var ship = await GetLocalShip(response, data.Json);
            if (ship == null)
            {
                return;
            }
            double x = (double)data.Json["x"];
            double y = (double)data.Json["y"];

            if (x != 0 || y != 0)
            {
                int energyRequired = (int)Math.Ceiling(ship.Area * (Math.Abs(x) + Math.Abs(y)));
                int energySpent = Math.Min(energyRequired, (int)Math.Floor(ship.Energy));
                ship.Energy -= energySpent;
                double accelerationApplied = (double)energySpent / (double)energyRequired;
                ship.Velocity += Vector2.Multiply(new Vector2(x, y), accelerationApplied);
            }

            await response.Send(200);
        }


        /// <summary>
        /// Handles a "ShipInfo" REST request, returning the player's spaceship info from the server.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("getShipInfo")]
        [ApiParam("token", typeof(string))]
        public async Task GetShipInfo(ApiResponse response, ApiData data)
        {
#if DEBUG
            // HACK: Force sudo params to be applied
            Bus.Update();
#endif

            var ship = await GetLocalShip(response, data.Json);
            if (ship == null)
            {
                return;
            }

            response.Data["id"] = ship.PublicId;
            response.Data["area"] = ship.Area;
            response.Data["energy"] = ship.Energy;
            response.Data["posX"] = ship.Pos.X;
            response.Data["posY"] = ship.Pos.Y;
            response.Data["velX"] = ship.Velocity.X;
            response.Data["velY"] = ship.Velocity.Y;
            response.Data["shieldWidth"] = ship.ShieldWidth * 180 / Math.PI;
            response.Data["shieldDir"] = ship.ShieldDir * 180 / Math.PI;
            await response.Send();
        }

        /// <summary>
        /// Returns whether the given point sits on the ship's shield or not.
        /// </summary>
        internal static bool IsPointOnShield(Spaceship ship, Vector2 point)
        {
            double arcAngle;
            return MathUtils.IsPointOnArc(point, ship.Pos, ship.ShieldDir, ship.ShieldWidth, ship.Radius(), out arcAngle);
        }

        /// <summary>
        /// Returns a list of local (to this quadtree node) ships that lie within a triangle with one vertex at pos, the center of its opposite side
        /// is at an angle of worldRad radians from the vertex, its two other sides are an angle scanWidth from this point, and
        /// its area will be equal to SCAN_ENERGY_SCALING_FACTOR times the energy spent
        /// </summary>
        public List<LocalSpaceship> CircleSectorScanLocal(Vector2 pos, double worldRad, double scanWidth, int energySpent)
        {
            double radius = MathUtils.ScanShootRadius(scanWidth, energySpent);
            // The circular sector is a triangle whose vertices are pos, and the points at an angle (worldDeg +- scanWidth) and distance radius
            // And a segment between those points on the circle centered at pos with that radius

            Vector2 leftPoint = new Vector2(radius * Math.Cos(worldRad + scanWidth), radius * Math.Sin(worldRad + scanWidth));
            Vector2 rightPoint = new Vector2(radius * Math.Cos(worldRad - scanWidth), radius * Math.Sin(worldRad - scanWidth));

            Console.WriteLine("Scanning with radius " + radius + "; In triangle " + pos.ToString() + "," + leftPoint.ToString() + "," + rightPoint.ToString());

            double scanRadius = radius;
            return QuadTreeNode.ShipsByToken.Values.Where((ship) =>
                MathUtils.CircleTriangleIntersection(ship.Pos, ship.Radius(), pos, leftPoint, rightPoint)
                || MathUtils.CircleSegmentIntersection(ship.Pos, ship.Radius(), pos, scanRadius, worldRad, scanWidth)
            )
            .ToList();
        }

        /// <summary>
        /// Handles a "Scan" REST request, returning a set of spaceships that are within the scan
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship, the angle of scanning, the width of scan, and the energy spent on the scan.true </param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("scan")]
        [ApiParam("token", typeof(string))]
        [ApiParam("direction", typeof(double))]
        [ApiParam("width", typeof(double))]
        [ApiParam("energy", typeof(int))]
        public async Task Scan(ApiResponse response, ApiData data)
        {

            var ship = await IntersectionParamCheck(response, data);
            if (ship == null)
            {
                return;
            }

            int energy = (int)data.Json["energy"];
            double directionDeg = (double)data.Json["direction"];
            double widthDeg = (double)data.Json["width"];

            energy = (int)Math.Min(energy, Math.Floor(ship.Energy));
            ship.Energy -= energy;

            Console.WriteLine($"Scan by {ship.PublicId}, pos={ship.Pos}, dir={directionDeg}°, width={widthDeg}°, energy spent={energy}");

            var scanMsg = new SShared.Messages.ScanShoot()
            {
                Originator = ship.Token,
                Origin = ship.Pos,
                Direction = MathUtils.Deg2Rad(directionDeg),
                ScaledShotEnergy = 0,
                Width = MathUtils.Deg2Rad(widthDeg),
                Radius = MathUtils.ScanShootRadius(MathUtils.Deg2Rad(widthDeg), energy),
            };

            ScanShootResults results = await QuadTreeNode.ScanShootRecur(scanMsg);
            ship.Area += results.AreaGain;

            JArray respDict = new JArray();
            foreach (var scanned in results.Struck)
            {
                if (scanned.Ship.Token == ship.Token)
                    continue;

                //The api doesnt have a return value for shooting, but ive left this in for now for testing purposes.
                JToken struckShipInfo = new JObject();
                struckShipInfo["id"] = scanned.Ship.PublicId;
                struckShipInfo["area"] = scanned.Ship.Area;
                struckShipInfo["posX"] = scanned.Ship.Pos.X;
                struckShipInfo["posY"] = scanned.Ship.Pos.Y;
                respDict.Add(struckShipInfo);
            }

            response.Data["scanned"] = respDict;
            await response.Send();
        }


        /// <summary>await P.ServerLoop(endpoints);
        /// Handles a "Shoot" REST request, damaging all ships caught in its blast. pew pew.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship, the angle to shoot at, the width of the shot, the energy to expend on the shot (determines distance), and damage (scaling) </param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("shoot")]
        [ApiParam("token", typeof(string))]
        [ApiParam("direction", typeof(double))]
        [ApiParam("width", typeof(double))]
        [ApiParam("energy", typeof(int))]
        [ApiParam("damage", typeof(double))]
        public async Task Shoot(ApiResponse response, ApiData data)
        {
#if DEBUG
            // HACK: Force game update so older tests still function
            UpdateGameState();
#endif
            LocalSpaceship ship = await IntersectionParamCheck(response, data, true);
            if (ship == null)
            {
                return;
            }
            double widthDeg = (double)data.Json["width"];
            double directionDeg = (double)data.Json["direction"];
            double damageScaling = (double)data.Json["damage"];

            int energy = (int)Math.Min((int)data.Json["energy"], Math.Floor(ship.Energy / damageScaling));
            ship.Energy -= energy * damageScaling; //remove energy for the shot

            Console.WriteLine($"Shot by {ship.PublicId}, pos={ship.Pos}, dir={directionDeg}°, width={widthDeg}°, energy spent={energy}, scaling={damageScaling}");

            var shootMsg = new SShared.Messages.ScanShoot()
            {
                Originator = ship.Token,
                Origin = ship.Pos,
                Direction = MathUtils.Deg2Rad(directionDeg),
                ScaledShotEnergy = energy * damageScaling,
                Width = MathUtils.Deg2Rad(widthDeg),
                Radius = MathUtils.ScanShootRadius(MathUtils.Deg2Rad(widthDeg), energy),
            };

            ScanShootResults results = await QuadTreeNode.ScanShootRecur(shootMsg);
            ship.Area += results.AreaGain;

            JArray respDict = new JArray();
            foreach (var struckShip in results.Struck)
            {
                // ignore our ship
                if (struckShip.Ship.Token == ship.Token)
                    continue;

                double preShotArea = struckShip.Ship.Area + Math.Abs(struckShip.AreaGain);

                //The api doesnt have a return value for shooting, but ive left this in for now for testing purposes.
                JToken struckShipInfo = new JObject();
                struckShipInfo["id"] = struckShip.Ship.PublicId;
                struckShipInfo["area"] = preShotArea;
                struckShipInfo["posX"] = struckShip.Ship.Pos.X;
                struckShipInfo["posY"] = struckShip.Ship.Pos.Y;
                respDict.Add(struckShipInfo);

                if (struckShip.AreaGain < 0.0) // ship ded
                {
                    var ourDeadShip = QuadTreeNode.ShipsByToken.GetValueOrDefault(struckShip.Ship.Token, null);
                    if (ourDeadShip != null)
                    {
                        QuadTreeNode.ShipsByToken.Remove(ourDeadShip.Token);
                        DeadShips.Add(ourDeadShip.Token, ourDeadShip);
                    }
                }
            }

            //Ship performed combat action, lock kill reward if not in combat from before
            if (ship.LastUpdate - ship.LastCombat > LocalSpaceship.COMBAT_COOLDOWN)
            {
                ship.KillReward = ship.Area;
            }
            ship.LastCombat = ship.LastUpdate;

            response.Data["struck"] = respDict;
            await response.Send();
        }

        /// <summary>
        /// Handles a "Shield" REST request, setting the shield direction and radius around the ship.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship, the center angle of the shield, and the half-width of the shield.</param>
        /// <param name="response">The response to be sent to the client.</param>
        [ApiRoute("shield")]
        [ApiParam("token", typeof(string))]
        [ApiParam("direction", typeof(double))]
        [ApiParam("width", typeof(double))]
        public async Task Shield(ApiResponse response, ApiData data)
        {
            var ship = await GetLocalShip(response, data.Json);
            if (ship == null)
            {
                return;
            }

            double dirDeg = (double)data.Json["direction"];
            double hWidthDeg = (double)data.Json["width"];
            ship.ShieldDir = MathUtils.Deg2Rad(dirDeg); // (autonormalized)
            if (hWidthDeg < 0.0 || hWidthDeg > 180.0)
            {
                response.Data["error"] = "Invalid angle passed (range: [0..180])";
                await response.Send(500);
            }
            ship.ShieldWidth = MathUtils.Deg2Rad(hWidthDeg);

            Console.WriteLine($"Shields up for {ship.PublicId}, width/2={hWidthDeg}°, dir={dirDeg}°");

            await response.Send(200);
        }

        /// <summary>
        /// Calculates the shotDamage applied to a ship. Shot damage drops off exponentially as distance increases, base =1.1
        /// </summary>


        /// <summary>
        /// Verifies the arguments passed in an intersection based request are appropriate; send an error response otherwise.
        /// Returns the spaceship at `spaceship["token"]` on success or null on error.
        /// </summary>
        private async Task<LocalSpaceship> IntersectionParamCheck(ApiResponse response, ApiData data, bool requireDamage = false)
        {
            var ship = await GetLocalShip(response, data.Json);
            if (ship == null)
            {
                return null;
            }

            String[] requiredParams = new String[3] { "direction", "width", "energy" };

            for (int i = 0; i < requiredParams.Length; i++)
            {
                if (data.Json[requiredParams[i]] == null)
                {
                    response.Data["error"] = "Requires parameter: " + requiredParams[i];
                    await response.Send(500);
                    return null;
                }
            }

            double direction = (double)data.Json["direction"];

            double width = (double)data.Json["width"];
            if (width <= 0 || width >= 90)
            {
                response.Data["error"] = "Width not in interval (0,90) degrees";
                await response.Send(500);
                return null;
            }

            int energy = (int)data.Json["energy"];
            if (energy <= 0)
            {
                response.Data["error"] = "Energy spent must be positive";
                await response.Send(500);
                return null;
            }

            if (requireDamage)
            {
                if (data.Json["damage"] == null)
                {
                    response.Data["error"] = "Requires parameter: " + "damage";
                    await response.Send(500);
                    return null;
                }

                double damage = (double)data.Json["damage"];
                if (damage <= 0)
                {
                    response.Data["error"] = "Damage scaling must be positive";
                    await response.Send(500);
                    return null;
                }
            }

            return ship;
        }

#if DEBUG
        /// <summary>
        /// A function that, when invoked, sets a certain parameter of `ship` to `value`.
        /// </summary>
        /// <param name="api">The API instance.</param>
        /// <param name="ship">The ship to set the attribute on.</param>
        /// <param name="value">The new value of the parameter to set.</param>
        private delegate void AttributeSetter(Api api, LocalSpaceship ship, JToken value);

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
            { "posX", (api, ship, posX) => ship.Pos = new Vector2((double)posX, ship.Pos.Y) },
            { "posY", (api, ship, posY) => ship.Pos = new Vector2(ship.Pos.X, (double)posY) },
            { "velX", (api, ship, velX) => ship.Velocity = new Vector2((double)velX, ship.Velocity.Y) },
            { "velY", (api, ship, velY) => ship.Velocity = new Vector2(ship.Velocity.X, (double)velY) },
            { "time", (api, ship, timeMs) => {
                api.gameTime.SetElapsedMillisecondsManually((long)timeMs);
                api.UpdateGameState();
            }}
        };

        /// <summary>
        /// "SuperUser DO"; debug-only message used to forcefully set attributes of a connected ship.
        /// </summary>
        public void OnSudo(NetPeer peer, Messages.Sudo data)
        {
            Console.WriteLine("Sudo: {0}", data.Json.ToString());

            LocalSpaceship ship = null;

            if (data.Json.ContainsKey("token"))
            {
                var token = (string)data.Json["token"];
                ship = QuadTreeNode.ShipsByToken.GetValueOrDefault(token, null);
                if (ship == null)
                {
                    return;
                }
            }

            foreach (var kv in data.Json)
            {
                if (kv.Key == "token") continue;

                AttributeSetter setter = SUDO_SETTER_MAP.GetValueOrDefault(kv.Key, null);
                if (setter == null)
                {
                    Console.Error.WriteLine("Sudo: Unrecognized attribute `" + kv.Key + "`");
                    return;
                }

                try
                {
                    setter.Invoke(this, ship, kv.Value);
                }
                catch (Exception exc)
                {
                    Console.Error.WriteLine("Sudo: Failed to set attribute `" + kv.Key + "`: " + exc.ToString());
                    return;
                }
            }

            // Send the same sudo back to the arbiter as ACK
            Bus.SendMessage(data, Bus.FirstPeer, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
#endif

    }
}




