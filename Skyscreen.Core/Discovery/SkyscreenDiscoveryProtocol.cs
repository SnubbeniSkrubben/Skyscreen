// Path: Skyscreen.Core/Discovery/SkyscreenDiscoveryProtocol.cs

namespace Skyscreen.Core.Discovery;

/// <summary>
/// Gemensamma konstanter för Skyscreens lokala server-discovery.
///
/// Discovery-protokollet är separat från det etablerade
/// Skyscreen-protokollet som används över WebSocket.
/// </summary>
public static class SkyscreenDiscoveryProtocol
{
    /// <summary>
    /// Aktuell version av discovery-protokollet.
    /// </summary>
    public const int CurrentProtocolVersion = 1;

    /// <summary>
    /// UDP-port som Skyscreen.Server lyssnar på för discovery-anrop.
    /// </summary>
    public const int DiscoveryPort = 5081;

    /// <summary>
    /// Identifierar ett discovery-anrop från Skyscreen.App.
    /// </summary>
    public const string RequestMessageType =
        "ServerDiscoveryRequest";

    /// <summary>
    /// Identifierar ett discovery-svar från Skyscreen.Server.
    /// </summary>
    public const string ResponseMessageType =
        "ServerDiscoveryResponse";
}