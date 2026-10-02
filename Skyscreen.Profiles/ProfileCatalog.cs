// Path: Skyscreen.Profiles/ProfileCatalog.cs

using Skyscreen.Core.Models;
using Skyscreen.Profiles.FA18C;

namespace Skyscreen.Profiles;

/// <summary>
/// Centralt register över alla DCS-moduler som Skyscreen känner till.
/// </summary>
public static class ProfileCatalog
{
    /// <summary>
    /// Returnerar alla registrerade flygplans- och helikopterprofiler.
    /// </summary>
    public static IReadOnlyList<AircraftModule> GetAll()
    {
        return
        [
            FA18CProfile.Create()
        ];
    }

    /// <summary>
    /// Returnerar alla profiler som för närvarande är aktiverade i Skyscreen.
    /// </summary>
    public static IReadOnlyList<AircraftModule> GetEnabled()
    {
        return GetAll()
            .Where(module => module.IsEnabled)
            .ToArray();
    }

    /// <summary>
    /// Söker efter en profil med hjälp av Skyscreens interna modul-ID.
    /// Exempel: "fa18c".
    /// </summary>
    public static AircraftModule? FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return GetAll().FirstOrDefault(
            module => string.Equals(
                module.Id,
                id,
                StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Söker efter en profil med hjälp av DCS interna modulnamn.
    /// Exempel: "FA-18C_hornet".
    /// </summary>
    public static AircraftModule? FindByDcsModuleName(string dcsModuleName)
    {
        if (string.IsNullOrWhiteSpace(dcsModuleName))
        {
            return null;
        }

        return GetAll().FirstOrDefault(
            module => string.Equals(
                module.DcsModuleName,
                dcsModuleName,
                StringComparison.OrdinalIgnoreCase));
    }
}