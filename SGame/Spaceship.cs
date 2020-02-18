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
                _shieldDir = MathUtils.NormalizeAngle(MathUtils.ClampAngle(value, 2.0 * Math.PI));
            }
        }

        private double _shieldWidth;

        /// <summary>
        /// The shield half extents, in radians (see `ShieldDir`'s documentation).
        /// Automatically clamped to 0..pi when setting it.
        /// </summary>
        public double ShieldWidth
        {
            get
            {
                return _shieldWidth;
            }
            set
            {
                _shieldWidth = MathUtils.ClampAngle(value, Math.PI);
            }
        }
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
            this.ShieldDir = 0.0;
            this.ShieldWidth = 0.0;
        }

        public void UpdateState()
        {
            long time = GameTime.ElapsedMilliseconds;
            double elapsedSeconds = (double)(time - LastUpdate) / 1000;
            Pos += Vector2.Multiply(Velocity, (double)elapsedSeconds);
            Energy = Math.Min(Area * 10, Energy + elapsedSeconds * Area);
            LastUpdate = time;
            if (this.LastUpdate - this.LastCombat > COMBAT_COOLDOWN)
            {
                // Only reset kill reward after cooldown has expired 
                this.KillReward = this.Area;
            }
            else this.KillReward = System.Math.Max(this.KillReward, this.Area);
        }

        public double Radius()
        {
            return System.Math.Sqrt(Area / System.Math.PI);
        }
    }

}
