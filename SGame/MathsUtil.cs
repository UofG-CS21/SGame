using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SGame.Tests")]
namespace SGame
{
    public static class MathsUtil
    {
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


    }
}
