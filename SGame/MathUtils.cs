using System.Math;

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
    }
}