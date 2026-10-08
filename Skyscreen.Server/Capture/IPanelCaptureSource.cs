// Path: Skyscreen.Server/Capture/IPanelCaptureSource.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Abstraktion för att fånga ett rektangulärt område från
/// datorns renderingsyta.
///
/// Högre lager ska inte känna till vilken konkret
/// Windows-captureteknik som används.
/// </summary>
public interface IPanelCaptureSource
{
    /// <summary>
    /// Fångar en enskild bildruta från angiven region.
    /// </summary>
    /// <param name="region">
    /// Området i det virtuella skrivbordets koordinatsystem
    /// som ska fångas.
    /// </param>
    /// <param name="cancellationToken">
    /// Token för kontrollerad avbrytning.
    /// </param>
    /// <returns>
    /// En rå bildruta med egen pixelbuffer.
    ///
    /// Det konkreta pixel-formatet anges av
    /// <see cref="PanelCaptureFrame.PixelFormat"/>.
    /// </returns>
    Task<PanelCaptureFrame> CaptureAsync(
        PanelCaptureRegion region,
        CancellationToken cancellationToken);
}