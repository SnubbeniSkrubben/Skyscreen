// Path: Skyscreen.App/Services/IServerEndpointProvider.cs

namespace Skyscreen.App.Services;

/// <summary>
/// Tillhandahåller den serverendpoint som Skyscreen.App
/// ska använda för anslutning till Skyscreen.Server.
///
/// Serveradressen hålls separat från klient- och transportlogiken
/// så att den senare kan komma från exempelvis inställningar,
/// discovery eller annan anslutningskonfiguration.
/// </summary>
public interface IServerEndpointProvider
{
    /// <summary>
    /// Hämtar den aktuella serverendpointen.
    /// </summary>
    Uri GetServerEndpoint();
}