// Path: Skyscreen.Server/Capture/PanelCaptureOptions.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Maskinspecifika inställningar för panelcapture.
///
/// Konfigurationen ska kunna bindas från exempelvis appsettings.json
/// och innehåller en lista med regioner för de paneler som den
/// aktuella datorn kan fånga.
/// </summary>
public sealed class PanelCaptureOptions
{
    /// <summary>
    /// Konfigurationssektionens namn.
    /// </summary>
    public const string SectionName = "Skyscreen:PanelCapture";

    /// <summary>
    /// Maskinspecifika capture-regioner.
    /// </summary>
    public List<PanelCaptureRegionConfiguration> Regions { get; init; } = [];
}