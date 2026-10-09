// Path: Skyscreen.Server/Streaming/JpegPanelFrameEncoderOptions.cs

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Inställningar för JPEG-kodning av panelbilder.
/// </summary>
public sealed class JpegPanelFrameEncoderOptions
{
    /// <summary>
    /// Konfigurationssektionens namn.
    /// </summary>
    public const string SectionName =
        "Skyscreen:Streaming:Jpeg";

    /// <summary>
    /// JPEG-kvalitet från 1 till 100.
    ///
    /// 80 används som första standardvärde för att ge
    /// en rimlig balans mellan bildkvalitet, datamängd
    /// och kodningstid.
    /// </summary>
    public int Quality { get; init; } = 80;
}