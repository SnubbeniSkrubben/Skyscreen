// Path: Skyscreen.Server/Streaming/PanelStreamingDiagnosticOptions.cs

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Inställningar för ett explicit diagnostiskt
/// engångstest av capture + JPEG-encoding.
///
/// Funktionen är avstängd som standard.
/// </summary>
public sealed class PanelStreamingDiagnosticOptions
{
    /// <summary>
    /// Konfigurationssektionens namn.
    /// </summary>
    public const string SectionName =
        "Skyscreen:StreamingDiagnostic";

    /// <summary>
    /// Anger om diagnostiktestet ska köras.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Skyscreens logiska modul-ID.
    /// Exempel: fa18c.
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// Skyscreens logiska panel-ID.
    /// Exempel: left-ddi.
    /// </summary>
    public string? PanelId { get; init; }

    /// <summary>
    /// Filnamn eller full sökväg där den kodade
    /// JPEG-bilden ska sparas.
    /// </summary>
    public string? OutputPath { get; init; }
}