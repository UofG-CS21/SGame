using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;

namespace SShared.Messages
{

    /// <summary>
    /// A message sent when a scanning or shooting action is requested.
    /// </summary>
    public class ScanShoot : IMessage
    {
        public static ushort Id { get { return 0x0010; } }

        /// <summary>
        /// The token of the ship who initiated the scan/shoot operation.
        /// </summary>
        public string Originator;

        /// <summary>
        /// The origin of the scan.
        /// </summary>
        public Vector2 Origin;

        /// <summary>
        /// World-space direction of the center of the scan.
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
        public double ScaledEnergy;

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            // NOTE: Id omitted - it gets [de]serialized by the bus
            writer.Put(Originator);
            writer.Put(Origin.X);
            writer.Put(Origin.Y);
            writer.Put(Direction);
            writer.Put(Width);
            writer.Put(Radius);
            writer.Put(ScaledEnergy);
        }

        public void Deserialize(NetDataReader reader)
        {
            // NOTE: Id omitted - it gets [de]serialized by the bus
            Originator = reader.GetString();
            Origin.X = reader.GetDouble();
            Origin.Y = reader.GetDouble();
            Direction = reader.GetDouble();
            Width = reader.GetDouble();
            Radius = reader.GetDouble();
            ScaledEnergy = reader.GetDouble();
        }
    }

    /// <summary>
    /// A message about a `ScanShoot` operation striking a ship.
    /// </summary>
    public class Struck : IMessage
    {
        public static ushort Id { get { return 0x0002; } }

        /// <summary>
        /// The token of the ship who initiated the scan/shoot operation.
        /// </summary>
        public string Originator = null;

        public struct ShipInfo
        {
            /// <summary>
            /// The struck ship.
            /// </summary>
            public Spaceship Ship;

            /// <summary>
            /// Area gain for the originator of the shot, if any. If negative, it means the shot was fatal.
            /// </summary>
            public double AreaGain;
        }

        /// <summary>
        /// Info on the ships that were struck / scanned.
        /// </summary>
        public List<ShipInfo> ShipsInfo = new List<ShipInfo>();

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            // NOTE: Id omitted - it gets [de]serialized by the bus
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
            // NOTE: Id omitted - it gets [de]serialized by the bus
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
    /// Serialization utilities.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Maps message type Ids to the underlying type.
        /// </summary>
        static Dictionary<ushort, Type> _msgTypes = new Dictionary<ushort, Type>();

        static Serialization()
        {
            // v--- Register all message types here ---v
            _msgTypes[ScanShoot.Id] = typeof(ScanShoot);
            _msgTypes[Struck.Id] = typeof(Struck);
        }

        /// <summary>
        /// A table of (message type id -> underlying Type) for all known bus message types.
        /// </summary>
        public static Dictionary<ushort, Type> MessageTypes { get { return _msgTypes; } }

        /// <summary>
        /// Registers serializers for all bus message types.
        /// </summary>
        public static void RegisterAllSerializers(NetSerializer serializer)
        {
            // v--- Extra types to be registered ---v
            serializer.RegisterNestedType<Spaceship>(() => new Spaceship(null));

            foreach (var (id, type) in _msgTypes)
            {
                // See: https://stackoverflow.com/a/3958029
                var typelessRegistrar = typeof(NetSerializer).GetMethod("RegisterNestedType");
                var registrar = typelessRegistrar.MakeGenericMethod(type);
                registrar.Invoke(serializer, new object[] { });
            }
        }
    }
}
