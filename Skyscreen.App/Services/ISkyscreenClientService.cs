// Path: Skyscreen.App/Services/ISkyscreenClientService.cs

using Skyscreen.Core.Protocol;

namespace Skyscreen.App.Services;

/// <summary>
/// Hanterar Skyscreen.Apps logiska anslutning till Skyscreen.Server.
///
/// Tjänsten använder det transportoberoende anslutningslagret och
/// ansvarar för klientregistrering, panelprenumerationer,
/// anslutningsstatus och serverstatus.
/// </summary>
public interface ISkyscreenClientService : IAsyncDisposable
{
    /// <summary>
    /// Anger om appen har en aktiv transportanslutning till servern.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Appens aktuella logiska anslutningstillstånd mot servern.
    /// </summary>
    SkyscreenConnectionState ConnectionState { get; }

    /// <summary>
    /// Senast mottagna status från servern.
    ///
    /// Är null tills ett ServerStatus-meddelande har mottagits
    /// på den aktuella serveranslutningen.
    /// </summary>
    ServerStatusMessage? ServerStatus { get; }

    /// <summary>
    /// Klientens stabila ClientId.
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// Utlöses när appens logiska anslutningstillstånd ändras.
    ///
    /// Det aktuella tillståndet läses från ConnectionState.
    /// </summary>
    event EventHandler? ConnectionStateChanged;

    /// <summary>
    /// Utlöses när ett nytt ServerStatus-meddelande har mottagits
    /// eller när tidigare serverstatus inte längre är giltig.
    ///
    /// Aktuell status läses från ServerStatus.
    /// </summary>
    event EventHandler? ServerStatusChanged;

    Task ConnectAsync(
        Uri endpoint,
        CancellationToken cancellationToken);

    Task SubscribePanelAsync(
        string subscriptionId,
        string moduleId,
        string panelId,
        bool receiveVideo,
        bool enableInput,
        CancellationToken cancellationToken);

    Task UnsubscribePanelAsync(
        string subscriptionId,
        CancellationToken cancellationToken);

    Task DisconnectAsync();
}