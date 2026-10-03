// Path: Skyscreen.Server/Transport/WebSocketClientConnectionListener.cs

using System.Net.WebSockets;
using System.Threading.Channels;
using Skyscreen.Core.Models;
using Skyscreen.Core.Protocol;

namespace Skyscreen.Server.Transport;

/// <summary>
/// Köar etablerade WebSocket-anslutningar så att serverns övriga
/// logik kan ta emot dem via den transportoberoende
/// IClientConnectionListener-abstraktionen.
/// </summary>
public sealed class WebSocketClientConnectionListener
    : IClientConnectionListener
{
    private readonly ISkyscreenMessageSerializer _serializer;

    private readonly Channel<IClientConnection> _connections =
        Channel.CreateUnbounded<IClientConnection>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });

    private readonly object _syncRoot = new();

    private bool _disposed;

    /// <summary>
    /// Skapar en WebSocket-baserad anslutningslyssnare.
    /// </summary>
    public WebSocketClientConnectionListener(
        ISkyscreenMessageSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        _serializer = serializer;
    }

    /// <summary>
    /// Registrerar en redan etablerad WebSocket-anslutning
    /// så att den kan hämtas via AcceptAsync.
    ///
    /// Returnerar den skapade transportoberoende anslutningen
    /// så att hosten kan följa dess livscykel.
    /// </summary>
    public Task<IClientConnection> RegisterAsync(
        WebSocket webSocket,
        ClientConnectionType connectionType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(webSocket);

        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(
                _disposed,
                this);

            string connectionId =
                Guid.NewGuid().ToString("N");

            WebSocketClientConnection connection = new(
                connectionId,
                webSocket,
                _serializer,
                connectionType);

            if (!_connections.Writer.TryWrite(connection))
            {
                throw new InvalidOperationException(
                    "WebSocket-anslutningen kunde inte registreras.");
            }

            return Task.FromResult<IClientConnection>(
                connection);
        }
    }

    /// <inheritdoc />
    public async Task<IClientConnection?> AcceptAsync(
        CancellationToken cancellationToken)
    {
        while (await _connections.Reader.WaitToReadAsync(
                   cancellationToken))
        {
            if (_connections.Reader.TryRead(
                    out IClientConnection? connection))
            {
                return connection;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _connections.Writer.TryComplete();
        }

        while (_connections.Reader.TryRead(
                   out IClientConnection? connection))
        {
            await connection.DisposeAsync();
        }
    }
}