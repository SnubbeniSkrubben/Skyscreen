// Path: Skyscreen.Core/Protocol/UnsubscribePanelMessage.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Skickas från Skyscreen.App till Skyscreen.Server när en klient
/// vill avsluta en specifik panelprenumeration.
/// </summary>
public sealed class UnsubscribePanelMessage : SkyscreenMessage
{
    /// <inheritdoc />
    public override string MessageType => "UnsubscribePanel";

    /// <summary>
    /// ID för klienten som äger prenumerationen.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Unikt ID för prenumerationen som ska avslutas.
    /// </summary>
    public required string SubscriptionId { get; init; }
}