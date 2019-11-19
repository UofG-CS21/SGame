using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGame
{
    /// The JSON and/or URL data passed to an API call.
    using ApiData = JObject;

    /// The HTTP response to an API call.
    using ApiResponse = HttpListenerResponse;


    /// <summary>
    /// Handles an API call at a given route.
    /// </summary>
    /// <param name="response">The response to write to.</param>
    /// <param name="data">The data passed to the API call.</param>
    delegate void ApiRouteDelegate(ApiResponse response, ApiData data);


    /// <summary>
    /// Uses Reflection to dispatch REST API calls to methods of a class.
    /// </summary>
    class Router<Api>
    {
        /// <summary>
        /// The internal object to call route handlers on.
        /// </summary>
        Api api;

        /// <summary>
        /// Maps route names to route handler delegates from `api`.
        /// </summary>
        Dictionary<string, ApiRouteDelegate> apiRoutes;

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
            this.apiRoutes = new Dictionary<string, ApiRouteDelegate>();
            var apiType = api.GetType();
            foreach(MethodInfo method in apiType.GetMethods())
            {
                var apiRouteAttr = method.GetCustomAttributes<ApiRoute>().FirstOrDefault();
                if(apiRouteAttr == null)
                {
                    // This method is not bound to an API route
                    continue;
                }

                var handler = method.CreateDelegate(typeof(ApiRouteDelegate), this.api) as ApiRouteDelegate;
                apiRoutes[apiRouteAttr.Route] = handler;
            }
        }

        public void Dispatch(string route, ApiResponse response, ApiData data)
        {
            var handler = apiRoutes.GetValueOrDefault(route, null);
            if(handler == null)
            {
                // FIXME: log error?
                return;
            }

            handler.Invoke(response, data);
        }
    }
}
