using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
    /// A node on the message bus system.
    /// </summary>
    public class NetNode : EventBasedNetListener, IDisposable
    {
        /// <summary>
        /// Key used to authenticate clients on the bus.
        /// </summary>
        public const string Secret = "HZWtdjPJZfJ+Oiu+i0GguGWYuRH9HWLVq2DzwQ276a4=";

        /// <summary>
        /// The underlying NetManager from LiteNetLib.
        /// </summary>
        public NetManager Host { get; private set; }

        NetDataWriter _writer;

        /// <summary>
        /// A serializer that can be used to serialize / deserialize any `BusMsgs.*`.
        /// </summary>
        public NetNodePacketProcessor PacketProcessor { get; private set; }

        /// <summary>
        /// Initializes a bus node.
        /// </summary>
        /// <param name="listenPort">If a valid UDP port number, start listening on this port.<param>
        public NetNode(int listenPort = -1)
        {
            this.ConnectionRequestEvent += ConnectionRequestHandler;
            this.PeerConnectedEvent += PeerConnectedHandler;
            this.PeerDisconnectedEvent += PeerDisconnectedHandler;
            this.NetworkReceiveEvent += NetworkReceivedHandler;

            Host = new NetManager(this);
            if (listenPort > 0)
            {
                Host.Start(listenPort);
            }
            else
            {
                Host.Start();
            }
            _writer = new NetDataWriter();

            PacketProcessor = new NetNodePacketProcessor();
            Messages.Serialization.RegisterAllSerializers(PacketProcessor);
        }

        /// <summary>
        /// Connects to the given host:port.
        /// </summary>
        public NetPeer Connect(string host, int port, string key = Secret)
        {
            return Host.Connect(host, port, Secret);
        }

        /// <summary>
        /// Poll events and update other internal state.
        /// </summary>
        public void Update()
        {
            Host.PollEvents();
        }

        /// <summary>
        /// Sends a bus message to every peer connected to this node.
        /// </summary>
        public void BroadcastMessage<T>(T message, DeliveryMethod delivery = DeliveryMethod.ReliableUnordered, NetPeer excludedPeer = null)
            where T : class, IMessage, new()
        {
            lock (_writer)
            {
                _writer.Reset();
                PacketProcessor.Write<T>(_writer, message);
                if (excludedPeer == null)
                {
                    Host.SendToAll(_writer, delivery);
                }
                else
                {
                    Host.SendToAll(_writer, delivery, excludedPeer);
                }
            }
        }

        /// <summary>
        /// Sends a bus message to a particular peer.
        /// </summary>
        public void SendMessage<T>(T message, NetPeer peer, DeliveryMethod delivery = DeliveryMethod.ReliableUnordered)
            where T : class, IMessage, new()
        {
            lock (_writer)
            {
                _writer.Reset();
                PacketProcessor.Write<T>(_writer, message);
                peer.Send(_writer, delivery);
            }
        }

        /// <summary>
        /// Queries the local host's IP addresses.
        /// </summary>
        public static List<IPAddress> LocalIPs
        {
            get
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(iface => iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(iface => iface.GetIPProperties().UnicastAddresses, (iface, ip) => ip.Address)
                    .ToList();
            }
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

        private void ConnectionRequestHandler(ConnectionRequest request)
        {
            Console.WriteLine($"Bus: connecting {request.RemoteEndPoint}");
            request.AcceptIfKey(Secret);
        }

        private void PeerConnectedHandler(NetPeer peer)
        {
            Console.WriteLine($"Bus: {peer.EndPoint} connected");
        }

        private void PeerDisconnectedHandler(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"Bus: {peer.EndPoint} disconnected ({disconnectInfo.Reason})");
        }

        private void NetworkReceivedHandler(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketProcessor.ReadAllPackets(peer, reader);
            reader.Recycle();
        }
    }

    /// <summary>
    /// Waits for a message of type T from a NetNode.
    /// </summary>
    public class MessageWaiter<T> where T : class, IMessage, new()
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
            Node.PacketProcessor.Events<T>().OnMessageReceived += OnMessageReceived;
        }

        internal void OnMessageReceived(NetPeer sender, T message)
        {
            if (Peer == null || sender == Peer)
            {
                if (Filter == null || Filter(message))
                {
                    Node.PacketProcessor.Events<T>().OnMessageReceived -= OnMessageReceived;
                    _completionSrc.SetResult(message);
                }
            }
        }
        ~MessageWaiter()
        {
            Node.PacketProcessor.Events<T>().OnMessageReceived -= OnMessageReceived;
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
