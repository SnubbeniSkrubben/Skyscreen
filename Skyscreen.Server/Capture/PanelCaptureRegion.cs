// Path: Skyscreen.Server/Capture/PanelCaptureRegion.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Beskriver ett rektangulärt område i Windows virtuella
/// skrivbord som ska fångas för en panel.
///
/// Koordinaterna är maskinspecifika runtime-data och får inte
/// hårdkodas utifrån utvecklingsdatorns skärmkonfiguration.
/// </summary>
public sealed record PanelCaptureRegion
{
    /// <summary>
    /// X-koordinat i det virtuella skrivbordets koordinatsystem.
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Y-koordinat i det virtuella skrivbordets koordinatsystem.
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// Bredden på området som ska fångas, i pixlar.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Höjden på området som ska fångas, i pixlar.
    /// </summary>
    public required int Height { get; init; }
}