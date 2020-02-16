using System;

namespace SShared
{
    /// <summary>
    /// Arguments for a ENet connection event involving a peer.
    /// </summary>
    public class BusPeerEventArgs : EventArgs
    {
        /// <summary>
        /// Creates new event args involving the given peer.
        /// </summary>
        public BusPeerEventArgs(ENet.Peer peer)
        {
            Peer = peer;
        }

        /// <summary>
        /// The ENet peer that triggered the event.
        /// </summary>
        public ENet.Peer Peer { get; private set; }
    }

    /// <summary>
    /// A delegate for "bus peer" events (ENet connect, disconnect, timeout).
    /// </summary>
    public delegate void BusPeerEventHandler(object sender, BusPeerEventArgs e);

    /// <summary>
    /// Arguments for a ENet connection event involving a peer and packet data.
    /// </summary>
    public class BusPeerDataEventArgs : BusPeerEventArgs, IDisposable
    {
        private bool disposed = false;

        /// <summary>
        /// Creates new event args involving the given peer, channel and data packet.
        /// </summary>
        public BusPeerDataEventArgs(ENet.Peer peer, byte channel, ENet.Packet packet)
            : base(peer)
        {
            Channel = channel;
            Packet = packet;
        }

        /// <summary>
        /// The ENet data packet that was transferred.
        /// </summary>
        public ENet.Packet Packet { get; private set; }

        /// <summary>
        /// The ENet channel data was transferred through.
        /// </summary>
        public byte Channel { get; private set; }

        // (See: Dispose pattern implementation...)

        ~BusPeerDataEventArgs()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Packet.Dispose();
                }
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// A delegate for "bus peer data" events (ENet receive).
    /// </summary>
    public delegate void BusPeerDataEventHandler(object sender, BusPeerDataEventArgs e);
}