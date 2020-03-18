using System;
using System.Net.Http;
using System.Threading.Tasks;
using SShared;
using Messages = SShared.Messages;

namespace SArbiter
{
    class ArbiterApi
    {
        public RoutingTable RoutingTable { get; set; }

        public ArbiterApi(RoutingTable routingTable)
        {
            this.RoutingTable = routingTable;
        }

        [ApiRoute("connect")]
        public async Task ConnectPlayer(ApiResponse response, ApiData data)
        {
            ArbiterTreeNode shipNode = null;
            var token = RoutingTable.AddNewShip(out shipNode);
            response.Data["token"] = token;

            // TODO: Add a timeout in case we don't get a reply from the SGame node?
            await new MessageWaiter<Messages.ShipConnected>(RoutingTable.BusMaster, shipNode.Peer, (msg) => msg.Token == token).Wait;

            await response.Send(200);
        }

        [ApiRoute("disconnect")]
        [ApiParam("token", typeof(string))]
        public async Task DisconnectPlayer(ApiResponse response, ApiData data)
        {
            var token = (string)data.Json["token"];

            RoutingTable.RemoveShip(token);
            await response.Send(200);
        }

        internal async Task ForwardRequest(string route, ApiResponse response, ApiData data)
        {
            var token = (string)data.Json["token"];
            var node = RoutingTable.NodeWithShip(token);
            if (node == null)
            {
                response.Data["error"] = $"No ship with token: {token}";
                await response.Send(500);
            }
            else
            {
                var url = $"{node.ApiUrl}{route}";
                await response.Redirect(url);
            }
        }

        [ApiRoute("accelerate")]
        [ApiParam("token", typeof(string))]
        public Task Accelerate(ApiResponse response, ApiData data) => ForwardRequest("accelerate", response, data);

        [ApiRoute("getShipInfo")]
        [ApiParam("token", typeof(string))]
        public Task GetShipInfo(ApiResponse response, ApiData data) => ForwardRequest("getShipInfo", response, data);

        [ApiRoute("scan")]
        [ApiParam("token", typeof(string))]
        public Task Scan(ApiResponse response, ApiData data) => ForwardRequest("scan", response, data);

        [ApiRoute("shield")]
        [ApiParam("token", typeof(string))]
        public Task Shield(ApiResponse response, ApiData data) => ForwardRequest("shield", response, data);

        [ApiRoute("shoot")]
        [ApiParam("token", typeof(string))]
        public Task Shoot(ApiResponse response, ApiData data) => ForwardRequest("shoot", response, data);

#if DEBUG
        [ApiRoute("sudo")]
        public async Task Sudo(ApiResponse response, ApiData data)
        {
            var message = new Messages.Sudo() { Json = data.Json };
            ArbiterTreeNode shipNode = null;
            if (data.Json.ContainsKey("token"))
            {
                var shipToken = (string)data.Json["token"];
                shipNode = RoutingTable.NodeWithShip(shipToken);
                if (shipNode == null)
                {
                    response.Data["error"] = $"No ship with token: {shipToken}";
                    await response.Send(500);
                }
                RoutingTable.BusMaster.SendMessage(message, shipNode.Peer, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                RoutingTable.BusMaster.BroadcastMessage(message);
                // Wait for the root node to ACK sudo. No good reason to pick the root, it's just guaranteed to exist.
                shipNode = RoutingTable.RootNode;
            }

            // Wait for the sudo to bounce back from the node
            await new MessageWaiter<Messages.Sudo>(RoutingTable.BusMaster, shipNode.Peer,
                (sudo) => sudo.Json.Equals(message.Json)).Wait;

            await response.Send(200);
        }
#endif

    }
}
