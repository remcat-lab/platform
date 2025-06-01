public class ValidatePermissionHandler : IApiHandler
{
    private const int DENY = 0b001;
    private const int ALLOW = 0b010;
    private const int DEFAULT_DENY = 0b100;

    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        Params? parameters = null;
        try
        {
            parameters = MemoryPackSerializer.Deserialize<Params>(request.Body);
        }
        catch
        {
            return new ApiResponse
            {
                StatusCode = 400,
                Content = JsonSerializer.Serialize(new { error = "Invalid request body format." })
            };
        }

        if (parameters == null ||
            string.IsNullOrEmpty(parameters.UserId) ||
            string.IsNullOrEmpty(parameters.DepartmentId) ||
            string.IsNullOrEmpty(parameters.Url))
        {
            return new ApiResponse
            {
                StatusCode = 400,
                Content = JsonSerializer.Serialize(new { error = "Missing required parameters." })
            };
        }

        int deptStatus = await GetDeptStatusPrefixMatchAsync(parameters.DepartmentId, parameters.Url);
        int userStatus = await GetUserStatusPrefixMatchAsync(parameters.UserId, parameters.Url);

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
        const string sql = @"
            SELECT TOP 1 Status
            FROM ACL_Department
            WHERE DepartmentId = @DepartmentId
              AND UrlPrefix IS NOT NULL
              AND @Url LIKE UrlPrefix + '%'
              AND (ExpireDate IS NULL OR ExpireDate > @Now)
            ORDER BY LEN(UrlPrefix) DESC";

        await using var connection = Setting.GetConnection("MainDb"); // 여기서 connection 생성
        await connection.OpenAsync();

        var status = await connection.QueryFirstOrDefaultAsync<int?>(sql, new { DepartmentId = departmentId, Url = url, Now = DateTime.UtcNow });
        return status ?? 0;
    }

    private async Task<int> GetUserStatusPrefixMatchAsync(string userId, string url)
    {
        const string sql = @"
            SELECT TOP 1 Status
            FROM ACL_User
            WHERE UserId = @UserId
              AND UrlPrefix IS NOT NULL
              AND @Url LIKE UrlPrefix + '%'
              AND (ExpireDate IS NULL OR ExpireDate > @Now)
            ORDER BY LEN(UrlPrefix) DESC";

        await using var connection = Setting.GetConnection("MainDb"); // 여기서 connection 생성
        await connection.OpenAsync();

        var status = await connection.QueryFirstOrDefaultAsync<int?>(sql, new { UserId = userId, Url = url, Now = DateTime.UtcNow });
        return status ?? 0;
    }
}
