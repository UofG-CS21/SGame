using System;
using SShared;

namespace SGame
{

    /// <summary>
    /// Class representing a single spaceship on the server side.
    /// </summary>
    class Spaceship : IQuadBounded
    {
        /// <summary>
        /// Token (= private key) of this spaceship.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Public id (= public key) of this spaceship. Extracted from `Token`.
        /// </summary>
        public string PublicId
        {
            get { return Token.Substring(Token.Length - 8); }
        }

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
        /// Is the spaceship marked as dead?
        /// </summary>
        public bool Dead { get; set; }

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
        /// WARNING: NOT automatically adjusted in the 0..pi range!
        /// </summary>
        public double ShieldWidth
        {
            get
            {
                return _shieldWidth;
            }
            set
            {
                _shieldWidth = value;
            }
        }
        /// <summary>
        /// Reward received by opponent for killing this ship
        /// </summary>
        public double KillReward { get; set; }

        /// <summary>
        /// The current bounds of the ship.
        /// </summary>
        public Quad Bounds => new Quad(Pos.X, Pos.Y, Radius());

        /// <summary>
        /// Number of milliseconds between combat actions that reset the kill reward
        ///</summary>
        public const double COMBAT_COOLDOWN = 60 * 1000; // one minute

        public Spaceship(string token, GameTime gameTime)
        {
            this.Token = token;
            this.GameTime = gameTime;
            this.Dead = false;
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

        /// <summary>
        /// Calculates energy used per second by a shield of width shieldWidth for a ship with given area
        /// </summary>
        private static double ShieldEnergyUsage(double shieldWidth, double area)
        {
            // shields should usually be turned off for most ships, so this will save cpu time overall
            if (shieldWidth == 0) return 0;

            // this satisfies the following:
            // 0. No shield -> No energy usage
            // 1. Shielding half of yourself means you stay energy-neutral
            // 2. You can shield yourself fully for as many seconds as your area
            // 3. Monotonic and continuous inbetween

            if (shieldWidth * 2 <= Math.PI)
                return area * (shieldWidth * 2) / Math.PI;
            else
                return area + 10 * (shieldWidth * 2 - Math.PI) / Math.PI;
        }

        public void UpdateState()
        {
            long time = GameTime.ElapsedMilliseconds;
            double elapsedSeconds = (double)(time - LastUpdate) / 1000;
            Pos += Vector2.Multiply(Velocity, (double)elapsedSeconds);

            double energyGain = Area;
            double shieldUsedEnergy = ShieldEnergyUsage(this.ShieldWidth, this.Area);

            // calculate how many seconds it would take us to use up all energy due to shield

            double timeToNoEnergy = double.MaxValue;

            if (shieldUsedEnergy > energyGain)
            {
                timeToNoEnergy = (Energy / (shieldUsedEnergy - energyGain));
            }

            // if the ship would have run out of energy, turn the shield off, set energy to 0,
            // and then allow energy to recover for the remaining time
            if (elapsedSeconds >= timeToNoEnergy)
            {
                ShieldWidth = 0;
                Energy = (elapsedSeconds - timeToNoEnergy) * energyGain;
            }
            else Energy += (energyGain - shieldUsedEnergy) * elapsedSeconds;

            Energy = Math.Min(Area * 10, Energy);

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
