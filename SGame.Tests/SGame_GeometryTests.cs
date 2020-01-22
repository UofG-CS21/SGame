
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Collections;
using System.Numerics;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SGame;
using Xunit;



namespace SGame.Tests
{


    public class SGame_GeometryTests
    {

        [Theory]
        [ClassData(typeof(pLSTestData))]
        public void Test_pointLineSign(Vector2 point, Vector2 triPoint1, Vector2 triPoint2, Vector2 triPoint3, bool expected)
        {
            int point_sign_1 = new Api().pointLineSign(point, triPoint1, triPoint2);
            int point_sign_2 = new Api().pointLineSign(point, triPoint2, triPoint3);
            int point_sign_3 = new Api().pointLineSign(point, triPoint3, triPoint1);
            //Avoid division by zero errors and normalise.
            if ((point_sign_1 >= 0 && point_sign_2 >= 0 && point_sign_3 >= 0) || (point_sign_1 <= 0 && point_sign_2 <= 0 && point_sign_3 <= 0))
            {
                Assert.Equal(expected, true);
            }
            else
            {
                Assert.Equal(expected, false);
            }
        }

        [Theory]
        [ClassData(typeof(CTSITestData))]
        public void CircleTriangleSideIntersection(Vector2 circleCenter, double radius, Vector2 linePoint1, Vector2 linePoint2, bool expected)
        {
            bool actual = new Api().CircleTriangleSideIntersection(circleCenter, radius, linePoint1, linePoint2);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [ClassData(typeof(CTITestData))]
        public void CircleTriangleIntersection(Vector2 circleCenter, double radius, Vector2 A, Vector2 B, Vector2 C, bool expected)
        {
            bool actual = new Api().CircleTriangleIntersection(circleCenter, radius, A, B, C);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [ClassData(typeof(CSITestData))]
        public void CircleSegmentIntersection(Vector2 circleCenter, float circleRadius, Vector2 segmentCenter, float segmentRadius, float segmentAngle, float segmentWidth, bool expected)
        {
            bool actual = new Api().CircleSegmentIntersection(circleCenter, circleRadius, segmentCenter, segmentRadius, segmentAngle, segmentWidth);
            Assert.Equal(expected, actual);
        }


    }

    public class CSITestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            //distance between centres is greater then the sum of the two circles radii
            yield return new object[] { new Vector2(0, 0), 2, new Vector2(-10, 0), 1, Math.PI, Math.PI / 2, false };

        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    //Everything after this is test data. Not sure if this is the cleanest way of storing it.
    public class pLSTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            //Point within triangle
            yield return new object[] { new Vector2(10, 0), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), true };



            //Point outside adjacent to line: 
            //tripoint1 to tripoint2
            yield return new object[] { new Vector2(-10, 0), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), false };

            //tripoint2 to tripoint3
            yield return new object[] { new Vector2(7, 4), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), false };

            //tripoint1 to tripoint3
            yield return new object[] { new Vector2(7, -4), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), false };



            //point on line, but within triangle still:
            //tripoint1 to tripoint2
            yield return new object[] { new Vector2(0, 0), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), true };

            //tripoint2 to tripoint3
            yield return new object[] { new Vector2(6, 3), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), true };

            //tripoint1 to tripoint3
            yield return new object[] { new Vector2(6, -3), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), true };



            //point on line, but outwith triangle :
            //tripoint1 to tripoint2,
            yield return new object[] { new Vector2(0, 10), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), false };

            //tripoint2 to tripoint3
            yield return new object[] { new Vector2(18, -1), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), false };

            //tripoint1 to tripoint3
            yield return new object[] { new Vector2(18, 1), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), false };


            //Point is same as tripoint:
            //point is same as tripoint1
            yield return new object[] { new Vector2(0, -5), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), true };

            //point is same as tripoint2
            yield return new object[] { new Vector2(0, 5), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), true };

            //point is same as tripoint3
            yield return new object[] { new Vector2(15, 0), new Vector2(0, -5), new Vector2(0, 5), new Vector2(15, 0), true };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class CTSITestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            //1 intersection
            yield return new object[] { new Vector2(-5, -6), 2, new Vector2(-13, -8), new Vector2(3, -8), true };
            yield return new object[] { new Vector2(-5, -6), 2, new Vector2(-13, -8), new Vector2(-5, 0), false };
            yield return new object[] { new Vector2(-5, -6), 2, new Vector2(-5, 0), new Vector2(3, -8), false };

            //2 intersections
            yield return new object[] { new Vector2(-7, -2), 2, new Vector2(-13, -6), new Vector2(-1, -6), false };
            yield return new object[] { new Vector2(-7, -2), 2, new Vector2(-7, 0), new Vector2(-1, -6), true };
            yield return new object[] { new Vector2(-7, -2), 2, new Vector2(-13, -6), new Vector2(-7, 0), true };

            //Triangle is completely within circle.
            yield return new object[] { new Vector2(-3, -2), 4, new Vector2(-6, -3), new Vector2(0, -3), true };
            yield return new object[] { new Vector2(-3, -2), 4, new Vector2(-3, 0), new Vector2(0, -3), true };
            yield return new object[] { new Vector2(-3, -2), 4, new Vector2(-6, -3), new Vector2(-3, 0), true };

            //Triangle is completly outwith circle.
            yield return new object[] { new Vector2(5, -4), 4, new Vector2(-6, -3), new Vector2(0, -3), false };
            yield return new object[] { new Vector2(5, -4), 4, new Vector2(-3, 0), new Vector2(0, -3), false };
            yield return new object[] { new Vector2(5, -4), 4, new Vector2(-6, -3), new Vector2(-3, 0), false };

            //Circle is completely within triangle.
            yield return new object[] { new Vector2(-3, -2), 1, new Vector2(-7, -4), new Vector2(1, -4), false };
            yield return new object[] { new Vector2(-3, -2), 1, new Vector2(-3, 0), new Vector2(1, -4), false };
            yield return new object[] { new Vector2(-3, -2), 1, new Vector2(-7, -4), new Vector2(-3, 0), false };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class CTITestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            //1 intersection
            yield return new object[] { new Vector2(-5, -6), 2, new Vector2(-13, -8), new Vector2(-5, 0), new Vector2(3, -8), true };

            //2 intersections
            yield return new object[] { new Vector2(-7, -2), 2, new Vector2(-13, -6), new Vector2(-7, 0), new Vector2(-1, -6), true };

            //Triangle is completely within circle.
            yield return new object[] { new Vector2(-3, -2), 4, new Vector2(-6, -3), new Vector2(-3, 0), new Vector2(0, -3), true };

            //Triangle is completly outwith circle.
            yield return new object[] { new Vector2(5, -4), 4, new Vector2(-6, -3), new Vector2(-3, 0), new Vector2(0, -3), false };

            //Circle is completely within triangle.
            yield return new object[] { new Vector2(-3, -2), 1, new Vector2(-7, -4), new Vector2(-3, 0), new Vector2(1, -4), true };

        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}