using System;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SGame
{
    /// <summary>
    /// An attribute used to mark SGame API route methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class ApiRoute : Attribute
    {
        /// <summary>
        /// Initializes the attribute. 
        /// </summary>
        /// <param name="route">The API route this method will be attached to.</param>
        public ApiRoute(string route)
        {
            this.Route = route;
        }

        /// <summary>
        /// The API route this method is attached to.
        /// </summary>
        public string Route { get; private set; }
    }

    /// <summary>
    /// An attribute used to decorate SGame `ApiRoute` methods,
    /// telling what parameters are expected from the user data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class ApiParam : Attribute
    {
        /// <summary>
        /// Marks that the decorated method expects a certain API parameter when called.
        /// </summary>
        /// <param name="name">The name of the expected parameter.</param>
        /// <param name="type">The type of the expected parameter (string, int, or any other JSON type).</param>
        public ApiParam(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
            this.Optional = true;
        }

        /// <summary>
        /// The name of the API parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the API parameter.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Is the API parameter optional?
        /// </summary>
        public bool Optional { get; set; }
    }

    /// <summary>
    /// Wraps the payload passed to a SGame API request.
    /// </summary>
    class ApiData
    {
        /// <summary>
        /// Inits a request's data given its parameters.
        /// </summary>
        /// <param name="data">The stored parameters.</param>
        internal ApiData(JObject data)
        {
            this.Json = data;
        }

        /// <summary>
        /// The stored parameters. 
        /// </summary>
        public JObject Json { get; private set; }
    }

    /// <summary>
    /// Wraps the response for a SGame API request.
    /// </summary>
    class ApiResponse
    {
        /// <summary>
        /// The underlying HTTP response.
        /// </summary>
        HttpWebResponse response;

        /// <summary>
        /// Inits an API response.
        /// </summary>
        /// <param name="response">The underlying HTTP response.</param>
        internal ApiResponse(HttpWebResponse response)
        {
            this.response = response;
        }

        /// <summary>
        /// Sends a response to the API request, closing it off.
        /// </summary>
        /// <param name="json">The JSON payload to attach to the response.</param>
        /// <param name="status">The HTTP status code of the response.</param>
        public void Send(JObject json, HttpStatusCode status=HttpStatusCode.Accepted)
        {
            string jsonStr = response.ToString(Formatting.None);

            response.ContentType = "application/json";
            response.StatusCode = status;

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
