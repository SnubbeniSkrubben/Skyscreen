// Path: Skyscreen.Server/Capture/PanelCaptureRegionConfiguration.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Maskinspecifik konfiguration för en panels capture-region.
///
/// ModuleId och PanelId identifierar den logiska Skyscreen-panelen.
/// Region beskriver var den exporterade DCS-panelen finns på den
/// aktuella datorns virtuella skrivbord.
/// </summary>
public sealed class PanelCaptureRegionConfiguration
{
    /// <summary>
    /// Skyscreens logiska modul-ID, exempelvis fa18c.
    /// </summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// Skyscreens logiska panel-ID, exempelvis left-ddi.
    /// </summary>
    public required string PanelId { get; init; }

    /// <summary>
    /// Den maskinspecifika capture-regionen.
    /// </summary>
    public required PanelCaptureRegion Region { get; init; }
}