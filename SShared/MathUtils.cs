using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using SShared;

//For an explanation of the maths contained in this file, see this file: https://stgit.dcs.gla.ac.uk/tp3-2019-cs21/cs21-main/-/wikis/Important-Documents/Geometry-Code-Documentation

namespace SShared
{
    /// <summary>
    /// A 2D ray, for raycasting.
    /// </summary>
    public struct Ray
    {
        /// <summary>
        /// World-space origin point of the ray.
        /// </summary>
        public Vector2 Origin { get; set; }

        Vector2 _dirVec;

        /// <summary>
        /// World-space direction vector of the ray (always autonormalized).
        /// </summary>
        public Vector2 DirVec
        {
            get
            {
                return _dirVec;
            }
            set
            {
                _dirVec = value.Normalized();
            }
        }

        /// <summary>
        /// World-space direction angle of the ray (prefer `DirVec` to this!).
        /// </summary>
        public double Dir
        {
            get
            {
                return Math.Atan2(_dirVec.Y, _dirVec.X);
            }
            set
            {
                _dirVec = MathUtils.DirVec(value);
            }
        }

        public Ray(Vector2 origin, Vector2 dirVec)
        {
            Origin = origin;
            _dirVec = dirVec;
        }

        public Ray(Vector2 origin, double dir)
        {
            Origin = origin;
            _dirVec = MathUtils.DirVec(dir);
        }


        /// <summary>
        /// Computes the intersections with the given circle (either none, only `hitNear`, or both `hitNear` and `hitFar`).
        /// Returns false on no hit.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="hitNear"></param>
        /// <param name="hitFar"></param>
        /// <returns></returns>
        public bool HitCircle(Vector2 center, double radius, out Vector2? hitNear, out Vector2? hitFar)
        {
            hitNear = null;
            hitFar = null;

            var q = Origin - center;

            double c1 = 1.0;
            double c2 = 2.0 * Vector2.Dot(q, DirVec);
            double c3 = Vector2.Dot(q, q) - radius * radius;
            double delta = c2 * c2 - 4.0 * c1 * c3;

            if (MathUtils.ToleranceEquals(delta, 0.0, 0.001))
            {
                delta = Math.Abs(delta);
            }

            switch (Math.Sign(delta))
            {
                case 1:
                    double sqrtDelta = Math.Sqrt(delta);

                    double t1 = (double)((-c2 - sqrtDelta) / (2.0 * c1));
                    hitNear = null;
                    if (t1 >= 0.0f)
                    {
                        hitNear = Origin + DirVec * t1;
                    }

                    double t2 = (double)((-c2 + sqrtDelta) / (2.0 * c1));
                    hitFar = null;
                    if (t2 >= 0.0f)
                    {
                        hitFar = Origin + DirVec * t2;
                    }

                    return hitNear != null || hitFar != null;

                case 0:
                    double t = (double)(-c2 / c1);
                    hitNear = null;
                    if (t >= 0)
                    {
                        hitNear = Origin + DirVec * (double)(-c2 / (2.0 * c1));
                    }
                    hitFar = null;
                    return true;

                default: // -1
                    return false;
            }
        }
    }

    /// <summary>
    /// Miscellaneous mathematical and geometrical utilities. 
    /// </summary>
    public static class MathUtils
    {

        /// <summary>
        /// Convert an angle from radians to degrees.
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static double Rad2Deg(double rad)
        {
            return rad * 180.0 / Math.PI;
        }

        /// <summary>
        /// Convert an angle from degrees to radians.
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double Deg2Rad(double deg)
        {
            return deg * Math.PI / 180.0;
        }


        /// <summary>
        /// Calculates the sign of a point relative to a line defined by two points
        /// </summary>
        /// <param name="point"></param>
        /// <param name="linePoint1"></param>
        /// <param name="linePoint2"></param>
        /// <returns></returns>
        public static int pointLineSign(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 Normal = new Vector2(linePoint2.Y - linePoint1.Y, -(linePoint2.X - linePoint1.X));
            return System.Math.Sign(Vector2.Dot(Normal, point - linePoint1));
        }

        /// <summary>
        /// This method determines whether on not a circle with the given center and radius will intersect a line representing one side of a triangle
        /// </summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="linePoint1"></param>
        /// <param name="linePoint2"></param>
        /// <returns></returns>
        public static bool CircleTriangleSideIntersection(Vector2 circleCenter, double radius, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 lineVector = linePoint2 - linePoint1;
            Vector2 point1ToCircle = circleCenter - linePoint1;

            double lengthAlongTriangleSide = Vector2.Dot(point1ToCircle, lineVector);

            if (lengthAlongTriangleSide > 0)
            {
                double sideLenghtSquared = lineVector.LengthSquared();

                lengthAlongTriangleSide = lengthAlongTriangleSide * lengthAlongTriangleSide / sideLenghtSquared;

                if (lengthAlongTriangleSide < sideLenghtSquared)
                {
                    if (point1ToCircle.LengthSquared() - lengthAlongTriangleSide <= radius * radius)
                        return true;
                }
            }

            return false;
        }


        // Based on http://www.phatcode.net/articles.php?id=459 
        /// <summary>
        /// Returns true iff the circle centered at circleCenter with radius 'radius' intersects the triangle with vertices A,B,C 
        /// </summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public static bool CircleTriangleIntersection(Vector2 circleCenter, double radius, Vector2 A, Vector2 B, Vector2 C)
        {

            Console.WriteLine("Testing intersection of " + circleCenter.ToString() + ", r=" + radius + " with " + A.ToString() + "," + B.ToString() + "," + C.ToString());

            Vector2 cA = A - circleCenter, cB = B - circleCenter, cC = C - circleCenter;

            if (radius * radius >= cA.LengthSquared()) return true;
            if (radius * radius >= cB.LengthSquared()) return true;
            if (radius * radius >= cC.LengthSquared()) return true;

            int sAB = pointLineSign(circleCenter, A, B);
            int sBC = pointLineSign(circleCenter, B, C);
            int sCA = pointLineSign(circleCenter, C, A);

            if (sAB >= 0 && sBC >= 0 && sCA >= 0) return true;
            if (sAB <= 0 && sBC <= 0 && sCA <= 0) return true;

            if (CircleTriangleSideIntersection(circleCenter, radius, A, B)) return true;
            if (CircleTriangleSideIntersection(circleCenter, radius, B, C)) return true;
            if (CircleTriangleSideIntersection(circleCenter, radius, C, A)) return true;

            return false;
        }


        /// <summary>
        /// Return true iff the circle cenered at circleCenter with radius circleRadius intersects 
        /// the segment of a circle centered at segmentRadius, with its midpoint in the direction segmentAngle, and its angular width 2*segmentWidth
        /// </summary>
        /// <param name="circleCenter"></param>
        /// <param name="circleRadius"></param>
        /// <param name="segmentCenter"></param>
        /// <param name="segmentRadius"></param>
        /// <param name="segmentAngle"></param>
        /// <param name="segmentWidth"></param>
        /// <returns></returns>
        public static bool CircleSegmentIntersection(Vector2 circleCenter, double circleRadius, Vector2 segmentCenter, double segmentRadius, double segmentAngle, double segmentWidth)
        {

            if (circleRadius + segmentRadius < Vector2.Subtract(segmentCenter, circleCenter).Length())
                return false;

            double circularSectorEdgeAngleDistance = segmentWidth * 2;

            Vector2 segmentCenterToCircleCenter = circleCenter - segmentCenter;

            double circleCenterAngle = (double)Math.Atan2(segmentCenterToCircleCenter.Y, segmentCenterToCircleCenter.X);

            double distance1 = Math.Abs((segmentAngle - segmentWidth) - circleCenterAngle);
            if (distance1 > Math.PI)
                distance1 = 2 * (double)Math.PI - distance1;

            if (distance1 > circularSectorEdgeAngleDistance)
                return false;

            double distance2 = Math.Abs((segmentAngle + segmentWidth) - circleCenterAngle);
            if (distance2 > Math.PI)
                distance2 = 2 * (double)Math.PI - distance2;

            if (distance2 > circularSectorEdgeAngleDistance)
                return false;

            return true;
        }

        /// <summary>
        /// Finds the two tangent points on a circle from an external point.
        /// `bisectAngle` will be set to the angle (0 to PI/2, in radians) between
        /// the line betwen `circleCenter` and `point` and one of the two tangents.
        /// </summary>
        /// <param name="circleCenter"></param>
        /// <param name="circleRadius"></param>
        /// <param name="point"></param>
        /// <param name="tg1"></param>
        /// <param name="tg2"></param>
        /// <param name="bisectAngle"></param>
        public static void CircleTangents(Vector2 circleCenter, double circleRadius, Vector2 point, out Vector2 tg1, out Vector2 tg2, out double bisectAngle)
        {
            Vector2 centerDelta = circleCenter - point;
            bisectAngle = Math.PI * 0.5 - Math.Acos(circleRadius / centerDelta.Length());
            double centerAngle = Math.Atan2(centerDelta.Y, centerDelta.X);

            double internalAngle = Math.PI / 2 - bisectAngle;
            tg1 = circleCenter - MathUtils.DirVec(centerAngle - internalAngle) * (double)circleRadius;
            tg2 = circleCenter - MathUtils.DirVec(centerAngle + internalAngle) * (double)circleRadius;
        }


        /// <summary>
        /// Returns true if the given point sits on top of a circle arc centered at `arcCenter`, with half-width `arcWidth` radians
        /// around `arcDir` and radius `arcRadius`. Outputs the angle on the arc if this is the case.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="arcCenter"></param>
        /// <param name="arcDir"></param>
        /// <param name="arcWidth"></param>
        /// <param name="arcRadius"></param>
        /// <param name="onArcAngle"></param>
        /// <returns></returns>
        public static bool IsPointOnArc(Vector2 point, Vector2 arcCenter, double arcDir, double arcWidth, double arcRadius, out double onArcAngle)
        {
            Vector2 dirVec = (point - arcCenter) * (double)(1.0 / arcRadius);
            if (!MathUtils.ToleranceEquals(dirVec.Length(), 1.0, 0.001))
            {
                onArcAngle = double.NaN;
                return false;

            }

            onArcAngle = Math.Atan2(dirVec.Y, dirVec.X) - arcDir;
            onArcAngle = MathUtils.NormalizeAngle(onArcAngle);

            return (-arcWidth <= onArcAngle && onArcAngle <= arcWidth) || (MathUtils.ToleranceEquals(MathUtils.Rad2Deg(-arcWidth), MathUtils.Rad2Deg(onArcAngle), 0.000001)) || (MathUtils.ToleranceEquals(MathUtils.Rad2Deg(arcWidth), MathUtils.Rad2Deg(onArcAngle), 0.000001));
        }


        /// <summary>
        /// Calculates the intersection point[s] between two circles.
        /// Returns true if any intersection is found, setting `inters1` or both `inters1` and `inters2` appropriately.
        /// </summary>
        /// <param name="center1"></param>
        /// <param name="radius1"></param>
        /// <param name="center2"></param>
        /// <param name="radius2"></param>
        /// <param name="inters1"></param>
        /// <param name="inters2"></param>
        /// <returns></returns>
        public static bool CircleCircleIntersection(Vector2 center1, double radius1, Vector2 center2, double radius2,
            out Vector2? inters1, out Vector2? inters2)
        {
            double rDist = (center1 - center2).Length();
            int distSign = Math.Sign(rDist - (radius1 + radius2));
            if ((distSign == 1) || (rDist + radius1 < radius2) || (rDist + radius2 < radius1))
            {
                inters1 = null;
                inters2 = null;
                return false;
            }
            else
            {
                double r1r2Sq = radius1 * radius1 - radius2 * radius2;
                double rDistSq = rDist * rDist;
                double c1 = r1r2Sq / (2.0 * rDistSq);
                Vector2 k1 = Vector2.Multiply(center1 + center2, 0.5f)
                             + Vector2.Multiply(center2 - center1, (double)c1);
                if ((distSign == 0) || (rDist + radius1 == radius2) || (rDist + radius2 == radius1))
                {
                    inters1 = k1;
                    inters2 = null;
                }
                else
                {
                    double c2 = 0.5 * Math.Sqrt(2.0 * (radius1 * radius1 + radius2 * radius2) / rDistSq - (r1r2Sq * r1r2Sq) / (rDistSq * rDistSq) - 1.0);
                    Vector2 k2 = Vector2.Multiply(new Vector2(center2.Y - center1.Y, center1.X - center2.X), (double)c2);
                    inters1 = k1 - k2;
                    inters2 = k1 + k2;
                }
                return true;
            }
        }

        /// <summary>
        /// Normalizes an angle in radians, i.e. makes it positive and between 0 and `clampValue`.
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="clampValue"></param>
        /// <returns></returns>
        public static double ClampAngle(double angle, double clampValue = 2.0 * Math.PI)
        {
            angle = angle % clampValue;
            if (angle < 0.0) angle = (2.0 * Math.PI) + angle;
            return angle;
        }


        /// <summary>
        /// Clamps an angle in radians to the -PI to PI range.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double NormalizeAngle(double angle)
        {
            if (angle > Math.PI)
            {
                angle -= 2.0 * Math.PI;
            }
            else if (angle < -Math.PI)
            {
                angle += 2.0 * Math.PI;
            }
            return angle;

        }

        /// <summary>
        /// Checks two numbers for equality within a tolerance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool ToleranceEquals(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }


        /// <summary>
        /// Performs spherical linear interpolation (slerp) between two vectors.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector2 Slerp(Vector2 a, Vector2 b, double t)
        {

            double aDotB = Vector2.Dot(a, b);
            double angle = Math.Acos(aDotB) * t;
            Vector2 delta = (b - a * aDotB).Normalized();
            return a * Math.Cos(angle) + delta * Math.Sin(angle);
        }

        /// <summary>
        /// Makes a direction vector out of an angle in radians.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Vector2 DirVec(double direction)
        {
            return new Vector2((double)Math.Cos(direction), (double)Math.Sin(direction));
        }

        /// <summary>
        /// Calculates the shotDamage applied to a ship. Shot damage drops off exponentially as distance increases, base =1.1
        /// Width is in radians.
        /// </summary>
        /// <param name="scaledEnergy"></param>
        /// <param name="width"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static double ShotDamage(double scaledEnergy, double width, double distance)
        {
            distance = Math.Max(distance, 1);
            return scaledEnergy / (Math.Max(1, Math.Pow(2, 2 * width)) * Math.Sqrt(distance));

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
        public const int SCAN_ENERGY_SCALING_FACTOR = 2000;

        /// <summary>
        /// Width is in radians.
        /// </summary>
        /// <param name="scanWidth"></param>
        /// <param name="energySpent"></param>
        /// <returns></returns>
        public static double ScanShootRadius(double scanWidth, double energySpent)
        {
            double areaScanned = energySpent * SCAN_ENERGY_SCALING_FACTOR;
            return Math.Sqrt(areaScanned / (2 * scanWidth));

        }


        /// <summary>
        /// Returns the percentage (0.0 to 1.0) of damage covered by a ship's shield when it is being shot
        /// from `shotOrigin` with a cone of half-width `shotWidth` radians and length `shotRadius`.
        /// WARNING: `width` and `shotDir` are in RADIANS!
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="shotOrigin"></param>
        /// <param name="shotDir"></param>
        /// <param name="shotWidth"></param>
        /// <param name="shotRadius"></param>
        /// <returns></returns>
        public static double ShieldingAmount(Spaceship ship, Vector2 shotOrigin, double shotDir, double shotWidth, double shotRadius)
        {
            double shipR = ship.Radius();
            if ((shotOrigin - ship.Pos).Length() <= shipR)
            {
                return 0.0;
            }

            if (ship.ShieldWidth < 0.001)
            {
                return 0.0;
            }

            shotDir = MathUtils.NormalizeAngle(MathUtils.ClampAngle(shotDir));

            Vector2 tgLeft, tgRight;
            double tgAngle;
            MathUtils.CircleTangents(ship.Pos, shipR, shotOrigin, out tgLeft, out tgRight, out tgAngle);


            Vector2 shipCenterDelta = ship.Pos - shotOrigin;
            double shipCenterAngle = Math.Atan2(shipCenterDelta.Y, shipCenterDelta.X);
            double CAS2SS = MathUtils.NormalizeAngle(shipCenterAngle - shotDir);
            Console.WriteLine("shot dir: " + MathUtils.Rad2Deg(shotDir) + "°, CAS2SS: " + MathUtils.Rad2Deg(CAS2SS) + "°, tgangle: " + MathUtils.Rad2Deg(tgAngle) + "°");

            double tgLeftAngleSS = -tgAngle + CAS2SS, tgRightAngleSS = tgAngle + CAS2SS;
            Console.WriteLine("LA " + tgLeftAngleSS + " RA " + tgRightAngleSS);
            if (tgLeftAngleSS > tgRightAngleSS)
            {
                (tgLeftAngleSS, tgRightAngleSS) = (tgRightAngleSS, tgLeftAngleSS);
                (tgLeft, tgRight) = (tgRight, tgLeft);
            }

            Vector2? capHitLeft, capHitRight;
            double leftCapAngleSS = Double.NegativeInfinity, rightCapAngleSS = Double.PositiveInfinity;
            if (MathUtils.CircleCircleIntersection(shotOrigin, shotRadius, ship.Pos, shipR, out capHitLeft, out capHitRight))
            {
                Console.WriteLine("The circular part of the shot intersects the ship!");
                if (capHitRight == null) capHitRight = capHitLeft;

                double capDist1 = (capHitLeft.Value - tgLeft).Length(), capDist2 = (capHitRight.Value - tgLeft).Length();
                if (capDist2 < capDist1)
                {
                    (capHitLeft, capHitRight) = (capHitRight, capHitLeft);
                }

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


            tgLeftAngleSS = MathUtils.NormalizeAngle(tgLeftAngleSS);
            tgRightAngleSS = MathUtils.NormalizeAngle(tgRightAngleSS);
            if (!Double.IsNegativeInfinity(leftCapAngleSS)) leftCapAngleSS = MathUtils.NormalizeAngle(leftCapAngleSS);
            if (!Double.IsPositiveInfinity(rightCapAngleSS)) rightCapAngleSS = MathUtils.NormalizeAngle(rightCapAngleSS);


            double leftRayAngleSS = Math.Max(Math.Max(-shotWidth, tgLeftAngleSS), leftCapAngleSS);
            double rightRayAngleSS = Math.Min(Math.Min(tgRightAngleSS, shotWidth), rightCapAngleSS);

            Vector2? leftHitNear = null, leftHitFar = null;
            bool leftHit = new Ray(shotOrigin, shotDir + leftRayAngleSS).HitCircle(ship.Pos, shipR, out leftHitNear, out leftHitFar);
            Vector2? rightHitNear = null, rightHitFar = null;
            bool rightHit = new Ray(shotOrigin, shotDir + rightRayAngleSS).HitCircle(ship.Pos, shipR, out rightHitNear, out rightHitFar);

            if (!leftHit || !rightHit)
            {
                throw new InvalidOperationException("Raycast missed during shield calculation!");
            }

            Console.WriteLine("LH = " + leftHitNear.Value + " RH = " + rightHitNear.Value);

            double leftVictimHit = Math.Atan2(leftHitNear.Value.Y - ship.Pos.Y, leftHitNear.Value.X - ship.Pos.X);
            double rightVictimHit = Math.Atan2(rightHitNear.Value.Y - ship.Pos.Y, rightHitNear.Value.X - ship.Pos.X);

            Console.WriteLine("From victim's point of view " + ship.Pos + ": " + leftVictimHit + "," + rightVictimHit);

            return ShotShieldIntersection(leftVictimHit, rightVictimHit, ship);
        }


        /// <summary>
        /// Returns the fraction of the shot, specified by the bounding angles on the victim [shotStart, shotStop]
        /// that is shielded by the shielder's shield
        /// </summary>
        /// <param name="shotStart"></param>
        /// <param name="shotStop"></param>
        /// <param name="shielder"></param>
        /// <returns></returns>
        public static double ShotShieldIntersection(double shotStart, double shotStop, Spaceship shielder)
        {

            shotStart = MathUtils.ClampAngle(shotStart);
            shotStop = MathUtils.ClampAngle(shotStop);

            Console.WriteLine("shot " + shotStart + "," + shotStop);

            if (Math.Abs(shotStop - shotStart) > Math.PI)
            {
                double larger = Math.Max(shotStart, shotStop);
                double smaller = Math.Min(shotStart, shotStop);
                shotStart = larger;
                shotStop = smaller;
            }
            else
            {
                double larger = Math.Max(shotStart, shotStop);
                double smaller = Math.Min(shotStart, shotStop);
                shotStart = smaller;
                shotStop = larger;
            }

            double shieldStart = MathUtils.ClampAngle(shielder.ShieldDir - shielder.ShieldWidth);
            double shieldStop = MathUtils.ClampAngle(shielder.ShieldDir + shielder.ShieldWidth);


            if (Math.Min(shieldStart, shieldStop) > MathUtils.ClampAngle(shielder.ShieldDir) || Math.Max(shieldStart, shieldStop) < MathUtils.ClampAngle(shielder.ShieldDir))
            {
                double larger = Math.Max(shieldStart, shieldStop);
                double smaller = Math.Min(shieldStart, shieldStop);
                shieldStop = smaller;
                shieldStart = larger;
            }
            else
            {
                double larger = Math.Max(shieldStart, shieldStop);
                double smaller = Math.Min(shieldStart, shieldStop);
                shieldStart = smaller;
                shieldStop = larger;
            }

            if (shotStop < shotStart) shotStop += 2 * Math.PI;
            if (shieldStart < shotStart) shieldStart += 2 * Math.PI;
            if (shieldStop < shotStart) shieldStop += 2 * Math.PI;


            Console.WriteLine("Shooting from " + shotStart + " to " + shotStop + ", shielded from " + shieldStart + " to " + shieldStop);

            double[] angles = { shotStart, shotStop, shieldStart, shieldStop };
            Array.Sort(angles);

            double distanceShielded = 0;


            if (angles[1] == shotStop)
            {
                if (angles[2] == shieldStop)
                    distanceShielded = shotStop - shotStart;
                else
                    distanceShielded = 0;
            }
            else if (angles[1] == shieldStart)
            {
                distanceShielded = angles[2] - angles[1];
            }
            else
            {
                distanceShielded = angles[1] - angles[0];

                if (angles[2] == shieldStart)
                {
                    distanceShielded += angles[3] - angles[2];
                }
            }

            double distanceTotal = shotStop - shotStart;
            return distanceShielded / distanceTotal;
        }

        public static bool DoesQuadIntersectCircleSector(Quad quad, Messages.ScanShoot msg)
        {
            Vector2 quadCentre = new Vector2(quad.CentreX, quad.CentreY);
            double maximumQuadRadius = 1.41421356237 * quad.Radius;
            Vector2 leftPoint = new Vector2(msg.Radius * Math.Cos(msg.Direction + msg.Width), msg.Radius * Math.Sin(msg.Direction + msg.Width));
            Vector2 rightPoint = new Vector2(msg.Radius * Math.Cos(msg.Direction - msg.Width), msg.Radius * Math.Sin(msg.Direction - msg.Width));

            return CircleTriangleIntersection(quadCentre, maximumQuadRadius, msg.Origin, leftPoint, rightPoint) || CircleSegmentIntersection(quadCentre, maximumQuadRadius, msg.Origin, msg.Radius, msg.Direction, msg.Width);
        }

        public static double RandomInRange(double min, double max)
        {
            Random random = new Random();
            return min + random.NextDouble() * (max - min);
        }

        public static Quad RandomQuadInQuad(Quad quad, double radius)
        {
            double centreX = RandomInRange(quad.X + radius, quad.X2 - radius);
            double centreY = RandomInRange(quad.Y2 + radius, quad.Y - radius);
            return new Quad(centreX, centreY, radius);
        }
    }
}
