// Path: Skyscreen.Core/Discovery/IServerDiscoveryMessageSerializer.cs

namespace Skyscreen.Core.Discovery;

/// <summary>
/// Serialiserar och deserialiserar Skyscreens separata
/// UDP-baserade discovery-meddelanden.
/// </summary>
public interface IServerDiscoveryMessageSerializer
{
    /// <summary>
    /// Serialiserar ett discovery-anrop.
    /// </summary>
    string SerializeRequest(
        ServerDiscoveryRequest request);

    /// <summary>
    /// Deserialiserar ett discovery-anrop.
    /// </summary>
    ServerDiscoveryRequest DeserializeRequest(
        string data);

    /// <summary>
    /// Serialiserar ett discovery-svar.
    /// </summary>
    string SerializeResponse(
        ServerDiscoveryResponse response);

    /// <summary>
    /// Deserialiserar ett discovery-svar.
    /// </summary>
    ServerDiscoveryResponse DeserializeResponse(
        string data);
}