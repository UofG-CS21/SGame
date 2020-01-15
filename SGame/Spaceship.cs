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

        /// <summary>
        /// Max hit points of the spaceship.
        /// </summary>
        public int MaxHitPoints { get; set; }

        /// <summary>
        /// Hit points of the spaceship. Nothing happens when this hits zero atm.
        /// </summary>
        public int HitPoints { get; set; }

        /// <summary>
        /// Normalizes an angle in radians, i.e. makes it positive and between 0 and 2pi.
        /// </summary>
        private static double NormalizeAngle(double angle)
        {
            angle = angle % (2.0 * Math.PI);
            if (angle < 0.0) angle = (2.0 * Math.PI) - angle;
            return angle;
        }

        private double _shieldDir;

        /// <summary>
        /// Direction towards which to shield, in radians relative to world coordinates.
        /// The shield extends for `ShieldWidth` radians clockwise + `ShieldWidth` radians counterclockwise.
        /// Automatically clamped to 0..2pi when setting it.
        /// </summary>
        public double ShieldDir
        {
            get
            {
                return _shieldDir;
            }
            set
            {
                _shieldDir = NormalizeAngle(value);
            }
        }

        private double _shieldWidth;

        /// <summary>
        /// The shield half extents, in radians (see `ShieldDir`'s documentation).
        /// Automatically clamped to 0..2pi when setting it.
        /// </summary>
        public double ShieldWidth
        {
            get
            {
                return _shieldWidth;
            }
            set
            {
                _shieldWidth = NormalizeAngle(value);
            }
        }

        /// <summary>
        /// Used to keep track of time deltas.
        /// </summary>
        public Stopwatch GameTime { get; set; }

        /// <summary>
        /// Timestamp of last time the ship's state was updated
        /// </summary>
        public double LastUpdate { get; set; }

        public Spaceship(int id, Stopwatch gameTime)
        {
            this.GameTime = gameTime;
            this.Id = id;
            this.Area = 1;
            this.Energy = 10.0;
            this.Pos = new Vector2(0, 0);
            this.Velocity = new Vector2(0, 0);
            this.LastUpdate = gameTime.ElapsedMilliseconds;
            this.MaxHitPoints = (int)(100 + (this.Area * this.Area * 0.2) + (this.Area));
            this.HitPoints = this.MaxHitPoints;
            this.ShieldDir = 0.0;
            this.ShieldWidth = 0.0;
        }

        public void UpdateState()
        {
            long time = GameTime.ElapsedMilliseconds;
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
