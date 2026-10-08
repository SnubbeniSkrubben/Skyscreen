// Path: Skyscreen.Server/Capture/PanelCaptureFrame.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Representerar en fångad bildruta från en panelregion.
///
/// Bilddata lagras med en positiv stride och radordning
/// uppifrån och ned.
///
/// Capture-lagret ansvarar endast för att producera råa
/// bildrutor. Kodning och nätverkstransport ska ligga i
/// separata lager.
/// </summary>
public sealed record PanelCaptureFrame
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
    /// Bildrutans pixel-format.
    /// </summary>
    public PanelCapturePixelFormat PixelFormat { get; init; } =
        PanelCapturePixelFormat.Bgrx32;

    /// <summary>
    /// Antal byte mellan början av två efterföljande pixelrader.
    ///
    /// För BGRX32 är minsta giltiga stride Width * 4.
    /// </summary>
    public required int Stride { get; init; }

    /// <summary>
    /// Bildrutans råa pixeldata.
    ///
    /// För BGRX32 lagras varje pixel som:
    /// Blue, Green, Red, unused.
    ///
    /// Bufferten ägs av denna bildruta och får inte återanvändas
    /// av capture-implementationen efter att bildrutan returnerats.
    /// </summary>
    public required byte[] PixelData { get; init; }

    /// <summary>
    /// UTC-tidpunkt då bildrutan fångades.
    /// </summary>
    public required DateTimeOffset CapturedAtUtc { get; init; }
}