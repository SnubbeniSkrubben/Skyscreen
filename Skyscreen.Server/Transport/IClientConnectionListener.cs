// Path: Skyscreen.Server/Transport/IClientConnectionListener.cs

namespace Skyscreen.Server.Transport;

/// <summary>
/// Abstraktion för en servertransport som väntar på och accepterar
/// inkommande Skyscreen-klientanslutningar.
///
/// Den konkreta implementationen kan senare använda exempelvis
/// lokal nätverkstransport utan att övrig serverlogik behöver känna
/// till den underliggande tekniken.
/// </summary>
public interface IClientConnectionListener : IAsyncDisposable
{
    /// <summary>
    /// Väntar på nästa inkommande klientanslutning.
    ///
    /// Returnerar null om lyssnaren avslutas normalt utan att
    /// ytterligare anslutningar accepteras.
    /// </summary>
    Task<IClientConnection?> AcceptAsync(
        CancellationToken cancellationToken);
}