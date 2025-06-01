public class HelloApiHandler : IApiHandler
{
    public async Task HandleAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"message\": \"Hello from handler\"}");
    }
}
