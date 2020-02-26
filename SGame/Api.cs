using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using SShared;


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
        /// All ships who died in this node. F.
        /// </summary>
        public Dictionary<string, Spaceship> DeadShips { get; set; }

        // start the gameTime stopwatch on API creation
        public Api(LocalQuadTreeNode quadTreeNode)
        {
            this.gameTime = new GameTime();
            this.QuadTreeNode = quadTreeNode;
            this.DeadShips = new Dictionary<string, Spaceship>();
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

        /// <summary>
        /// Handles a "connect" REST request, connecting a player to the server.
        /// Responds with a fresh spaceship ID and player token for that spaceship.
        /// </summary>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("connect")]
        public async Task ConnectPlayer(ApiResponse response, ApiData data)
        {
            // TODO: For persistency, if a player passes a pre-existing token the ship
            //       needs to be deserialized

            string playerToken;
            do
            {
                playerToken = Guid.NewGuid().ToString();
            }
            while (QuadTreeNode.ShipsByToken.ContainsKey(playerToken));

            QuadTreeNode.ShipsByToken[playerToken] = new LocalSpaceship(playerToken, gameTime);
            Console.WriteLine($"Connected player #{QuadTreeNode.ShipsByToken.Count} (token={playerToken})");

            response.Data["token"] = playerToken;
            await response.Send();
        }

        /// <summary>
        /// Handles a "disconnect" REST request, disconnecting a player from the server.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship to disconnect.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("disconnect")]
        [ApiParam("token", typeof(string))]
        public async Task DisconnectPlayer(ApiResponse response, ApiData data)
        {
            var ship = await GetLocalShip(response, data.Json);
            if (ship == null)
            {
                return;
            }

            // TODO: For persistency, need to serialize the ship to database

            Console.WriteLine($"Disconnecting player (token={ship.Token})");
            QuadTreeNode.ShipsByToken.Remove(ship.Token);
            await response.Send(200);
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
            UpdateGameState();

            var ship = await GetLocalShip(response, data.Json);
            if (ship == null)
            {
                return;
            }
            double x = (double)data.Json["x"];
            double y = (double)data.Json["y"];

            int energyRequired = (int)Math.Ceiling(ship.Area * (Math.Abs(x) + Math.Abs(y)));
            int energySpent = Math.Min(energyRequired, (int)Math.Floor(ship.Energy));
            ship.Energy -= energySpent;
            double accelerationApplied = (double)energySpent / (double)energyRequired;
            ship.Velocity += Vector2.Multiply(new Vector2(x, y), accelerationApplied);

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
            UpdateGameState();

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
        /// Returns the fraction of the shot, specified by the bounding angles on the victim [shotStart, shotStop]
        /// that is shielded by the shielder's shield
        /// </summary>
        internal static double ShotShieldIntersection(double shotStart, double shotStop, Spaceship shielder)
        {
            // we will work with positive angles 
            // (the amount of cases should be the same, but we don't have to think about negative values)

            shotStart = MathUtils.ClampAngle(shotStart);
            shotStop = MathUtils.ClampAngle(shotStop);
            // make it so that going from shotStart to shotStop is the arc of the shot, going counterclockwise

            Console.WriteLine("shot " + shotStart + "," + shotStop);

            // case 1: shot goes through the problematic point 0=2pi
            if (Math.Abs(shotStop - shotStart) > Math.PI)
            {
                // shotStart is thus the larger angle (below the x-axis), shotStop the other one
                double larger = Math.Max(shotStart, shotStop);
                double smaller = Math.Min(shotStart, shotStop);
                shotStart = larger;
                shotStop = smaller;
            }
            // case 2: it doesn't
            else
            {
                // The shot is the 'direct' path from smaller angle to higher angle
                // so shotStart is the smaller angle, shotStop is the larger one
                double larger = Math.Max(shotStart, shotStop);
                double smaller = Math.Min(shotStart, shotStop);
                shotStart = smaller;
                shotStop = larger;
            }

            double shieldStart = MathUtils.ClampAngle(shielder.ShieldDir - shielder.ShieldWidth);
            double shieldStop = MathUtils.ClampAngle(shielder.ShieldDir + shielder.ShieldWidth);

            // make it so that going from shieldStart to shieldStop is the arc of the shield, going counterclockwise

            // case 1: shield goes through the problematic point 0=2pi
            // iff both shieldStart and shieldStop are {smaller, larger} than shieldDir
            if (Math.Min(shieldStart, shieldStop) > MathUtils.ClampAngle(shielder.ShieldDir) || Math.Max(shieldStart, shieldStop) < MathUtils.ClampAngle(shielder.ShieldDir))
            {
                // shieldStart is the larger angle, shieldStop is smaller
                double larger = Math.Max(shieldStart, shieldStop);
                double smaller = Math.Min(shieldStart, shieldStop);
                shieldStop = smaller;
                shieldStart = larger;
            }
            // case 2: shield does not go through the problematic point 0=2pi
            else
            {
                // shieldStart is the smaller angle, shieldStop is larger
                double larger = Math.Max(shieldStart, shieldStop);
                double smaller = Math.Min(shieldStart, shieldStop);
                shieldStart = smaller;
                shieldStop = larger;
            }

            // now we want to sort the angles counter-clockwise, and have them in the order we would have encountered them
            // if we want for a counter-clockwise walk from shotStart
            // -> add 2PI to all angles smaller than shotStart
            if (shotStop < shotStart) shotStop += 2 * Math.PI;
            if (shieldStart < shotStart) shieldStart += 2 * Math.PI;
            if (shieldStop < shotStart) shieldStop += 2 * Math.PI;

            // If these values are identical, e.g. shieldStop == shotStop, it might be that you can fake-block/pass-through shields
            // this is an easter egg for hackers that want to fight double precision with their coordinated friend for no benefit 

            Console.WriteLine("Shooting from " + shotStart + " to " + shotStop + ", shielded from " + shieldStart + " to " + shieldStop);

            double[] angles = { shotStart, shotStop, shieldStart, shieldStop };
            Array.Sort(angles);

            double distanceShielded = 0;

            // case 1: shotStop is the first encountered angle after shotStart
            // either entire shot is in, or out, of the shield
            if (angles[1] == shotStop)
            {
                // if next angle is ShieldStop, we were in
                if (angles[2] == shieldStop)
                    distanceShielded = shotStop - shotStart;
                else    // otherwise we were out 
                    distanceShielded = 0;
            }
            // case 2: shieldStart is the first encountered angle
            else if (angles[1] == shieldStart)
            {
                // we will shield everything from this point to the next
                // (whether that is shieldStop or shotStop)
                distanceShielded = angles[2] - angles[1];
            }
            // case 3: shieldStop is the first encountered angle
            else
            {
                // we have been shielded the entire time from shotStart to here
                distanceShielded = angles[1] - angles[0];

                // if we encounter shieldStart before shotStop, we will be shielded for the final journey
                if (angles[2] == shieldStart)
                {
                    distanceShielded += angles[3] - angles[2];
                }
            }

            // return the proportion of the shot angle that we have been shielded for
            double distanceTotal = shotStop - shotStart;
            return distanceShielded / distanceTotal;
        }

        /// <summary>
        /// Returns the percentage (0.0 to 1.0) of damage covered by a ship's shield when it is being shot
        /// from `shotOrigin` with a cone of half-width `shotWidth` radians and length `shotRadius`.
        /// WARNING: `width` and `shotDir` are in DEGREES!
        /// </summary>
        internal static double ShieldingAmount(Spaceship ship, Vector2 shotOrigin, double shotDir, double shotWidth, double shotRadius)
        {
            double shipR = ship.Radius();
            if ((shotOrigin - ship.Pos).Length() <= shipR)
            {
                // No shielding if the shooter is shooting from INSIDE the other ship!
                return 0.0;
            }

            if (ship.ShieldWidth < 0.001)
            {
                // Fast-track
                return 0.0;
            }

            // IMPORTANT - ShotWidth input is in degrees, (and already clamped from 0..180° at the source)
            shotWidth = MathUtils.Deg2Rad(shotWidth);
            // IMPORTANT - ShotDir input is in degrees, and needs:
            // - Being converted to radians
            // - Being clamped from 0..2pi (to prevent malicious input from breaking the code)
            // - Being normalized from -pi..pi (to prevent calculations below from potentially breaking)
            shotDir = MathUtils.NormalizeAngle(MathUtils.ClampAngle(MathUtils.Deg2Rad(shotDir)));

            // Find the two tangent points from the origin of the shot to the ship + `tgAngle`, i.e.
            // the bisector angle between the (shot origin to ship center) line and the two tangents.
            Vector2 tgLeft, tgRight;
            double tgAngle;
            MathUtils.CircleTangents(ship.Pos, shipR, shotOrigin, out tgLeft, out tgRight, out tgAngle);

            // Bring the two angles formed by the shot tangents from
            // "center axis" space (= reference is the line between the center of the ship and the shot origin)
            // to "shot cone" space (= reference is the center axis of the shot cone)
            // Mark them "left" and "right", where left <= right always - but note that they can both be positive and/or negative!
            Vector2 shipCenterDelta = ship.Pos - shotOrigin;
            double shipCenterAngle = Math.Atan2(shipCenterDelta.Y, shipCenterDelta.X);
            double CAS2SS = MathUtils.NormalizeAngle(shipCenterAngle - shotDir); //< NOTE: Normalized, or things can break (deltas multiple of 2pi...)
            Console.WriteLine("shot dir: " + MathUtils.Rad2Deg(shotDir) + "°, CAS2SS: " + MathUtils.Rad2Deg(CAS2SS) + "°, tgangle: " + MathUtils.Rad2Deg(tgAngle) + "°");

            double tgLeftAngleSS = -tgAngle + CAS2SS, tgRightAngleSS = tgAngle + CAS2SS;
            Console.WriteLine("LA " + tgLeftAngleSS + " RA " + tgRightAngleSS);
            if (tgLeftAngleSS > tgRightAngleSS)
            {
                (tgLeftAngleSS, tgRightAngleSS) = (tgRightAngleSS, tgLeftAngleSS);
                (tgLeft, tgRight) = (tgRight, tgLeft);
            }

            // If the circle arc "cap" of the shoot cone intersects with the ship circle, take the intersection points
            // into account for raycasting calculations:
            // - Calculate the world-space angles from the shot origin to each point
            // - Bring the angles to "shot cone" space (= reference is the center axis of the shot cone)
            // - Mark them "left" and "right", where left <= right always - but note that they can both be positive and/or negative!
            Vector2? capHitLeft, capHitRight;
            double leftCapAngleSS = Double.NegativeInfinity, rightCapAngleSS = Double.PositiveInfinity;
            if (MathUtils.CircleCircleIntersection(shotOrigin, shotRadius, ship.Pos, shipR, out capHitLeft, out capHitRight))
            {
                Console.WriteLine("The circular part of the shot intersects the ship!");
                if (capHitRight == null) capHitRight = capHitLeft;

                // First put the intersection points so that the "left" cap hit point is nearest to the "left" tangent point,
                // and the "right" cap point is nearest to the "right" tangent point;

                double capDist1 = (capHitLeft.Value - tgLeft).Length(), capDist2 = (capHitRight.Value - tgLeft).Length();
                if (capDist2 < capDist1)
                {
                    (capHitLeft, capHitRight) = (capHitRight, capHitLeft);
                }

                // Now calculate angles between the cap hits and the shot origin and bring them from world-space to shot-space
                // **Only if the distance between the shot origin and the respective tangent point is greater than the distance
                //   between the shot origin and the respective cap hit point the cap hit point angles are using during
                //   raycast angle calculations!!** (draw a picture if this sentence is not clear)
                Vector2 leftCapDelta = capHitLeft.Value - shotOrigin, rightCapDelta = capHitRight.Value - shotOrigin;
                Vector2 leftTgDelta = tgLeft - shotOrigin, rightTgDelta = tgRight - shotOrigin;
                if (leftCapDelta.LengthSquared() < leftTgDelta.LengthSquared())
                {
                    leftCapAngleSS = Math.Atan2(leftCapDelta.Y, leftCapDelta.X) - shotDir;
                }
                if (rightCapDelta.LengthSquared() < rightTgDelta.LengthSquared())
                {
                    rightCapAngleSS = Math.Atan2(rightCapDelta.Y, rightCapDelta.X) - shotDir;
                }

                Console.WriteLine($"leftCapAngleSS={MathUtils.Rad2Deg(leftCapAngleSS)}°, rightCapAngleSS={MathUtils.Rad2Deg(rightCapAngleSS)}°");
            }
            // else just ignore the cone cap -> the values being set to +/-infinity will do the trick

            // Normalize angles so that they are in the -180° to 180° range
            // (This is to make sure the Min/Max comparisons below work)
            tgLeftAngleSS = MathUtils.NormalizeAngle(tgLeftAngleSS);
            tgRightAngleSS = MathUtils.NormalizeAngle(tgRightAngleSS);
            if (!Double.IsNegativeInfinity(leftCapAngleSS)) leftCapAngleSS = MathUtils.NormalizeAngle(leftCapAngleSS);
            if (!Double.IsPositiveInfinity(rightCapAngleSS)) rightCapAngleSS = MathUtils.NormalizeAngle(rightCapAngleSS);

            // Now need to raycast from the shot origin to the ship.
            // - Everything "behind" the tangent points is covered by the rest of the ship.
            // - On the left and right hand sides of the cones, one only checks for shielding up to:
            //   - The side of the shot cone; or
            //   - The tangent on that side; or
            //   - The intersection between the circle arc "cap" of the shoot cone and the circle of the ship
            // Note that "left" and "right" edges are specular in the direction they choose between the edge of the shot cone and the tangent on that side!
            double leftRayAngleSS = Math.Max(Math.Max(-shotWidth, tgLeftAngleSS), leftCapAngleSS);
            double rightRayAngleSS = Math.Min(Math.Min(tgRightAngleSS, shotWidth), rightCapAngleSS);

            Vector2? leftHitNear = null, leftHitFar = null;
            bool leftHit = new Ray(shotOrigin, shotDir + leftRayAngleSS).HitCircle(ship.Pos, shipR, out leftHitNear, out leftHitFar);
            Vector2? rightHitNear = null, rightHitFar = null;
            bool rightHit = new Ray(shotOrigin, shotDir + rightRayAngleSS).HitCircle(ship.Pos, shipR, out rightHitNear, out rightHitFar);

            if (!leftHit || !rightHit)
            {
                // Raycast missed - this should never happen here!
                throw new InvalidOperationException("Raycast missed during shield calculation!");
            }

            Console.WriteLine("LH = " + leftHitNear.Value + " RH = " + rightHitNear.Value);

            // Now calculate the bounding angles of the victim that is being shot at
            double leftVictimHit = Math.Atan2(leftHitNear.Value.Y - ship.Pos.Y, leftHitNear.Value.X - ship.Pos.X);
            double rightVictimHit = Math.Atan2(rightHitNear.Value.Y - ship.Pos.Y, rightHitNear.Value.X - ship.Pos.X);

            Console.WriteLine("From victim's point of view " + ship.Pos + ": " + leftVictimHit + "," + rightVictimHit);

            // Now calculate the intersection between the angles [leftVictimHit, rightVictimHit] and [victim.shieldDir - victim.shieldWidth, victim.shieldDir + victim.shieldWidth]

            return ShotShieldIntersection(leftVictimHit, rightVictimHit, ship);
        }

        private int SCAN_ENERGY_SCALING_FACTOR = 2000;

        /// <summary>
        /// Returns a list of local (to this quadtree node) ships that lie within a triangle with one vertex at pos, the center of its opposite side
        /// is at an angle of worldDeg degrees from the vertex, its two other sides are an angle scanWidth from this point, and
        /// its area will be equal to SCAN_ENERGY_SCALING_FACTOR times the energy spent
        /// </summary>
        public List<LocalSpaceship> CircleSectorScanLocal(Vector2 pos, double worldDeg, double scanWidth, int energySpent, out double radius)
        {
            // The radius of the cone will be such that the area scanned is energySpent * SCAN_ENERGY_SCALING_FACTOR
            double areaScanned = energySpent * SCAN_ENERGY_SCALING_FACTOR;

            // Convert angles to radians
            worldDeg = (Math.PI * worldDeg) / 180.0;
            scanWidth = (Math.PI * scanWidth) / 180.0;

            // We want the radius of the circle, such that a sercular sector of angle 2*scanwidth has area areaScanned
            radius = (double)Math.Sqrt(areaScanned / (2 * scanWidth));

            // The circular sector is a triangle whose vertices are pos, and the points at an angle (worldDeg +- scanWidth) and distance radius
            // And a segment between those points on the circle centered at pos with that radius

            Vector2 leftPoint = new Vector2(radius * Math.Cos(worldDeg + scanWidth), radius * Math.Sin(worldDeg + scanWidth));
            Vector2 rightPoint = new Vector2(radius * Math.Cos(worldDeg - scanWidth), radius * Math.Sin(worldDeg - scanWidth));

            Console.WriteLine("Scanning with radius " + radius + "; In triangle " + pos.ToString() + "," + leftPoint.ToString() + "," + rightPoint.ToString());

            double scanRadius = radius;
            return QuadTreeNode.ShipsByToken.Values.Where((ship) =>
                MathUtils.CircleTriangleIntersection(ship.Pos, ship.Radius(), pos, leftPoint, rightPoint)
                || MathUtils.CircleSegmentIntersection(ship.Pos, ship.Radius(), pos, scanRadius, worldDeg, scanWidth)
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
            UpdateGameState();

            var ship = await IntersectionParamCheck(response, data);
            if (ship == null)
            {
                return;
            }

            int energy = (int)data.Json["energy"];
            double direction = (double)data.Json["direction"];
            double width = (double)data.Json["width"];
            energy = (int)Math.Min(energy, Math.Floor(ship.Energy));
            ship.Energy -= energy;

            Console.WriteLine($"Scan by {ship.PublicId}, pos={ship.Pos}, dir={direction}, width={width}, energy spent={energy}");

            double scanRadius;
            // FIXME: Recursive instead of local scan
            List<LocalSpaceship> scannedShips = CircleSectorScanLocal(ship.Pos, direction, width, energy, out scanRadius);
            JArray respDict = new JArray();
            foreach (Spaceship scannedShip in scannedShips)
            {
                // ignore our ship
                if (scannedShip == ship)
                    continue;

                JToken scannedShipInfo = new JObject();
                scannedShipInfo["id"] = scannedShip.PublicId;
                scannedShipInfo["area"] = scannedShip.Area;
                scannedShipInfo["posX"] = scannedShip.Pos.X;
                scannedShipInfo["posY"] = scannedShip.Pos.Y;
                respDict.Add(scannedShipInfo);
            }

            response.Data["scanned"] = respDict;
            await response.Send();
        }

        /// <summary>
        /// Minimum ship area, below which it is considered dead.
        /// </summary>
        private const double MINIMUM_AREA = 0.75;

        /// <summary>
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
            LocalSpaceship ship = await IntersectionParamCheck(response, data, true);
            if (ship == null)
            {
                return;
            }
            double width = (double)data.Json["width"];
            double direction = (double)data.Json["direction"];
            double damageScaling = (double)data.Json["damage"];

            int energy = (int)Math.Min((int)data.Json["energy"], Math.Floor(ship.Energy / damageScaling));
            ship.Energy -= energy * damageScaling; //remove energy for the shot

            Console.WriteLine($"Shot by {ship.PublicId}, pos={ship.Pos}, dir={direction}, width={width}, energy spent={energy}, scaling={damageScaling}");

            // FIXME Scan recursively instead of locally
            double shotRadius;
            List<LocalSpaceship> localStruckShips = CircleSectorScanLocal(ship.Pos, direction, width, energy, out shotRadius);
            JArray respDict = new JArray();
            foreach (LocalSpaceship localStruckShip in localStruckShips)
            {
                // ignore our ship
                if (localStruckShip == ship)
                    continue;

                //The api doesnt have a return value for shooting, but ive left this in for now for testing purposes.
                JToken struckShipInfo = new JObject();
                struckShipInfo["id"] = localStruckShip.PublicId;
                struckShipInfo["area"] = localStruckShip.Area;
                struckShipInfo["posX"] = localStruckShip.Pos.X;
                struckShipInfo["posY"] = localStruckShip.Pos.Y;
                respDict.Add(struckShipInfo);

                double shipDistance = (localStruckShip.Pos - ship.Pos).Length();
                double damage = ShotDamage(energy, width, damageScaling, shipDistance);
                double shielding = ShieldingAmount(localStruckShip, ship.Pos, direction, width, shotRadius);
                if (shielding > 0.0)
                {
                    Console.WriteLine($"{localStruckShip.PublicId} shielded itself for {shielding * 100.0}% of {ship.PublicId}'s shot (= {damage * shielding} damage)");
                }
                damage *= (1.0 - shielding);

                // We have killed a ship, gain it's kill reward, and move struck ship to the graveyard
                if (localStruckShip.Area - damage < MINIMUM_AREA)
                {
                    ship.Area += localStruckShip.KillReward;

                    QuadTreeNode.ShipsByToken.Remove(localStruckShip.Token);
                    DeadShips.Add(localStruckShip.Token, localStruckShip);
                }
                else // Struck ship survived - note that it's in combat
                {
                    if (localStruckShip.LastUpdate - localStruckShip.LastCombat > LocalSpaceship.COMBAT_COOLDOWN)
                    {
                        // Reset kill reward when hit ship was not in combat
                        localStruckShip.KillReward = localStruckShip.Area;
                    }
                    localStruckShip.LastCombat = localStruckShip.LastUpdate;
                    localStruckShip.Area -= damage;
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
        private double ShotDamage(int energy, double width, double scaling, double distance)
        {
            distance = Math.Max(distance, 1);
            width = (double)(Math.PI * width) / 180.0;
            return (double)(energy * scaling) / (Math.Max(1, Math.Pow(2, 2 * width)) * Math.Sqrt(distance));

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
        /// Verifies the arguments passed in an intersection based request are appropriate; send an error response otherwise.
        /// Returns the spaceship at `spaceship["token"]` on success or null on error.
        /// </summary>
        private async Task<LocalSpaceship> IntersectionParamCheck(ApiResponse response, ApiData data, bool requireDamage = false)
        {
            UpdateGameState();
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
            { "time", (api, ship, timeMs) => api.gameTime.SetElapsedMillisecondsManually((long)timeMs) }
        };

        /// <summary>
        /// "SuperUser DO"; debug-only endpoint used to forcefully set attributes of a connected ship via REST.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("sudo")]
        [ApiParam("token", typeof(string), Optional = true)]
        public async Task Sudo(ApiResponse response, ApiData data)
        {
            LocalSpaceship ship = null;

            if (data.Json.ContainsKey("token"))
            {
                var token = (string)data.Json["token"];
                ship = QuadTreeNode.ShipsByToken.GetValueOrDefault(token, null);
                if (ship == null)
                {
                    response.Data["error"] = "Ship not found for given token.";
                    await response.Send(500);
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
                    await response.Send(500);
                    return;
                }

                try
                {
                    setter.Invoke(this, ship, kv.Value);
                }
                catch (Exception exc)
                {
                    response.Data["error"] = "Failed to set attribute `" + kv.Key + "`: " + exc.ToString();
                    await response.Send(500);
                    return;
                }
            }

            await response.Send(200);
        }
#endif

    }
}




