// Path: Skyscreen.Server/Streaming/WebSocketVideoStreamHandshakeReceiver.cs

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Skyscreen.Core.Protocol;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Läser det första handshake-meddelandet från en
/// separat video-WebSocket.
///
/// Klassen ansvarar endast för WebSocket-mottagning och
/// deserialisering. Validering mot serverns aktuella
/// klientsession och panelprenumeration görs separat.
/// </summary>
public sealed class WebSocketVideoStreamHandshakeReceiver
{
    private const int ReceiveBufferSize = 4096;
    private const int MaxHandshakeBytes = 16 * 1024;

    private readonly JsonVideoStreamHandshakeSerializer _serializer;

    public WebSocketVideoStreamHandshakeReceiver(
        JsonVideoStreamHandshakeSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        _serializer = serializer;
    }

    /// <summary>
    /// Läser exakt ett textmeddelande från video-WebSocketen
    /// och tolkar det som en VideoStreamHandshake.
    /// </summary>
    public async Task<VideoStreamHandshake> ReceiveAsync(
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(webSocket);

        if (webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException(
                "Video-WebSocketen är inte öppen.");
        }

        byte[] buffer =
            new byte[ReceiveBufferSize];

        using MemoryStream messageStream =
            new();

        while (true)
        {
            WebSocketReceiveResult result =
                await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

            if (result.MessageType ==
                WebSocketMessageType.Close)
            {
                throw new InvalidOperationException(
                    "Videoanslutningen stängdes innan handshaken togs emot.");
            }

            if (result.MessageType !=
                WebSocketMessageType.Text)
            {
                throw new InvalidOperationException(
                    "Det första meddelandet på videoanslutningen måste vara en textbaserad JSON-handshake.");
            }

            if (messageStream.Length + result.Count >
                MaxHandshakeBytes)
            {
                throw new InvalidOperationException(
                    $"Video-handshaken får inte överstiga {MaxHandshakeBytes} byte.");
            }

            await messageStream.WriteAsync(
                buffer.AsMemory(
                    0,
                    result.Count),
                cancellationToken);

            if (result.EndOfMessage)
            {
                break;
            }
        }

        if (messageStream.Length == 0)
        {
            throw new InvalidOperationException(
                "Video-handshaken är tom.");
        }

        string json =
            Encoding.UTF8.GetString(
                messageStream.ToArray());

        try
        {
            return _serializer.Deserialize(
                json);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Video-handshaken innehåller ogiltig JSON.",
                exception);
        }
    }
}