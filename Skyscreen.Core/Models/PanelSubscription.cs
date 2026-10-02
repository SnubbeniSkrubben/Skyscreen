// Path: Skyscreen.Core/Models/PanelSubscription.cs

namespace Skyscreen.Core.Models;

/// <summary>
/// Beskriver vilken DCS-panel en viss Skyscreen-klient
/// för närvarande prenumererar på.
/// </summary>
public sealed class PanelSubscription
{
    /// <summary>
    /// Unikt ID för prenumerationen.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// ID för klienten/plattan som prenumerationen tillhör.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// ID för vald DCS-modul.
    /// Exempel: "fa18c".
    /// </summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// ID för vald cockpitpanel.
    /// Exempel: "left-ddi".
    /// </summary>
    public required string PanelId { get; init; }

    /// <summary>
    /// Anger om klienten ska ta emot livebild för panelen.
    /// </summary>
    public bool ReceiveVideo { get; init; } = true;

    /// <summary>
    /// Anger om klienten får skicka interaktiva kommandon
    /// till panelen, exempelvis knapptryck och rattändringar.
    /// </summary>
    public bool EnableInput { get; init; } = true;

    /// <summary>
    /// Tidpunkt då prenumerationen skapades.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Anger om prenumerationen fortfarande är aktiv.
    /// </summary>
    public bool IsActive { get; init; } = true;
}