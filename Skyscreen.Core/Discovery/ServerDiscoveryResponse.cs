// Path: Skyscreen.Core/Discovery/ServerDiscoveryResponse.cs

namespace Skyscreen.Core.Discovery;

/// <summary>
/// Discovery-svar som Skyscreen.Server skickar tillbaka via UDP.
///
/// Appen använder IP-adressen som UDP-paketet kom ifrån tillsammans
/// med WebSocketPort och WebSocketPath för att bygga serverendpointen.
/// </summary>
public sealed class ServerDiscoveryResponse
{
    /// <summary>
    /// Version av discovery-protokollet.
    /// </summary>
    public int ProtocolVersion { get; init; } =
        SkyscreenDiscoveryProtocol.CurrentProtocolVersion;

    /// <summary>
    /// Identifierar meddelandetypen.
    /// </summary>
    public string MessageType { get; init; } =
        SkyscreenDiscoveryProtocol.ResponseMessageType;

    /// <summary>
    /// Samma RequestId som mottogs i ServerDiscoveryRequest.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// TCP-port som Skyscreen.Server använder för WebSocket.
    /// </summary>
    public required int WebSocketPort { get; init; }

    /// <summary>
    /// Sökväg till serverns WebSocket-endpoint.
    /// Exempel: /ws.
    /// </summary>
    public required string WebSocketPath { get; init; }
}