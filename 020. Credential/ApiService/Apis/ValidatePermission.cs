using System.Text.Json;

public class ValidatePermissionHandler : IApiHandler
{
    private readonly IDbContext _db;

    private const int DENY = 0b001;
    private const int ALLOW = 0b010;
    private const int DEFAULT_DENY = 0b100;

    public ValidatePermissionHandler(IDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        if (!request.QueryParameters.TryGetValue("userId", out var userId) ||
            !request.QueryParameters.TryGetValue("departmentId", out var departmentId) ||
            !request.QueryParameters.TryGetValue("url", out var url))
        {
            return new ApiResponse
            {
                StatusCode = 400,
                Content = JsonSerializer.Serialize(new { error = "Missing required query parameters." })
            };
        }

        int deptStatus = await GetDeptStatusPrefixMatchAsync(departmentId, url);
        int userStatus = await GetUserStatusPrefixMatchAsync(userId, url);

        bool hasAccess = EvaluatePermission(deptStatus, userStatus);

        var result = new { AccessGranted = hasAccess };
        string json = JsonSerializer.Serialize(result);

        return new ApiResponse
        {
            StatusCode = 200,
            Content = json
        };
    }

    private bool EvaluatePermission(int deptStatus, int userStatus)
    {
        if ((deptStatus & DENY) == DENY)
            return false;

        if ((deptStatus & ALLOW) == ALLOW)
        {
            if ((userStatus & DENY) == DENY)
                return false;
            return true;
        }

        if ((deptStatus & DEFAULT_DENY) == DEFAULT_DENY)
        {
            if ((userStatus & ALLOW) == ALLOW)
                return true;
            return false;
        }

        return false;
    }

    private async Task<int> GetDeptStatusPrefixMatchAsync(string departmentId, string url)
    {
        var now = DateTime.UtcNow;

        var entries = await _db.ACL_Department
            .Where(d => d.DepartmentId == departmentId
                     && d.UrlPrefix != null
                     && url.StartsWith(d.UrlPrefix)
                     && (d.ExpireDate == null || d.ExpireDate > now))
            .OrderByDescending(d => d.UrlPrefix.Length)
            .ToListAsync();

        return entries.Count > 0 ? entries[0].Status : 0;
    }

    private async Task<int> GetUserStatusPrefixMatchAsync(string userId, string url)
    {
        var now = DateTime.UtcNow;

        var entries = await _db.ACL_User
            .Where(u => u.UserId == userId
                     && u.UrlPrefix != null
                     && url.StartsWith(u.UrlPrefix)
                     && (u.ExpireDate == null || u.ExpireDate > now))
            .OrderByDescending(u => u.UrlPrefix.Length)
            .ToListAsync();

        return entries.Count > 0 ? entries[0].Status : 0;
    }
}
