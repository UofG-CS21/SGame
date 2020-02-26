using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using SShared;

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
        public bool HitCircle(Vector2 center, double radius, out Vector2? hitNear, out Vector2? hitFar)
        {
            hitNear = null;
            hitFar = null;

            // ray: P = rayOrigin + [cos(rayDir), sin(rayDir)] * t, t >= 0
            // circle: dot(Q, Q) = circleRadius^2, where Q = P - circleCenter
            // Then let P = Q and solve for t
            var q = Origin - center;

            // You get a quadratic in the form c1 * t^2 + c2 * t + c3 = 0 where:
            double c1 = 1.0; // = Vector2.Dot(rd, rd);
            double c2 = 2.0 * Vector2.Dot(q, DirVec);
            double c3 = Vector2.Dot(q, q) - radius * radius;
            double delta = c2 * c2 - 4.0 * c1 * c3;

            // FIXME: Prevents issues with incorrect rounding
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
        public static double Rad2Deg(double rad)
        {
            return rad * 180.0 / Math.PI;
        }

        /// <summary>
        /// Convert an angle from degrees to radians.
        /// </summary>
        public static double Deg2Rad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        // Calculates the sign of a point relative to a line defined by two points
        public static int pointLineSign(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            // Calculate the (not normalized) normal to the line
            Vector2 Normal = new Vector2(linePoint2.Y - linePoint1.Y, -(linePoint2.X - linePoint1.X));
            // The sign is equal to the sign of the dot product of the normal, and the vector from point1 to the tested point
            return System.Math.Sign(Vector2.Dot(Normal, point - linePoint1));
        }

        public static bool CircleTriangleSideIntersection(Vector2 circleCenter, double radius, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 lineVector = linePoint2 - linePoint1;
            Vector2 point1ToCircle = circleCenter - linePoint1;

            double lengthAlongTriangleSide = Vector2.Dot(point1ToCircle, lineVector);

            // If the length is negative, the cosine of the angle is negative, so it lies more than 90 degrees around linePoint 1
            // For that to intersect the triangle side, linePoint1 would already lie within the circle
            // But we have checked that in Case 1, so it must not lie. Therefore such circle does not intersect the triangle side
            if (lengthAlongTriangleSide > 0)
            {
                double sideLenghtSquared = lineVector.LengthSquared();

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
        public static bool CircleTriangleIntersection(Vector2 circleCenter, double radius, Vector2 A, Vector2 B, Vector2 C)
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
        public static bool CircleSegmentIntersection(Vector2 circleCenter, double circleRadius, Vector2 segmentCenter, double segmentRadius, double segmentAngle, double segmentWidth)
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
        /// Casts a ray to a circle, checking for the intersection points.
        /// Returns true if any intersection is found, setting `inters1` or both `inters1` and `inters2` appropriately.
        /// If both `inters1` and `inters2` are outputted, `inters1` is the intersection point nearest to `rayOrigin`.
        /// </summary>
        public static bool RaycastCircle(Vector2 rayOrigin, double rayDir, Vector2 circleCenter, double circleRadius,
            out Vector2? inters1, out Vector2? inters2)
        {
            return RaycastCircle(rayOrigin, MathUtils.DirVec(rayDir), circleCenter, circleRadius, out inters1, out inters2);
        }

        /// <summary>
        /// Casts a ray to a circle, checking for the intersection points.
        /// Returns true if any intersection is found, setting `inters1` or both `inters1` and `inters2` appropriately.
        /// If both `inters1` and `inters2` are outputted, `inters1` is the intersection point nearest to `rayOrigin`.
        /// </summary>
        public static bool RaycastCircle(Vector2 rayOrigin, Vector2 rayDir, Vector2 circleCenter, double circleRadius,
            out Vector2? inters1, out Vector2? inters2)
        {
            Console.WriteLine("Raycast from O=" + rayOrigin + ", dir=" + rayDir + ", circleCenter=" + circleCenter + ", circleRadius=" + circleRadius);

            inters1 = null;
            inters2 = null;

            // ray: P = rayOrigin + [cos(rayDir), sin(rayDir)] * t, t >= 0
            // circle: dot(Q, Q) = circleRadius^2, where Q = P - circleCenter
            // Then let P = Q and solve for t
            var q = rayOrigin - circleCenter;

            // You get a quadratic in the form c1 * t^2 + c2 * t + c3 = 0 where:
            double c1 = 1.0; // = Vector2.Dot(rd, rd);
            double c2 = 2.0 * Vector2.Dot(q, rayDir);
            double c3 = Vector2.Dot(q, q) - circleRadius * circleRadius;
            double delta = c2 * c2 - 4.0 * c1 * c3;

            if (MathUtils.ToleranceEquals(delta, 0.0, 0.000001))
            {
                Console.WriteLine(delta);
                delta = Math.Abs(delta);
            }

            switch (Math.Sign(delta))
            {
                case 1:
                    double sqrtDelta = Math.Sqrt(delta);

                    double t1 = (double)((-c2 - sqrtDelta) / (2.0 * c1));
                    inters1 = null;
                    if (t1 >= 0.0f)
                    {
                        inters1 = rayOrigin + rayDir * t1;
                    }

                    double t2 = (double)((-c2 + sqrtDelta) / (2.0 * c1));
                    inters2 = null;
                    if (t2 >= 0.0f)
                    {
                        inters2 = rayOrigin + rayDir * t2;
                    }

                    return inters1 != null || inters2 != null;

                case 0:
                    double t = (double)(-c2 / c1);
                    inters1 = null;
                    if (t >= 0)
                    {
                        inters1 = rayOrigin + rayDir * (double)(-c2 / (2.0 * c1));
                    }
                    inters2 = null;
                    return true;

                default: // -1
                    inters1 = null;
                    inters2 = null;
                    return false;
            }
        }

        /// <summary>
        /// Finds the two tangent points on a circle from an external point.
        /// `bisectAngle` will be set to the angle (0 to PI/2, in radians) between
        /// the line betwen `circleCenter` and `point` and one of the two tangents.
        /// </summary>
        public static void CircleTangents(Vector2 circleCenter, double circleRadius, Vector2 point, out Vector2 tg1, out Vector2 tg2, out double bisectAngle)
        {
            // Consider the segment from `point` to `circleCenter` and the triangles it forms with the two radii from
            // `circleCenter` to the tangents. Then if alpha is the angle between a radius and the line between the two centers,
            // alpha = arccos(adj / hyp) = arccos(radius / centerDist)
            // From that angle you can calculate the bisector angle at the external point, then the two tangent points as needed.
            Vector2 centerDelta = circleCenter - point;
            bisectAngle = Math.PI * 0.5 - Math.Acos(circleRadius / centerDelta.Length());
            double centerAngle = Math.Atan2(centerDelta.Y, centerDelta.X);

            //Internal is the third angle in the rightangled triangle which connects the tangent point, circle centre and point.
            double internalAngle = Math.PI / 2 - bisectAngle;
            tg1 = circleCenter - MathUtils.DirVec(centerAngle - internalAngle) * (double)circleRadius;
            tg2 = circleCenter - MathUtils.DirVec(centerAngle + internalAngle) * (double)circleRadius;
        }

        /// <summary>
        /// Returns true if the given point sits on top of a circle arc centered at `arcCenter`, with half-width `arcWidth` radians
        /// around `arcDir` and radius `arcRadius`. Outputs the angle on the arc if this is the case.
        /// </summary>
        public static bool IsPointOnArc(Vector2 point, Vector2 arcCenter, double arcDir, double arcWidth, double arcRadius, out double onArcAngle)
        {
            // arc: P = arcCenter + arcRadius * [cos(alpha), sin(alpha)], where alpha = arcDir + arcWidth * t, where -1 <= t <= 1
            // let point = P...
            Vector2 dirVec = (point - arcCenter) * (double)(1.0 / arcRadius);
            if (!MathUtils.ToleranceEquals(dirVec.Length(), 1.0, 0.001))
            {
                // Can't be a valid direction vector
                onArcAngle = double.NaN;
                return false;

            }

            onArcAngle = Math.Atan2(dirVec.Y, dirVec.X) - arcDir;
            onArcAngle = MathUtils.NormalizeAngle(onArcAngle);


            //Since vectors have to be doubles, There is some uncertainty introuduced when double angles are converted. So i added a tolerance.
            return (-arcWidth <= onArcAngle && onArcAngle <= arcWidth) || (MathUtils.ToleranceEquals(MathUtils.Rad2Deg(-arcWidth), MathUtils.Rad2Deg(onArcAngle), 0.000001)) || (MathUtils.ToleranceEquals(MathUtils.Rad2Deg(arcWidth), MathUtils.Rad2Deg(onArcAngle), 0.000001));
        }

        /// <summary>
        /// Calculates the intersection point[s] between two circles.
        /// Returns true if any intersection is found, setting `inters1` or both `inters1` and `inters2` appropriately.
        /// </summary>
        public static bool CircleCircleIntersection(Vector2 center1, double radius1, Vector2 center2, double radius2,
            out Vector2? inters1, out Vector2? inters2)
        {
            // See: https://math.stackexchange.com/a/1367732
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
        public static double ClampAngle(double angle, double clampValue = 2.0 * Math.PI)
        {
            angle = angle % clampValue;
            if (angle < 0.0) angle = (2.0 * Math.PI) + angle;
            return angle;
        }

        /// <summary>
        /// Clamps an angle in radians to the -PI to PI range.
        /// </summary>
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
        public static bool ToleranceEquals(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }

        /// <summary>
        /// Performs spherical linear interpolation (slerp) between two vectors.
        /// </summary>
        public static Vector2 Slerp(Vector2 a, Vector2 b, double t)
        {
            // See: https://keithmaggio.wordpress.com/2011/02/15/math-magician-lerp-slerp-and-nlerp/
            double aDotB = Vector2.Dot(a, b);
            double angle = Math.Acos(aDotB) * t;
            Vector2 delta = (b - a * aDotB).Normalized();
            return a * Math.Cos(angle) + delta * Math.Sin(angle);
        }

        /// <summary>
        /// Makes a direction vector out of an angle in radians.
        /// </summary>
        public static Vector2 DirVec(double direction)
        {
            return new Vector2((double)Math.Cos(direction), (double)Math.Sin(direction));
        }





        /// <summary>
        /// Calculates the shotDamage applied to a ship. Shot damage drops off exponentially as distance increases, base =1.1
        /// Width is in radians.
        /// </summary>
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

        /// Width is in radians.
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
        public static double ShieldingAmount(Spaceship ship, Vector2 shotOrigin, double shotDir, double shotWidth, double shotRadius)
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

            // IMPORTANT - ShotDir input is in degrees, and needs:
            // - Being clamped from 0..2pi (to prevent malicious input from breaking the code)
            // - Being normalized from -pi..pi (to prevent calculations below from potentially breaking)
            shotDir = MathUtils.NormalizeAngle(MathUtils.ClampAngle(shotDir));

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

        /// <summary>
        /// Returns the fraction of the shot, specified by the bounding angles on the victim [shotStart, shotStop]
        /// that is shielded by the shielder's shield
        /// </summary>
        public static double ShotShieldIntersection(double shotStart, double shotStop, Spaceship shielder)
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
    }
}
