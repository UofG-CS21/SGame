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
        /// Makes a direction vector out of an angle in radians.
        /// </summary>
        public static Vector2 DirVec(double direction)
        {
            return new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction));
        }
    }
}
