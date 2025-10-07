using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Metadata;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var htmlDocs = HtmlDocumentationGenerator.GenerateHtml();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
app.UseWebSockets(webSocketOptions);

app.MapGet("/health", async context =>
{
    context.Response.StatusCode = 200;
    await context.Response.WriteAsync("OK");
});

app.Map("/", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine($"WebSocket client connected from {context.Connection.RemoteIpAddress}");
        
        var handler = new WebSocketHandler(webSocket);
        await handler.HandleConnectionAsync();
    }
    else if (context.Request.Method == "GET")
    {
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(htmlDocs);
    }
    else if (context.Request.Method == "OPTIONS")
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(SchemaBuilder.GenerateJsonSchema());
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket connections only");
    }
});

string bindAddress;
if (args.Length > 0)
{
    var arg = args[0];
    if (arg.StartsWith("http://") || arg.StartsWith("https://"))
    {
        bindAddress = arg;
    }
    else if (File.Exists(arg) || arg.EndsWith(".sock") || arg.StartsWith("/"))
    {
        bindAddress = $"unix:{arg}";
    }
    else
    {
        bindAddress = arg;
    }
}
else
{
    bindAddress = "http://127.0.0.1:3030";
}

app.Urls.Add(bindAddress);

Console.WriteLine($"PKHeX WebSocket Server started on {bindAddress}");
Console.WriteLine("Waiting for WebSocket connections...");

app.Run();
