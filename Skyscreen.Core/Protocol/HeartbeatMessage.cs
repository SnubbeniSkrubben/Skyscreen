// Path: Skyscreen.Core/Protocol/HeartbeatMessage.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Skickas mellan Skyscreen.App och Skyscreen.Server för att
/// verifiera att anslutningen fortfarande är aktiv.
/// </summary>
public sealed class HeartbeatMessage : SkyscreenMessage
{
    /// <inheritdoc />
    public override string MessageType => "Heartbeat";
}