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
        // 콘솔 인자로 포트 받기 (예: --port 5001)
        int port = 5000;
        var portArg = args.FirstOrDefault(a => a.StartsWith("--port"));
        if (portArg != null && int.TryParse(portArg.Split('=').LastOrDefault(), out var parsedPort))
        {
            port = parsedPort;
        }

        var builder = WebApplication.CreateBuilder(args);

        // Configure Kestrel
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 1L * 1024 * 1024 * 1024; // 1GB
            options.Listen(IPAddress.Any, port, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
            });
        });

        // Add CORS
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

        // Add response compression
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

        // API Handler 등록
        var apiMap = DiscoverApiHandlers(app.Services, apiHandlerAssemblies);

        app.Use(async (context, next) =>
        {
            if (apiMap.TryGetValue(context.Request.Path, out var handler))
            {
                await handler.HandleAsync(context);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Not Found");
            }
        });

        app.Run();
    }

    public static Dictionary<PathString, IApiHandler> DiscoverApiHandlers(IServiceProvider services, Assembly[] assemblies)
    {
        var apiMap = new Dictionary<PathString, IApiHandler>();

        foreach (var assembly in assemblies)
        {
            var handlers = assembly
                .GetTypes()
                .Where(t => typeof(IApiHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => new
                {
                    Type = t,
                    Route = t.GetCustomAttribute<RouteAttribute>()?.Template
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Route));

            foreach (var handler in handlers)
            {
                if (!apiMap.ContainsKey(handler.Route))
                {
                    var instance = (IApiHandler)Activator.CreateInstance(handler.Type)!;
                    apiMap[handler.Route] = instance;
                }
            }
        }

        return apiMap;
    }
}
