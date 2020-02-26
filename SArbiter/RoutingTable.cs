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

        ArbiterTreeNode _root = new ArbiterTreeNode();

        Dictionary<string, ArbiterTreeNode> _nodeByShipToken = new Dictionary<string, ArbiterTreeNode>();

        HashSet<string> _shipPublicIds = new HashSet<string>();

        public ArbiterTreeNode NodeWithShip(string token)
        {
            return _nodeByShipToken.GetValueOrDefault(token, null);
        }

        internal string AddShipToken()
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

        internal string AddNewShip(out ArbiterTreeNode parentNode)
        {
            string token = AddShipToken();
            parentNode = _root.RandomLeafNode();
            _nodeByShipToken[token] = parentNode;
            return token;
        }
    }
}
