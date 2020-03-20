using System;
using System.Text;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SShared;
using LiteNetLib;
using Messages = SShared.Messages;
using System.Net.Http;

namespace SArbiter
{
    internal class RoutingTable
    {
        const uint MaxDepth = SShared.QuadTreeNode<SShared.Spaceship>.MaxDepth;

        Dictionary<string, ArbiterTreeNode> _nodeByShipToken = new Dictionary<string, ArbiterTreeNode>();

        HashSet<string> _shipPublicIds = new HashSet<string>();

        public ArbiterTreeNode RootNode { get; set; }

        public NetNode BusMaster { get; set; }

        public double UniverseSize { get; set; }

        public RoutingTable(NetNode busMaster, ArbiterTreeNode rootNode, double universeSize)
        {
            this.BusMaster = busMaster;
            this.RootNode = rootNode;
            this.UniverseSize = universeSize;
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

        public async Task<bool> RemoveSGameNode(NetPeer peer)
        {
            if (RootNode == null) return false;

            var disconnectedNode = RootNode.Traverse().Cast<ArbiterTreeNode>().Where((node) => node.Peer == peer).FirstOrDefault();
            if (disconnectedNode == null) return false;

            ArbiterTreeNode substituteNode;
            if (disconnectedNode.Parent == null)
            {
                ArbiterTreeNode childToPromote = (ArbiterTreeNode)disconnectedNode.FirstChild();
                if (childToPromote == null)
                {
                    substituteNode = null;
                    RootNode = null;
                }
                else
                {
                    substituteNode = childToPromote;
                }
            }
            else
            {
                ArbiterTreeNode leafToPromote = (ArbiterTreeNode)disconnectedNode.RandomLeafNode();
                if (leafToPromote == null)
                {
                    // No leaf means `disconnectedNode` had no childtren, so we just send the ships to the parent
                    substituteNode = (ArbiterTreeNode)disconnectedNode.Parent;
                }
                else
                {
                    substituteNode = leafToPromote;
                }
            }

            if (substituteNode != null)
            {
                Console.Error.WriteLine("Moving ships that were in {0} to {1}", disconnectedNode.Path(), substituteNode.Path());
            }
            else
            {
                Console.Error.WriteLine("There is no node to promote to root!");
            }

            // New substitute node retains his children but also gets the new ones
            // We assume the disconnected node persisted its ships to Elastic before dying;
            // if it has, the connect REST calls below will transfer the ships to the substitute!
            foreach (var token in _nodeByShipToken.Keys.ToList())
            {
                if (_nodeByShipToken[token] == disconnectedNode)
                {
                    if (substituteNode != null)
                    {
                        var connectWaiter = new MessageWaiter<Messages.ShipConnected>(BusMaster, substituteNode.Peer).Wait;
                        BusMaster.SendMessage(new Messages.ShipConnected() { Token = token }, substituteNode.Peer);
                        await connectWaiter;
                    }
                    _nodeByShipToken[token] = substituteNode;
                }
            }

            if (substituteNode != null)
            {
                if (substituteNode.Parent != null)
                {
                    substituteNode.Parent.SetChild(substituteNode.Quadrant, null);
                }

                if (disconnectedNode.Parent != null)
                {
                    disconnectedNode.Parent.SetChild(disconnectedNode.Quadrant, substituteNode);
                }
                else
                {
                    substituteNode.MakeRoot(new Quad(0.0, 0.0, UniverseSize));
                    RootNode = substituteNode;
                }

                BusMaster.BroadcastMessage(new Messages.NodeOffline()
                {
                    Path = disconnectedNode.Path(),
                    ApiUrl = disconnectedNode.ApiUrl,
                }, DeliveryMethod.ReliableOrdered);

                BusMaster.BroadcastMessage(new Messages.NodeConfig()
                {
                    BusAddress = substituteNode.BusAddress,
                    BusPort = substituteNode.BusPort,
                    Bounds = substituteNode.Bounds,
                    Path = substituteNode.Path(),
                    ApiUrl = substituteNode.ApiUrl,
                }, DeliveryMethod.ReliableOrdered);
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

#if false
            // This is just for testing - in debug mode, assume there are just two SGame nodes and round-robin ships to them
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
