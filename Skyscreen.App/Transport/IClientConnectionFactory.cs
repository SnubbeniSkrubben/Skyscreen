// Path: Skyscreen.App/Transport/IClientConnectionFactory.cs

namespace Skyscreen.App.Transport;

/// <summary>
/// Skapar en aktiv anslutning från Skyscreen.App till Skyscreen.Server.
///
/// Högre lager ska kunna begära en anslutning utan att själva behöva
/// skapa eller hantera den konkreta transportimplementationen.
/// </summary>
public interface IClientConnectionFactory
{
    /// <summary>
    /// Etablerar en anslutning till angiven serverendpoint.
    /// </summary>
    Task<IClientConnection> ConnectAsync(
        Uri endpoint,
        CancellationToken cancellationToken);
}