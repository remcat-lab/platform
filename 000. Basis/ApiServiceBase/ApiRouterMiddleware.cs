using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiServer;

public class ApiRouterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<PathString, IApiHandler> _apiMap;

    public ApiRouterMiddleware(RequestDelegate next, Dictionary<PathString, IApiHandler> apiMap)
    {
        _next = next;
        _apiMap = apiMap;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_apiMap.TryGetValue(context.Request.Path, out var handler))
        {
            await handler.HandleAsync(context);
        }
        else
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Not Found");
        }
    }
}