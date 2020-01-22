
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

        // [Theory]
        // [ClassData(typeof(pLSTestData))]
        // public void Test_pointLineSign(Vector2 point, Vector2 linePoint1, Vector2 linePoint2, int expected_sign)
        // {
        //     int actual_sign = new Api().pointLineSign(point, linePoint1, linePoint2);
        //     //Avoid division by zero errors
        //     if (actual_sign != 0)
        //     {
        //         actual_sign = actual_sign / Math.Abs(actual_sign);
        //     }
        //     Assert.Equal(expected_sign, actual_sign);
        // }

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
        [ClassData(typeof(CTITestData))]
        public void CircleSegmentIntersection(Vector2 circleCenter, float circleRadius, Vector2 segmentCenter, float segmentRadius, float segmentAngle, float segmentWidth, bool expected)
        {
            bool actual = new Api().CircleSegmentIntersection(circleCenter, circleRadius, segmentCenter, segmentRadius, segmentAngle, segmentWidth);
            Assert.Equal(expected, actual);
        }


    }


    //Everything after this is test data. Not sure if this is the cleanest way of storing it.
    public class pLSTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new Vector2(10, 0), new Vector2(0, -5), new Vector2(0, 5), 1 };
            yield return new object[] { new Vector2(-10, 0), new Vector2(0, -5), new Vector2(0, 5), -1 };
            yield return new object[] { new Vector2(0, 10), new Vector2(-5, 0), new Vector2(5, 0), 1 };
            yield return new object[] { new Vector2(0, -10), new Vector2(-5, 0), new Vector2(5, 0), -1 };
            yield return new object[] { new Vector2(0, 0), new Vector2(-5, 0), new Vector2(5, 0), 0 };
            yield return new object[] { new Vector2(12, 15), new Vector2(-4, 16), new Vector2(8, 12), 1 };
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

    public class CSITestData : IEnumerable<object[]>
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