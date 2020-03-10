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
        public string ApiUrl { get; set; }

        public NetPeer Peer { get; set; }

        public ArbiterTreeNode(string apiUrl, NetPeer peer)
            : base(null, new Quad(0.0, 0.0, Double.MaxValue), 0)
        {
            this.ApiUrl = apiUrl;
            this.Peer = peer;
        }

        public ArbiterTreeNode(QuadTreeNode<ArbiterTreeItem> parent, Quad bounds, uint depth, string apiUrl, NetPeer peer)
            : base(parent, bounds, depth)
        {
            this.ApiUrl = apiUrl;
            this.Peer = peer;
        }

        public override Task<List<ArbiterTreeItem>> CheckRangeLocal(Quad range) => throw new NotImplementedException();
    }
}
