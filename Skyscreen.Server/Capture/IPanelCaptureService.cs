// Path: Skyscreen.Server/Capture/IPanelCaptureService.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Högre capture-abstraktion för logiska Skyscreen-paneler.
///
/// Tjänsten översätter ModuleId/PanelId till en maskinspecifik
/// capture-region och använder därefter den konkreta
/// capture-källan för att producera en bildruta.
/// </summary>
public interface IPanelCaptureService
{
    /// <summary>
    /// Försöker fånga en bildruta för angiven panel.
    /// </summary>
    /// <param name="moduleId">
    /// Skyscreens logiska modul-ID, exempelvis fa18c.
    /// </param>
    /// <param name="panelId">
    /// Skyscreens logiska panel-ID, exempelvis left-ddi.
    /// </param>
    /// <param name="cancellationToken">
    /// Token för kontrollerad avbrytning.
    /// </param>
    /// <returns>
    /// En bildruta om panelen har en konfigurerad capture-region,
    /// annars null.
    /// </returns>
    Task<PanelCaptureFrame?> CaptureAsync(
        string moduleId,
        string panelId,
        CancellationToken cancellationToken);
}