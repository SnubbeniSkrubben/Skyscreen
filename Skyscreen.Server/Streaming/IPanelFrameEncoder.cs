// Path: Skyscreen.Server/Streaming/IPanelFrameEncoder.cs

using Skyscreen.Server.Capture;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Abstraktion för att koda en rå panelbild
/// till ett format som är lämpligt för nätverkstransport.
/// </summary>
public interface IPanelFrameEncoder
{
    /// <summary>
    /// Kodar en rå capture-bildruta.
    /// </summary>
    /// <param name="frame">
    /// Den råa bildrutan som ska kodas.
    /// </param>
    /// <param name="cancellationToken">
    /// Token för kontrollerad avbrytning.
    /// </param>
    /// <returns>
    /// Den kodade bildrutan.
    /// </returns>
    Task<EncodedPanelFrame> EncodeAsync(
        PanelCaptureFrame frame,
        CancellationToken cancellationToken);
}