using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace SShared
{
    /// <summary>
    /// Handles an API call at a given route.
    /// </summary>
    /// <param name="response">The response to write to.</param>
    /// <param name="data">The data passed to the API call.</param>
    delegate Task ApiRouteDelegate(ApiResponse response, ApiData data);

    /// <summary>
    /// Uses Reflection to dispatch REST API calls to methods of a class.
    /// </summary>
    public class Router<Api>
    {
        /// <summary>
        /// Mapped to each route string in `apiRoutes`. 
        /// </summary>
        class RouteData
        {
            /// <summary>
            /// The handler functor called when this route is reached.
            /// </summary>
            public ApiRouteDelegate Delegate { get; set; }

            /// <summary>
            /// The list of parameters this route expects to find.
            /// </summary>
            public List<ApiParam> Params { get; set; }
        }

        /// <summary>
        /// The internal object to call route handlers on.
        /// </summary>
        Api api;

        /// <summary>
        /// Maps route names to route handler delegates from `api`.
        /// </summary>
        Dictionary<string, RouteData> apiRoutes;

        /// <summary>
        /// Initializes a router that will dispatch calls to `api`'s route handler methods.
        /// Route handlers have an `[ApiRoute("route")]` attribute.
        /// </summary>
        /// <param name="api">The object to search route handlers in.</param>
        public Router(Api api)
        {
            this.api = api;

            // Find all methods in `Api` decorated with `ApiRoute`
            // and register them into the internal (route -> delegate) map
            this.apiRoutes = new Dictionary<string, RouteData>();
            var apiType = api.GetType();
            foreach (MethodInfo method in apiType.GetMethods())
            {
                var apiRouteAttr = method.GetCustomAttributes<ApiRoute>().FirstOrDefault();
                if (apiRouteAttr == null)
                {
                    continue;
                }

                if (apiRoutes.ContainsKey(apiRouteAttr.Route))
                {
                    throw new InvalidOperationException("A route at '" + apiRouteAttr.Route + "is already registered!");
                }

                RouteData routeData = apiRoutes[apiRouteAttr.Route] = new RouteData();
                routeData.Params = method.GetCustomAttributes<ApiParam>().ToList();

                var handler = method.CreateDelegate(typeof(ApiRouteDelegate), this.api) as ApiRouteDelegate;
                routeData.Delegate = handler;
            }
        }

        /// <summary>
        /// Checks if the current param is present in `data` (if it's not optional)
        /// and that it has the correct type.
        /// </summary>
        /// <param name="param">The parameter to check.</param>
        /// <param name="data">The data to search the param in.</param>
        /// <returns>An error message on error or null otherwise.</returns>
        string CheckParam(ApiParam param, ApiData data)
        {
            JToken token;
            bool hasAttr = data.Json.TryGetValue(param.Name, out token);
            if (!hasAttr)
            {
                if (!param.Optional)
                {
                    return $"Missing required parameter: {param.Name}";
                }
            }

            try
            {
                var castedValue = token.ToObject(param.Type);
            }
            catch (Exception exc)
            {
                return $"Expected parameter {param.Name} to be a {param.Type.Name} ({exc.Message})";
            }

            return null;
        }

        /// <summary>
        /// Routes a request to the right route delegate in the `Api`.
        /// </summary>
        /// <param name="route">The request route (e.g. "connect").</param>
        /// <param name="response">The response to be written to.</param>
        /// <param name="data">The JSON and/or query string data in the request.</param>
        /// <returns>true if a non-error response was sent, false otherwise</returns>
        public async Task<bool> Dispatch(string route, ApiResponse response, ApiData data)
        {
            var routeData = apiRoutes.GetValueOrDefault(route, null);
            if (routeData == null)
            {
                response.Data["error"] = $"Route {route} not found";
                await response.Send(404);
                return false;
            }

            bool ok = false;
            try
            {
                var paramError = routeData.Params
                    .Select(param => CheckParam(param, data))
                    .SkipWhile(string.IsNullOrEmpty)
                    .FirstOrDefault();

                if (paramError != null)
                {
                    response.Data["error"] = paramError;
                    await response.Send(500);
                    return false;
                }

                await routeData.Delegate.Invoke(response, data);
                ok = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception at {ex.Source}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (!response.Sent)
                {
                    Console.Error.WriteLine("Error: response was not sent");
                    response.Data["error"] = "Internal server error";
                    await response.Send(500);
                }
            }
            return ok;
        }
    }
}
