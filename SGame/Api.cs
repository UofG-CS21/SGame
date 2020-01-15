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
            if (maybeId == null || !ships.ContainsKey(maybeId.Value))
            {
                Console.WriteLine("Ship not found for given token");
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

        // Calculates the sign of a point relative to a line defined by two points
        int pointLineSign(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            // Calculate the (not normalized) normal to the line
            Vector2 Normal = new Vector2(linePoint2.Y - linePoint1.Y, -(linePoint2.X - linePoint1.X));
            // The sign is equal to the sign of the dot product of the normal, and the vector from point1 to the tested point
            return System.Math.Sign(Vector2.Dot(Normal, point - linePoint1));
        }

        bool CircleTriangleSideIntersection(Vector2 circleCenter, double radius, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 lineVector = linePoint2 - linePoint1;
            Vector2 point1ToCircle = circleCenter - linePoint1;

            float lengthAlongTriangleSide = Vector2.Dot(point1ToCircle, lineVector);

            // If the length is negative, the cosine of the angle is negative, so it lies more than 90 degrees around linePoint 1
            // For that to intersect the triangle side, linePoint1 would already lie within the circle
            // But we have checked that in Case 1, so it must not lie. Therefore such circle does not intersect the triangle side
            if (lengthAlongTriangleSide > 0)
            {
                float sideLenghtSquared = lineVector.LengthSquared();

                // Since we want to keep using squared distances, instead of doing 
                // lengthAlongTriangleSide /= sideLength, we do
                // lengthAlongTriangleSide * lengthAlongTriangleSide / sideLengthSquared
                lengthAlongTriangleSide = lengthAlongTriangleSide * lengthAlongTriangleSide / sideLenghtSquared;

                // If the length along the triangle is greater than the side of the triangle
                // It would intersect with the line past the second triangle vertex
                // Which we have, as before, checked in Case 1
                if (lengthAlongTriangleSide < sideLenghtSquared)
                {
                    // We have the squared lengths of the vectors circle->point1 and point1->perpendicularPointOnLineVector
                    // We use Pythagorean theorem to find the length of side between circlePoint and perpendicularPointOnLineVector
                    // There is an intersection if it is not greater than the radius
                    // We never square-root either side of the Pythagorean equation
                    if (point1ToCircle.LengthSquared() - lengthAlongTriangleSide <= radius * radius)
                        return true;
                }
            }

            return false;
        }

        // Returns true iff the circle centered at circleCenter with radius 'radius' intersects the triangle with vertices A,B,C 
        // Based on http://www.phatcode.net/articles.php?id=459 
        private bool CircleTriangleIntersection(Vector2 circleCenter, double radius, Vector2 A, Vector2 B, Vector2 C)
        {

            Console.WriteLine("Testing intersection of " + circleCenter.ToString() + ", r=" + radius + " with " + A.ToString() + "," + B.ToString() + "," + C.ToString());

            //Console.WriteLine("Case 1");
            // Case 1: Triangle vertex within circle

            // Calculate position vectors for A,B,C with origin at circleCenter (c stands for 'centered')
            Vector2 cA = A - circleCenter, cB = B - circleCenter, cC = C - circleCenter;

            // Check whether any of them are close enough to circleCenter
            if (radius * radius >= cA.LengthSquared()) return true;
            if (radius * radius >= cB.LengthSquared()) return true;
            if (radius * radius >= cC.LengthSquared()) return true;

            //Console.WriteLine("Case 2");
            // Case 2: Circle center within triangle

            // We calculate the sign of the position of the circleCenter relative to each side
            // If it lies within the triangle, they will all be the same
            int sAB = pointLineSign(circleCenter, A, B);
            int sBC = pointLineSign(circleCenter, B, C);
            int sCA = pointLineSign(circleCenter, C, A);

            if (sAB >= 0 && sBC >= 0 && sCA >= 0) return true;
            if (sAB <= 0 && sBC <= 0 && sCA <= 0) return true;

            //Console.WriteLine("Case 3");
            // Case 3: Circle intersects triangle side
            if (CircleTriangleSideIntersection(circleCenter, radius, A, B)) return true;
            if (CircleTriangleSideIntersection(circleCenter, radius, B, C)) return true;
            if (CircleTriangleSideIntersection(circleCenter, radius, C, A)) return true;

            // No intersections were found
            return false;
        }

        // Return true iff the circle cenered at circleCenter with radius circleRadius intersects 
        // the segment of a circle centered at segmentRadius, with its midpoint in the direction segmentAngle, and its angular width 2*segmentWidth
        private bool CircleSegmentIntersection(Vector2 circleCenter, float circleRadius, Vector2 segmentCenter, float segmentRadius, float segmentAngle, float segmentWidth)
        {
            // If the centers of the segment-circle and the ship circle are further appart than the sum of their radii, 
            if (circleRadius + segmentRadius < Vector2.Subtract(segmentCenter, circleCenter).Length())
                return false;

            // We have already checked for intersection between the triangle part of the circular sector and the ship circle
            // Therefore, we know there is no intersection between the edges of the circular sector and the ship
            // Thus, either the ship is strictly within the segment,
            // Or all its points of intersection are within the segment itself
            // In both cases, the center of the circle must be inbetween the angles formed by the circular sector's edges from its center
            // And if they are, and we know they are close enough to the circle to intersect, then they intersect

            // So all we need to know is whether circleCenter lies between the angles of segmentAngle + segmentWidth and segmentAngle - segmentWidth
            // The difference between the angle formed by the circleCenter, and both the circular sector's angles, must be smaller than the difference
            // between the circular sector's angles (i.e. the angle of circleCenter is closer to both edges of the circular sector, than they are to each other)

            float circularSectorEdgeAngleDistance = segmentWidth * 2;

            Vector2 segmentCenterToCircleCenter = circleCenter - segmentCenter;

            float circleCenterAngle = (float)Math.Atan2(segmentCenterToCircleCenter.Y, segmentCenterToCircleCenter.X);

            float distance1 = Math.Abs((segmentAngle - segmentWidth) - circleCenterAngle);
            if (distance1 > Math.PI)
                distance1 = 2 * (float)Math.PI - distance1;

            if (distance1 > circularSectorEdgeAngleDistance)
                return false;

            float distance2 = Math.Abs((segmentAngle + segmentWidth) - circleCenterAngle);
            if (distance2 > Math.PI)
                distance2 = 2 * (float)Math.PI - distance2;

            if (distance2 > circularSectorEdgeAngleDistance)
                return false;

            return true;
        }

        /// <summary>
        /// Returns a list of ID's of ships that lie within a triangle with one vertex at pos, the center of its opposite side
        /// is at an angle of worldDeg degrees from the vertex, its two other sides are an angle scanWidth from this point, and
        /// its area will be equal to SCAN_ENERGY_SCALING_FACTOR times the energy spent
        /// </summary>

        private int SCAN_ENERGY_SCALING_FACTOR = 1000;
        public List<int> CircleSectorScan(Vector2 pos, double worldDeg, double scanWidth, int energySpent)
        {
            // The radius of the cone will be such that the area scanned is energySpent * SCAN_ENERGY_SCALING_FACTOR
            double areaScanned = energySpent * SCAN_ENERGY_SCALING_FACTOR;

            // Convert angles to radians
            worldDeg = (Math.PI * worldDeg) / 180.0;
            scanWidth = (Math.PI * scanWidth) / 180.0;

            // We want the radius of the circle, such that a sercular sector of angle 2*scanwidth has area areaScanned
            float radius = (float)Math.Sqrt(areaScanned / scanWidth);

            // The circular sector is a triangle whose vertices are pos, and the points at an angle (worldDeg +- scanWidth) and distance radius
            // And a segment between those points on the circle centered at pos with that radius

            Vector2 leftPoint = new Vector2(radius * (float)Math.Cos(worldDeg + scanWidth), radius * (float)Math.Sin(worldDeg + scanWidth));
            Vector2 rightPoint = new Vector2(radius * (float)Math.Cos(worldDeg - scanWidth), radius * (float)Math.Sin(worldDeg - scanWidth));

            Console.WriteLine("Scanning with radius " + radius + "; In triangle " + pos.ToString() + "," + leftPoint.ToString() + "," + rightPoint.ToString());

            List<int> result = new List<int>();

            // Go through all spaceships and add those that intersect with our triangle
            foreach (int id in ships.Keys)
            {
                if (CircleTriangleIntersection(ships[id].Pos, ships[id].Radius(), pos, leftPoint, rightPoint) || CircleSegmentIntersection(ships[id].Pos, (float)ships[id].Radius(), pos, radius, (float)worldDeg, (float)scanWidth))
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
            var maybeid = GetSpaceshipId(data.Json);
            if (maybeid == null)
            {
                response.Data["error"] = "Could not find spaceship for given token";
                response.Send(500);
                return;
            }

            int id = maybeid.Value;

            String[] requiredParams = new String[3] { "direction", "width", "energy" };

            for (int i = 0; i < requiredParams.Length; i++)
            {
                if (data.Json[requiredParams[i]] == null)
                {
                    response.Data["error"] = "Requires parameter: " + requiredParams[i];
                    response.Send(500);
                    return;
                }
            }

            float direction = (float)data.Json["direction"];

            float width = (float)data.Json["width"];
            if (width <= 0 || width >= 90)
            {
                response.Data["error"] = "Width not in interval (0,90) degrees";
                response.Send(500);
                return;
            }

            int energy = (int)data.Json["energy"];
            if (energy <= 0)
            {
                response.Data["error"] = "Energy spent must be positive";
                response.Send(500);
                return;
            }

            Spaceship ship = ships[id];
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

        /// <summary>
        /// Handles a "Shoot" REST request, damaging all ships caught in its blast. pew pew.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship, the angle to shoot at, the width of the shot, and the energy to expend on the shot.true </param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("shoot")]
        [ApiParam("token", typeof(string))]
        [ApiParam("direction", typeof(float))]
        [ApiParam("width", typeof(float))]
        [ApiParam("energy", typeof(int))]

        public void Shoot(ApiResponse response, ApiData data)
        {
            //Check that the arguments for each parameter are valid
            int id = intersectionParamCheck(response, data);
            if (id == -1)
            {
                return;
            }
            Spaceship ship = ships[id];
            float width = (float)data.Json["width"];
            float direction = (float)data.Json["direction"];

            int energy = (int)Math.Min((int)data.Json["energy"], Math.Floor(ship.Energy));
            ships[id].Energy -= energy; //remove energy for the shot

            Console.WriteLine("Shot by " + id + ", pos = " + ships[id].Pos.ToString() + " , direction = " + direction + ", width = " + width + ", energy spent = " + energy);

            List<int> struck = CircleSectorScan(ship.Pos, direction, width, energy);
            JArray struckShips = new JArray();
            foreach (int struckShipId in struck)
            {
                // ignore our ship
                if (struckShipId == id)
                    continue;

                ships[struckShipId].HitPoints -= shotDamage(energy, width, distanceBetweenShips(ship.Pos.X, ship.Pos.Y, ships[struckShipId].Pos.X, ships[struckShipId].Pos.Y));

                //The api doesnt have a return value for shooting, but ive left this in for now for testing purposes.
                JToken struckShipInfo = new JObject();
                struckShipInfo["id"] = struckShipId;
                struckShipInfo["area"] = ships[struckShipId].Area;
                struckShipInfo["posX"] = ships[struckShipId].Pos.X;
                struckShipInfo["posY"] = ships[struckShipId].Pos.Y;
                struckShipInfo["hp"] = ships[struckShipId].HitPoints;
                struckShips.Add(struckShipInfo);
            }

            response.Data["struck"] = struckShips;
            response.Send();
        }


        /// <summary>
        /// Calculates the shotDamage applied to a ship. Shot damage drops off exponentially as distance increases, base =1.1
        /// </summary>
        private int shotDamage(int energy, float width, int distance)
        {
            return (int)((energy) / ((width / 10) * Math.Pow(1.1, distance)));
        }

        /// <summary>
        /// Verifies the arguments passed in an intersection based request are appropriate.
        /// </summary>
        private int intersectionParamCheck(ApiResponse response, ApiData data)
        {
            UpdateGameState();
            var maybeid = GetSpaceshipId(data.Json);
            if (maybeid == null)
            {
                response.Data["error"] = "Could not find spaceship for given token";
                response.Send(500);
                return -1;
            }

            int id = maybeid.Value;

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
            return id;
        }

        private int distanceBetweenShips(float x1, float y1, float x2, float y2)
        {
            return (int)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

#if DEBUG
        /// <summary>
        /// A function that, when invoked, sets a certain parameter of `ship` to `value`.
        /// </summary>
        /// <param name="ship">The ship to set the attribute on.</param>
        /// <param name="value">The new value of the parameter to set.</param>
        private delegate void AttributeSetter(Spaceship ship, JToken value);

        /// <summary>
        /// The map of `JSON key name -> AttributeSetter` used by `Sudo`.
        /// Note that the names reflect those used by `GetShipInfo`!
        /// 
        /// In the future, if setters become more complex than just setting a value, one can replace the lambdas with
        /// references to a more complex function/method.
        /// </summary>
        private static readonly Dictionary<string, AttributeSetter> SUDO_SETTER_MAP = new Dictionary<string, AttributeSetter>
        {
            { "area", (ship, value) => ship.Area = (double)value },
            { "energy", (ship, energy) => ship.Energy = (double)energy },
            { "posX", (ship, posX) => ship.Pos = new Vector2((float)posX, ship.Pos.Y) },
            { "posY", (ship, posY) => ship.Pos = new Vector2(ship.Pos.X, (float)posY) },
            { "velX", (ship, velX) => ship.Velocity = new Vector2((float)velX, ship.Velocity.Y) },
            { "velY", (ship, velY) => ship.Velocity = new Vector2(ship.Velocity.X, (float)velY) },
        };

        /// <summary>
        /// "SuperUser DO"; debug-only endpoint used to forcefully set attributes of a connected ship via REST.
        /// </summary>
        /// <param name="data">The JSON payload of the request, containing the token of the ship.</param>
        /// <param name="response">The HTTP response to the client.</param>
        [ApiRoute("sudo")]
        [ApiParam("token", typeof(string))]

        public void Sudo(ApiResponse response, ApiData data)
        {
            var id = GetSpaceshipId(data.Json);
            if (id == null)
            {
                response.Data["error"] = "Could not find spaceship (did you pass a valid `token`?)";
                response.Send(500);
                return;
            }

            Spaceship ship = ships[id.Value];
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
                    setter.Invoke(ship, kv.Value);
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




