// Path: Skyscreen.Server/Streaming/WebSocketPanelVideoStreamer.cs

using System.Net.WebSockets;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Strömmar kodade panelbilder över en etablerad
/// video-WebSocket.
///
/// Varje EncodedPanelFrame skickas som exakt ett binärt
/// WebSocket-meddelande.
///
/// Samtidigt övervakas inkommande trafik så att en normal
/// WebSocket-close från klienten upptäcks omedelbart.
/// Efter handshaken får klienten inte skicka ytterligare
/// text- eller binärdata på videoanslutningen.
/// </summary>
public sealed class WebSocketPanelVideoStreamer
{
    private const int ReceiveBufferSize = 1024;

    private static readonly TimeSpan CloseTimeout =
        TimeSpan.FromSeconds(2);

    /// <summary>
    /// Strömmar frames tills klienten stänger anslutningen,
    /// servern avbryter operationen eller ett transportfel inträffar.
    /// </summary>
    public async Task StreamAsync(
        WebSocket webSocket,
        PanelVideoStreamSubscription subscription,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(webSocket);
        ArgumentNullException.ThrowIfNull(subscription);

        if (webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException(
                "Video-WebSocketen är inte öppen.");
        }

        using CancellationTokenSource streamCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        Task sendTask =
            SendFramesAsync(
                webSocket,
                subscription,
                streamCancellation.Token);

        Task<ClientCloseInfo> receiveTask =
            WaitForClientCloseAsync(
                webSocket,
                streamCancellation.Token);

        Task completedTask =
            await Task.WhenAny(
                sendTask,
                receiveTask);

        if (completedTask == receiveTask)
        {
            ClientCloseInfo closeInfo;

            try
            {
                closeInfo =
                    await receiveTask;
            }
            catch
            {
                streamCancellation.Cancel();

                await IgnoreCancellationAsync(
                    sendTask,
                    streamCancellation.Token);

                throw;
            }

            streamCancellation.Cancel();

            await IgnoreCancellationAsync(
                sendTask,
                streamCancellation.Token);

            if (webSocket.State ==
                WebSocketState.CloseReceived)
            {
                using CancellationTokenSource closeCancellation =
                    new(CloseTimeout);

                await webSocket.CloseOutputAsync(
                    closeInfo.CloseStatus,
                    closeInfo.CloseDescription,
                    closeCancellation.Token);
            }

            return;
        }

        try
        {
            await sendTask;
        }
        finally
        {
            streamCancellation.Cancel();

            await IgnoreCancellationAsync(
                receiveTask,
                streamCancellation.Token);
        }
    }

    /// <summary>
    /// Skickar den senaste kodade framen till klienten.
    /// Varje frame skickas som ett komplett binärt
    /// WebSocket-meddelande.
    /// </summary>
    private static async Task SendFramesAsync(
        WebSocket webSocket,
        PanelVideoStreamSubscription subscription,
        CancellationToken cancellationToken)
    {
        await foreach (
            EncodedPanelFrame frame
            in subscription.Frames.ReadAllAsync(
                cancellationToken))
        {
            if (webSocket.State != WebSocketState.Open)
            {
                return;
            }

            await webSocket.SendAsync(
                frame.Data.AsMemory(),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                cancellationToken);
        }
    }

    /// <summary>
    /// Väntar på att klienten skickar WebSocket-close.
    ///
    /// Efter den initiala video-handshaken är videoanslutningen
    /// enkelriktad från server till klient. Ytterligare text- eller
    /// binärmeddelanden från klienten betraktas därför som
    /// protokollfel.
    /// </summary>
    private static async Task<ClientCloseInfo>
        WaitForClientCloseAsync(
            WebSocket webSocket,
            CancellationToken cancellationToken)
    {
        byte[] buffer =
            new byte[ReceiveBufferSize];

        while (true)
        {
            WebSocketReceiveResult result =
                await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

            if (result.MessageType ==
                WebSocketMessageType.Close)
            {
                return new ClientCloseInfo(
                    result.CloseStatus ??
                    WebSocketCloseStatus.NormalClosure,
                    result.CloseStatusDescription);
            }

            throw new InvalidOperationException(
                "Videoanslutningen accepterar ingen klientdata efter handshaken.");
        }
    }

    /// <summary>
    /// Väntar in en task som förväntas avbrytas när
    /// videoanslutningens gemensamma cancellation-token stoppas.
    /// </summary>
    private static async Task IgnoreCancellationAsync(
        Task task,
        CancellationToken cancellationToken)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Förväntad avbrytning när den andra halvan
            // av videoanslutningen redan har avslutats.
        }
    }

    /// <summary>
    /// Information från klientens WebSocket-close.
    /// </summary>
    private sealed record ClientCloseInfo(
        WebSocketCloseStatus CloseStatus,
        string? CloseDescription);
}