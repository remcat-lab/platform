using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ApiServer;

public static class ApiServerBuilder
{
    public static void Run(string[] args, params Assembly[] apiHandlerAssemblies)
    {
        int port = 5000;
        var portArg = args.FirstOrDefault(a => a.StartsWith("--port"));
        if (portArg != null && int.TryParse(portArg.Split('=').LastOrDefault(), out var parsedPort))
        {
            port = parsedPort;
        }

        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 1L * 1024 * 1024 * 1024;
            options.Listen(IPAddress.Any, port, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost", "http://127.0.0.1")
                      .SetIsOriginAllowed(origin => Regex.IsMatch(origin, @"^http://.*\.domain\.com(:\d+)?$") || origin.StartsWith("http://localhost"))
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/x-memorypack",
                "application/octet-stream"
            });
        });
        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        var app = builder.Build();

        app.UseCors();
        app.UseResponseCompression();

        var apiMap = ApiHandlerScanner.DiscoverApiHandlers(apiHandlerAssemblies);

        app.UseMiddleware<ApiRouterMiddleware>(apiMap);

        app.Run();
    }
}