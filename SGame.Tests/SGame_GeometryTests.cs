
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

    // int pointLineSign(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
    //     {
    //         // Calculate the (not normalized) normal to the line
    //         Vector2 Normal = new Vector2(linePoint2.Y - linePoint1.Y, -(linePoint2.X - linePoint1.X));
    //         // The sign is equal to the sign of the dot product of the normal, and the vector from point1 to the tested point
    //         return System.Math.Sign(Vector2.Dot(Normal, point - linePoint1));
    //     }

    public class SGame_GeometryTests
    {

        [Theory]
        [ClassData(typeof(pLSTestData))]
        public void Test_pointLineSign(Vector2 point, Vector2 linePoint1, Vector2 linePoint2, int expected_sign)
        {
            int actual_sign = new Api().pointLineSign(point, linePoint1, linePoint2);
            //Avoid division by zero errors
            if (actual_sign != 0)
            {
                actual_sign = actual_sign / Math.Abs(actual_sign);
            }
            Assert.Equal(expected_sign, actual_sign);
        }

    }

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
}