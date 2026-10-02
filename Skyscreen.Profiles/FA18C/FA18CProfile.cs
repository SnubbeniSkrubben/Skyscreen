// Path: Skyscreen.Profiles/FA18C/FA18CProfile.cs

using Skyscreen.Core.Models;

namespace Skyscreen.Profiles.FA18C;

/// <summary>
/// Profildefinition för DCS F/A-18C Hornet.
/// </summary>
public static class FA18CProfile
{
    /// <summary>
    /// Skapar och returnerar F/A-18C Hornet-profilen.
    /// </summary>
    public static AircraftModule Create()
    {
        return new AircraftModule
        {
            Id = "fa18c",

            DisplayName = "F/A-18C Hornet",

            DcsModuleName = "FA-18C_hornet",

            Manufacturer = "Boeing",

            Category = AircraftCategory.Aircraft,

            IsEnabled = true,

            Panels =
            [
                new PanelDefinition
                {
                    Id = "left-ddi",

                    DisplayName = "Left DDI",

                    Type = PanelType.Hybrid,

                    DcsViewportName = "LEFT_MFCD",

                    BezelAsset = "fa18c/left-ddi.png",

                    IsEnabled = true,

                    Controls = Array.Empty<PanelControlDefinition>()
                },

                new PanelDefinition
                {
                    Id = "right-ddi",

                    DisplayName = "Right DDI",

                    Type = PanelType.Hybrid,

                    DcsViewportName = "RIGHT_MFCD",

                    BezelAsset = "fa18c/right-ddi.png",

                    IsEnabled = true,

                    Controls = Array.Empty<PanelControlDefinition>()
                },

                new PanelDefinition
                {
                    Id = "ampcd",

                    DisplayName = "AMPCD",

                    Type = PanelType.Hybrid,

                    DcsViewportName = "CENTER_MFCD",

                    BezelAsset = "fa18c/ampcd.png",

                    IsEnabled = true,

                    Controls = Array.Empty<PanelControlDefinition>()
                }
            ]
        };
    }
}