// Path: Skyscreen.Server/Streaming/EncodedPanelFrame.cs

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Representerar en kodad panelbild som är redo
/// för nätverkstransport.
///
/// Capture-lagret producerar råa PanelCaptureFrame.
/// Streaming-lagret ansvarar därefter för encoding
/// och distribution till klienter.
/// </summary>
public sealed record EncodedPanelFrame
{
    /// <summary>
    /// Bildrutans bredd i pixlar.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Bildrutans höjd i pixlar.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// MIME-typ för den kodade bilddatan.
    ///
    /// För första implementationen används image/jpeg.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Den kodade bilddatan.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// UTC-tidpunkt då den ursprungliga
    /// capture-bildrutan fångades.
    /// </summary>
    public required DateTimeOffset CapturedAtUtc { get; init; }
}