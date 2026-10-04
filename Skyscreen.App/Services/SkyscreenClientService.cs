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
/// - panelprenumerationer,
/// - kontinuerlig mottagning av servermeddelanden,
/// - periodisk heartbeat till servern,
/// - kontrollerad nedstängning av anslutningen.
///
/// Automatisk återanslutning implementeras i ett senare steg.
/// </summary>
public sealed class SkyscreenClientService : ISkyscreenClientService
{
    private static readonly TimeSpan HeartbeatInterval =
        TimeSpan.FromSeconds(5);

    private readonly IClientConnectionFactory _connectionFactory;
    private readonly ILogger<SkyscreenClientService> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IClientConnection? _connection;

    private CancellationTokenSource? _receiveLoopCancellation;
    private Task? _receiveLoopTask;

    private CancellationTokenSource? _heartbeatLoopCancellation;
    private Task? _heartbeatLoopTask;

    private bool _disposed;

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
    /// Stabil identifierare för den här appinstallationen.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// Etablerar anslutningen till servern och registrerar klienten
    /// med ett ConnectClient-meddelande.
    ///
    /// När registreringsmeddelandet har skickats startas appens
    /// mottagningsloop och periodiska heartbeat-loop.
    /// </summary>
    public async Task ConnectAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection?.IsConnected == true)
            {
                return;
            }

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
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Skapar eller uppdaterar en panelprenumeration
    /// för den aktuella klienten.
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
            if (_connection?.IsConnected != true)
            {
                throw new InvalidOperationException(
                    "Klienten måste vara ansluten innan en panelprenumeration kan skapas.");
            }

            await _connection.SendAsync(
                new SubscribePanelMessage
                {
                    SubscriptionId = subscriptionId,
                    ClientId = ClientId,
                    ModuleId = moduleId,
                    PanelId = panelId,
                    ReceiveVideo = receiveVideo,
                    EnableInput = enableInput
                },
                cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Avslutar den angivna panelprenumerationen
    /// för den aktuella klienten.
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
            if (_connection?.IsConnected != true)
            {
                throw new InvalidOperationException(
                    "Klienten måste vara ansluten innan en panelprenumeration kan avslutas.");
            }

            await _connection.SendAsync(
                new UnsubscribePanelMessage
                {
                    ClientId = ClientId,
                    SubscriptionId = subscriptionId
                },
                cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Avslutar den aktuella anslutningen och stoppar
    /// mottagnings- och heartbeat-looparna.
    /// </summary>
    public async Task DisconnectAsync()
    {
        IClientConnection? connection;

        CancellationTokenSource? receiveLoopCancellation;
        Task? receiveLoopTask;

        CancellationTokenSource? heartbeatLoopCancellation;
        Task? heartbeatLoopTask;

        await _connectionLock.WaitAsync();

        try
        {
            connection = _connection;

            receiveLoopCancellation =
                _receiveLoopCancellation;

            receiveLoopTask =
                _receiveLoopTask;

            heartbeatLoopCancellation =
                _heartbeatLoopCancellation;

            heartbeatLoopTask =
                _heartbeatLoopTask;

            _connection = null;

            _receiveLoopCancellation = null;
            _receiveLoopTask = null;

            _heartbeatLoopCancellation = null;
            _heartbeatLoopTask = null;
        }
        finally
        {
            _connectionLock.Release();
        }

        receiveLoopCancellation?.Cancel();
        heartbeatLoopCancellation?.Cancel();

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

        if (connection is not null)
        {
            await connection.DisposeAsync();
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

                // Själva hanteringen av inkommande meddelanden
                // implementeras stegvis när respektive funktion
                // införs, exempelvis ServerStatus.
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
            }
        }
        finally
        {
            _connectionLock.Release();
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
}