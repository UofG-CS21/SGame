﻿using System;
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

            // If the lenght is negative, the cosine of the angle is negative, so it lies more than 90 degrees around linePoint 1
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

        /// <summary>
        /// Returns a list of ID's of ships that lie within a triangle with one vertex at pos, the center of its opposite side
        /// is at an angle of worldDeg degrees from the vertex, its two other sides are an angle scanWidth from this point, and
        /// its area will be equal to SCAN_ENERGY_SCALING_FACTOR times the energy spent
        /// </summary>

        private int SCAN_ENERGY_SCALING_FACTOR = 1000;
        public List<int> TriangleScan(Vector2 pos, double worldDeg, double scanWidth, int energySpent)
        {
            // The radius of the cone will be such that the area scanned is energySpent * SCAN_ENERGY_SCALING_FACTOR
            double areaScanned = energySpent * SCAN_ENERGY_SCALING_FACTOR;

            // Convert angles to radians
            worldDeg = (Math.PI * worldDeg) / 180.0;
            scanWidth = (Math.PI * scanWidth) / 180.0;

            // We have the area of the triangle (areaScanned) and an angle at one of its vertices (2*scanWidth)
            // We want its Height (in the direction of worldDeg), and Base (perpendicular to worldDeg)

            // Its Height is given by formula Height = Sqrt( areaScanned * sin(PI/2 - scanWidth) / sin(scanWidth) )
            // Its Base is then 2 * (areaScanned / Height)
            float triangleHeight = (float)Math.Sqrt(areaScanned * Math.Sin(Math.PI / 2 - scanWidth) / Math.Sin(scanWidth));
            float triangleBase = 2 * (float)areaScanned / triangleHeight;

            // Find a point on the base of the triangle which is triangleHeight away - its components are the cosine and sine of worldDeg, scaled by triangleHeight
            Vector2 triangleBaseCenter = new Vector2(triangleHeight * (float)Math.Cos(worldDeg), triangleHeight * (float)Math.Sin(worldDeg));

            // Find the other two vertices of the triangle (the third is pos). They are triangleBaseCenter +- (half of triangleBase) * (vector perpendicular to [triangleBaseCenter-pos])  
            Vector2 triangleCenterVector = Vector2.Normalize(Vector2.Subtract(triangleBaseCenter, pos));
            Vector2 perpendicularVector = new Vector2(-triangleCenterVector.Y, triangleCenterVector.X);

            Vector2 leftPoint = triangleBaseCenter + (triangleBase / 2) * perpendicularVector;
            Vector2 rightPoint = triangleBaseCenter - (triangleBase / 2) * perpendicularVector;

            //Console.WriteLine("H: " + triangleHeight + ", B: " + triangleBase + ", BC: " + triangleBaseCenter.Tostring() + ", ")

            Console.WriteLine("Scanning in triangle " + pos.ToString() + "," + leftPoint.ToString() + "," + rightPoint.ToString());

            List<int> result = new List<int>();

            // Go through all spaceships and add those that intersect with our triangle
            foreach (int id in ships.Keys)
            {
                if (CircleTriangleIntersection(ships[id].Pos, ships[id].Radius(), pos, leftPoint, rightPoint))
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

            List<int> scanned = TriangleScan(ship.Pos, direction, width, energy);
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

    }


}


