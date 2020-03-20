using System;
using LiteNetLib.Utils;

namespace SShared
{
    /// <summary>
    /// A 2D vector of double-precision doubles.
    /// </summary>
    public struct Vector2 : INetSerializable
    {
        /// <summary>
        /// The coordinates of the vector.
        /// </summary>
        public double X, Y;

        /// <summary>
        /// Initializes the Vector2 given its coordinates.
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public Vector2(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        /// <summary>
        /// Calculates the dot product between two vectors.
        /// </summary>
        public static double Dot(Vector2 vec1, Vector2 vec2) => vec1.X * vec2.X + vec1.Y * vec2.Y;

        /// <summary>
        /// Calculates the squared length of the vector.
        /// </summary>
        public double LengthSquared() => X * X + Y * Y;

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        public double Length() => Math.Sqrt(X * X + Y * Y);

        /// <summary>
        /// Returns a normalized version of this vector.
        /// </summary>
        public Vector2 Normalized() => Vector2.Multiply(this, 1.0 / Length());

        /// <summary>
        /// Calculates the component-wise sum of two vectors.
        /// </summary>
        public static Vector2 operator +(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);

        /// <summary>
        /// Calculates the component-wise sum of two vectors.
        /// </summary>
        public static Vector2 Add(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);

        /// <summary>
        /// Calculates the component-wise difference of two vectors.
        /// </summary>
        public static Vector2 operator -(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);

        /// <summary>
        /// Calculates the component-wise difference of two vectors.
        /// </summary>
        public static Vector2 Subtract(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        public static Vector2 operator *(Vector2 vec, double scalar) => new Vector2(vec.X * scalar, vec.Y * scalar);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        public static Vector2 Multiply(Vector2 vec, double scalar) => new Vector2(vec.X * scalar, vec.Y * scalar);

        /// <summary>
        /// Negates a vector component-wise.
        /// </summary>
        public static Vector2 operator -(Vector2 self) => new Vector2(-self.X, -self.Y);

        /// <summary>
        /// Returns a string representation of a vector. 
        /// </summary>
        public override String ToString() => $"({X:F4}, {Y:F4})";

        // ----- INetSerializable ----------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(X);
            writer.Put(Y);
        }

        public void Deserialize(NetDataReader reader)
        {
            X = reader.GetDouble();
            Y = reader.GetDouble();
        }
    }
}
