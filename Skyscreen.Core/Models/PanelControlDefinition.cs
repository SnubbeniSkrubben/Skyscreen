// Path: Skyscreen.Core/Models/PanelControlDefinition.cs

namespace Skyscreen.Core.Models;

/// <summary>
/// Beskriver en interaktiv kontroll på en cockpitpanel.
/// Exempel: pushbutton, knapp eller ratt.
/// </summary>
public sealed class PanelControlDefinition
{
    /// <summary>
    /// Unikt ID för kontrollen inom panelen.
    /// Exempel: "pb01".
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Namnet som kan visas i Skyscreen.
    /// Exempel: "PB 01".
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Vilken typ av kontroll detta är.
    /// </summary>
    public PanelControlType Type { get; init; }

    /// <summary>
    /// Kommandot eller kontroll-ID:t som används mot DCS.
    /// Exempel: "LEFT_DDI_PB_01".
    /// </summary>
    public required string DcsControlId { get; init; }

    /// <summary>
    /// Kontrollens horisontella position i panelens koordinatsystem.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Kontrollens vertikala position i panelens koordinatsystem.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Kontrollens bredd i panelens koordinatsystem.
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Kontrollens höjd i panelens koordinatsystem.
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// Anger om kontrollen för närvarande ska vara aktiv i Skyscreen.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Typ av interaktiv kontroll på en cockpitpanel.
/// </summary>
public enum PanelControlType
{
    PushButton = 1,
    ToggleButton = 2,
    Rotary = 3,
    Axis = 4
}