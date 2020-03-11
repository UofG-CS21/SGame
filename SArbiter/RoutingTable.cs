using System;
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

        public string AddNewShip(out ArbiterTreeNode parentNode)
        {
            string token = AddShipToken();
            parentNode = (ArbiterTreeNode)RootNode.RandomLeafNode();
            _nodeByShipToken[token] = parentNode;

            BusMaster.SendMessage(new Messages.ShipConnected() { Token = token }, parentNode.Peer);

            return token;
        }

        public bool MoveShip(Spaceship ship, ArbiterTreeNode transferNode){
            string token = ship.Token;
            if (!_nodeByShipToken.Remove(token)){
                return false;
            }

            _nodeByShipToken[token] = transferNode;
            
            Messages.ShipTransferred msg = new Messages.ShipTransferred() { Ship = ship };
            BusMaster.SendMessage(msg, transferNode.Peer);
            return true;

        }

        public bool RemoveShip(string token)
        {
            if (!_nodeByShipToken.Remove(token))
            {
                _shipPublicIds.Remove(PublicIdFromToken(token));
                return false;
            }

            BusMaster.BroadcastMessage(new Messages.ShipDisconnected() { Token = token });

            return true;
        }
    }
}
