using Microsoft.AspNetCore.Http;
using System;

namespace ApiServer
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RouteAttribute : Attribute
    {
        public PathString Template { get; }

        public RouteAttribute(string template)
        {
            Template = template;
        }
    }
}