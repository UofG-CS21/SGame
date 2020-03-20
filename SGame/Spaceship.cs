using System;
using SShared;
using Newtonsoft.Json.Linq;

namespace SGame
{
    /// <summary>
    /// A spaceship simulated on this SGame node; has more data than a public `Spaceship`.
    /// </summary>
    class LocalSpaceship : Spaceship
    {
        /// <summary>
        /// The game timer used for this spaceship. 
        /// </summary>
        public GameTime GameTime { get; set; }

        /// <summary>
        /// Timestamp of last time the ship was updated.
        /// </summary>
        public double LastUpdate { get; set; }

        /// <summary>
        /// Timestamp of last time the ship was in combat.
        /// </summary>
        public double LastCombat { get; set; }

        /// <summary>
        /// Number of milliseconds between combat actions that reset the kill reward
        ///</summary>
        public const double COMBAT_COOLDOWN = 60 * 1000; // one minute

        public LocalSpaceship(string token, GameTime gameTime)
            : base(token)
        {
            this.GameTime = gameTime;
            this.LastUpdate = gameTime.ElapsedMilliseconds;
            this.LastCombat = this.LastUpdate;
        }

        public LocalSpaceship(Spaceship transferredSpaceship, GameTime gameTime)
        {
            this.Area = transferredSpaceship.Area;
            this.Energy = transferredSpaceship.Energy;
            this.KillReward = transferredSpaceship.KillReward;
            this.Pos = transferredSpaceship.Pos;
            this.ShieldDir = transferredSpaceship.ShieldDir;
            this.ShieldWidth = transferredSpaceship.ShieldWidth;
            this.Token = transferredSpaceship.Token;
            this.Velocity = transferredSpaceship.Velocity;

            this.GameTime = gameTime;
            this.LastUpdate = gameTime.ElapsedMilliseconds;
            this.LastCombat = this.LastUpdate;
        }

        public static LocalSpaceship FromJson(JObject json, GameTime gameTime)
        {
            LocalSpaceship ship = new LocalSpaceship(Spaceship.FromJson(json), gameTime);
            long now = gameTime.ElapsedMilliseconds;
            ship.LastUpdate = now - (long)json["lastUpdateDelta"];
            ship.LastCombat = now - (long)json["lastCombatDelta"];
            return ship;
        }

        public new JObject ToJson()
        {
            JObject json = base.ToJson();
            long now = GameTime.ElapsedMilliseconds;
            json["lastUpdateDelta"] = now - LastUpdate;
            json["lastCombatDelta"] = now - LastCombat;
            return json;
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
    }

}
