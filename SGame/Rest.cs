using System;

namespace SGame
{
    /// <summary>
    /// An attribute used to mark REST API handler methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class RestApi : Attribute
    {
        /// <summary>
        /// Initializes the attribute. 
        /// </summary>
        /// <param name="route">The REST route this method will be attached to.</param>
        internal RestApi(string route)
        {
            this.Route = route;
        }

        /// <summary>
        /// The REST route this method is attached to.
        /// </summary>
        internal string Route { get; set; }
    }
}
