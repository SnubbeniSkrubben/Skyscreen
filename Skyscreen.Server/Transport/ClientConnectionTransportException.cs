// Path: Skyscreen.Server/Transport/ClientConnectionTransportException.cs

namespace Skyscreen.Server.Transport;

/// <summary>
/// Representerar ett fel i den underliggande transporten för en
/// Skyscreen-klientanslutning.
///
/// Högre serverlager ska kunna hantera transportfel utan att behöva
/// känna till om anslutningen använder WebSocket, USB eller någon
/// annan framtida transport.
/// </summary>
public sealed class ClientConnectionTransportException : Exception
{
    /// <summary>
    /// Skapar ett transportfel med ett beskrivande meddelande.
    /// </summary>
    public ClientConnectionTransportException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Skapar ett transportfel och bevarar det ursprungliga
    /// transportundantaget som inner exception.
    /// </summary>
    public ClientConnectionTransportException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}