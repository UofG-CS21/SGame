using System.Numerics;
using System.Diagnostics;
using System;
using System.IO;

namespace SGame
{

    /// <summary>
    /// Class representing a single spaceship on the server side.
    /// </summary>
    class Spaceship
    {
        /// <summary>
        /// Energy of the spaceship.
        /// </summary>
        public double Energy { get; set; }

        /// <summary>
        /// Area of the spaceship.
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// Position of the spaceship.
        /// </summary>
        public Vector2 Pos { get; set; }

        /// <summary>
        /// Velocity of the spaceship.
        /// </summary>
        public Vector2 Velocity { get; set; }

        /// <summary>
        /// ID of the spaceship (only assigned at creation).
        /// </summary>
        public int Id { get; }

        public Stopwatch gameTime { get; set; }

        /// <summary>
        /// Timestamp of last time the ship's state was updated
        /// </summary>
        public double LastUpdate { get; set; }

        public Spaceship(int id, Stopwatch gameTime)
        {
            this.gameTime = gameTime;
            this.Id = id;
            this.Area = 1;
            this.Energy = 10.0;
            this.Pos = new Vector2(0, 0);
            this.Velocity = new Vector2(0, 0);
            this.LastUpdate = gameTime.ElapsedMilliseconds;
        }

        public void UpdateState()
        {
            long time = gameTime.ElapsedMilliseconds;
            double elapsedSeconds = (double)(time - LastUpdate) / 1000;
            Pos += Vector2.Multiply(Velocity, (float)elapsedSeconds);
            Energy = Math.Min(Area * 10, Energy + elapsedSeconds * Area);
            LastUpdate = time;
        }

        public double Radius()
        {
            return System.Math.Sqrt(Area / System.Math.PI);
        }
    }

}