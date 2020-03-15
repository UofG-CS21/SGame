using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Linq.Expressions;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SShared.Messages
{
    /// <summary>
    /// A message sent from a node to the arbiter to publish information about itself,
    /// or sent from the arbiter to nodes to let them know when configuration changes for themselves / another node.
    /// </summary>
    public class NodeConfig : IMessage
    {
        /// <summary>
        /// The bounds of the quadtree node managed by the node in question.
        /// </summary>
        public Quad Bounds { get; set; }

        /// <summary>
        /// The path to the node in question from the root node of the tree.
        /// </summary>
        public PathString Path { get; set; }

        /// <summary>
        /// Externally-visible IP address of the `NetNode` for the node in question
        /// - used both for the event bus and the HTTP REST API.
        /// </summary>
        public IPAddress BusAddress { get; set; }

        /// <summary>
        /// Externally-visible HTTP address the SGame REST API is being served on for the node in question.
        /// </summary>
        public string ApiUrl { get; set; }

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Bounds.CentreX); writer.Put(Bounds.CentreY); writer.Put(Bounds.Radius);
            Path.Serialize(writer);
            writer.PutBytesWithLength(BusAddress.GetAddressBytes());
            writer.Put(ApiUrl);
        }

        public void Deserialize(NetDataReader reader)
        {
            Bounds = new Quad(reader.GetDouble(), reader.GetDouble(), reader.GetDouble());
            Path = new PathString();
            Path.Deserialize(reader);
            byte[] ipBytes = reader.GetBytesWithLength();
            BusAddress = new IPAddress(ipBytes);
            ApiUrl = reader.GetString();
        }
    }

    /// <summary>
    /// A message sent to the arbiter when a ship needs to be transferred to its parent node.
    /// </summary>
    public class TransferShip : IMessage
    {

        /// <summary>
        /// The ship being transferred
        /// </summary>
        public Spaceship Ship;

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            Ship.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Ship.Deserialize(reader);
        }
    }

    /// <summary>
    /// A message sent to a node when a ship has been transferred to it.
    /// </summary>
    public class ShipTransferred : IMessage
    {
        /// <summary>
        /// The ship being transferred
        /// </summary>
        public Spaceship Ship;

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {

            Ship.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Ship.Deserialize(reader);
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
            processor.RegisterNestedType<TransferShip>(() => new TransferShip());
            processor.RegisterNestedType<ShipTransferred>(() => new ShipTransferred());
            processor.RegisterNestedType<NodeConfig>(() => new NodeConfig());
        }
    }

    /// <summary>
    /// A sudo call. 
    /// </summary>
    public class Sudo : IMessage
    {
        /// <summary>
        /// JSON payload of the call.
        /// </summary>
        public JObject Json { get; set; }

        // -- INetSerializable -------------------------------------------------

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Json.ToString(Formatting.None));
        }

        public void Deserialize(NetDataReader reader)
        {
            Json = JObject.Parse(reader.GetString());
        }
    }
}
