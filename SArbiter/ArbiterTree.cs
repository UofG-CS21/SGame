using System;
using SShared;
using LiteNetLib;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SArbiter
{
    internal class ArbiterTreeItem : IQuadBounded
    {
        public string token;

        public Quad Bounds => throw new NotImplementedException();
    }

    internal class ArbiterTreeNode : QuadTreeNode<ArbiterTreeItem>
    {
        public NetPeer Peer { get; set; }

        public string ApiUrl { get; set; }

        public uint BusPort { get; set; }

        public uint ShipCount { get; set; }

        public ArbiterTreeNode(NetPeer peer, uint busPort, string apiUrl)
            : base(new Quad(0.0, 0.0, Double.MaxValue), 0)
        {
            this.ApiUrl = apiUrl;
            this.BusPort = busPort;
            this.Peer = peer;
            this.ShipCount = 0;
        }

        public ArbiterTreeNode(QuadTreeNode<ArbiterTreeItem> parent, Quadrant quadrant, uint depth, NetPeer peer, uint busPort, string apiUrl)
            : base(parent, quadrant, depth)
        {
            this.ApiUrl = apiUrl;
            this.BusPort = busPort;
            this.Peer = peer;
            this.ShipCount = 0;
        }

        public override Task<List<ArbiterTreeItem>> CheckRangeLocal(Quad range) => throw new NotImplementedException();
    }
}
