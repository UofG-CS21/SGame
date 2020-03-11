using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LiteNetLib.Utils;

namespace SShared.Messages
{

    /// <summary>
    /// A message sent when a scanning or shooting action is requested.
    /// </summary>
    public class ScanShoot : IMessage
    {
        /// <summary>
        /// The token of the ship who initiated the scan/shoot operation.
        /// </summary>
        public string Originator;

        /// <summary>
        /// The origin of the scan.
        /// </summary>
        public Vector2 Origin;

        /// <summary>
        /// World-space direction of the center of the scan **in radians**.
        /// </summary>
        public double Direction;

        /// <summary>
        /// Half-width of the scan cone, **in radians**.
        /// </summary>
        public double Width;

        /// <summary>
        /// Radius of the scan cone.
        /// </summary>
        public double Radius;

        /// <summary>
        /// `energy * scalingFactor` for a shot; Zero for just scanning
        /// </summary>
        public double ScaledShotEnergy;

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Originator);
            writer.Put(Origin.X);
            writer.Put(Origin.Y);
            writer.Put(Direction);
            writer.Put(Width);
            writer.Put(Radius);
            writer.Put(ScaledShotEnergy);
        }

        public void Deserialize(NetDataReader reader)
        {
            Originator = reader.GetString();
            Origin.X = reader.GetDouble();
            Origin.Y = reader.GetDouble();
            Direction = reader.GetDouble();
            Width = reader.GetDouble();
            Radius = reader.GetDouble();
            ScaledShotEnergy = reader.GetDouble();
        }
    }

    /// <summary>
    /// A message about a `ScanShoot` operation striking a ship.
    /// </summary>
    public class Struck : IMessage
    {
        /// <summary>
        /// The token of the ship who initiated the scan/shoot operation.
        /// </summary>
        public string Originator { get; set; }

        public class ShipInfo
        {
            /// <summary>
            /// The struck ship.
            /// </summary>
            public Spaceship Ship { get; set; }

            /// <summary>
            /// Area gain for the originator of the shot, if any. If negative, it means the shot was fatal.
            /// </summary>
            public double AreaGain { get; set; }
        }

        /// <summary>
        /// Info on the ships that were struck / scanned.
        /// </summary>
        public List<ShipInfo> ShipsInfo = new List<ShipInfo>();

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Originator);

            writer.Put(ShipsInfo.Count);
            foreach (var info in ShipsInfo)
            {
                info.Ship.Serialize(writer);
                writer.Put(info.AreaGain);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            Originator = reader.GetString(64);

            int infoCount = reader.GetInt();
            ShipsInfo = Enumerable.Range(0, infoCount).Select((i) =>
            {
                Spaceship ship = new Spaceship();
                ship.Deserialize(reader);
                return new ShipInfo()
                {
                    Ship = ship,
                    AreaGain = reader.GetDouble(),
                };
            }).ToList();
        }
    }

    /// <summary>
    /// A message about a ship connecting to the SArbiter.
    /// </summary>
    public class ShipConnected : IMessage
    {
        /// <summary>
        /// Token of the ship.
        /// </summary>
        public string Token = null;

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString();
        }
    }

    /// <summary>
    /// A message about a ship disconnecting from the SArbiter.
    /// </summary>
    public class ShipDisconnected : IMessage
    {
        /// <summary>
        /// Token of the ship.
        /// </summary>
        public string Token = null;

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString();
        }
    }

    /// <summary>
    /// Serialization utilities.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Registers serializers for all bus message types.
        /// </summary>
        public static void RegisterAllSerializers(NetNodePacketProcessor processor)
        {
            // v--- Extra types to be registered ---v
            processor.RegisterNestedType<Spaceship>(() => new Spaceship(null));
            // v--- Register types here ---v
            processor.RegisterNestedType<ScanShoot>(() => new ScanShoot());
            processor.RegisterNestedType<Struck>(() => new Struck());
            processor.RegisterNestedType<ShipConnected>(() => new ShipConnected());
            processor.RegisterNestedType<ShipDisconnected>(() => new ShipDisconnected());
        }
    }
}
