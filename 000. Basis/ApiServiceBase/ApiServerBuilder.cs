using ApiHost.Handlers;
using ApiHost.Middleware;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace ApiHost;

public static class ApiServerBuilder
{
    public static WebApplication Build(string[] args, int port)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Kestrel 설정 (HTTP/1.1 + 포트)
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1;
            });
        });

        // 2. ResponseCompression 설정
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

        // 3. CORS 설정
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultCors", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                    origin == "http://localhost" ||
                    Regex.IsMatch(origin, @"^http:\/\/.*\.domain\.com$"))
                .AllowAnyMethod()
                .AllowAnyHeader();
            });
        });

        // 4. app 생성
        var app = builder.Build();

        // 5. API 핸들러 DI 없이 Dictionary 생성
        var apiByUrl = new Dictionary<string, IApiHandler>(StringComparer.OrdinalIgnoreCase)
        {
            ["/api/hello"] = new HelloApiHandler()
            // 외부 프로젝트의 핸들러도 여기 등록 가능
        };

        // 6. Middleware 등록
        app.UseCors("DefaultCors");
        app.UseResponseCompression();

        // 1GB maxRequestBodySize 설정
        app.Use(async (context, next) =>
        {
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (feature != null && !feature.IsReadOnly)
                feature.MaxRequestBodySize = 1_073_741_824; // 1GB

            await next();
        });

        app.UseMiddleware<ApiRouterMiddleware>(apiByUrl);

        app.Run(async context =>
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Unknown endpoint.");
        });

        return app;
    }
}
