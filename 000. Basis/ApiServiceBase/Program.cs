using ApiHost;

var port = 5000;
var portArg = args.FirstOrDefault(a => a.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg.Split('=')[1], out var parsed))
    port = parsed;

var app = ApiServerBuilder.Build(args, port);
app.Run();
