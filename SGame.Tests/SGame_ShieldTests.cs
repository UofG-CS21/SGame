
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



    }

    //First set of tests: tests a variety of points which sit on the circle of which the arc is a section. width of arc increases from 0 to 360 as tests progress 
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
}