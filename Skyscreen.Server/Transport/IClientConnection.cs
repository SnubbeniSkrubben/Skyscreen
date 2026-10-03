// Path: Skyscreen.Server/Transport/IClientConnection.cs

using Skyscreen.Core.Models;
using Skyscreen.Core.Protocol;

namespace Skyscreen.Server.Transport;

/// <summary>
/// Abstraktion för en aktiv anslutning mellan Skyscreen.Server
/// och en Skyscreen-klient.
///
/// Implementationen kan senare använda exempelvis Wi-Fi eller
/// USB-baserad nätverksanslutning utan att serverns övriga logik
/// behöver känna till den underliggande transporten.
/// </summary>
public interface IClientConnection : IAsyncDisposable
{
    /// <summary>
    /// Unikt ID för den fysiska/logiska anslutningen.
    ///
    /// Detta är inte samma sak som klientens ClientId.
    /// ClientId blir känt först när klienten skickar ConnectClient.
    /// </summary>
    string ConnectionId { get; }

    /// <summary>
    /// Anger vilken transporttyp anslutningen använder.
    /// </summary>
    ClientConnectionType ConnectionType { get; }

    /// <summary>
    /// Slutförs när anslutningens livscykel har avslutats.
    ///
    /// Används exempelvis av transportens host/end-point för att
    /// hålla den underliggande anslutningen vid liv tills klienten
    /// kopplar från eller anslutningen stängs av servern.
    /// </summary>
    Task Completion { get; }

    /// <summary>
    /// Tar emot nästa logiska Skyscreen-meddelande från klienten.
    ///
    /// Returnerar null om anslutningen avslutas normalt innan
    /// ytterligare meddelanden tas emot.
    /// </summary>
    Task<SkyscreenMessage?> ReceiveAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Skickar ett logiskt Skyscreen-meddelande till klienten.
    /// </summary>
    Task SendAsync(
        SkyscreenMessage message,
        CancellationToken cancellationToken);
}