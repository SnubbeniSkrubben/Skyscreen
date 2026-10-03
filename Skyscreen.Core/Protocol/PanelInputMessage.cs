// Path: Skyscreen.Core/Protocol/PanelInputMessage.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Skickas från Skyscreen.App till Skyscreen.Server när användaren
/// interagerar med en kontroll på en cockpitpanel.
/// </summary>
public sealed class PanelInputMessage : SkyscreenMessage
{
    /// <inheritdoc />
    public override string MessageType => "PanelInput";

    /// <summary>
    /// ID för klienten som skickar input.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// ID för panelprenumerationen som inputen tillhör.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// ID för DCS-modulen, exempelvis "fa18c".
    /// </summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// ID för panelen, exempelvis "left-ddi".
    /// </summary>
    public required string PanelId { get; init; }

    /// <summary>
    /// ID för kontrollen inom panelprofilen, exempelvis "pb01".
    /// </summary>
    public required string ControlId { get; init; }

    /// <summary>
    /// Anger vilken knapphändelse som inträffade.
    /// </summary>
    public PanelInputAction Action { get; init; }
}

/// <summary>
/// Typ av knapphändelse som skickas från klienten.
/// Rotary- och axelinput modelleras separat när deras semantik definieras.
/// </summary>
public enum PanelInputAction
{
    Press = 1,
    Release = 2
}