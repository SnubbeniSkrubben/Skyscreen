// Path: Skyscreen.Core/Protocol/SkyscreenMessage.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Baskontrakt för logiska meddelanden mellan Skyscreen.App och Skyscreen.Server.
/// Meddelandet är transportoberoende och ska kunna användas oavsett om
/// kommunikationen sker via Wi-Fi eller USB.
/// </summary>
public abstract class SkyscreenMessage
{
    /// <summary>
    /// Aktuell version av Skyscreens kommunikationsprotokoll.
    /// </summary>
    public const int CurrentProtocolVersion = 1;

    /// <summary>
    /// Protokollversion som meddelandet använder.
    /// </summary>
    public int ProtocolVersion { get; init; } = CurrentProtocolVersion;

    /// <summary>
    /// Identifierar den logiska meddelandetypen.
    /// </summary>
    public abstract string MessageType { get; }
}