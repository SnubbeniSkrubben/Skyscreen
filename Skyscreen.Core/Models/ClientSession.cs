// Path: Skyscreen.Core/Models/ClientSession.cs

namespace Skyscreen.Core.Models;

/// <summary>
/// Beskriver en ansluten Skyscreen-klient, exempelvis en Android-platta.
/// Varje klient kan vara kopplad till en egen DCS-modul och panel.
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
    /// ID för vald DCS-modul.
    /// Exempel: "fa18c".
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// ID för vald cockpitpanel.
    /// Exempel: "left-ddi".
    /// </summary>
    public string? PanelId { get; init; }

    /// <summary>
    /// Vilken transport klienten använder för anslutningen.
    /// </summary>
    public ClientConnectionType ConnectionType { get; init; }

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
    Wifi = 1,
    Usb = 2
}