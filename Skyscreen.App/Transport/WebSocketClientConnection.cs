// Path: Skyscreen.App/Transport/WebSocketClientConnection.cs

using System.Net.WebSockets;
using System.Text;
using Skyscreen.Core.Protocol;

namespace Skyscreen.App.Transport;

/// <summary>
/// Hanterar en etablerad WebSocket-anslutning från Skyscreen.App
/// till Skyscreen.Server.
///
/// Klassen ansvarar endast för WebSocket-transporten och använder den
/// gemensamma protokollserialiseraren för logiska Skyscreen-meddelanden.
/// </summary>
public sealed class WebSocketClientConnection : IClientConnection
{
    private const int ReceiveBufferSize = 4096;

    private readonly ClientWebSocket _webSocket;
    private readonly ISkyscreenMessageSerializer _serializer;

    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Skapar en WebSocket-baserad anslutning som redan har etablerats
    /// mot Skyscreen.Server.
    /// </summary>
    public WebSocketClientConnection(
        ClientWebSocket webSocket,
        ISkyscreenMessageSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(webSocket);
        ArgumentNullException.ThrowIfNull(serializer);

        _webSocket = webSocket;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public bool IsConnected =>
        _webSocket.State == WebSocketState.Open;

    /// <inheritdoc />
    public Task Completion => _completion.Task;

    /// <inheritdoc />
    public async Task<SkyscreenMessage?> ReceiveAsync(
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[ReceiveBufferSize];

        using MemoryStream messageStream = new();

        try
        {
            while (true)
            {
                WebSocketReceiveResult result =
                    await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    throw new InvalidOperationException(
                        "Skyscreen styrprotokoll accepterar endast WebSocket-textmeddelanden.");
                }

                await messageStream.WriteAsync(
                    buffer.AsMemory(0, result.Count),
                    cancellationToken);

                if (result.EndOfMessage)
                {
                    break;
                }
            }
        }
        catch (WebSocketException exception)
        {
            throw new ClientConnectionTransportException(
                "WebSocket-anslutningen kunde inte ta emot data.",
                exception);
        }

        string data = Encoding.UTF8.GetString(
            messageStream.ToArray());

        return _serializer.Deserialize(data);
    }

    /// <inheritdoc />
    public async Task SendAsync(
        SkyscreenMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException(
                "WebSocket-anslutningen är inte öppen.");
        }

        string data = _serializer.Serialize(message);
        byte[] payload = Encoding.UTF8.GetBytes(data);

        try
        {
            await _webSocket.SendAsync(
                new ArraySegment<byte>(payload),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);
        }
        catch (WebSocketException exception)
        {
            throw new ClientConnectionTransportException(
                "WebSocket-anslutningen kunde inte skicka data.",
                exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_webSocket.State is WebSocketState.Open
                or WebSocketState.CloseReceived)
            {
                try
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Skyscreen-klienten avslutar anslutningen.",
                        CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    // Anslutningen kan redan ha brutits på transportnivå.
                }
            }
        }
        finally
        {
            _webSocket.Dispose();

            // Completion signaleras först när transportens hela
            // nedstängning och close-handshake är färdig.
            _completion.TrySetResult(true);
        }
    }
}