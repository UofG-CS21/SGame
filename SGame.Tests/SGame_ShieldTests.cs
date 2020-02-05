
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
            expectedAngle = Math.Round(Api.Deg2Rad(expectedAngle), 6);

            Api.CircleTangents(circleCenter, circleRadius, point, out tg1, out tg2, out bisectAngle);
            //Round to accout for uncertainty
            double[] actualVectorValues = new double[] { Math.Round(tg1.X, 6), Math.Round(tg1.Y, 6), Math.Round(tg2.X, 6), Math.Round(tg2.Y, 6) };
            double[] expectedVectorValues = new double[] { Math.Round((expectedTangent1).X, 6), Math.Round((expectedTangent1).Y, 6), Math.Round((expectedTangent2).X, 6), Math.Round((expectedTangent2).Y, 6) };
            Assert.Equal(expectedVectorValues, actualVectorValues);
            Assert.Equal(expectedAngle, Math.Round(bisectAngle, 6));
        }
    }

    public class CircleTangentsTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            Vector2 circleCenter = new Vector2(1, 0);
            double circleRadius = 1;
            Vector2 point, expectedTangent1, expectedTangent2;
            double expectedAngle;

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(4, 0), expectedTangent1 = new Vector2((float)1.333333, (float)-0.942809), expectedTangent2 = new Vector2((float)1.333333, (float)0.942809), expectedAngle = 19.4712206344907 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(-2, 0), expectedTangent1 = new Vector2((float)0.666667, (float)0.9428090415821), expectedTangent2 = new Vector2((float)0.666667, (float)-0.9428090415821), expectedAngle = 19.4712206344907 };

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(-1, 2), expectedTangent1 = new Vector2((float)1.4114378277661, (float)0.9114378277661), expectedTangent2 = new Vector2((float)0.0885621722339, (float)-0.4114378277661), expectedAngle = 20.7048110546354 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(3, -2), expectedTangent1 = new Vector2((float)0.5885621722339, (float)-0.9114378277661), expectedTangent2 = new Vector2((float)1.9114378277661, (float)0.4114378277661), expectedAngle = 20.7048110546354 };

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(3, 2), expectedTangent1 = new Vector2((float)1.9114378277661, (float)-0.4114378277661), expectedTangent2 = new Vector2((float)0.5885621722339, (float)0.9114378277661), expectedAngle = 20.7048110546354 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(-1, -2), expectedTangent1 = new Vector2((float)0.0885621722339, (float)0.4114378277661), expectedTangent2 = new Vector2((float)1.4114378277661, (float)-0.9114378277661), expectedAngle = 20.70481105463547 };

            yield return new object[] { circleCenter, circleRadius, point = new Vector2(1, 2), expectedTangent1 = new Vector2((float)1.8660254037844, (float)0.5), expectedTangent2 = new Vector2((float)0.1339745962156, (float)0.5), expectedAngle = 30 };
            yield return new object[] { circleCenter, circleRadius, point = new Vector2(1, -2), expectedTangent1 = new Vector2((float)0.1339745962156, (float)-0.5), expectedTangent2 = new Vector2((float)1.8660254037844, (float)-0.5), expectedAngle = 30 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public class IPOATestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        //starting arcdir 0; TODO: Add other starting directions.
        //0 degree arc. Should only notice point at new Vector2(4,0)
        Vector2[] points = new Vector2[] { new Vector2(4, 0), new Vector2((float)3.9993907806256, (float)0.0698096257491), new Vector2((float)3.9993907806256, (float)-0.0698096257491), new Vector2((float)3.4641016151378, 2), new Vector2((float)3.4641016151378, -2), new Vector2((float)3.4286692028084, (float)2.0601522996402), new Vector2((float)3.4286692028084, (float)-2.0601522996402), new Vector2(2, (float)3.4641016151378), new Vector2(2, (float)-3.4641016151378), new Vector2((float)1.9392384809853, (float)3.4984788285576), new Vector2((float)1.9392384809853, (float)-3.4984788285576), new Vector2(0, 4), new Vector2(0, -4), new Vector2((float)-0.0698096257491, (float)3.9993907806256), new Vector2((float)-0.0698096257491, (float)-3.9993907806256), new Vector2(-2, (float)3.4641016151378), new Vector2(-2, (float)-3.4641016151378), new Vector2((float)-2.0601522996402, (float)3.4286692028084), new Vector2((float)-2.0601522996402, (float)-3.4286692028084), new Vector2((float)-3.4641016151378, 2), new Vector2((float)-3.4641016151378, -2), new Vector2((float)-3.4984788285576, (float)1.9392384809853), new Vector2((float)-3.4984788285576, (float)-1.9392384809853), new Vector2((float)-3.9993907806256, (float)0.0698096257491), new Vector2((float)-3.9993907806256, (float)-0.0698096257491), new Vector2(-4, 0) };
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
