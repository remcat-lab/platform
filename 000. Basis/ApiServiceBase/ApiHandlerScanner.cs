public static class ApiHandlerScanner
{
    public static Dictionary<string, Type> DiscoverApiHandlers(IServiceCollection services, params Assembly[] assemblies)
    {
        var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        var handlerTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(IApiHandler).IsAssignableFrom(t))
            .ToList();

        foreach (var type in handlerTypes)
        {
            var routeAttr = type.GetCustomAttribute<RouteAttribute>();
            if (routeAttr == null) continue;

            services.AddScoped(type);
            map[routeAttr.Path] = type;
        }

        return map;
    }
}
