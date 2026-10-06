// Path: Skyscreen.App/Services/SkyscreenClientService.cs

using Microsoft.Extensions.Logging;
using Skyscreen.App.Transport;
using Skyscreen.Core.Protocol;

namespace Skyscreen.App.Services;

/// <summary>
/// Hanterar appens logiska anslutning till Skyscreen.Server.
///
/// Tjänsten ansvarar för:
/// - etablering av transportanslutning,
/// - registrering av stabilt ClientId,
/// - klientens önskade panelprenumerationer,
/// - kontinuerlig mottagning av servermeddelanden,
/// - periodisk heartbeat till servern,
/// - automatisk återanslutning efter transportavbrott,
/// - återställning av önskade panelprenumerationer,
/// - anslutningsstatus för UI,
/// - mottagning av ServerStatus,
/// - kontrollerad nedstängning av anslutningen.
/// </summary>
public sealed class SkyscreenClientService : ISkyscreenClientService
{
    private static readonly TimeSpan HeartbeatInterval =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan ReconnectDelay =
        TimeSpan.FromSeconds(2);

    private readonly IClientConnectionFactory _connectionFactory;
    private readonly ILogger<SkyscreenClientService> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private readonly Dictionary<string, DesiredPanelSubscription>
        _desiredSubscriptions =
            new(StringComparer.OrdinalIgnoreCase);

    private IClientConnection? _connection;

    private CancellationTokenSource? _receiveLoopCancellation;
    private Task? _receiveLoopTask;

    private CancellationTokenSource? _heartbeatLoopCancellation;
    private Task? _heartbeatLoopTask;

    private CancellationTokenSource? _reconnectLoopCancellation;
    private Task? _reconnectLoopTask;

    private Uri? _serverEndpoint;
    private bool _automaticReconnectEnabled;
    private bool _disposed;

    private SkyscreenConnectionState _connectionState =
        SkyscreenConnectionState.Disconnected;

    private ServerStatusMessage? _serverStatus;

    public SkyscreenClientService(
        IClientConnectionFactory connectionFactory,
        IClientIdentityProvider clientIdentityProvider,
        ILogger<SkyscreenClientService> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(clientIdentityProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactory = connectionFactory;
        _logger = logger;

        ClientId = clientIdentityProvider.GetOrCreateClientId();
    }

    /// <summary>
    /// Anger om den aktuella transportanslutningen är öppen.
    /// </summary>
    public bool IsConnected =>
        _connection?.IsConnected == true;

    /// <summary>
    /// Appens aktuella logiska anslutningstillstånd mot servern.
    /// </summary>
    public SkyscreenConnectionState ConnectionState =>
        _connectionState;

    /// <summary>
    /// Senast mottagna status från den aktuella serveranslutningen.
    ///
    /// Är null när ingen giltig ServerStatus finns.
    /// </summary>
    public ServerStatusMessage? ServerStatus =>
        _serverStatus;

    /// <summary>
    /// Stabil identifierare för den här appinstallationen.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// Utlöses när appens logiska anslutningstillstånd ändras.
    /// </summary>
    public event EventHandler? ConnectionStateChanged;

    /// <summary>
    /// Utlöses när ett nytt ServerStatus-meddelande tas emot
    /// eller när tidigare serverstatus inte längre är giltig.
    /// </summary>
    public event EventHandler? ServerStatusChanged;

    /// <summary>
    /// Etablerar anslutningen till servern och registrerar klienten
    /// med ett ConnectClient-meddelande.
    ///
    /// Endpointen sparas så att samma server senare kan användas
    /// vid automatisk återanslutning.
    ///
    /// Om det första anslutningsförsöket misslyckas startas
    /// återanslutningsloopen ändå.
    /// </summary>
    public async Task ConnectAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        SetConnectionState(
            SkyscreenConnectionState.Connecting);

        ClearServerStatus();

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            _serverEndpoint = endpoint;
            _automaticReconnectEnabled = true;

            if (_connection?.IsConnected == true)
            {
                SetConnectionState(
                    SkyscreenConnectionState.Connected);

                return;
            }

            CancelReconnectLoopLocked();

            if (_connection is not null)
            {
                _receiveLoopCancellation?.Cancel();
                _heartbeatLoopCancellation?.Cancel();

                await _connection.DisposeAsync();

                _connection = null;

                _receiveLoopCancellation = null;
                _receiveLoopTask = null;

                _heartbeatLoopCancellation = null;
                _heartbeatLoopTask = null;
            }

            try
            {
                await ConnectCoreLockedAsync(
                    endpoint,
                    cancellationToken);
            }
            catch
            {
                StartReconnectLoopLocked();

                SetConnectionState(
                    SkyscreenConnectionState.Reconnecting);

                throw;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Registrerar att appen önskar en viss panelprenumeration.
    ///
    /// Det önskade tillståndet sparas även om transportanslutningen
    /// för tillfället är nere. Vid återanslutning registrerar appen
    /// därför exakt de paneler som fortfarande önskas.
    /// </summary>
    public async Task SubscribePanelAsync(
        string subscriptionId,
        string moduleId,
        string panelId,
        bool receiveVideo,
        bool enableInput,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

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

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            DesiredPanelSubscription desiredSubscription =
                new(
                    subscriptionId,
                    moduleId,
                    panelId,
                    receiveVideo,
                    enableInput);

            _desiredSubscriptions[subscriptionId] =
                desiredSubscription;

            IClientConnection? connection =
                _connection;

            if (connection?.IsConnected != true)
            {
                _logger.LogDebug(
                    "Panelprenumerationen {SubscriptionId} sparades lokalt och skickas när serveranslutningen är tillgänglig.",
                    subscriptionId);

                return;
            }

            try
            {
                await SendSubscriptionAsync(
                    connection,
                    desiredSubscription,
                    cancellationToken);
            }
            catch (ClientConnectionTransportException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Panelprenumerationen {SubscriptionId} sparades lokalt men kunde inte skickas eftersom transportanslutningen bröts.",
                    subscriptionId);
            }
            catch (InvalidOperationException exception)
                when (!connection.IsConnected)
            {
                _logger.LogWarning(
                    exception,
                    "Panelprenumerationen {SubscriptionId} sparades lokalt men transportanslutningen hann stängas innan den kunde skickas.",
                    subscriptionId);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Tar bort den angivna panelprenumerationen från appens
    /// önskade tillstånd.
    ///
    /// Om servern är ansluten skickas även UnsubscribePanel direkt.
    /// Om servern är frånkopplad räcker det att ta bort det lokala
    /// önskade tillståndet, eftersom en framtida serveranslutning
    /// börjar med tom prenumerationslista.
    /// </summary>
    public async Task UnsubscribePanelAsync(
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException(
                "SubscriptionId får inte vara tomt.",
                nameof(subscriptionId));
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            _desiredSubscriptions.Remove(
                subscriptionId);

            IClientConnection? connection =
                _connection;

            if (connection?.IsConnected != true)
            {
                return;
            }

            try
            {
                await connection.SendAsync(
                    new UnsubscribePanelMessage
                    {
                        ClientId = ClientId,
                        SubscriptionId = subscriptionId
                    },
                    cancellationToken);
            }
            catch (ClientConnectionTransportException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Panelprenumerationen {SubscriptionId} togs bort lokalt men UnsubscribePanel kunde inte skickas eftersom transportanslutningen bröts.",
                    subscriptionId);
            }
            catch (InvalidOperationException exception)
                when (!connection.IsConnected)
            {
                _logger.LogWarning(
                    exception,
                    "Panelprenumerationen {SubscriptionId} togs bort lokalt men transportanslutningen hann stängas innan UnsubscribePanel kunde skickas.",
                    subscriptionId);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Avslutar den aktuella anslutningen och stoppar
    /// mottagnings-, heartbeat- och återanslutningslooparna.
    ///
    /// Ett uttryckligt DisconnectAsync innebär att automatisk
    /// återanslutning inte längre ska ske.
    /// </summary>
    public async Task DisconnectAsync()
    {
        IClientConnection? connection;

        CancellationTokenSource? receiveLoopCancellation;
        Task? receiveLoopTask;

        CancellationTokenSource? heartbeatLoopCancellation;
        Task? heartbeatLoopTask;

        CancellationTokenSource? reconnectLoopCancellation;
        Task? reconnectLoopTask;

        await _connectionLock.WaitAsync();

        try
        {
            _automaticReconnectEnabled = false;
            _serverEndpoint = null;

            connection = _connection;

            receiveLoopCancellation =
                _receiveLoopCancellation;

            receiveLoopTask =
                _receiveLoopTask;

            heartbeatLoopCancellation =
                _heartbeatLoopCancellation;

            heartbeatLoopTask =
                _heartbeatLoopTask;

            reconnectLoopCancellation =
                _reconnectLoopCancellation;

            reconnectLoopTask =
                _reconnectLoopTask;

            _connection = null;

            _receiveLoopCancellation = null;
            _receiveLoopTask = null;

            _heartbeatLoopCancellation = null;
            _heartbeatLoopTask = null;

            _reconnectLoopCancellation = null;
            _reconnectLoopTask = null;
        }
        finally
        {
            _connectionLock.Release();
        }

        SetConnectionState(
            SkyscreenConnectionState.Disconnected);

        ClearServerStatus();

        receiveLoopCancellation?.Cancel();
        heartbeatLoopCancellation?.Cancel();
        reconnectLoopCancellation?.Cancel();

        if (receiveLoopTask is not null)
        {
            try
            {
                await receiveLoopTask;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skyscreen-klientens mottagningsloop kunde inte avslutas normalt.");
            }
        }
        else
        {
            receiveLoopCancellation?.Dispose();
        }

        if (heartbeatLoopTask is not null)
        {
            try
            {
                await heartbeatLoopTask;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skyscreen-klientens heartbeat-loop kunde inte avslutas normalt.");
            }
        }
        else
        {
            heartbeatLoopCancellation?.Dispose();
        }

        if (reconnectLoopTask is not null)
        {
            try
            {
                await reconnectLoopTask;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skyscreen-klientens återanslutningsloop kunde inte avslutas normalt.");
            }
        }
        else
        {
            reconnectLoopCancellation?.Dispose();
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Etablerar en fysisk anslutning, registrerar ClientId och
    /// återställer appens aktuella önskade panelprenumerationer.
    ///
    /// Metoden anropas endast medan _connectionLock hålls.
    /// </summary>
    private async Task ConnectCoreLockedAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        IClientConnection connection =
            await _connectionFactory.ConnectAsync(
                endpoint,
                cancellationToken);

        try
        {
            await connection.SendAsync(
                new ConnectClientMessage
                {
                    ClientId = ClientId
                },
                cancellationToken);

            DesiredPanelSubscription[] subscriptions =
                _desiredSubscriptions.Values
                    .ToArray();

            foreach (DesiredPanelSubscription subscription
                     in subscriptions)
            {
                await SendSubscriptionAsync(
                    connection,
                    subscription,
                    cancellationToken);
            }

            CancellationTokenSource receiveLoopCancellation =
                new();

            CancellationTokenSource heartbeatLoopCancellation =
                new();

            _connection = connection;

            _receiveLoopCancellation =
                receiveLoopCancellation;

            _heartbeatLoopCancellation =
                heartbeatLoopCancellation;

            _receiveLoopTask =
                ReceiveLoopAsync(
                    connection,
                    receiveLoopCancellation);

            _heartbeatLoopTask =
                HeartbeatLoopAsync(
                    connection,
                    heartbeatLoopCancellation);

            SetConnectionState(
                SkyscreenConnectionState.Connected);

            _logger.LogInformation(
                "Skyscreen-klientanslutningen etablerad. Återställda panelprenumerationer: {SubscriptionCount}",
                subscriptions.Length);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Lyssnar kontinuerligt efter logiska Skyscreen-meddelanden
    /// från servern tills anslutningen avslutas eller mottagningen
    /// avbryts.
    /// </summary>
    private async Task ReceiveLoopAsync(
        IClientConnection connection,
        CancellationTokenSource cancellationSource)
    {
        try
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                SkyscreenMessage? message =
                    await connection.ReceiveAsync(
                        cancellationSource.Token);

                if (message is null)
                {
                    _logger.LogInformation(
                        "Skyscreen.Server avslutade klientanslutningen.");

                    break;
                }

                _logger.LogDebug(
                    "Meddelande mottaget från Skyscreen.Server: {MessageType}",
                    message.MessageType);

                if (message is ServerStatusMessage serverStatus)
                {
                    SetServerStatus(
                        serverStatus);

                    _logger.LogInformation(
                        "ServerStatus mottagen. ServerVersion: {ServerVersion}, DcsStatus: {DcsStatus}, ActiveModuleId: {ActiveModuleId}",
                        serverStatus.ServerVersion,
                        serverStatus.IsDcsRunning,
                        serverStatus.ActiveModuleId);
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationSource.IsCancellationRequested)
        {
            // Normal nedstängning av mottagningsloopen.
        }
        catch (ClientConnectionTransportException exception)
        {
            _logger.LogWarning(
                exception,
                "Transportanslutningen till Skyscreen.Server bröts.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ett oväntat fel inträffade i Skyscreen-klientens mottagningsloop.");
        }
        finally
        {
            await HandleReceiveLoopCompletedAsync(
                connection,
                cancellationSource);
        }
    }

    /// <summary>
    /// Skickar periodiskt Heartbeat-meddelanden så länge den
    /// aktuella transportanslutningen är aktiv.
    /// </summary>
    private async Task HeartbeatLoopAsync(
        IClientConnection connection,
        CancellationTokenSource cancellationSource)
    {
        using PeriodicTimer timer =
            new(HeartbeatInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(
                       cancellationSource.Token))
            {
                if (!connection.IsConnected)
                {
                    break;
                }

                await connection.SendAsync(
                    new HeartbeatMessage(),
                    cancellationSource.Token);

                _logger.LogDebug(
                    "Heartbeat skickad till Skyscreen.Server.");
            }
        }
        catch (OperationCanceledException)
            when (cancellationSource.IsCancellationRequested)
        {
            // Normal nedstängning av heartbeat-loopen.
        }
        catch (ClientConnectionTransportException exception)
        {
            _logger.LogWarning(
                exception,
                "Heartbeat kunde inte skickas eftersom transportanslutningen bröts.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ett oväntat fel inträffade i Skyscreen-klientens heartbeat-loop.");
        }
        finally
        {
            cancellationSource.Dispose();
        }
    }

    /// <summary>
    /// Försöker återansluta till den senast använda serverendpointen
    /// tills anslutningen lyckas eller återanslutningen avbryts.
    /// </summary>
    private async Task ReconnectLoopAsync(
        Uri endpoint,
        CancellationTokenSource cancellationSource)
    {
        try
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                await Task.Delay(
                    ReconnectDelay,
                    cancellationSource.Token);

                try
                {
                    await _connectionLock.WaitAsync(
                        cancellationSource.Token);

                    try
                    {
                        if (_disposed
                            || !_automaticReconnectEnabled)
                        {
                            ClearReconnectLoopStateLocked(
                                cancellationSource);

                            return;
                        }

                        if (_connection?.IsConnected == true)
                        {
                            SetConnectionState(
                                SkyscreenConnectionState.Connected);

                            ClearReconnectLoopStateLocked(
                                cancellationSource);

                            return;
                        }

                        await ConnectCoreLockedAsync(
                            endpoint,
                            cancellationSource.Token);

                        ClearReconnectLoopStateLocked(
                            cancellationSource);
                    }
                    finally
                    {
                        _connectionLock.Release();
                    }

                    _logger.LogInformation(
                        "Skyscreen.App återansluten till servern {Endpoint}. ClientId: {ClientId}",
                        endpoint,
                        ClientId);

                    return;
                }
                catch (OperationCanceledException)
                    when (cancellationSource.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    SetConnectionState(
                        SkyscreenConnectionState.Reconnecting);

                    _logger.LogDebug(
                        exception,
                        "Återanslutningsförsök till Skyscreen.Server misslyckades. Nytt försök görs om {ReconnectDelaySeconds} sekunder.",
                        ReconnectDelay.TotalSeconds);
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationSource.IsCancellationRequested)
        {
            // Normal avbrytning av återanslutningsloopen.
        }
        finally
        {
            await _connectionLock.WaitAsync();

            try
            {
                ClearReconnectLoopStateLocked(
                    cancellationSource);
            }
            finally
            {
                _connectionLock.Release();
                cancellationSource.Dispose();
            }
        }
    }

    /// <summary>
    /// Startar återanslutningsloopen om den är aktiverad och
    /// ingen annan återanslutningsloop redan körs.
    ///
    /// Metoden anropas endast medan _connectionLock hålls.
    /// </summary>
    private void StartReconnectLoopLocked()
    {
        if (_disposed
            || !_automaticReconnectEnabled
            || _serverEndpoint is null)
        {
            return;
        }

        if (_reconnectLoopTask is not null
            && !_reconnectLoopTask.IsCompleted)
        {
            return;
        }

        CancellationTokenSource cancellationSource =
            new();

        _reconnectLoopCancellation =
            cancellationSource;

        _reconnectLoopTask =
            ReconnectLoopAsync(
                _serverEndpoint,
                cancellationSource);

        SetConnectionState(
            SkyscreenConnectionState.Reconnecting);
    }

    /// <summary>
    /// Avbryter en pågående återanslutningsloop.
    ///
    /// Metoden anropas endast medan _connectionLock hålls.
    /// </summary>
    private void CancelReconnectLoopLocked()
    {
        CancellationTokenSource? cancellationSource =
            _reconnectLoopCancellation;

        _reconnectLoopCancellation = null;
        _reconnectLoopTask = null;

        cancellationSource?.Cancel();
    }

    /// <summary>
    /// Rensar reconnect-referenserna endast om de fortfarande
    /// tillhör den angivna återanslutningsloopen.
    ///
    /// Metoden anropas endast medan _connectionLock hålls.
    /// </summary>
    private void ClearReconnectLoopStateLocked(
        CancellationTokenSource cancellationSource)
    {
        if (!ReferenceEquals(
                _reconnectLoopCancellation,
                cancellationSource))
        {
            return;
        }

        _reconnectLoopCancellation = null;
        _reconnectLoopTask = null;
    }

    /// <summary>
    /// Skickar en önskad panelprenumeration till den
    /// angivna transportanslutningen.
    /// </summary>
    private Task SendSubscriptionAsync(
        IClientConnection connection,
        DesiredPanelSubscription subscription,
        CancellationToken cancellationToken)
    {
        return connection.SendAsync(
            new SubscribePanelMessage
            {
                SubscriptionId = subscription.SubscriptionId,
                ClientId = ClientId,
                ModuleId = subscription.ModuleId,
                PanelId = subscription.PanelId,
                ReceiveVideo = subscription.ReceiveVideo,
                EnableInput = subscription.EnableInput
            },
            cancellationToken);
    }

    /// <summary>
    /// Rensar den aktiva anslutningen när mottagningsloopen
    /// avslutas på serverns eller transportens initiativ.
    ///
    /// Om anslutningen redan har kopplats bort eller ersatts
    /// påverkas inte den nya anslutningen.
    /// </summary>
    private async Task HandleReceiveLoopCompletedAsync(
        IClientConnection connection,
        CancellationTokenSource cancellationSource)
    {
        bool disposeConnection = false;
        bool reconnectWillRun = false;
        CancellationTokenSource? heartbeatLoopCancellation = null;

        await _connectionLock.WaitAsync();

        try
        {
            if (ReferenceEquals(
                    _connection,
                    connection))
            {
                _connection = null;

                if (ReferenceEquals(
                        _receiveLoopCancellation,
                        cancellationSource))
                {
                    _receiveLoopCancellation = null;
                }

                _receiveLoopTask = null;

                heartbeatLoopCancellation =
                    _heartbeatLoopCancellation;

                _heartbeatLoopCancellation = null;
                _heartbeatLoopTask = null;

                disposeConnection = true;

                ClearServerStatus();

                StartReconnectLoopLocked();

                reconnectWillRun =
                    _automaticReconnectEnabled
                    && _serverEndpoint is not null
                    && !_disposed;
            }
        }
        finally
        {
            _connectionLock.Release();
        }

        if (disposeConnection && !reconnectWillRun)
        {
            SetConnectionState(
                SkyscreenConnectionState.Disconnected);
        }

        heartbeatLoopCancellation?.Cancel();

        try
        {
            if (disposeConnection)
            {
                await connection.DisposeAsync();
            }
        }
        finally
        {
            cancellationSource.Dispose();
        }
    }

    /// <summary>
    /// Uppdaterar klientens logiska anslutningstillstånd och
    /// signalerar ändringen till intresserade UI-komponenter.
    /// </summary>
    private void SetConnectionState(
        SkyscreenConnectionState connectionState)
    {
        if (_connectionState == connectionState)
        {
            return;
        }

        _connectionState = connectionState;

        try
        {
            ConnectionStateChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ett fel inträffade i en ConnectionStateChanged-prenumerant.");
        }
    }

    /// <summary>
    /// Sparar senast mottagna ServerStatus och signalerar ändringen.
    /// </summary>
    private void SetServerStatus(
        ServerStatusMessage serverStatus)
    {
        ArgumentNullException.ThrowIfNull(serverStatus);

        _serverStatus = serverStatus;

        try
        {
            ServerStatusChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ett fel inträffade i en ServerStatusChanged-prenumerant.");
        }
    }

    /// <summary>
    /// Tar bort tidigare ServerStatus när den inte längre kan
    /// betraktas som aktuell för den aktiva serveranslutningen.
    /// </summary>
    private void ClearServerStatus()
    {
        if (_serverStatus is null)
        {
            return;
        }

        _serverStatus = null;

        try
        {
            ServerStatusChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ett fel inträffade i en ServerStatusChanged-prenumerant.");
        }
    }

    /// <summary>
    /// Frigör klienttjänstens resurser och avslutar
    /// eventuell aktiv anslutning.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await DisconnectAsync();

        _connectionLock.Dispose();
    }

    /// <summary>
    /// Beskriver en panelprenumeration som appen önskar ska vara aktiv.
    ///
    /// Modellen är medvetet lokal för appens klienttjänst och beskriver
    /// inte serverns aktuella sessionsstatus.
    /// </summary>
    private sealed record DesiredPanelSubscription(
        string SubscriptionId,
        string ModuleId,
        string PanelId,
        bool ReceiveVideo,
        bool EnableInput);
}
