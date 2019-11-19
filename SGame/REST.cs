using System;

namespace SGame
{
    /// <summary>
    /// An attribute used to mark REST API handler methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class RestApi : Attribute
    {
    }
}
