// Path: Skyscreen.App/Transport/WebSocketClientConnectionFactory.cs

using System.Net.WebSockets;
using Skyscreen.Core.Protocol;

namespace Skyscreen.App.Transport;

/// <summary>
/// Skapar WebSocket-baserade anslutningar från Skyscreen.App
/// till Skyscreen.Server.
/// </summary>
public sealed class WebSocketClientConnectionFactory
    : IClientConnectionFactory
{
    private static readonly TimeSpan ConnectionTimeout =
        TimeSpan.FromSeconds(3);

    private readonly ISkyscreenMessageSerializer _serializer;

    /// <summary>
    /// Skapar fabriken med Skyscreens gemensamma
    /// protokollserialiserare.
    /// </summary>
    public WebSocketClientConnectionFactory(
        ISkyscreenMessageSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task<IClientConnection> ConnectAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (endpoint.Scheme != Uri.UriSchemeWs
            && endpoint.Scheme != Uri.UriSchemeWss)
        {
            throw new ArgumentException(
                "Serverendpointen måste använda ws eller wss.",
                nameof(endpoint));
        }

        ClientWebSocket webSocket = new();

        using CancellationTokenSource connectionCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        connectionCancellation.CancelAfter(
            ConnectionTimeout);

        try
        {
            await webSocket.ConnectAsync(
                endpoint,
                connectionCancellation.Token);

            return new WebSocketClientConnection(
                webSocket,
                _serializer);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested
                  && connectionCancellation.IsCancellationRequested)
        {
            webSocket.Dispose();

            throw new ClientConnectionTransportException(
                $"WebSocket-anslutningen till '{endpoint}' överskred tidsgränsen på {ConnectionTimeout.TotalSeconds:0} sekunder.",
                exception);
        }
        catch (WebSocketException exception)
        {
            webSocket.Dispose();

            throw new ClientConnectionTransportException(
                $"WebSocket-anslutningen till '{endpoint}' kunde inte etableras.",
                exception);
        }
        catch
        {
            webSocket.Dispose();
            throw;
        }
    }
}