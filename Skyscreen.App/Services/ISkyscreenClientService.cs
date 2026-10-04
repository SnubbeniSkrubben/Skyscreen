// Path: Skyscreen.App/Services/ISkyscreenClientService.cs

namespace Skyscreen.App.Services;

/// <summary>
/// Hanterar Skyscreen.Apps logiska anslutning till Skyscreen.Server.
///
/// Tjänsten ska använda det transportoberoende anslutningslagret och
/// ansvarar för klientregistrering och senare protokollflöden som
/// panelprenumerationer och återanslutning.
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
    /// Kopplar från den aktuella serveranslutningen.
    /// </summary>
    Task DisconnectAsync();
}