// Path: Skyscreen.Server/Capture/PanelCaptureDiagnosticOptions.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Inställningar för ett explicit diagnostiskt
/// engångstest av panelcapture.
///
/// Funktionen ska vara avstängd som standard och används
/// endast för att verifiera capture-kedjan på en viss dator.
/// </summary>
public sealed class PanelCaptureDiagnosticOptions
{
    /// <summary>
    /// Konfigurationssektionens namn.
    /// </summary>
    public const string SectionName =
        "Skyscreen:PanelCaptureDiagnostic";

    /// <summary>
    /// Anger om det diagnostiska engångstestet ska köras.
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
    /// Filnamn eller full sökväg där den diagnostiska
    /// capture-bilden ska sparas.
    /// </summary>
    public string? OutputPath { get; init; }
}