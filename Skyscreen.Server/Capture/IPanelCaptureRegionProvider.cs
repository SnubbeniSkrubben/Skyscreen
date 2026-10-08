// Path: Skyscreen.Server/Capture/IPanelCaptureRegionProvider.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Tillhandahåller den maskinspecifika capture-regionen
/// för en logisk Skyscreen-panel.
///
/// Profilinformationen identifierar vilken panel som önskas,
/// medan denna provider ansvarar för var panelens renderingsyta
/// faktiskt finns på den aktuella Windows-datorn.
///
/// På så sätt behöver exempelvis F/A-18C Left DDI inte känna till
/// några skärmkoordinater.
/// </summary>
public interface IPanelCaptureRegionProvider
{
    /// <summary>
    /// Försöker hitta capture-regionen för angiven modul och panel.
    /// </summary>
    /// <param name="moduleId">
    /// Skyscreens logiska modul-ID, exempelvis fa18c.
    /// </param>
    /// <param name="panelId">
    /// Skyscreens logiska panel-ID, exempelvis left-ddi.
    /// </param>
    /// <param name="region">
    /// Den maskinspecifika regionen om en konfiguration finns.
    /// </param>
    /// <returns>
    /// true om en capture-region finns konfigurerad, annars false.
    /// </returns>
    bool TryGetRegion(
        string moduleId,
        string panelId,
        out PanelCaptureRegion? region);
}