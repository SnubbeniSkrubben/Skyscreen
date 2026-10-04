// Path: Skyscreen.App/Transport/IClientConnection.cs

using Skyscreen.Core.Protocol;

namespace Skyscreen.App.Transport;

/// <summary>
/// Abstraktion för en aktiv anslutning från Skyscreen.App
/// till Skyscreen.Server.
///
/// Högre lager i appen ska arbeta med logiska Skyscreen-meddelanden
/// och inte känna till om den underliggande transporten använder
/// WebSocket, Wi-Fi, USB eller någon framtida transportlösning.
/// </summary>
public interface IClientConnection : IAsyncDisposable
{
    /// <summary>
    /// Anger om den underliggande transportanslutningen är öppen.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Slutförs när anslutningens livscykel har avslutats.
    /// </summary>
    Task Completion { get; }

    /// <summary>
    /// Tar emot nästa logiska Skyscreen-meddelande från servern.
    ///
    /// Returnerar null om anslutningen avslutas normalt innan
    /// ytterligare meddelanden tas emot.
    /// </summary>
    Task<SkyscreenMessage?> ReceiveAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Skickar ett logiskt Skyscreen-meddelande till servern.
    /// </summary>
    Task SendAsync(
        SkyscreenMessage message,
        CancellationToken cancellationToken);
}