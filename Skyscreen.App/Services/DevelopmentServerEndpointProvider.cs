// Path: Skyscreen.App/Services/DevelopmentServerEndpointProvider.cs

namespace Skyscreen.App.Services;

/// <summary>
/// Tillhandahåller serverendpointen för den aktuella
/// utvecklingsmiljön med Android-emulator.
///
/// Android-emulatorn använder 10.0.2.2 för att nå datorn
/// som emulatorn körs på.
/// </summary>
public sealed class DevelopmentServerEndpointProvider
    : IServerEndpointProvider
{
    private static readonly Uri ServerEndpoint =
        new("ws://10.0.2.2:5080/ws");

    /// <inheritdoc />
    public Uri GetServerEndpoint()
    {
        return ServerEndpoint;
    }
}