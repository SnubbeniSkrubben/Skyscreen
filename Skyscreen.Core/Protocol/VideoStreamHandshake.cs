// Path: Skyscreen.Core/Protocol/VideoStreamHandshake.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Handshake som skickas från Skyscreen.App till Skyscreen.Server
/// när en separat videoström ska bindas till en redan etablerad
/// panelprenumeration.
///
/// Detta är ett transportnära kontrakt för videoanslutningen och
/// är därför inte ett SkyscreenMessage.
/// </summary>
public sealed class VideoStreamHandshake
{
    /// <summary>
    /// Protokollversion som klienten använder.
    /// </summary>
    public int ProtocolVersion { get; init; } =
        SkyscreenMessage.CurrentProtocolVersion;

    /// <summary>
    /// Stabilt ID för klienten som äger prenumerationen.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// ID för den redan etablerade panelprenumerationen
    /// som videoströmmen ska bindas till.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>
    /// ID för DCS-modulen.
    /// Exempel: "fa18c".
    /// </summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// ID för panelen.
    /// Exempel: "left-ddi".
    /// </summary>
    public required string PanelId { get; init; }
}