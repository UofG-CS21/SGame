using System;
using System.Runtime.InteropServices;

namespace SShared
{
    /// <summary>
    /// A master node in the event bus.
    /// Usually associated with the `SArbiter` instance controlling the event bus.
    /// </summary>
    public sealed class BusMaster : IDisposable
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

        public BusMaster(string hostname, ushort port, uint maxPeers)
        {
            if (!BusContext.Instance.Inited)
            {
                throw new ExternalException("ENet could not be initialized");
            }

            Host = new ENet.Host();

            ENet.Address addr = new ENet.Address();
            if (!addr.SetHost(hostname))
            {
                throw new InvalidOperationException($"Could not bind to hostname: {hostname}");
            }
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
        /// Emitted when a peer (client) connects to the bus.
        /// </summary>
        public event BusPeerEventHandler OnPeerConnect;

        /// <summary>
        /// Emitted when a peer (client) disconnects from the bus.
        /// </summary>
        public event BusPeerEventHandler OnPeerDisconnect;

        /// <summary>
        /// Emitted when a peer (client) times out.
        /// </summary>
        public event BusPeerEventHandler OnPeerTimeout;

        /// <summary>
        /// Emitted when receiving data from a peer (client).
        /// </summary>
        public event BusPeerDataEventHandler OnPeerReceive;

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
                        OnPeerConnect(this, new BusPeerEventArgs(evt.Peer));
                        break;
                    case ENet.EventType.Disconnect:
                        OnPeerDisconnect(this, new BusPeerEventArgs(evt.Peer));
                        break;
                    case ENet.EventType.Timeout:
                        OnPeerTimeout(this, new BusPeerEventArgs(evt.Peer));
                        break;
                    case ENet.EventType.Receive:
                        using (var evtArgs = new BusPeerDataEventArgs(evt.Peer, evt.ChannelID, evt.Packet))
                        {
                            OnPeerReceive(this, evtArgs);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Convenience function. Creates a ENet packet with the given contents
        /// and broadcasts it to all ENet peers connected to the given channel.
        /// </summary>
        public void Broadcast(byte channel, byte[] data)
        {
            ENet.Packet packet = default(ENet.Packet);
            packet.Create(data);
            Host.Broadcast(channel, ref packet);
            packet.Dispose();
        }
    }

}