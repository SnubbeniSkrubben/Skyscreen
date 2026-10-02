// Path: Skyscreen.Core/Models/AircraftModule.cs

namespace Skyscreen.Core.Models;

/// <summary>
/// Beskriver en DCS-modul som Skyscreen kan stödja,
/// exempelvis F/A-18C Hornet eller AH-64D Apache.
/// </summary>
public sealed class AircraftModule
{
    /// <summary>
    /// Skyscreens interna unika ID för modulen.
    /// Exempel: "fa18c".
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Namnet som visas för användaren i Skyscreen.
    /// Exempel: "F/A-18C Hornet".
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Modulnamnet som DCS använder internt.
    /// Exempel: "FA-18C_hornet".
    /// </summary>
    public required string DcsModuleName { get; init; }

    /// <summary>
    /// Tillverkare, exempelvis Boeing eller Lockheed Martin.
    /// </summary>
    public string? Manufacturer { get; init; }

    /// <summary>
    /// Anger om modulen är ett flygplan eller en helikopter.
    /// </summary>
    public AircraftCategory Category { get; init; }

    /// <summary>
    /// Anger om modulen för närvarande ska visas som tillgänglig i appen.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Paneler som finns tillgängliga för modulen.
    /// Exempel: Left DDI, Right DDI och AMPCD.
    /// </summary>
    public IReadOnlyList<PanelDefinition> Panels { get; init; }
        = Array.Empty<PanelDefinition>();
}

/// <summary>
/// Huvudkategori för en DCS-modul.
/// </summary>
public enum AircraftCategory
{
    Aircraft = 1,
    Helicopter = 2
}