// Path: Skyscreen.Core/Protocol/ServerStatusMessage.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Skickas från Skyscreen.Server till Skyscreen.App för att
/// rapportera serverns och DCS-anslutningens aktuella status.
/// </summary>
public sealed class ServerStatusMessage : SkyscreenMessage
{
    /// <inheritdoc />
    public override string MessageType => "ServerStatus";

    /// <summary>
    /// Versionen av Skyscreen.Server som skickar statusmeddelandet.
    /// </summary>
    public required string ServerVersion { get; init; }

    /// <summary>
    /// Anger om DCS för närvarande kör.
    ///
    /// Null innebär att servern ännu inte kan avgöra DCS-status.
    /// </summary>
    public bool? IsDcsRunning { get; init; }

    /// <summary>
    /// ID för den DCS-modul som servern har identifierat som aktiv.
    /// Är null när ingen modul har identifierats.
    /// </summary>
    public string? ActiveModuleId { get; init; }
}