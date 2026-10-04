// Path: Skyscreen.Server/Program.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Skyscreen.Core.Models;
using Skyscreen.Core.Protocol;
using Skyscreen.Server;
using Skyscreen.Server.Services;
using Skyscreen.Server.Transport;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ClientSessionManager>();

builder.Services.AddSingleton<
    ISkyscreenMessageSerializer,
    JsonSkyscreenMessageSerializer>();

builder.Services.AddSingleton<
    WebSocketClientConnectionListener>();

builder.Services.AddSingleton<IClientConnectionListener>(
    serviceProvider =>
        serviceProvider.GetRequiredService<
            WebSocketClientConnectionListener>());

builder.Services.AddHostedService<ClientConnectionService>();
builder.Services.AddHostedService<Worker>();

string webSocketPath =
    builder.Configuration["Skyscreen:WebSocket:Path"]
    ?? "/ws";

WebApplication app = builder.Build();

app.UseWebSockets();

app.Map(
    webSocketPath,
    async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return;
        }

        WebSocketClientConnectionListener connectionListener =
            context.RequestServices.GetRequiredService<
                WebSocketClientConnectionListener>();

        IHostApplicationLifetime applicationLifetime =
            context.RequestServices.GetRequiredService<
                IHostApplicationLifetime>();

        using System.Net.WebSockets.WebSocket webSocket =
            await context.WebSockets.AcceptWebSocketAsync();

        // En aktiv WebSocket-request får inte blockera serverns
        // kontrollerade nedstängning. När hosten börjar stoppa
        // avbryts därför transporten direkt så att serverns
        // anslutningshantering kan avslutas utan att invänta
        // Kestrels shutdown-timeout.
        using CancellationTokenRegistration stoppingRegistration =
            applicationLifetime.ApplicationStopping.Register(
                webSocket.Abort);

        IClientConnection connection =
            await connectionListener.RegisterAsync(
                webSocket,
                ClientConnectionType.Wifi,
                context.RequestAborted);

        await connection.Completion;
    });

app.Run();