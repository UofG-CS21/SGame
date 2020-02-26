using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SShared
{
    /// <summary>
    /// Implemented by all bus messages.
    /// </summary>
    public interface IMessage : INetSerializable
    {
        /// <summary>
        /// A identifier code for this type of message.
        /// </summary>
        public static ushort Id { get; }
    }

    /// <summary>
    /// Arguments to an event involving a bus message. 
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(NetPeer sender, IMessage message)
        {
            Sender = sender;
            Message = message;
        }

        public NetPeer Sender { get; private set; }

        public IMessage Message { get; private set; }
    };

    /// <summary>
    /// A node on the message bus system.
    /// </summary>
    public class NetNode : INetEventListener, IDisposable
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
        /// True if this node is a server node, false if it's a client.
        /// </summary>
        public bool IsServer { get; private set; }

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
        public NetNode(string hostname, int port)
        {
            Host = new NetManager(this);
            if (hostname != null)
            {
                IsServer = false;
                Host.Start();
                Host.Connect(hostname, port, Secret);
            }
            else
            {
                IsServer = true;
                Host.Start(port);
                //_netHost.BroadcastReceiveEnable = true;
            }

            _writer = new NetDataWriter();

            Serializer = new NetSerializer();
            Messages.Serialization.RegisterAllSerializers(Serializer);
        }

        /// <summary>
        /// Poll events and update other internal state.
        /// </summary>
        public void Update()
        {
            Host.PollEvents();
        }

        /// <summary>
        /// An event that is triggered when a message is received by this node.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessageReceived;

        /// <summary>
        /// Sends a bus message to every peer connected to this node.
        /// </summary>
        public void BroadcastMessage<T>(T message, DeliveryMethod delivery = DeliveryMethod.ReliableUnordered)
            where T : class, IMessage, new()
        {
            _writer.Reset();
            Serializer.Serialize<T>(_writer, message);
            Host.SendToAll(_writer, delivery);
        }

        /// <summary>
        /// Sends a bus message to a particular peer.
        /// </summary>
        public void SendMessage<T>(T message, NetPeer peer, DeliveryMethod delivery = DeliveryMethod.ReliableUnordered)
            where T : class, IMessage, new()
        {
            _writer.Reset();
            Serializer.Serialize<T>(_writer, message);
            peer.Send(_writer, delivery);
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
                if (Messages.Serialization.MessageTypes.TryGetValue(msgId, out msgType))
                {
                    // Expand the appropriate `NetSerializer.Deserialize<TMessage>()` and call it to deserialize the message
                    // See: https://stackoverflow.com/a/3958029
                    var typelessDeserialize = typeof(NetSerializer).GetMethod("Deserialize");
                    var deserialize = typelessDeserialize.MakeGenericMethod(msgType);
                    var message = (IMessage)deserialize.Invoke(Serializer, new object[] { reader });
                    // Dispatch the event
                    OnMessageReceived.Invoke(this, new MessageEventArgs(peer, message));
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

    /// <summary>
    /// Waits for a message of type T from a NetNode.
    /// </summary>
    public class MessageWaiter<T> where T : class, IMessage
    {
        TaskCompletionSource<T> _completionSrc;

        /// <summary>
        /// Starts waiting for a `T` message coming from `node`.
        /// If `peer` is not null, ensures that the sender is `peer`.
        /// If `filter` is not null, ensures that the message passes the given filter.
        /// </summary>
        public MessageWaiter(NetNode node, NetPeer peer = null, Predicate<T> filter = null)
        {
            Node = node;
            Peer = peer;
            _completionSrc = new TaskCompletionSource<T>();
            Node.OnMessageReceived += OnMessageReceived;
        }

        internal void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (Peer == null || e.Sender == Peer)
            {
                var tMsg = e.Message as T;
                if (tMsg != null && (Filter == null || Filter(tMsg)))
                {
                    Node.OnMessageReceived -= OnMessageReceived;
                    _completionSrc.SetResult(tMsg);
                }
            }
        }
        ~MessageWaiter()
        {
            Node.OnMessageReceived -= OnMessageReceived;
        }

        public NetNode Node { get; private set; }

        public NetPeer Peer { get; private set; }

        public Predicate<T> Filter { get; private set; }

        public Task<T> Wait
        {
            get { return _completionSrc.Task; }
        }
    }
}
