// Path: Skyscreen.App/Services/SkyscreenConnectionState.cs

namespace Skyscreen.App.Services;

/// <summary>
/// Beskriver Skyscreen.Apps aktuella logiska anslutningstillstånd
/// mot Skyscreen.Server.
/// </summary>
public enum SkyscreenConnectionState
{
    /// <summary>
    /// Ingen aktiv serveranslutning finns och ingen automatisk
    /// återanslutning pågår.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Ett initialt anslutningsförsök pågår.
    /// </summary>
    Connecting,

    /// <summary>
    /// Appen har en aktiv transportanslutning till servern.
    /// </summary>
    Connected,

    /// <summary>
    /// Den tidigare anslutningen har försvunnit och appen försöker
    /// automatiskt återansluta.
    /// </summary>
    Reconnecting
}