// Path: Skyscreen.App/Services/ISkyscreenClientService.cs

namespace Skyscreen.App.Services;

/// <summary>
/// Hanterar Skyscreen.Apps logiska anslutning till Skyscreen.Server.
///
/// Tjänsten använder det transportoberoende anslutningslagret och
/// ansvarar för klientregistrering, panelprenumerationer och senare
/// protokollflöden som återanslutning.
/// </summary>
public interface ISkyscreenClientService : IAsyncDisposable
{
    /// <summary>
    /// Anger om appen har en aktiv transportanslutning till servern.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Klientens stabila ClientId.
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// Ansluter till servern och registrerar klienten med ConnectClient.
    /// </summary>
    Task ConnectAsync(
        Uri endpoint,
        CancellationToken cancellationToken);

    /// <summary>
    /// Skapar eller uppdaterar en panelprenumeration för klienten.
    ///
    /// SubscriptionId ska stabilt identifiera den logiska
    /// prenumerationen så att samma prenumeration senare kan
    /// uppdateras eller avslutas.
    /// </summary>
    Task SubscribePanelAsync(
        string subscriptionId,
        string moduleId,
        string panelId,
        bool receiveVideo,
        bool enableInput,
        CancellationToken cancellationToken);

    /// <summary>
    /// Avslutar den angivna panelprenumerationen.
    /// </summary>
    Task UnsubscribePanelAsync(
        string subscriptionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Kopplar från den aktuella serveranslutningen.
    /// </summary>
    Task DisconnectAsync();
}