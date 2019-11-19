using System;

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
        internal ApiRoute(string route)
        {
            this.Route = route;
        }

        /// <summary>
        /// The API route this method is attached to.
        /// </summary>
        internal string Route { get; set; }
    }
}
