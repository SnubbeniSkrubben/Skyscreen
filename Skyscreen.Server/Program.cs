// Path: Skyscreen.Server/Program.cs

using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skyscreen.Core.Discovery;
using Skyscreen.Core.Models;
using Skyscreen.Core.Protocol;
using Skyscreen.Server;
using Skyscreen.Server.Capture;
using Skyscreen.Server.Services;
using Skyscreen.Server.Streaming;
using Skyscreen.Server.Transport;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ClientSessionManager>();

builder.Services.AddSingleton<
    ISkyscreenMessageSerializer,
    JsonSkyscreenMessageSerializer>();

builder.Services.AddSingleton<
    IServerDiscoveryMessageSerializer,
    JsonServerDiscoveryMessageSerializer>();

builder.Services.AddSingleton<
    JsonVideoStreamHandshakeSerializer>();

builder.Services.Configure<PanelCaptureOptions>(
    builder.Configuration.GetSection(
        PanelCaptureOptions.SectionName));

builder.Services.Configure<PanelCaptureDiagnosticOptions>(
    builder.Configuration.GetSection(
        PanelCaptureDiagnosticOptions.SectionName));

builder.Services.Configure<JpegPanelFrameEncoderOptions>(
    builder.Configuration.GetSection(
        JpegPanelFrameEncoderOptions.SectionName));

builder.Services.Configure<PanelStreamingDiagnosticOptions>(
    builder.Configuration.GetSection(
        PanelStreamingDiagnosticOptions.SectionName));

builder.Services.Configure<PanelVideoStreamOptions>(
    builder.Configuration.GetSection(
        PanelVideoStreamOptions.SectionName));

builder.Services.AddSingleton<
    IPanelCaptureRegionProvider,
    ConfiguredPanelCaptureRegionProvider>();

builder.Services.AddSingleton<
    IPanelCaptureSource,
    WindowsGdiPanelCaptureSource>();

builder.Services.AddSingleton<
    IPanelCaptureService,
    PanelCaptureService>();

builder.Services.AddSingleton<
    IPanelFrameEncoder,
    SkiaJpegPanelFrameEncoder>();

builder.Services.AddSingleton<
    VideoStreamHandshakeValidator>();

builder.Services.AddSingleton<
    WebSocketVideoStreamHandshakeReceiver>();

builder.Services.AddSingleton<
    PanelVideoStreamManager>();

builder.Services.AddSingleton<
    WebSocketPanelVideoStreamer>();

builder.Services.AddSingleton<
    WebSocketClientConnectionListener>();

builder.Services.AddSingleton<IClientConnectionListener>(
    serviceProvider =>
        serviceProvider.GetRequiredService<
            WebSocketClientConnectionListener>());

builder.Services.AddHostedService<ClientConnectionService>();
builder.Services.AddHostedService<ServerDiscoveryService>();
builder.Services.AddHostedService<PanelCaptureDiagnosticService>();
builder.Services.AddHostedService<PanelStreamingDiagnosticService>();
builder.Services.AddHostedService<Worker>();

string webSocketPath =
    builder.Configuration["Skyscreen:WebSocket:Path"]
    ?? "/ws";

string videoWebSocketPath =
    builder.Configuration["Skyscreen:VideoWebSocket:Path"]
    ?? "/video";

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

        using WebSocket webSocket =
            await context.WebSockets.AcceptWebSocketAsync();

        // En aktiv WebSocket-request får inte blockera serverns
        // kontrollerade nedstängning.
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

app.Map(
    videoWebSocketPath,
    async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return;
        }

        WebSocketVideoStreamHandshakeReceiver handshakeReceiver =
            context.RequestServices.GetRequiredService<
                WebSocketVideoStreamHandshakeReceiver>();

        VideoStreamHandshakeValidator handshakeValidator =
            context.RequestServices.GetRequiredService<
                VideoStreamHandshakeValidator>();

        PanelVideoStreamManager videoStreamManager =
            context.RequestServices.GetRequiredService<
                PanelVideoStreamManager>();

        WebSocketPanelVideoStreamer videoStreamer =
            context.RequestServices.GetRequiredService<
                WebSocketPanelVideoStreamer>();

        IHostApplicationLifetime applicationLifetime =
            context.RequestServices.GetRequiredService<
                IHostApplicationLifetime>();

        ILoggerFactory loggerFactory =
            context.RequestServices.GetRequiredService<
                ILoggerFactory>();

        ILogger logger =
            loggerFactory.CreateLogger(
                "Skyscreen.Server.VideoStream");

        using WebSocket webSocket =
            await context.WebSockets.AcceptWebSocketAsync();

        using CancellationTokenRegistration stoppingRegistration =
            applicationLifetime.ApplicationStopping.Register(
                webSocket.Abort);

        try
        {
            VideoStreamHandshake handshake =
                await handshakeReceiver.ReceiveAsync(
                    webSocket,
                    context.RequestAborted);

            VideoStreamHandshakeValidationResult validationResult =
                handshakeValidator.Validate(
                    handshake);

            if (!validationResult.IsValid)
            {
                logger.LogWarning(
                    "Video-handshake nekades. Orsak: {FailureReason}",
                    validationResult.FailureReason);

                using CancellationTokenSource rejectedCloseCancellation =
                    new(TimeSpan.FromSeconds(2));

                await webSocket.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    "Video-handshaken godkändes inte.",
                    rejectedCloseCancellation.Token);

                return;
            }

            logger.LogInformation(
                "Video-handshake godkänd. ClientId: {ClientId}, SubscriptionId: {SubscriptionId}, ModuleId: {ModuleId}, PanelId: {PanelId}",
                handshake.ClientId,
                handshake.SubscriptionId,
                handshake.ModuleId,
                handshake.PanelId);

            await using PanelVideoStreamSubscription videoSubscription =
                videoStreamManager.Subscribe(
                    handshake.SubscriptionId,
                    handshake.ModuleId,
                    handshake.PanelId);

            await videoStreamer.StreamAsync(
                webSocket,
                videoSubscription,
                context.RequestAborted);

            logger.LogInformation(
                "Videoanslutning avslutad. ClientId: {ClientId}, SubscriptionId: {SubscriptionId}, ModuleId: {ModuleId}, PanelId: {PanelId}",
                handshake.ClientId,
                handshake.SubscriptionId,
                handshake.ModuleId,
                handshake.PanelId);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested ||
                  applicationLifetime.ApplicationStopping.IsCancellationRequested)
        {
            webSocket.Abort();
        }
        catch (WebSocketException exception)
            when (!applicationLifetime.ApplicationStopping.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Video-WebSocketen bröts på transportnivå.");

            webSocket.Abort();
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Videoanslutningen kunde inte behandlas.");

            if (webSocket.State == WebSocketState.Open)
            {
                using CancellationTokenSource errorCloseCancellation =
                    new(TimeSpan.FromSeconds(2));

                try
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.InvalidPayloadData,
                        "Videoanslutningen kunde inte behandlas.",
                        errorCloseCancellation.Token);
                }
                catch
                {
                    webSocket.Abort();
                }
            }
        }
    });

app.Run();