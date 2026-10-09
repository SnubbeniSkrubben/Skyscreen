// Path: Skyscreen.Server/Streaming/PanelVideoStreamOptions.cs

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Konfiguration för Skyscreens gemensamma
/// panelvideoströmmar.
/// </summary>
public sealed class PanelVideoStreamOptions
{
    /// <summary>
    /// Konfigurationssektion i appsettings.
    /// </summary>
    public const string SectionName =
        "Skyscreen:Streaming:Video";

    /// <summary>
    /// Målfrekvens för panelcapture och kodning.
    ///
    /// Det faktiska antalet frames per sekund kan bli lägre
    /// om capture eller kodning tar längre tid än bildintervallet.
    /// </summary>
    public int FramesPerSecond { get; init; } = 20;
}