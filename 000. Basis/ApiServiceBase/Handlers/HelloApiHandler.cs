[Route("/api/hello")]
public class HelloApiHandler : IApiHandler
{
    private readonly ILogger<HelloApiHandler> _logger;

    public HelloApiHandler(ILogger<HelloApiHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        _logger.LogInformation("HelloApiHandler executed");
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"message\": \"Hello from handler\"}");
    }
}
