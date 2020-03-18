using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using SShared;
using LiteNetLib;
using Messages = SShared.Messages;

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

        public ArbiterTreeNode AddSGameNode(NetPeer peer, IPAddress busAddress, uint busPort, string apiUrl)
        {
            if (this.RootNode == null)
            {
                this.RootNode = new ArbiterTreeNode(peer, busAddress, busPort, apiUrl);
                return this.RootNode;
            }
            else
            {
                // TODO: Different node assignment logic?
                Random random = new Random();
                ArbiterTreeNode parent = this.RootNode;
                while (true)
                {
                    int randomQuadrant = random.Next() % 4;
                    for (int i = 0; i < 4; i++)
                    {
                        var quadrant = (Quadrant)((randomQuadrant + i) % 4);
                        if (parent.Child(quadrant) == null)
                        {
                            var node = new ArbiterTreeNode(peer, busAddress, busPort, apiUrl);
                            parent.SetChild(quadrant, node);
                            return node;
                        }
                    }
                    parent = parent.Child((Quadrant)randomQuadrant) as ArbiterTreeNode;
                }
            }
        }

        public bool RemoveSGameNode(NetPeer peer)
        {
            if (RootNode == null) return false;

            var node = RootNode.Traverse().Cast<ArbiterTreeNode>().Where((node) => node.Peer == peer).FirstOrDefault();
            if (node == null) return false;

            if (node.Parent == null)
            {
                RootNode = null;
            }
            else
            {
                node.Parent.EraseChild(node);
            }

            foreach (var token in _nodeByShipToken.Keys.ToList())
            {
                if (_nodeByShipToken[token] == node)
                {
                    _nodeByShipToken[token] = node.Parent as ArbiterTreeNode;
                }
            }
            return true;
        }

        public ArbiterTreeNode NodeWithShip(string token)
        {
            return _nodeByShipToken.GetValueOrDefault(token, null);
        }

        private static string PublicIdFromToken(string token) => token.Substring(token.Length - 8);

        private string AddShipToken()
        {
            string token, pid;
            do
            {
                token = Guid.NewGuid().ToString();
                pid = PublicIdFromToken(token);
            }
            while (_shipPublicIds.Contains(pid));

            _shipPublicIds.Add(pid);
            return token;
        }

        public string AddNewShip(out ArbiterTreeNode parentNode, string token)
        {
            if (token == null)
                token = AddShipToken();
            else
                _shipPublicIds.Add(PublicIdFromToken(token));
            parentNode = (ArbiterTreeNode)RootNode.RandomLeafNode();
            _nodeByShipToken[token] = parentNode;

            BusMaster.SendMessage(new Messages.ShipConnected() { Token = token }, parentNode.Peer);

            return token;
        }

#if DEBUG
        internal int _shipCount = 0;
#endif

        public string AddNewShip(out ArbiterTreeNode parentNode)
        {
            string token = AddShipToken();

#if DEBUG
            // FIXME This is just for testing - in debug mode, assume there are just two SGame nodes and round-robin ships to them
            if ((_shipCount++) % 2 == 0)
            {
                parentNode = (ArbiterTreeNode)RootNode;
            }
            else
            {
                parentNode = null;
                for (int i = 0; i < 4; i++)
                {
                    var q = RootNode.Child((Quadrant)i);
                    if (q == null) continue;
                    parentNode = (ArbiterTreeNode)q;
                    break;
                }
            }
#else
            parentNode = (ArbiterTreeNode)RootNode.RandomLeafNode();
#endif

            _nodeByShipToken[token] = parentNode;
            parentNode.ShipCount++;

            BusMaster.SendMessage(new Messages.ShipConnected() { Token = token }, parentNode.Peer);
            return token;
        }

        public bool MoveShip(Spaceship ship, ArbiterTreeNode transferNode)
        {
            string token = ship.Token;
            ArbiterTreeNode sourceNode;
            if (!_nodeByShipToken.Remove(token, out sourceNode))
            {
                return false;
            }

            _nodeByShipToken[token] = transferNode;
            sourceNode.ShipCount--;
            transferNode.ShipCount++;

            Messages.ShipTransferred msg = new Messages.ShipTransferred() { Ship = ship };
            BusMaster.SendMessage(msg, transferNode.Peer);
            return true;
        }

        public bool RemoveShip(string token)
        {
            ArbiterTreeNode node;
            if (!_nodeByShipToken.Remove(token, out node))
            {
                return false;
            }

            _shipPublicIds.Remove(PublicIdFromToken(token));
            node.ShipCount--;

            BusMaster.BroadcastMessage(new Messages.ShipDisconnected() { Token = token });
            return true;
        }
    }
}
