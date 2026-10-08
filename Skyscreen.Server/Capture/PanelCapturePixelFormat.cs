// Path: Skyscreen.Server/Capture/PanelCapturePixelFormat.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Pixel-format som kan användas av Skyscreens
/// interna panelcapture.
/// </summary>
public enum PanelCapturePixelFormat
{
    /// <summary>
    /// 32 bitar per pixel i byteordningen:
    /// Blue, Green, Red, unused.
    ///
    /// Den fjärde byten ska inte tolkas som
    /// en giltig alfakanal.
    /// </summary>
    Bgrx32 = 1
}