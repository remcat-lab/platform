public interface IApiHandler
{
    Task HandleAsync(HttpContext context);
}
