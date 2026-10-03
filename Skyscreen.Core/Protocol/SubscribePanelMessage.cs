// Path: Skyscreen.Core/Protocol/SubscribePanelMessage.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Skickas från Skyscreen.App till Skyscreen.Server när en klient
/// vill börja prenumerera på en viss panel.
/// </summary>
public sealed class SubscribePanelMessage : SkyscreenMessage
{
    /// <inheritdoc />
    public override string MessageType => "SubscribePanel";

    /// <summary>
    /// Unikt ID för prenumerationen.
    /// ID:t används för att senare kunna uppdatera eller avsluta
    /// just denna panelprenumeration.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// ID för klienten som skapar prenumerationen.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// ID för DCS-modulen, exempelvis "fa18c".
    /// </summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// ID för panelen, exempelvis "left-ddi".
    /// </summary>
    public required string PanelId { get; init; }

    /// <summary>
    /// Anger om klienten vill ta emot livebild för panelen.
    /// </summary>
    public bool ReceiveVideo { get; init; } = true;

    /// <summary>
    /// Anger om klienten ska kunna skicka input för panelen.
    /// </summary>
    public bool EnableInput { get; init; } = true;
}