public class ApiRouterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, IApiHandler> _apiByUrl;

    public ApiRouterMiddleware(RequestDelegate next, Dictionary<string, IApiHandler> apiByUrl)
    {
        _next = next;
        _apiByUrl = apiByUrl;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path != null && _apiByUrl.TryGetValue(path, out var handler))
        {
            await handler.HandleAsync(context);
            return;
        }

        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("API not found");
    }
}
