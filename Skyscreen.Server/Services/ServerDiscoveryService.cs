// Path: Skyscreen.Server/Services/ServerDiscoveryService.cs

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Skyscreen.Core.Discovery;

namespace Skyscreen.Server.Services;

/// <summary>
/// Lyssnar efter UDP-baserade discovery-anrop från Skyscreen.App
/// och svarar klienten med information om serverns WebSocket-endpoint.
/// </summary>
public sealed class ServerDiscoveryService : BackgroundService
{
    private readonly ILogger<ServerDiscoveryService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServerDiscoveryMessageSerializer _serializer;

    /// <summary>
    /// Skapar serverns discovery-tjänst.
    /// </summary>
    public ServerDiscoveryService(
        ILogger<ServerDiscoveryService> logger,
        IConfiguration configuration,
        IServerDiscoveryMessageSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serializer);

        _logger = logger;
        _configuration = configuration;
        _serializer = serializer;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        int webSocketPort = GetWebSocketPort();
        string webSocketPath = GetWebSocketPath();

        using UdpClient udpClient =
            new(
                new IPEndPoint(
                    IPAddress.Any,
                    SkyscreenDiscoveryProtocol.DiscoveryPort));

        _logger.LogInformation(
            "Skyscreen discovery startad på UDP-port {DiscoveryPort}. WebSocket-endpoint: port {WebSocketPort}, path {WebSocketPath}",
            SkyscreenDiscoveryProtocol.DiscoveryPort,
            webSocketPort,
            webSocketPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult received =
                    await udpClient.ReceiveAsync(
                        stoppingToken);

                string data =
                    Encoding.UTF8.GetString(
                        received.Buffer);

                ServerDiscoveryRequest request =
                    _serializer.DeserializeRequest(data);

                ServerDiscoveryResponse response = new()
                {
                    RequestId = request.RequestId,
                    WebSocketPort = webSocketPort,
                    WebSocketPath = webSocketPath
                };

                string responseData =
                    _serializer.SerializeResponse(
                        response);

                byte[] responseBytes =
                    Encoding.UTF8.GetBytes(
                        responseData);

                await udpClient.SendAsync(
                    responseBytes,
                    received.RemoteEndPoint,
                    stoppingToken);

                _logger.LogDebug(
                    "Discovery-svar skickat till {RemoteEndPoint}. RequestId: {RequestId}",
                    received.RemoteEndPoint,
                    request.RequestId);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (JsonException exception)
            {
                _logger.LogDebug(
                    exception,
                    "Ogiltigt discovery-meddelande ignorerades.");
            }
            catch (SocketException exception)
                when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Nätverksfel i Skyscreen discovery.");
            }
            catch (Exception exception)
                when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(
                    exception,
                    "Oväntat fel i Skyscreen discovery.");
            }
        }
    }

    /// <summary>
    /// Hämtar WebSocket-porten från Kestrels serverkonfiguration.
    /// </summary>
    private int GetWebSocketPort()
    {
        string? endpointUrl =
            _configuration[
                "Kestrel:Endpoints:Skyscreen:Url"];

        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            throw new InvalidOperationException(
                "Kestrels Skyscreen-endpoint saknas i konfigurationen.");
        }

        if (!Uri.TryCreate(
                endpointUrl,
                UriKind.Absolute,
                out Uri? endpointUri))
        {
            throw new InvalidOperationException(
                $"Ogiltig Kestrel-endpoint: '{endpointUrl}'.");
        }

        if (endpointUri.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "Kestrels Skyscreen-endpoint innehåller en ogiltig port.");
        }

        return endpointUri.Port;
    }

    /// <summary>
    /// Hämtar WebSocket-sökvägen från serverkonfigurationen.
    /// </summary>
    private string GetWebSocketPath()
    {
        string webSocketPath =
            _configuration[
                "Skyscreen:WebSocket:Path"]
            ?? "/ws";

        if (string.IsNullOrWhiteSpace(webSocketPath) ||
            !webSocketPath.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Ogiltig WebSocket-sökväg: '{webSocketPath}'.");
        }

        return webSocketPath;
    }
}