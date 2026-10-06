// Path: Skyscreen.Core/Discovery/ServerDiscoveryRequest.cs

namespace Skyscreen.Core.Discovery;

/// <summary>
/// Discovery-anrop som Skyscreen.App skickar via UDP broadcast
/// för att hitta en Skyscreen.Server på det lokala nätverket.
/// </summary>
public sealed class ServerDiscoveryRequest
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
        SkyscreenDiscoveryProtocol.RequestMessageType;

    /// <summary>
    /// Unikt ID för just detta discovery-försök.
    ///
    /// Servern skickar tillbaka samma värde i svaret så att Appen
    /// kan koppla ihop svar med rätt pågående discovery-anrop.
    /// </summary>
    public required string RequestId { get; init; }
}