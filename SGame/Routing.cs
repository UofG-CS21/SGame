using System;
using System.Net;
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
}
