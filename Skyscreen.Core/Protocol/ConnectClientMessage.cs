// Path: Skyscreen.Core/Protocol/ConnectClientMessage.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Skickas från Skyscreen.App till Skyscreen.Server när en klient
/// vill registrera sig och skapa eller återuppta sin session.
/// </summary>
public sealed class ConnectClientMessage : SkyscreenMessage
{
    /// <inheritdoc />
    public override string MessageType => "ConnectClient";

    /// <summary>
    /// Stabilt och unikt ID för klienten.
    /// ID:t ska senare kunna återanvändas vid återanslutning.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Valfritt läsbart namn för klienten, exempelvis "Vänster platta".
    /// </summary>
    public string? ClientName { get; init; }
}