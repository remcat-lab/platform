using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ApiServer
{
    public interface IApiHandler
    {
        Task HandleAsync(HttpContext context);
    }
}