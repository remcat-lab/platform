using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// === 1. 콘솔 인자로 포트 추출 ===
var port = 5000; // default
var portArg = args.FirstOrDefault(arg => arg.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg.Split('=')[1], out var parsedPort))
{
    port = parsedPort;
}

var builder = WebApplication.CreateBuilder(args);

// === 2. Kestrel: HTTP/1.1 + 포트 지정 ===
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

// === 3. ResponseCompression 등록 ===
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/memorypack",
        "application/octet-stream"
    });
});
builder.Services.Configure<GzipCompressionProviderOptions>(opts =>
{
    opts.Level = CompressionLevel.Fastest;
});

// === 4. CORS 설정 ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            return origin == "http://localhost" ||
                   Regex.IsMatch(origin, @"^http:\/\/.*\.domain\.com$");
        })
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

// === 5. API 라우팅 Dictionary 정의 ===
var apiByUrl = new Dictionary<string, IApiHandler>(StringComparer.OrdinalIgnoreCase)
{
    ["/api/hello"] = new HelloApiHandler()
};

// === 6. Middleware pipeline 구성 ===
app.UseCors("DefaultCors");
app.UseResponseCompression();

// RequestBody Size 설정
app.Use(async (context, next) =>
{
    var maxSizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (maxSizeFeature != null && maxSizeFeature.IsReadOnly == false)
        maxSizeFeature.MaxRequestBodySize = 1_073_741_824; // 1GB

    await next();
});

// 라우팅 처리
app.UseMiddleware<ApiRouterMiddleware>(apiByUrl);

// fallback
app.Run(async context =>
{
    context.Response.StatusCode = 404;
    await context.Response.WriteAsync("Unknown endpoint.");
});

app.Run();
