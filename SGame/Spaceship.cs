using System.Numerics;
using System;

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
        /// The game timer used for this spaceship. 
        /// </summary>
        public GameTime GameTime { get; set; }

        /// Max hit points of the spaceship.
        /// </summary>
        public double LastUpdate { get; set; }

        /// <summary>
        /// Timestamp of last time the ship was in combat
        /// </summary>
        public double LastCombat { get; set; }

        /// <summary>
        /// Reward received by opponent for killing this ship
        /// </summary>
        public double KillReward { get; set; }

        /// <summary>
        /// Number of milliseconds between combat actions that reset the kill reward
        ///</summary>
        public const double COMBAT_COOLDOWN = 60 * 1000; // one minute

        public Spaceship(int id, GameTime gameTime)
        {
            this.GameTime = gameTime;
            this.Id = id;
            this.Area = 1;
            this.Energy = 10.0;
            this.Pos = new Vector2(0, 0);
            this.Velocity = new Vector2(0, 0);
            this.LastUpdate = gameTime.ElapsedMilliseconds;
            this.LastCombat = this.LastUpdate;
            this.KillReward = this.Area;
        }

        public void UpdateState()
        {
            long time = GameTime.ElapsedMilliseconds;
            double elapsedSeconds = (double)(time - LastUpdate) / 1000;
            Pos += Vector2.Multiply(Velocity, (float)elapsedSeconds);
            Energy = Math.Min(Area * 10, Energy + elapsedSeconds * Area);
            LastUpdate = time;
            if (this.LastUpdate - this.LastCombat > COMBAT_COOLDOWN)
                this.KillReward = this.Area;
        }

        public double Radius()
        {
            return System.Math.Sqrt(Area / System.Math.PI);
        }
    }

}
