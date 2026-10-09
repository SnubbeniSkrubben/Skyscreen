// Path: Skyscreen.Server/Streaming/PanelVideoStreamManager.cs

using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skyscreen.Server.Capture;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Hanterar gemensamma videoströmmar för logiska Skyscreen-paneler.
///
/// Exakt en capture- och kodningsloop körs per ModuleId + PanelId,
/// oavsett hur många klienter som prenumererar på samma panel.
///
/// Den färdigkodade bildrutan distribueras därefter till samtliga
/// aktuella videoprenumeranter för panelen.
/// </summary>
public sealed class PanelVideoStreamManager
{
    private const int MinimumFramesPerSecond = 1;
    private const int MaximumFramesPerSecond = 60;

    private static readonly TimeSpan CaptureUnavailableRetryDelay =
        TimeSpan.FromSeconds(1);

    private static readonly TimeSpan ProducerErrorRetryDelay =
        TimeSpan.FromSeconds(1);

    private readonly object _syncRoot = new();

    private readonly Dictionary<string, PanelStreamState> _streams =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IPanelCaptureService _captureService;
    private readonly IPanelFrameEncoder _frameEncoder;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<PanelVideoStreamManager> _logger;

    private readonly TimeSpan _frameInterval;

    public PanelVideoStreamManager(
        IPanelCaptureService captureService,
        IPanelFrameEncoder frameEncoder,
        IHostApplicationLifetime applicationLifetime,
        IOptions<PanelVideoStreamOptions> options,
        ILogger<PanelVideoStreamManager> logger)
    {
        ArgumentNullException.ThrowIfNull(captureService);
        ArgumentNullException.ThrowIfNull(frameEncoder);
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        int framesPerSecond =
            options.Value.FramesPerSecond;

        if (framesPerSecond is
            < MinimumFramesPerSecond or
            > MaximumFramesPerSecond)
        {
            throw new InvalidOperationException(
                $"{nameof(PanelVideoStreamOptions.FramesPerSecond)} " +
                $"måste vara mellan {MinimumFramesPerSecond} och " +
                $"{MaximumFramesPerSecond}.");
        }

        _captureService = captureService;
        _frameEncoder = frameEncoder;
        _applicationLifetime = applicationLifetime;
        _logger = logger;

        _frameInterval =
            TimeSpan.FromSeconds(
                1.0 / framesPerSecond);
    }

    /// <summary>
    /// Registrerar en ny mottagare för en logisk panel.
    ///
    /// Om detta är panelens första videoprenumerant startas
    /// panelens gemensamma capture- och kodningsloop.
    /// </summary>
    public PanelVideoStreamSubscription Subscribe(
        string subscriptionId,
        string moduleId,
        string panelId)
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

        string normalizedSubscriptionId =
            subscriptionId.Trim();

        string normalizedModuleId =
            moduleId.Trim();

        string normalizedPanelId =
            panelId.Trim();

        string streamKey =
            CreateStreamKey(
                normalizedModuleId,
                normalizedPanelId);

        string internalSubscriberId =
            Guid.NewGuid()
                .ToString("N");

        lock (_syncRoot)
        {
            if (!_streams.TryGetValue(
                    streamKey,
                    out PanelStreamState? state))
            {
                CancellationTokenSource cancellationSource =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        _applicationLifetime.ApplicationStopping);

                state = new PanelStreamState(
                    normalizedModuleId,
                    normalizedPanelId,
                    cancellationSource);

                _streams.Add(
                    streamKey,
                    state);

                PanelStreamState producerState =
                    state;

                CancellationToken producerCancellationToken =
                    cancellationSource.Token;

                state.ProducerTask =
                    Task.Run(
                        () => RunProducerAsync(
                            producerState,
                            producerCancellationToken),
                        CancellationToken.None);

                _logger.LogInformation(
                    "Gemensam panelvideoström startad. ModuleId: {ModuleId}, PanelId: {PanelId}",
                    normalizedModuleId,
                    normalizedPanelId);
            }

            PanelVideoStreamSubscription subscription =
                new(
                    normalizedSubscriptionId,
                    normalizedModuleId,
                    normalizedPanelId,
                    () => UnsubscribeAsync(
                        streamKey,
                        internalSubscriberId));

            state.Subscribers.Add(
                internalSubscriberId,
                subscription);

            _logger.LogInformation(
                "Videoprenumerant registrerad. ModuleId: {ModuleId}, PanelId: {PanelId}, SubscriptionId: {SubscriptionId}, Antal mottagare: {SubscriberCount}",
                normalizedModuleId,
                normalizedPanelId,
                normalizedSubscriptionId,
                state.Subscribers.Count);

            return subscription;
        }
    }

    /// <summary>
    /// Kör panelens gemensamma capture- och kodningsloop.
    /// </summary>
    private async Task RunProducerAsync(
        PanelStreamState state,
        CancellationToken cancellationToken)
    {
        bool captureUnavailableLogged =
            false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                long frameStartedTimestamp =
                    Stopwatch.GetTimestamp();

                try
                {
                    PanelCaptureFrame? capturedFrame =
                        await _captureService.CaptureAsync(
                            state.ModuleId,
                            state.PanelId,
                            cancellationToken);

                    if (capturedFrame is null)
                    {
                        if (!captureUnavailableLogged)
                        {
                            _logger.LogWarning(
                                "Ingen capture-region finns för panelvideoströmmen. ModuleId: {ModuleId}, PanelId: {PanelId}",
                                state.ModuleId,
                                state.PanelId);

                            captureUnavailableLogged =
                                true;
                        }

                        await Task.Delay(
                            CaptureUnavailableRetryDelay,
                            cancellationToken);

                        continue;
                    }

                    captureUnavailableLogged =
                        false;

                    EncodedPanelFrame encodedFrame =
                        await _frameEncoder.EncodeAsync(
                            capturedFrame,
                            cancellationToken);

                    PanelVideoStreamSubscription[] subscribers =
                        GetSubscribersSnapshot(
                            state);

                    if (subscribers.Length == 0)
                    {
                        break;
                    }

                    foreach (PanelVideoStreamSubscription subscriber
                             in subscribers)
                    {
                        subscriber.TryPublish(
                            encodedFrame);
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Fel i gemensam panelvideoström. ModuleId: {ModuleId}, PanelId: {PanelId}",
                        state.ModuleId,
                        state.PanelId);

                    await Task.Delay(
                        ProducerErrorRetryDelay,
                        cancellationToken);

                    continue;
                }

                TimeSpan elapsed =
                    Stopwatch.GetElapsedTime(
                        frameStartedTimestamp);

                TimeSpan remainingDelay =
                    _frameInterval - elapsed;

                if (remainingDelay > TimeSpan.Zero)
                {
                    await Task.Delay(
                        remainingDelay,
                        cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal avslutning när sista mottagaren kopplas bort
            // eller när servern stängs ned.
        }
        finally
        {
            _logger.LogInformation(
                "Gemensam panelvideoström avslutad. ModuleId: {ModuleId}, PanelId: {PanelId}",
                state.ModuleId,
                state.PanelId);
        }
    }

    /// <summary>
    /// Returnerar en ögonblicksbild av panelens aktuella
    /// videoprenumeranter.
    /// </summary>
    private PanelVideoStreamSubscription[]
        GetSubscribersSnapshot(
            PanelStreamState state)
    {
        lock (_syncRoot)
        {
            return state.Subscribers.Values
                .ToArray();
        }
    }

    /// <summary>
    /// Tar bort en videoprenumerant.
    ///
    /// När panelens sista mottagare försvinner stoppas även
    /// panelens gemensamma producent.
    /// </summary>
    private async ValueTask UnsubscribeAsync(
        string streamKey,
        string internalSubscriberId)
    {
        CancellationTokenSource? cancellationSource =
            null;

        Task? producerTask =
            null;

        string? moduleId =
            null;

        string? panelId =
            null;

        int remainingSubscriberCount =
            0;

        lock (_syncRoot)
        {
            if (!_streams.TryGetValue(
                    streamKey,
                    out PanelStreamState? state))
            {
                return;
            }

            if (!state.Subscribers.Remove(
                    internalSubscriberId))
            {
                return;
            }

            moduleId =
                state.ModuleId;

            panelId =
                state.PanelId;

            remainingSubscriberCount =
                state.Subscribers.Count;

            if (remainingSubscriberCount == 0)
            {
                _streams.Remove(
                    streamKey);

                cancellationSource =
                    state.CancellationSource;

                producerTask =
                    state.ProducerTask;
            }
        }

        _logger.LogInformation(
            "Videoprenumerant borttagen. ModuleId: {ModuleId}, PanelId: {PanelId}, Antal mottagare: {SubscriberCount}",
            moduleId,
            panelId,
            remainingSubscriberCount);

        if (cancellationSource is null)
        {
            return;
        }

        cancellationSource.Cancel();

        if (producerTask is not null)
        {
            try
            {
                await producerTask;
            }
            catch (OperationCanceledException)
            {
                // Normal avslutning.
            }
        }

        cancellationSource.Dispose();
    }

    /// <summary>
    /// Skapar en intern nyckel för en logisk panel.
    /// Dictionaryn är case-insensitive.
    /// </summary>
    private static string CreateStreamKey(
        string moduleId,
        string panelId)
    {
        return $"{moduleId}\u001F{panelId}";
    }

    /// <summary>
    /// Internt tillstånd för en gemensam panelvideoström.
    /// </summary>
    private sealed class PanelStreamState
    {
        public PanelStreamState(
            string moduleId,
            string panelId,
            CancellationTokenSource cancellationSource)
        {
            ModuleId = moduleId;
            PanelId = panelId;
            CancellationSource = cancellationSource;
        }

        public string ModuleId { get; }

        public string PanelId { get; }

        public CancellationTokenSource CancellationSource { get; }

        public Dictionary<string, PanelVideoStreamSubscription>
            Subscribers
        { get; } =
                new(StringComparer.OrdinalIgnoreCase);

        public Task? ProducerTask { get; set; }
    }
}