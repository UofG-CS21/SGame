using System;
using System.Runtime.InteropServices;

namespace SShared
{
    /// <summary>
    /// A node in the event bus (either master/SArbiter or client/SGame).
    /// </summary>
    public sealed class BusNode : IDisposable
    {
        /// <summary>
        /// Number of milliseconds after which to timeout when waiting for ENet events.
        /// 0 means "do not wait" (-> non-blocking).
        /// </summary>
        public const uint EventTimeout = 0;

        /// <summary>
        /// The wrapped ENet host.
        /// </summary>
        public ENet.Host Host { get; private set; }

        /// <summary>
        /// Creates a new bus node.
        /// </summary>
        /// <param name="hostname">Set it to a hostname to connect to if this is a client node, or null if this is a bus master.</param>
        /// <param name="port">The UDP port to communicate over.</param>
        /// <param name="maxPeers">The maximum number of clients or peers that this node will be able to communicate with.</param>
        public BusNode(String hostname, ushort port, uint maxPeers)
        {
            if (!BusContext.Instance.Inited)
            {
                throw new ExternalException("ENet could not be initialized");
            }

            Host = new ENet.Host();

            ENet.Address addr = new ENet.Address();
            if (hostname != null)
            {
                if (!addr.SetHost(hostname))
                {
                    throw new InvalidOperationException($"Could not set host: {hostname}");
                }
            }
            // Do not `SetHost` or `SetIP`
            addr.Port = port;

            Host.Create(addr, (int)maxPeers);
        }

        /// <summary>
        /// Disposes of the internal ENet connection.
        /// </summary>
        public void Dispose()
        {
            Host.Flush();
            Host.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Emitted when a peer connects to the bus.
        /// </summary>
        public event BusPeerEventHandler OnConnect;

        /// <summary>
        /// Emitted when a peer disconnects from the bus.
        /// </summary>
        public event BusPeerEventHandler OnDisconnect;

        /// <summary>
        /// Emitted when a peer times out.
        /// </summary>
        public event BusPeerEventHandler OnTimeout;

        /// <summary>
        /// Emitted when receiving data from a peer.
        /// </summary>
        public event BusPeerDataEventHandler OnReceive;

        /// <summary>
        /// Pool for any new ENet events (triggering an appropriate )
        /// </summary>
        public void Update()
        {
            ENet.Event evt;

            bool hasEvents = true;
            while (hasEvents)
            {
                switch (Math.Sign(Host.CheckEvents(out evt)))
                {
                    case -1: // Error
                        // TODO: Log error? (this could get spammed once per update!!)
                        goto case 0;
                    case 0: // No more messages 
                        if (Host.Service((int)EventTimeout, out evt) <= 0)
                        {
                            // TODO: Log the error?
                            break;
                        }
                        hasEvents = false;
                        break;
                }

                switch (evt.Type)
                {
                    case ENet.EventType.Connect:
                        OnConnect(this, new BusPeerEventArgs(evt.Peer));
                        break;
                    case ENet.EventType.Disconnect:
                        OnDisconnect(this, new BusPeerEventArgs(evt.Peer));
                        break;
                    case ENet.EventType.Timeout:
                        OnTimeout(this, new BusPeerEventArgs(evt.Peer));
                        break;
                    case ENet.EventType.Receive:
                        using (var evtArgs = new BusPeerDataEventArgs(evt.Peer, evt.ChannelID, evt.Packet))
                        {
                            OnReceive(this, evtArgs);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Creates a ENet packet with the given contents and broadcasts it to all ENet peers connected to the given channel.
        /// </summary>
        public void Broadcast(byte[] data, int length, byte channel, ENet.PacketFlags flags = ENet.PacketFlags.None)
        {
            ENet.Packet packet = default(ENet.Packet);
            packet.Create(data, length, flags);
            Host.Broadcast(channel, ref packet);
            packet.Dispose();
        }

        /// <summary>
        /// Creates a ENet packet with the given contents and sends it to all given peers over the given channel.
        /// </summary>
        public void Multicast(byte[] data, int length, byte channel, ENet.Peer[] peers, ENet.PacketFlags flags = ENet.PacketFlags.None)
        {
            ENet.Packet packet = default(ENet.Packet);
            packet.Create(data, length, flags);
            Host.Broadcast(channel, ref packet, peers);
            packet.Dispose();
        }

        /// <summary>
        /// Creates a ENet packet with the given contents and sends it to the given peer.
        /// </summary>
        public static void Send(byte[] data, int length, byte channel, ENet.Peer target, ENet.PacketFlags flags = ENet.PacketFlags.None)
        {
            ENet.Packet packet = default(ENet.Packet);
            packet.Create(data, length, flags);
            target.Send(channel, ref packet);
            packet.Dispose();
        }
    }

}