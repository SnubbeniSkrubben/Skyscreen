// Path: Skyscreen.Core/Models/ClientSession.cs

namespace Skyscreen.Core.Models;

/// <summary>
/// Beskriver en ansluten Skyscreen-klient, exempelvis en Android-platta.
/// En klient kan ha en eller flera aktiva panelprenumerationer.
/// </summary>
public sealed class ClientSession
{
    /// <summary>
    /// Unikt ID för klienten.
    /// Kan exempelvis vara ett GUID som genereras av appen.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Ett läsbart namn för klienten.
    /// Exempel: "Vänster platta" eller "Tablet 1".
    /// </summary>
    public string? ClientName { get; init; }

    /// <summary>
    /// Vilken transport klienten använder för anslutningen.
    /// </summary>
    public ClientConnectionType ConnectionType { get; init; }

    /// <summary>
    /// Paneler som klienten för närvarande prenumererar på.
    ///
    /// Exempel:
    /// Tablet-A -> F/A-18C Left DDI
    /// Tablet-B -> F/A-18C Right DDI
    ///
    /// Arkitekturen tillåter även flera panelprenumerationer
    /// per klient om det behövs i framtiden.
    /// </summary>
    public IReadOnlyList<PanelSubscription> Subscriptions { get; init; }
        = Array.Empty<PanelSubscription>();

    /// <summary>
    /// Tidpunkt då klienten anslöt till Skyscreen.Server.
    /// </summary>
    public DateTimeOffset ConnectedAtUtc { get; init; }

    /// <summary>
    /// Anger om klienten fortfarande betraktas som ansluten.
    /// </summary>
    public bool IsConnected { get; init; } = true;
}

/// <summary>
/// Typ av anslutning mellan Skyscreen.App och Skyscreen.Server.
/// </summary>
public enum ClientConnectionType
{
    Unknown = 0,

    /// <summary>
    /// Klienten kommunicerar med servern via lokalt nätverk/Wi-Fi.
    /// </summary>
    Wifi = 1,

    /// <summary>
    /// Klienten kommunicerar med servern via USB-baserad nätverksanslutning.
    /// </summary>
    Usb = 2
}