using System;
using System.Numerics;

namespace SGame
{
    /// <summary>
    /// Miscellaneous mathematical and geometrical utilities. 
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Normalizes an angle in radians, i.e. makes it positive and between 0 and `clampValue`.
        /// </summary>
        public static double NormalizeAngle(double angle, double clampValue = 2.0 * Math.PI)
        {
            angle = angle % clampValue;
            if (angle < 0.0) angle = (2.0 * Math.PI) - angle;
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
        /// Improved version of Math.Atan2. Gives actual result based on unit circle.
        /// </summary>
        public static double BetterArcTan(double y, double x)
        {
            double result;
            if (x == 0)
            {
                if (y > 0)
                {
                    result = Math.PI / 2;
                }
                else if (y < 0)
                {
                    result = -Math.PI / 2;
                }
                else
                {
                    result = 0;
                }
                return result;
            }
            result = Math.Atan2(y, x);
            if (x < 0)
            {
                if (result >= 0 && result < Math.PI / 2)
                {
                    result = result - Math.PI;
                }
                else if (result < 0 && result > -Math.PI / 2)
                {
                    result = result + Math.PI;
                }
            }

            if (y == 0)
            {
                result = Api.Deg2Rad((x >= 0 ? 0 : 180));
            }






            return result;
        }



        /// <summary>
        /// Makes a direction vector out of an angle in radians.
        /// </summary>
        public static Vector2 DirVec(double direction)
        {
            return new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction));
        }
    }
}
