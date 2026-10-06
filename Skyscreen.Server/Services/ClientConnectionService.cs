// Path: Skyscreen.Server/Services/ClientConnectionService.cs

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Skyscreen.Core.Models;
using Skyscreen.Core.Protocol;
using Skyscreen.Server.Transport;

namespace Skyscreen.Server.Services;

/// <summary>
/// Tar emot etablerade klientanslutningar från transportlagret
/// och hanterar varje klient oberoende av övriga anslutningar.
/// </summary>
public sealed class ClientConnectionService : BackgroundService
{
    private readonly ILogger<ClientConnectionService> _logger;
    private readonly IClientConnectionListener _connectionListener;
    private readonly ClientSessionManager _clientSessionManager;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private readonly ConcurrentDictionary<string, Task> _connectionTasks =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _clientConnectionSyncRoot = new();

    private readonly Dictionary<string, string> _currentConnectionIdsByClientId =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Skapar tjänsten som hanterar inkommande klientanslutningar.
    /// </summary>
    public ClientConnectionService(
        ILogger<ClientConnectionService> logger,
        IClientConnectionListener connectionListener,
        ClientSessionManager clientSessionManager,
        IHostApplicationLifetime applicationLifetime)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(connectionListener);
        ArgumentNullException.ThrowIfNull(clientSessionManager);
        ArgumentNullException.ThrowIfNull(applicationLifetime);

        _logger = logger;
        _connectionListener = connectionListener;
        _clientSessionManager = clientSessionManager;
        _applicationLifetime = applicationLifetime;
    }

    /// <summary>
    /// Väntar på inkommande anslutningar och startar en separat
    /// asynkron hantering för varje klient.
    /// </summary>
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                IClientConnection? connection =
                    await _connectionListener.AcceptAsync(
                        stoppingToken);

                if (connection is null)
                {
                    break;
                }

                _logger.LogInformation(
                    "Klientanslutning accepterad. ConnectionId: {ConnectionId}, Transport: {ConnectionType}",
                    connection.ConnectionId,
                    connection.ConnectionType);

                Task connectionTask =
                    HandleConnectionAsync(
                        connection,
                        stoppingToken);

                _connectionTasks[connection.ConnectionId] =
                    connectionTask;

                _ = connectionTask.ContinueWith(
                    completedTask =>
                    {
                        _connectionTasks.TryRemove(
                            connection.ConnectionId,
                            out _);
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal nedstängning av servern.
        }

        Task[] remainingTasks =
            _connectionTasks.Values.ToArray();

        if (remainingTasks.Length > 0)
        {
            await Task.WhenAll(remainingTasks);
        }
    }

    /// <summary>
    /// Läser och hanterar protokollmeddelanden från en enskild klient.
    /// </summary>
    private async Task HandleConnectionAsync(
        IClientConnection connection,
        CancellationToken stoppingToken)
    {
        string? registeredClientId = null;

        await using (connection)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    SkyscreenMessage? message =
                        await connection.ReceiveAsync(
                            stoppingToken);

                    if (message is null)
                    {
                        break;
                    }

                    _logger.LogInformation(
                        "Meddelande mottaget från {ConnectionId}: {MessageType}",
                        connection.ConnectionId,
                        message.MessageType);

                    registeredClientId =
                        await HandleMessageAsync(
                            connection,
                            message,
                            registeredClientId,
                            stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Normal nedstängning av servern.
            }
            catch (ConnectionAbortedException)
                when (_applicationLifetime.ApplicationStopping.IsCancellationRequested)
            {
                // WebSocket-anslutningen aborteras avsiktligt när
                // serverhosten stängs ned. Detta är en normal del
                // av serverns kontrollerade nedstängning.
            }
            catch (ClientConnectionTransportException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Klientanslutningen {ConnectionId} bröts på transportnivå.",
                    connection.ConnectionId);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Oväntat fel vid hantering av anslutningen {ConnectionId}.",
                    connection.ConnectionId);
            }
            finally
            {
                if (registeredClientId is not null &&
                    TryReleaseClientConnection(
                        registeredClientId,
                        connection.ConnectionId))
                {
                    _clientSessionManager.MarkDisconnected(
                        registeredClientId);

                    _logger.LogInformation(
                        "Klientsession markerad som frånkopplad. ClientId: {ClientId}",
                        registeredClientId);
                }

                _logger.LogInformation(
                    "Klientanslutning avslutad. ConnectionId: {ConnectionId}",
                    connection.ConnectionId);
            }
        }
    }

    /// <summary>
    /// Routar ett mottaget protokollmeddelande till rätt serverlogik.
    /// Returnerar det ClientId som är bundet till anslutningen.
    /// </summary>
    private async Task<string?> HandleMessageAsync(
        IClientConnection connection,
        SkyscreenMessage message,
        string? registeredClientId,
        CancellationToken cancellationToken)
    {
        switch (message)
        {
            case ConnectClientMessage connectClient:
                return await HandleConnectClientAsync(
                    connection,
                    connectClient,
                    registeredClientId,
                    cancellationToken);

            case SubscribePanelMessage subscribePanel:
                HandleSubscribePanel(
                    connection,
                    subscribePanel,
                    registeredClientId);

                return registeredClientId;

            case UnsubscribePanelMessage unsubscribePanel:
                HandleUnsubscribePanel(
                    connection,
                    unsubscribePanel,
                    registeredClientId);

                return registeredClientId;

            case HeartbeatMessage:
                HandleHeartbeat(
                    connection,
                    registeredClientId);

                return registeredClientId;

            default:
                _logger.LogDebug(
                    "Ingen serverhantering är ännu implementerad för meddelandetypen {MessageType}.",
                    message.MessageType);

                return registeredClientId;
        }
    }

    /// <summary>
    /// Registrerar en ny klient eller återansluter en befintlig klient.
    ///
    /// En fysisk anslutning får inte byta ClientId efter att den
    /// har registrerats.
    /// </summary>
    private async Task<string> HandleConnectClientAsync(
        IClientConnection connection,
        ConnectClientMessage message,
        string? registeredClientId,
        CancellationToken cancellationToken)
    {
        if (registeredClientId is not null)
        {
            if (!string.Equals(
                    registeredClientId,
                    message.ClientId,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "En etablerad klientanslutning får inte byta ClientId.");
            }

            lock (_clientConnectionSyncRoot)
            {
                if (!_currentConnectionIdsByClientId.TryGetValue(
                        registeredClientId,
                        out string? currentConnectionId) ||
                    !string.Equals(
                        currentConnectionId,
                        connection.ConnectionId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Klientanslutningen har ersatts av en nyare anslutning.");
                }
            }

            // Ett upprepat ConnectClient från samma aktuella
            // anslutning behöver inte registrera sessionen på nytt.
            await SendServerStatusAsync(
                connection,
                cancellationToken);

            return registeredClientId;
        }

        ClientSession session;

        lock (_clientConnectionSyncRoot)
        {
            session =
                _clientSessionManager.RegisterOrReconnect(
                    message.ClientId,
                    message.ClientName,
                    connection.ConnectionType,
                    DateTimeOffset.UtcNow);

            _currentConnectionIdsByClientId[session.ClientId] =
                connection.ConnectionId;
        }

        _logger.LogInformation(
            "Klientsession registrerad. ClientId: {ClientId}, Prenumerationer: {SubscriptionCount}",
            session.ClientId,
            session.Subscriptions.Count);

        await SendServerStatusAsync(
            connection,
            cancellationToken);

        return session.ClientId;
    }

    /// <summary>
    /// Skickar serverns aktuella status till den registrerade klienten.
    ///
    /// DCS-status är null tills faktisk DCS-detektering har
    /// implementerats och kan ge ett verifierat svar.
    /// </summary>
    private async Task SendServerStatusAsync(
        IClientConnection connection,
        CancellationToken cancellationToken)
    {
        await connection.SendAsync(
            new ServerStatusMessage
            {
                ServerVersion = GetServerVersion(),
                IsDcsRunning = null,
                ActiveModuleId = null
            },
            cancellationToken);

        _logger.LogDebug(
            "ServerStatus skickad till klientanslutningen {ConnectionId}.",
            connection.ConnectionId);
    }

    /// <summary>
    /// Hämtar serverversionen från assemblyns versionsmetadata.
    /// </summary>
    private static string GetServerVersion()
    {
        Assembly assembly =
            typeof(ClientConnectionService).Assembly;

        string? informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    /// <summary>
    /// Skapar eller uppdaterar en panelprenumeration för den
    /// aktuella klientanslutningen.
    /// </summary>
    private void HandleSubscribePanel(
        IClientConnection connection,
        SubscribePanelMessage message,
        string? registeredClientId)
    {
        if (registeredClientId is null)
        {
            throw new InvalidOperationException(
                "ConnectClient måste skickas innan SubscribePanel.");
        }

        if (!string.Equals(
                registeredClientId,
                message.ClientId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SubscribePanel.ClientId matchar inte den registrerade klientanslutningen.");
        }

        if (string.IsNullOrWhiteSpace(message.SubscriptionId))
        {
            throw new InvalidOperationException(
                "SubscribePanel.SubscriptionId får inte vara tomt.");
        }

        if (string.IsNullOrWhiteSpace(message.ModuleId))
        {
            throw new InvalidOperationException(
                "SubscribePanel.ModuleId får inte vara tomt.");
        }

        if (string.IsNullOrWhiteSpace(message.PanelId))
        {
            throw new InvalidOperationException(
                "SubscribePanel.PanelId får inte vara tomt.");
        }

        PanelSubscription subscription;

        lock (_clientConnectionSyncRoot)
        {
            EnsureCurrentConnection(
                registeredClientId,
                connection.ConnectionId);

            ClientSession? currentSession =
                _clientSessionManager.FindByClientId(
                    registeredClientId);

            if (currentSession is null)
            {
                throw new InvalidOperationException(
                    "Den registrerade klientsessionen kunde inte hittas.");
            }

            PanelSubscription? existingSubscription =
                currentSession.Subscriptions.FirstOrDefault(
                    item => string.Equals(
                        item.SubscriptionId,
                        message.SubscriptionId,
                        StringComparison.OrdinalIgnoreCase));

            subscription = new PanelSubscription
            {
                SubscriptionId = message.SubscriptionId,
                ClientId = registeredClientId,
                ModuleId = message.ModuleId,
                PanelId = message.PanelId,
                ReceiveVideo = message.ReceiveVideo,
                EnableInput = message.EnableInput,
                CreatedAtUtc =
                    existingSubscription?.CreatedAtUtc
                    ?? DateTimeOffset.UtcNow,
                IsActive = true
            };

            bool updated =
                _clientSessionManager.UpsertSubscription(
                    registeredClientId,
                    subscription);

            if (!updated)
            {
                throw new InvalidOperationException(
                    "Panelprenumerationen kunde inte registreras.");
            }
        }

        _logger.LogInformation(
            "Panelprenumeration registrerad. ClientId: {ClientId}, SubscriptionId: {SubscriptionId}, ModuleId: {ModuleId}, PanelId: {PanelId}",
            subscription.ClientId,
            subscription.SubscriptionId,
            subscription.ModuleId,
            subscription.PanelId);
    }

    /// <summary>
    /// Avslutar en panelprenumeration för den aktuella
    /// klientanslutningen.
    ///
    /// Operationen är idempotent. Om prenumerationen redan är
    /// borttagen betraktas det inte som ett protokollfel.
    /// </summary>
    private void HandleUnsubscribePanel(
        IClientConnection connection,
        UnsubscribePanelMessage message,
        string? registeredClientId)
    {
        if (registeredClientId is null)
        {
            throw new InvalidOperationException(
                "ConnectClient måste skickas innan UnsubscribePanel.");
        }

        if (!string.Equals(
                registeredClientId,
                message.ClientId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "UnsubscribePanel.ClientId matchar inte den registrerade klientanslutningen.");
        }

        if (string.IsNullOrWhiteSpace(message.SubscriptionId))
        {
            throw new InvalidOperationException(
                "UnsubscribePanel.SubscriptionId får inte vara tomt.");
        }

        bool removed;

        lock (_clientConnectionSyncRoot)
        {
            EnsureCurrentConnection(
                registeredClientId,
                connection.ConnectionId);

            removed =
                _clientSessionManager.RemoveSubscription(
                    registeredClientId,
                    message.SubscriptionId);
        }

        if (removed)
        {
            _logger.LogInformation(
                "Panelprenumeration avslutad. ClientId: {ClientId}, SubscriptionId: {SubscriptionId}",
                registeredClientId,
                message.SubscriptionId);
        }
        else
        {
            _logger.LogDebug(
                "Panelprenumerationen var redan borttagen eller saknades. ClientId: {ClientId}, SubscriptionId: {SubscriptionId}",
                registeredClientId,
                message.SubscriptionId);
        }
    }

    /// <summary>
    /// Registrerar att den aktuella klientanslutningen fortfarande
    /// är aktiv genom att uppdatera sessionens heartbeat-tidpunkt.
    /// </summary>
    private void HandleHeartbeat(
        IClientConnection connection,
        string? registeredClientId)
    {
        if (registeredClientId is null)
        {
            throw new InvalidOperationException(
                "ConnectClient måste skickas innan Heartbeat.");
        }

        DateTimeOffset heartbeatAtUtc =
            DateTimeOffset.UtcNow;

        lock (_clientConnectionSyncRoot)
        {
            EnsureCurrentConnection(
                registeredClientId,
                connection.ConnectionId);

            bool updated =
                _clientSessionManager.UpdateHeartbeat(
                    registeredClientId,
                    heartbeatAtUtc);

            if (!updated)
            {
                throw new InvalidOperationException(
                    "Heartbeat kunde inte registreras för klientsessionen.");
            }
        }

        _logger.LogDebug(
            "Heartbeat registrerad. ClientId: {ClientId}, HeartbeatAtUtc: {HeartbeatAtUtc}",
            registeredClientId,
            heartbeatAtUtc);
    }

    /// <summary>
    /// Säkerställer att en anslutning fortfarande är klientens
    /// aktuella fysiska anslutning.
    /// </summary>
    private void EnsureCurrentConnection(
        string clientId,
        string connectionId)
    {
        if (!_currentConnectionIdsByClientId.TryGetValue(
                clientId,
                out string? currentConnectionId) ||
            !string.Equals(
                currentConnectionId,
                connectionId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Klientanslutningen har ersatts av en nyare anslutning.");
        }
    }

    /// <summary>
    /// Frigör kopplingen endast om anslutningen fortfarande är
    /// klientens aktuella anslutning.
    ///
    /// Detta förhindrar att en gammal anslutning markerar en
    /// nyligen återansluten klient som frånkopplad.
    /// </summary>
    private bool TryReleaseClientConnection(
        string clientId,
        string connectionId)
    {
        lock (_clientConnectionSyncRoot)
        {
            if (!_currentConnectionIdsByClientId.TryGetValue(
                    clientId,
                    out string? currentConnectionId))
            {
                return false;
            }

            if (!string.Equals(
                    currentConnectionId,
                    connectionId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            _currentConnectionIdsByClientId.Remove(clientId);

            return true;
        }
    }
}