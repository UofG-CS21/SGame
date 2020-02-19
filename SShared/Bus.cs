using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SShared
{

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

        NetSerializer _serializer;

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
            // TODO IMPLEMENT: Do something when a message is received from a connected peer
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
