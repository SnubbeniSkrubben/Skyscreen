// Path: Skyscreen.Core/Models/PanelDisplayArea.cs

namespace Skyscreen.Core.Models;

/// <summary>
/// Beskriver var den faktiska DCS-displayytan ligger
/// inom en cockpitpanel.
/// </summary>
public sealed class PanelDisplayArea
{
    /// <summary>
    /// Horisontell position inom panelens koordinatsystem.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Vertikal position inom panelens koordinatsystem.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Displayytans bredd.
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Displayytans höjd.
    /// </summary>
    public double Height { get; init; }
}