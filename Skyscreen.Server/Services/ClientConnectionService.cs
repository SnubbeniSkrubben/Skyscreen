// Path: Skyscreen.Server/Services/ClientConnectionService.cs

using System.Collections.Concurrent;
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
        ClientSessionManager clientSessionManager)
    {
        _logger = logger;
        _connectionListener = connectionListener;
        _clientSessionManager = clientSessionManager;
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
                        HandleMessage(
                            connection,
                            message,
                            registeredClientId);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Normal nedstängning av servern.
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
    private string? HandleMessage(
        IClientConnection connection,
        SkyscreenMessage message,
        string? registeredClientId)
    {
        switch (message)
        {
            case ConnectClientMessage connectClient:
                return HandleConnectClient(
                    connection,
                    connectClient,
                    registeredClientId);

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
    private string HandleConnectClient(
        IClientConnection connection,
        ConnectClientMessage message,
        string? registeredClientId)
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

        return session.ClientId;
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
