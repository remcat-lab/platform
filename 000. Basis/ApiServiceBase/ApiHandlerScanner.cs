using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ApiServer;

public static class ApiHandlerScanner
{
    public static Dictionary<PathString, IApiHandler> DiscoverApiHandlers(Assembly[] assemblies)
    {
        var apiMap = new Dictionary<PathString, IApiHandler>();

        foreach (var assembly in assemblies)
        {
            var handlers = assembly
                .GetTypes()
                .Where(t => typeof(IApiHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => new
                {
                    Type = t,
                    Route = t.GetCustomAttribute<RouteAttribute>()?.Template
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Route));

            foreach (var handler in handlers)
            {
                if (!apiMap.ContainsKey(handler.Route))
                {
                    var instance = (IApiHandler)Activator.CreateInstance(handler.Type)!;
                    apiMap[handler.Route] = instance;
                }
            }
        }

        return apiMap;
    }
}