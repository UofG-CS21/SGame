using System;
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

#if DEBUG
        [ApiRoute("sudo")]
        [ApiParam("token", typeof(string))]
        public Task Sudo(ApiResponse response, ApiData data) => ForwardRequest("sudo", response, data);
#endif

    }
}
