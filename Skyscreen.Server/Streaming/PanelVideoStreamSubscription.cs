// Path: Skyscreen.Server/Streaming/PanelVideoStreamSubscription.cs

using System.Threading.Channels;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Representerar en enskild mottagare av kodade videoframes
/// från en logisk Skyscreen-panel.
///
/// Varje prenumeration har en egen bounded channel med kapacitet
/// för en frame. Om mottagaren inte hinner konsumera föregående
/// frame ersätts den av den nyaste för att undvika växande latency.
/// </summary>
public sealed class PanelVideoStreamSubscription
    : IAsyncDisposable
{
    private readonly Channel<EncodedPanelFrame> _channel;
    private readonly Func<ValueTask> _unsubscribeAsync;

    private int _disposed;

    internal PanelVideoStreamSubscription(
        string subscriptionId,
        string moduleId,
        string panelId,
        Func<ValueTask> unsubscribeAsync)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException(
                "SubscriptionId får inte vara tomt.",
                nameof(subscriptionId));
        }

        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new ArgumentException(
                "ModuleId får inte vara tomt.",
                nameof(moduleId));
        }

        if (string.IsNullOrWhiteSpace(panelId))
        {
            throw new ArgumentException(
                "PanelId får inte vara tomt.",
                nameof(panelId));
        }

        ArgumentNullException.ThrowIfNull(unsubscribeAsync);

        SubscriptionId = subscriptionId;
        ModuleId = moduleId;
        PanelId = panelId;

        _unsubscribeAsync = unsubscribeAsync;

        _channel =
            Channel.CreateBounded<EncodedPanelFrame>(
                new BoundedChannelOptions(1)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest,
                    AllowSynchronousContinuations = false
                });
    }

    /// <summary>
    /// ID för panelprenumerationen som videoanslutningen
    /// är bunden till.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Logiskt modul-ID.
    /// </summary>
    public string ModuleId { get; }

    /// <summary>
    /// Logiskt panel-ID.
    /// </summary>
    public string PanelId { get; }

    /// <summary>
    /// Ger mottagaren åtkomst till videoframes.
    /// </summary>
    public ChannelReader<EncodedPanelFrame> Frames =>
        _channel.Reader;

    /// <summary>
    /// Försöker publicera den senaste framen till denna mottagare.
    ///
    /// Vid full kö kastas föregående oskickade frame bort enligt
    /// DropOldest-policy så att mottagaren alltid ligger så nära
    /// realtid som möjligt.
    /// </summary>
    internal bool TryPublish(
        EncodedPanelFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Volatile.Read(ref _disposed) != 0)
        {
            return false;
        }

        return _channel.Writer.TryWrite(frame);
    }

    /// <summary>
    /// Avslutar prenumerationen och kopplar bort den från
    /// den gemensamma panelströmmen.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(
                ref _disposed,
                1) != 0)
        {
            return;
        }

        _channel.Writer.TryComplete();

        await _unsubscribeAsync();
    }
}