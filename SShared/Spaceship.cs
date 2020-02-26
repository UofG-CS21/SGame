using System;
using LiteNetLib.Utils;

namespace SShared
{
    /// <summary>
    /// Represents a spaceship (or at least what is publicly known about it).
    /// </summary>
    public class Spaceship : IQuadBounded, INetSerializable
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

        public Spaceship(string token = null)
        {
            this.Token = token;

            this.Area = 1;
            this.Pos = new Vector2(0, 0);
            this.Energy = 10.0;

            this.KillReward = this.Area;
            this.ShieldDir = 0.0;
            this.ShieldWidth = 0.0;
        }

        /// <summary>
        /// Calculates energy used per second by a shield of width shieldWidth for a ship with given area
        /// </summary>
        protected static double ShieldEnergyUsage(double shieldWidth, double area)
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

        public double Radius()
        {
            return System.Math.Sqrt(Area / System.Math.PI);
        }

        // ===== INetSerializable ==============================================

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token);
            writer.Put(Energy);
            writer.Put(Area);
            writer.Put(Pos.X);
            writer.Put(Pos.Y);
            writer.Put(ShieldDir);
            writer.Put(ShieldWidth);
            writer.Put(KillReward);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString();
            Energy = reader.GetDouble();
            Area = reader.GetDouble();
            Pos = new Vector2(reader.GetDouble(), reader.GetDouble());
            ShieldDir = reader.GetDouble();
            ShieldWidth = reader.GetDouble();
            KillReward = reader.GetDouble();
        }
    }
}
