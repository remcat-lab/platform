public class ApiRouterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, Type> _apiMap;
    private readonly IServiceProvider _provider;

    public ApiRouterMiddleware(RequestDelegate next,
                               Dictionary<string, Type> apiMap,
                               IServiceProvider provider)
    {
        _next = next;
        _apiMap = apiMap;
        _provider = provider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        if (path != null && _apiMap.TryGetValue(path, out var handlerType))
        {
            if (_provider.GetService(handlerType) is IApiHandler handler)
            {
                await handler.HandleAsync(context);
                return;
            }

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Handler type not resolved");
            return;
        }

        await _next(context);
    }
}
