// Path: Skyscreen.Server/Services/ClientSessionManager.cs

using Skyscreen.Core.Models;

namespace Skyscreen.Server.Services;

/// <summary>
/// Hanterar aktiva Skyscreen-klientsessioner på servern.
/// Varje ansluten platta får en egen session.
/// </summary>
public sealed class ClientSessionManager
{
    private readonly object _syncRoot = new();

    private readonly Dictionary<string, ClientSession> _sessions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returnerar en ögonblicksbild av alla aktuella sessioner.
    /// </summary>
    public IReadOnlyList<ClientSession> GetAll()
    {
        lock (_syncRoot)
        {
            return _sessions.Values
                .ToArray();
        }
    }

    /// <summary>
    /// Hämtar en specifik session med klientens ID.
    /// </summary>
    public ClientSession? FindByClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        lock (_syncRoot)
        {
            return _sessions.TryGetValue(
                clientId,
                out ClientSession? session)
                    ? session
                    : null;
        }
    }

    /// <summary>
    /// Registrerar eller ersätter en klientsession.
    /// </summary>
    public void Register(ClientSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(session.ClientId))
        {
            throw new ArgumentException(
                "ClientId får inte vara tomt.",
                nameof(session));
        }

        lock (_syncRoot)
        {
            _sessions[session.ClientId] = session;
        }
    }

    /// <summary>
    /// Registrerar en ny klient eller återansluter en befintlig klient.
    ///
    /// Vid återanslutning bevaras klientens befintliga
    /// panelprenumerationer.
    ///
    /// Hela operationen sker under samma lås så att en samtidig
    /// sessionsändring inte kan orsaka att prenumerationer tappas.
    /// </summary>
    public ClientSession RegisterOrReconnect(
        string clientId,
        string? clientName,
        ClientConnectionType connectionType,
        DateTimeOffset connectedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException(
                "ClientId får inte vara tomt.",
                nameof(clientId));
        }

        lock (_syncRoot)
        {
            _sessions.TryGetValue(
                clientId,
                out ClientSession? currentSession);

            ClientSession updatedSession = new()
            {
                ClientId = clientId,
                ClientName = clientName,
                ConnectionType = connectionType,
                ConnectedAtUtc = connectedAtUtc,
                IsConnected = true,
                Subscriptions =
                    currentSession?.Subscriptions.ToArray()
                    ?? Array.Empty<PanelSubscription>()
            };

            _sessions[clientId] = updatedSession;

            return updatedSession;
        }
    }

    /// <summary>
    /// Markerar en registrerad klientsession som frånkopplad.
    ///
    /// Sessionen och dess panelprenumerationer behålls så att de
    /// kan återanvändas om samma klient återansluter.
    /// </summary>
    public bool MarkDisconnected(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            if (!_sessions.TryGetValue(
                    clientId,
                    out ClientSession? currentSession))
            {
                return false;
            }

            if (!currentSession.IsConnected)
            {
                return true;
            }

            ClientSession updatedSession = new()
            {
                ClientId = currentSession.ClientId,
                ClientName = currentSession.ClientName,
                ConnectionType = currentSession.ConnectionType,
                ConnectedAtUtc = currentSession.ConnectedAtUtc,
                IsConnected = false,
                Subscriptions = currentSession.Subscriptions.ToArray()
            };

            _sessions[clientId] = updatedSession;

            return true;
        }
    }

    /// <summary>
    /// Tar bort en klientsession.
    /// Returnerar true om sessionen fanns och togs bort.
    /// </summary>
    public bool Remove(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            return _sessions.Remove(clientId);
        }
    }

    /// <summary>
    /// Ersätter klientens aktiva panelprenumerationer.
    /// </summary>
    public bool SetSubscriptions(
        string clientId,
        IReadOnlyList<PanelSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        lock (_syncRoot)
        {
            if (!_sessions.TryGetValue(
                    clientId,
                    out ClientSession? currentSession))
            {
                return false;
            }

            ClientSession updatedSession = new()
            {
                ClientId = currentSession.ClientId,
                ClientName = currentSession.ClientName,
                ConnectionType = currentSession.ConnectionType,
                ConnectedAtUtc = currentSession.ConnectedAtUtc,
                IsConnected = currentSession.IsConnected,
                Subscriptions = subscriptions.ToArray()
            };

            _sessions[clientId] = updatedSession;

            return true;
        }
    }

    /// <summary>
    /// Lägger till eller ersätter en enskild panelprenumeration
    /// för en specifik klient.
    /// </summary>
    public bool UpsertSubscription(
        string clientId,
        PanelSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        lock (_syncRoot)
        {
            if (!_sessions.TryGetValue(
                    clientId,
                    out ClientSession? currentSession))
            {
                return false;
            }

            List<PanelSubscription> subscriptions =
                currentSession.Subscriptions.ToList();

            int existingIndex = subscriptions.FindIndex(
                item => string.Equals(
                    item.SubscriptionId,
                    subscription.SubscriptionId,
                    StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                subscriptions[existingIndex] = subscription;
            }
            else
            {
                subscriptions.Add(subscription);
            }

            ClientSession updatedSession = new()
            {
                ClientId = currentSession.ClientId,
                ClientName = currentSession.ClientName,
                ConnectionType = currentSession.ConnectionType,
                ConnectedAtUtc = currentSession.ConnectedAtUtc,
                IsConnected = currentSession.IsConnected,
                Subscriptions = subscriptions
            };

            _sessions[clientId] = updatedSession;

            return true;
        }
    }

    /// <summary>
    /// Tar bort en specifik panelprenumeration från en klient.
    /// </summary>
    public bool RemoveSubscription(
        string clientId,
        string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(subscriptionId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            if (!_sessions.TryGetValue(
                    clientId,
                    out ClientSession? currentSession))
            {
                return false;
            }

            PanelSubscription[] subscriptions =
                currentSession.Subscriptions
                    .Where(
                        item => !string.Equals(
                            item.SubscriptionId,
                            subscriptionId,
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            if (subscriptions.Length ==
                currentSession.Subscriptions.Count)
            {
                return false;
            }

            ClientSession updatedSession = new()
            {
                ClientId = currentSession.ClientId,
                ClientName = currentSession.ClientName,
                ConnectionType = currentSession.ConnectionType,
                ConnectedAtUtc = currentSession.ConnectedAtUtc,
                IsConnected = currentSession.IsConnected,
                Subscriptions = subscriptions
            };

            _sessions[clientId] = updatedSession;

            return true;
        }
    }

    /// <summary>
    /// Returnerar alla aktiva prenumerationer för en viss modul och panel.
    ///
    /// Den här metoden blir viktig när servern senare ska avgöra
    /// vilka plattor som ska få en viss videoström.
    /// </summary>
    public IReadOnlyList<PanelSubscription> FindSubscriptions(
        string moduleId,
        string panelId)
    {
        if (string.IsNullOrWhiteSpace(moduleId) ||
            string.IsNullOrWhiteSpace(panelId))
        {
            return Array.Empty<PanelSubscription>();
        }

        lock (_syncRoot)
        {
            return _sessions.Values
                .Where(session => session.IsConnected)
                .SelectMany(session => session.Subscriptions)
                .Where(subscription =>
                    subscription.IsActive &&
                    string.Equals(
                        subscription.ModuleId,
                        moduleId,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        subscription.PanelId,
                        panelId,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}