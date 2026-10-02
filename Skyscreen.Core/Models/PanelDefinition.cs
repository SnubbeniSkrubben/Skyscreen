// Path: Skyscreen.Core/Models/PanelDefinition.cs

namespace Skyscreen.Core.Models;

/// <summary>
/// Beskriver en enskild cockpitpanel som Skyscreen kan visa och styra.
/// Exempel: vänster DDI i F/A-18C eller höger MFD i F-16C.
/// </summary>
public sealed class PanelDefinition
{
    /// <summary>
    /// Unikt ID för panelen inom modulen.
    /// Exempel: "left-ddi".
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Namnet som visas för användaren.
    /// Exempel: "Left DDI".
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Vilken typ av panel detta är.
    /// </summary>
    public PanelType Type { get; init; }

    /// <summary>
    /// Panelens totala logiska bredd.
    /// Används som gemensamt koordinatsystem för grafik,
    /// displayyta och touchkontroller.
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Panelens totala logiska höjd.
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// Om panelen innehåller en DCS-display anger denna
    /// var displayytan ligger inom panelens koordinatsystem.
    /// </summary>
    public PanelDisplayArea? DisplayArea { get; init; }

    /// <summary>
    /// Namnet på den viewport/export som DCS använder.
    /// Exempel: "LEFT_MFCD".
    /// Kan vara null för paneler som inte har en videodisplay.
    /// </summary>
    public string? DcsViewportName { get; init; }

    /// <summary>
    /// Sökväg eller resursnamn till panelens bezel-/bakgrundsgrafik.
    /// </summary>
    public string? BezelAsset { get; init; }

    /// <summary>
    /// Panelens interaktiva kontroller, exempelvis pushbuttons och rattar.
    /// </summary>
    public IReadOnlyList<PanelControlDefinition> Controls { get; init; }
        = Array.Empty<PanelControlDefinition>();

    /// <summary>
    /// Anger om panelen för närvarande ska vara tillgänglig i Skyscreen.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Huvudtyp för en cockpitpanel.
/// </summary>
public enum PanelType
{
    Display = 1,
    ControlPanel = 2,
    Hybrid = 3
}