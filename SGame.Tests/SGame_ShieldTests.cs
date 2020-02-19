
using System;
using System.Collections.Generic;
using System.Collections;
using SGame;
using Xunit;

namespace SGame.Tests
{
    public class SGame_ShieldTests
    {
        [Theory]
        [ClassData(typeof(IPOATestData))]
        public void IsPointOnArcTest(Vector2 point, Vector2 arcCenter, double arcDir, double arcWidth, double arcRadius, bool expected)
        {

            double onArcAngle = double.MaxValue;
            bool actual = Api.IsPointOnArc(point, arcCenter, Api.Deg2Rad(arcDir), Api.Deg2Rad(arcWidth), arcRadius, out onArcAngle);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [ClassData(typeof(CircleTangentsTestData))]
        public void CircleTangentsTest(Vector2 circleCenter, double circleRadius, Vector2 point, Vector2 expectedTangent1, Vector2 expectedTangent2, double expectedAngle)
        {
            Vector2 tg1, tg2;
            double bisectAngle;
            expectedAngle = Math.Round(Api.Deg2Rad(expectedAngle), 14);

            Api.CircleTangents(circleCenter, circleRadius, point, out tg1, out tg2, out bisectAngle);
            //Round to account for geogebra rounding.
            double[] actualVectorValues = new double[] { Math.Round(tg1.X, 6), Math.Round(tg1.Y, 6), Math.Round(tg2.X, 6), Math.Round(tg2.Y, 6) };
            double[] expectedVectorValues = new double[] { Math.Round((expectedTangent1).X, 6), Math.Round((expectedTangent1).Y, 6), Math.Round((expectedTangent2).X, 6), Math.Round((expectedTangent2).Y, 6) };
            Assert.Equal(expectedVectorValues, actualVectorValues);
            Assert.Equal(expectedAngle, Math.Round(bisectAngle, 14));
        }

        [Theory]
        [ClassData(typeof(RaycastCircleTestData))]
        public void RaycastCircleTest(Vector2 rayOrigin, double rayDir, Vector2 circleCenter, double circleRadius,
            Vector2? expectedInters1, Vector2? expectedInters2, bool expectedBool)
        {

            Vector2? inters1, inters2;
            rayDir = Api.Deg2Rad(rayDir);
            bool result = Api.RaycastCircle(rayOrigin, rayDir, circleCenter, circleRadius,
            out inters1, out inters2);




            //Gotta make sure vector is null or not to perform correct comparison.
            //Round to account for geogebra rounding.
            if (inters1 == null || expectedInters1 == null)
            {
                Assert.Equal(expectedInters1, inters1);
                Assert.Equal(expectedInters2, inters2);
            }
            else if (inters2 == null)
            {
                double[] actualIntersection1Values = new double[] { Math.Round(((Vector2)inters1).X, 6), Math.Round(((Vector2)inters1).Y, 6), };
                double[] expectedIntersection1Values = new double[] { Math.Round(((Vector2)expectedInters1).X, 6), Math.Round(((Vector2)expectedInters1).Y, 6) };
                Assert.Equal(expectedIntersection1Values, actualIntersection1Values);
                Assert.Equal(expectedInters2, inters2);
            }
            else
            {
                //if expectedInters2 == null, im investigating a tangent point. The case of returning two vectors may be caused by geogebra giving me an angle thats slighly off.
                //So, if both the vectors are basically the same value, we let it slide.
                if (expectedInters2 == null)
                {
                    Console.WriteLine("HAAAAAAAAAAAA    " + inters1 + "  OAAAAAAAA   " + inters2);
                    if (MathUtils.ToleranceEquals(((Vector2)inters1).X, ((Vector2)inters2).X, 0.000001) && MathUtils.ToleranceEquals(((Vector2)inters1).Y, ((Vector2)inters2).Y, 0.000001))
                    {
                        expectedInters2 = inters2;
                    }
                    Assert.NotNull(expectedInters2);
                }
                double[] actualIntersectionValues = new double[] { Math.Round(((Vector2)inters1).X, 5), Math.Round(((Vector2)inters1).Y, 5), Math.Round(((Vector2)inters2).X, 5), Math.Round(((Vector2)inters2).Y, 5), };
                double[] expectedIntersectionValues = new double[] { Math.Round(((Vector2)expectedInters1).X, 5), Math.Round(((Vector2)expectedInters1).Y, 5), Math.Round(((Vector2)expectedInters2).X, 5), Math.Round(((Vector2)expectedInters2).Y, 5) };
                Assert.Equal(expectedIntersectionValues, actualIntersectionValues);
            }

            Assert.Equal(expectedBool, result);


        }
        [Theory]
        [ClassData(typeof(CircleCircleIntersectionTestData))]
        public void CircleCircleIntersectionTest(Vector2 center1, double radius1, Vector2 center2, double radius2,
            Vector2? expectedInters1, Vector2? expectedInters2, bool expectedBool)
        {

            Vector2? inters1, inters2;
            bool result = Api.CircleCircleIntersection(center1, radius1, center2, radius2, out inters1, out inters2);


            //Gotta make sure vector is null or not to perform correct comparison.
            //Round to account for geogebra rounding.
            if (inters1 == null)
            {
                Assert.Equal(expectedInters1, inters1);
                Assert.Equal(expectedInters2, inters2);
            }
            else if (inters2 == null)
            {
                double[] actualIntersection1Values = new double[] { Math.Round(((Vector2)inters1).X, 6), Math.Round(((Vector2)inters1).Y, 6), };
                double[] expectedIntersection1Values = new double[] { Math.Round(((Vector2)expectedInters1).X, 6), Math.Round(((Vector2)expectedInters1).Y, 6) };
                Assert.Equal(expectedIntersection1Values, actualIntersection1Values);
                Assert.Equal(expectedInters2, inters2);
            }
            else
            {
                //if expectedInters2 == null, im investigating a single or no point of intersection.
                //However CircleCircleIntersections still returns non null values when circle1 contins circle2 or vice-versa
                Assert.NotNull(expectedInters2);
                double[] actualIntersectionValues = new double[] { Math.Round(((Vector2)inters1).X, 5), Math.Round(((Vector2)inters1).Y, 5), Math.Round(((Vector2)inters2).X, 5), Math.Round(((Vector2)inters2).Y, 5), };
                double[] expectedIntersectionValues = new double[] { Math.Round(((Vector2)expectedInters1).X, 5), Math.Round(((Vector2)expectedInters1).Y, 5), Math.Round(((Vector2)expectedInters2).X, 5), Math.Round(((Vector2)expectedInters2).Y, 5) };
                Assert.Equal(expectedIntersectionValues, actualIntersectionValues);
            }
            Assert.Equal(expectedBool, result);


        }

        [Theory]
        [ClassData(typeof(ShieldingAmountTestData))]
        internal void ShieldingAmountTest(Spaceship ship, Vector2 shotOrigin, double shotDir, double shotWidth, double shotRadius, double expectedValue)
        {

            double actual = Api.ShieldingAmount(ship, shotOrigin, shotDir, shotWidth, shotRadius);
            // Assert.Equal(expectedValue, actual);
            Assert.True(MathUtils.ToleranceEquals(expectedValue, actual, 0.02));

        }
    }



    //TODO: Ask paolo or mustafa if there is an easier way to work with test ships

    public class ShieldingAmountTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            GameTime gameTime = new GameTime();
            Spaceship ship;
            Vector2 shotOrigin;
            double shotDir, shotWidth, shotRadius, shipRadius;
            double expectedValue;

            //Test case 1: Attacker is within defending ship. No damage should be shielded.

            gameTime.Reset();
            ship = new Spaceship(1, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 10;
            ship.Area = shipRadius * shipRadius * Math.PI;

            yield return new object[] { ship, shotOrigin = new Vector2(5, 5), shotDir = -126.869897645844, shotWidth = 1, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };

            //Test case 2: Attacker is on edge of defenders radius

            gameTime.Reset();
            ship = new Spaceship(2, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            yield return new object[] { ship, shotOrigin = new Vector2(2, 3), shotDir = -90, shotWidth = 1, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };


            //Test case 3: Ray is entirely blocked by shield (Centre of shot passes through centre of ship)

            gameTime.Reset();
            ship = new Spaceship(3, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(135);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -45, shotWidth = 15, (ship.Pos - shotOrigin).Length(), expectedValue = 1.0 };

            //Test case 4: Same as Test case 3 but shot comes from opposite side

            gameTime.Reset();
            ship = new Spaceship(4, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(315);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(5, -2), shotDir = 135, shotWidth = 15, (ship.Pos - shotOrigin).Length(), expectedValue = 1.0 };

            //Test case 5: Ray is unimpeded by shield. (Centre of shot passes through centre of ship)

            gameTime.Reset();
            ship = new Spaceship(5, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(45);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -45, shotWidth = 15, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };

            //Test case 6: Ray is unimpeded on entry but impeded on exit. (Centre of shot passes through centre of ship)

            gameTime.Reset();
            ship = new Spaceship(6, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(315);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -45, shotWidth = 5, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };



            //Test case 7: Ray is entirely blocked by shield (Centre of shot does not pass through centre of ship)

            gameTime.Reset();
            ship = new Spaceship(7, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(130);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -30, shotWidth = 5, (ship.Pos - shotOrigin).Length(), expectedValue = 1.0 };

            //Test case 8: Same as Test case 7 but shot comes from opposite side

            gameTime.Reset();
            ship = new Spaceship(8, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(315);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(5, -2), shotDir = 135, shotWidth = 15, (ship.Pos - shotOrigin).Length(), expectedValue = 1.0 };

            //Test case 9: Ray is unimpeded by shield. (Centre of shot does not pass through centre of ship)

            gameTime.Reset();
            ship = new Spaceship(9, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(45);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -53, shotWidth = 5, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };

            //Test case 10: Ray is unimpeded on entry but impeded on exit. (Centre of shot does not pass through centre of ship)

            gameTime.Reset();
            ship = new Spaceship(10, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(290);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -53, shotWidth = 5, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };

            //Test case 11: Ray is partially blocked by shield. 
            gameTime.Reset();
            ship = new Spaceship(11, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(105);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -45, shotWidth = 15, (ship.Pos - shotOrigin).Length(), expectedValue = 0.5 };

            //Test case 12: Ray is almost completely blocked by shield. 
            gameTime.Reset();
            ship = new Spaceship(12, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(45);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(2, 5), shotDir = -90, shotWidth = 15, (ship.Pos - shotOrigin).Length(), expectedValue = (Api.Deg2Rad(75) - Api.Deg2Rad(73.82604780385)) / Api.Deg2Rad(30) };

            //Test case 13: Ray is just barely completely blocked by shield. (at this point, 1.0 should be returned)
            gameTime.Reset();
            ship = new Spaceship(13, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(123.3330639104773);
            ship.ShieldWidth = Api.Deg2Rad(30);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -45, shotWidth = 15, (ship.Pos - shotOrigin).Length(), expectedValue = 1.0 };


            //The following test cases test the edge case that the two intersection points are covered but some section of the centre isnt.
            //Test case 14: 
            gameTime.Reset();
            ship = new Spaceship(14, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(-90);
            ship.ShieldWidth = Api.Deg2Rad(179);
            yield return new object[] { ship, shotOrigin = new Vector2(-1, 4), shotDir = -45, shotWidth = 30, (ship.Pos - shotOrigin).Length(), expectedValue = (58.0 / 60.0) };


            // //Test case 15: 
            // gameTime.Reset();
            // ship = new Spaceship(15, gameTime);
            // ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            // shipRadius = 2;
            // ship.Area = shipRadius * shipRadius * Math.PI;
            // ship.ShieldDir = Api.Deg2Rad(176.076);
            // ship.ShieldWidth = Api.Deg2Rad(179);
            // yield return new object[] { ship, shotOrigin = new Vector2(5.5, 1), shotDir = 180, shotWidth = 5, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };


            //Test case 16: 
            gameTime.Reset();
            ship = new Spaceship(16, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 2;
            ship.Area = shipRadius * shipRadius * Math.PI;
            ship.ShieldDir = Api.Deg2Rad(133.573);
            ship.ShieldWidth = Api.Deg2Rad(179);
            yield return new object[] { ship, shotOrigin = new Vector2(5, -1.5), shotDir = 360 - 206.565051177078, shotWidth = 5, (ship.Pos - shotOrigin).Length(), expectedValue = 1.0 };





        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    public class CircleCircleIntersectionTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            Vector2 center1, center2;
            double radius1, radius2;
            Vector2? expectedInters1, expectedInters2;

            //Test case 1: Circle centers are identical TODO: Consult paolo about inteneded functionality


            //Test case 2: Circle 1 completly contains circle 2 (No intersections)

            yield return new object[] { center1 = new Vector2(-2, 1), radius1 = 4, center2 = new Vector2(-1, 1), radius2 = 1, expectedInters1 = null, expectedInters2 = null, false };

            //Test case 3: Circle 1 contains circle 2 (1 intersection)

            yield return new object[] { center1 = new Vector2(-2, 1), radius1 = 4, center2 = new Vector2(-2, 4), radius2 = 1, expectedInters1 = new Vector2(-2, 5), null, true };

            //Test case 4: Circle 1 contains circle 2 (2 intersections) 

            yield return new object[] { center1 = new Vector2(-2, 1), radius1 = 4, center2 = new Vector2(-2, 4.44), radius2 = 1, expectedInters1 = new Vector2(-2.8877983962749, 4.9002325581395), new Vector2(-1.1122016037251, 4.9002325581395), true };

            //Test case 5: Circle 2 completely contains circle 1 (No intersections)

            yield return new object[] { center1 = new Vector2(1, 1), radius1 = 1, center2 = new Vector2(2, 1), radius2 = 4, expectedInters1 = null, expectedInters2 = null, false };

            //Test case 6: Circle 2 contains circle 1 (1 intersection)

            yield return new object[] { center1 = new Vector2(2, 4), radius1 = 1, center2 = new Vector2(2, 1), radius2 = 4, expectedInters1 = new Vector2(2, 5), null, true };

            //Test case 7: Circle 2 contains circle 1 (2 intersections)

            yield return new object[] { center1 = new Vector2(2, 4.44), radius1 = 1, center2 = new Vector2(2, 1), radius2 = 4, expectedInters1 = new Vector2(2.8877983962749, 4.9002325581395), new Vector2(1.1122016037251, 4.9002325581395), true };

            //Test case 8: Circles are radius1+radius2 units apart (1 intersection)

            yield return new object[] { center1 = new Vector2(-2, 1), radius1 = 4, center2 = new Vector2(4, 1), radius2 = 2, expectedInters1 = new Vector2(2, 1), null, true };

            //Test case 9: Circles are less than radius1+radius2 units apart (2 intersections)

            yield return new object[] { center1 = new Vector2(-2, 1), radius1 = 4, center2 = new Vector2(4, 1), radius2 = 3, expectedInters1 = new Vector2(1.5833333333333, 2.7775607506418), new Vector2(1.5833333333333, -0.7775607506418), true };

            //Test case 10: Circles are too far apart to intersect

            yield return new object[] { center1 = new Vector2(0, 0), radius1 = 4, center2 = new Vector2(100, 100), radius2 = 1, expectedInters1 = null, expectedInters2 = null, false };




        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class RaycastCircleTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            Vector2 circleCenter = new Vector2(6, 6);
            double circleRadius = 2;
            Vector2? rayOrigin, expectedInters1, expectedInters2;
            double rayDir;

            //Test case 1: Ray misses circle (but is within same half)

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 49.3987053549955, circleCenter, circleRadius, expectedInters1 = null, expectedInters2 = null, false };

            //Test case 2: Ray misses circle (but ray with opposite direction would intersect)

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = -147.9946167919165, circleCenter, circleRadius, expectedInters1 = null, expectedInters2 = null, false };

            //Test case 3: Ray misses but passes very close to tangent1

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 44.25, circleCenter, circleRadius, expectedInters1 = null, expectedInters2 = null, false };

            //Test case 4: Ray misses but passes very close to tangent2

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 19.765, circleCenter, circleRadius, expectedInters1 = null, expectedInters2 = null, false };

            //Test case 5: Ray intersects at tangent1

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 44.24494150586, circleCenter, circleRadius, expectedInters1 = new Vector2(4.6045455665963, 7.4327270934459), expectedInters2 = null, true };

            //Test case 6: Ray intersects at tangent2

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 19.7658249103069, circleCenter, circleRadius, expectedInters1 = new Vector2(6.6763533098082, 4.1178347043069), expectedInters2 = null, true };

            //Test case 7: Ray intersects (twice) within tangent1's hemisphere

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 35, circleCenter, circleRadius, expectedInters1 = new Vector2(4.129530544741, 5.2919434931143), expectedInters2 = new Vector2(7.3050937057939, 7.5154967565446), true };

            //Test case 8: Ray intersects (twice) within tangent2's hemisphere

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 25, circleCenter, circleRadius, expectedInters1 = new Vector2(5.0036416948396, 4.2658517572774), expectedInters2 = new Vector2(7.9688813982476, 5.6485657392418), true };

            //Test case 9: Ray intersects (twice) and passes through circle center

            yield return new object[] { rayOrigin = new Vector2(-2, 1), rayDir = 32.0053832080835, circleCenter, circleRadius, expectedInters1 = new Vector2(4.3040033919898, 4.9400021199936), expectedInters2 = new Vector2(7.6959966080102, 7.0599978800064), true };

            //Test case 10: Ray originates from within circle (but not the center)

            //TODO: Ask Paolo and Sam how this should work

            //Test case 11: Ray originates from same point as circle center


        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    public class CircleTangentsTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            Vector2 circleCenter = new Vector2(1, 0);
            double circleRadius = 1;
            Vector2 point, expectedTangent1, expectedTangent2;
            double expectedAngle;

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(4, 0), expectedTangent1 = new Vector2((double)1.333333, (double)-0.942809), expectedTangent2 = new Vector2((double)1.333333, (double)0.942809), expectedAngle = 19.4712206344907 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(-2, 0), expectedTangent1 = new Vector2((double)0.666667, (double)0.9428090415821), expectedTangent2 = new Vector2((double)0.666667, (double)-0.9428090415821), expectedAngle = 19.4712206344907 };

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(-1, 2), expectedTangent1 = new Vector2((double)1.4114378277661, (double)0.9114378277661), expectedTangent2 = new Vector2((double)0.0885621722339, (double)-0.4114378277661), expectedAngle = 20.7048110546354 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(3, -2), expectedTangent1 = new Vector2((double)0.5885621722339, (double)-0.9114378277661), expectedTangent2 = new Vector2((double)1.9114378277661, (double)0.4114378277661), expectedAngle = 20.7048110546354 };

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(3, 2), expectedTangent1 = new Vector2((double)1.9114378277661, (double)-0.4114378277661), expectedTangent2 = new Vector2((double)0.5885621722339, (double)0.9114378277661), expectedAngle = 20.7048110546354 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(-1, -2), expectedTangent1 = new Vector2((double)0.0885621722339, (double)0.4114378277661), expectedTangent2 = new Vector2((double)1.4114378277661, (double)-0.9114378277661), expectedAngle = 20.70481105463547 };

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(1, 2), expectedTangent1 = new Vector2((double)1.8660254037844, (double)0.5), expectedTangent2 = new Vector2((double)0.1339745962156, (double)0.5), expectedAngle = 30 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(1, -2), expectedTangent1 = new Vector2((double)0.1339745962156, (double)-0.5), expectedTangent2 = new Vector2((double)1.8660254037844, (double)-0.5), expectedAngle = 30 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    public class IPOATestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            //starting arcdir 0; TODO: Add other starting directions.
            //0 degree arc. Should only notice point at new Vector2(4,0)
            Vector2[] points = new Vector2[] { new Vector2(4, 0), new Vector2((double)3.9993907806256, (double)0.0698096257491), new Vector2((double)3.9993907806256, (double)-0.0698096257491), new Vector2((double)3.4641016151378, 2), new Vector2((double)3.4641016151378, -2), new Vector2((double)3.4286692028084, (double)2.0601522996402), new Vector2((double)3.4286692028084, (double)-2.0601522996402), new Vector2(2, (double)3.4641016151378), new Vector2(2, (double)-3.4641016151378), new Vector2((double)1.9392384809853, (double)3.4984788285576), new Vector2((double)1.9392384809853, (double)-3.4984788285576), new Vector2(0, 4), new Vector2(0, -4), new Vector2((double)-0.0698096257491, (double)3.9993907806256), new Vector2((double)-0.0698096257491, (double)-3.9993907806256), new Vector2(-2, (double)3.4641016151378), new Vector2(-2, (double)-3.4641016151378), new Vector2((double)-2.0601522996402, (double)3.4286692028084), new Vector2((double)-2.0601522996402, (double)-3.4286692028084), new Vector2((double)-3.4641016151378, 2), new Vector2((double)-3.4641016151378, -2), new Vector2((double)-3.4984788285576, (double)1.9392384809853), new Vector2((double)-3.4984788285576, (double)-1.9392384809853), new Vector2((double)-3.9993907806256, (double)0.0698096257491), new Vector2((double)-3.9993907806256, (double)-0.0698096257491), new Vector2(-4, 0) };
            bool[] expected = new bool[] { true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

            for (int j = 0; j < points.Length; j++)
            {
                yield return new object[] { points[j], new Vector2(0, 0), 0, 0, 4, expected[j] };
            }

            //60 degree arc. should notice 5 points
            expected = new bool[] { true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

            for (int j = 0; j < points.Length; j++)
            {
                yield return new object[] { points[j], new Vector2(0, 0), 0, 30, 4, expected[j] };
            }

            //120 degree arc. should notice 9 points
            expected = new bool[] { true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

            for (int j = 0; j < points.Length; j++)
            {
                yield return new object[] { points[j], new Vector2(0, 0), 0, 60, 4, expected[j] };
            }

            //180 degree arc. should notice 13 points
            expected = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false };

            for (int j = 0; j < points.Length; j++)
            {
                yield return new object[] { points[j], new Vector2(0, 0), 0, 90, 4, expected[j] };
            }

            //240 degree arc. should notice 17 points
            expected = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false };

            for (int j = 0; j < points.Length; j++)
            {
                yield return new object[] { points[j], new Vector2(0, 0), 0, 120, 4, expected[j] };
            }

            //300 degree arc. should notice 21 points
            expected = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false };

            for (int j = 0; j < points.Length; j++)
            {
                yield return new object[] { points[j], new Vector2(0, 0), 0, 150, 4, expected[j] };
            }

            //360 degree arc. should notice all points
            expected = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };

            for (int j = 0; j < points.Length; j++)
            {
                yield return new object[] { points[j], new Vector2(0, 0), 0, 180, 4, expected[j] };
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}