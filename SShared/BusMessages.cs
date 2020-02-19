using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace SShared.BusMsgs
{

    /// <summary>
    /// A message sent when a scanning or shooting action is requested.
    /// </summary>
    public struct ScanShoot : IBusMessage
    {
        public static ushort Id { get { return 0x0010; } }

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
        /// `energy * scalingFactor` for a shot; Zero for just scanning
        /// </summary>
        public double ScaledEnergy;

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            // NOTE: Id omitted - it gets [de]serialized by the bus
            writer.Put(Origin.X);
            writer.Put(Origin.Y);
            writer.Put(Direction);
            writer.Put(Width);
            writer.Put(ScaledEnergy);
        }

        public void Deserialize(NetDataReader reader)
        {
            // NOTE: Id omitted - it gets [de]serialized by the bus
            Origin.X = reader.GetDouble();
            Origin.Y = reader.GetDouble();
            Direction = reader.GetDouble();
            Width = reader.GetDouble();
            ScaledEnergy = reader.GetDouble();
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
