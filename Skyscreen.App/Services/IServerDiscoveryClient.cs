// Path: Skyscreen.App/Services/IServerDiscoveryClient.cs

namespace Skyscreen.App.Services;

/// <summary>
/// Söker efter en Skyscreen.Server på det lokala nätverket.
/// </summary>
public interface IServerDiscoveryClient
{
    /// <summary>
    /// Försöker hitta en Skyscreen.Server och returnerar dess
    /// WebSocket-endpoint.
    ///
    /// Returnerar null om ingen kompatibel server hittas inom
    /// discovery-försökets timeout.
    /// </summary>
    Task<Uri?> DiscoverAsync(
        CancellationToken cancellationToken);
}