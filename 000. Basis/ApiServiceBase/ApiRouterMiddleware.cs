public class ApiRouterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, Type> _apiMap;
    private readonly IServiceProvider _serviceProvider;

    public ApiRouterMiddleware(RequestDelegate next, Dictionary<string, Type> apiMap, IServiceProvider serviceProvider)
    {
        _next = next;
        _apiMap = apiMap;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString().ToLowerInvariant();

        if (_apiMap.TryGetValue(path, out var handlerType))
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = (IApiHandler)scope.ServiceProvider.GetRequiredService(handlerType);
            await handler.HandleAsync(context);
            return;
        }

        await _next(context);
    }
}
