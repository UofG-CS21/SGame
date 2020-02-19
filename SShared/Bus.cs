using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SShared
{
    /// <summary>
    /// Implemented by all bus messages.
    /// </summary>
    public interface IBusMessage : INetSerializable
    {
        /// <summary>
        /// A identifier code for this type of message.
        /// </summary>
        public static ushort Id { get; }
    }

    /// <summary>
    /// Arguments to an event involving a bus message. 
    /// </summary>
    public class BusMessageEventArgs : EventArgs
    {
        public BusMessageEventArgs(IBusMessage message)
        {
            Message = message;
        }

        public IBusMessage Message { get; private set; }
    };

    /// <summary>
    /// A node on the event bus.
    /// </summary>
    public class BusNode : INetEventListener, IDisposable
    {
        /// <summary>
        /// Key used to authenticate clients on the bus.
        /// </summary>
        public const string Secret = "HZWtdjPJZfJ+Oiu+i0GguGWYuRH9HWLVq2DzwQ276a4=";

        /// <summary>
        /// The underlying NetManager from LiteNetLib.
        /// </summary>
        public NetManager Host { get; private set; }

        /// <summary>
        /// True if this node is a bus master, false if it's just a normal (client) node.
        /// </summary>
        public bool IsMaster { get; private set; }

        NetDataWriter _writer;

        /// <summary>
        /// A serializer that can be used to serialize / deserialize any `BusMsgs.*`.
        /// </summary>
        public NetSerializer Serializer { get; private set; }

        /// <summary>
        /// Initializes a bus node.
        /// </summary>
        /// <param name="hostname">If not null, connect to the given hostname/address; if null, start a server node.</param>
        /// <param name="port">The port to connect to (or bind to for server nodes).</param>
        public BusNode(string hostname, int port)
        {
            Host = new NetManager(this);
            if (hostname != null)
            {
                IsMaster = false;
                Host.Start();
                Host.Connect(hostname, port, Secret);
            }
            else
            {
                IsMaster = true;
                Host.Start(port);
                //_netHost.BroadcastReceiveEnable = true;
            }

            _writer = new NetDataWriter();

            Serializer = new NetSerializer();
            BusMsgs.Serialization.RegisterAllSerializers(Serializer);
        }

        /// <summary>
        /// Poll events and update other internal state.
        /// </summary>
        public void Update()
        {
            Host.PollEvents();
        }

        /// <summary>
        /// An event that is triggered when a message is received from the bus.
        /// </summary>
        public event EventHandler<BusMessageEventArgs> OnBusMessageReceived;

        /// <summary>
        /// Sends a bus message to every peer connected to this node.
        /// </summary>
        public void SendBusMessage<T>(T message, DeliveryMethod delivery = DeliveryMethod.ReliableUnordered)
            where T : class, IBusMessage, new()
        {
            _writer.Reset();
            Serializer.Serialize<T>(_writer, message);
            Host.SendToAll(_writer, delivery);
        }

        // ===== Dispose pattern =======================================================================================

        protected virtual void Dispose(bool disposing)
        {
            if (Host.IsRunning && disposing)
            {
                Host.Stop();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // ===== LiteNetLib event handlers =============================================================================

        public void OnConnectionRequest(ConnectionRequest request)
        {
            Console.WriteLine($"Bus: connecting {request.RemoteEndPoint}");
            request.AcceptIfKey(Secret);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Bus: {peer.EndPoint} connected");
            // TODO IMPLEMENT: Add peer to routing table and notify the other peers of this
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"Bus: {peer.EndPoint} disconnected ({disconnectInfo})");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.Error.WriteLine($"Bus: {endPoint} error: {socketError}");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            // Read the packet and deserialize a bus message from it...
            ushort msgId;
            if (reader.TryGetUShort(out msgId))
            {
                Type msgType;
                if (BusMsgs.Serialization.MessageTypes.TryGetValue(msgId, out msgType))
                {
                    // Expand the appropriate `NetSerializer.Deserialize<TMessage>()` and call it to deserialize the message
                    // See: https://stackoverflow.com/a/3958029
                    var typelessDeserialize = typeof(NetSerializer).GetMethod("Deserialize");
                    var deserialize = typelessDeserialize.MakeGenericMethod(msgType);
                    var message = (IBusMessage)deserialize.Invoke(Serializer, new object[] { reader });
                    // Dispatch the event
                    OnBusMessageReceived.Invoke(this, new BusMessageEventArgs(message));
                }
                // else: unknown message type, ignore
            }
            // else: broken message, ignore

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // TODO IMPLEMENT: Do something when a message is received from a not-connected peer (?)
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // TODO: Log the new ping for the peer?
        }
    }
}
