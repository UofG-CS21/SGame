using System;
using System.Linq;
using System.Collections.Generic;
using SShared;
using LiteNetLib;

namespace SArbiter
{
    internal class RoutingTable
    {
        const uint MaxDepth = SShared.QuadTreeNode<SShared.Spaceship>.MaxDepth;

        Dictionary<string, ArbiterTreeNode> _nodeByShipToken = new Dictionary<string, ArbiterTreeNode>();

        HashSet<string> _shipPublicIds = new HashSet<string>();

        public ArbiterTreeNode RootNode { get; set; }

        public NetNode BusMaster { get; set; }

        public RoutingTable(NetNode busMaster, ArbiterTreeNode rootNode)
        {
            this.BusMaster = busMaster;
            this.RootNode = rootNode;
        }

        public ArbiterTreeNode NodeWithShip(string token)
        {
            return _nodeByShipToken.GetValueOrDefault(token, null);
        }

        private string AddShipToken()
        {
            string token, pid;
            do
            {
                token = new Guid().ToString();
                pid = token.Substring(token.Length - 8);
            }
            while (_shipPublicIds.Contains(pid));

            _shipPublicIds.Add(pid);
            return token;
        }

        public string AddNewShip(out ArbiterTreeNode parentNode)
        {
            string token = AddShipToken();
            parentNode = (ArbiterTreeNode)RootNode.RandomLeafNode();
            _nodeByShipToken[token] = parentNode;
            return token;
        }
    }
}
