// Path: Skyscreen.App/Pages/PanelPage.xaml.cs

using Skyscreen.Core.Models;
using Skyscreen.Profiles;

namespace Skyscreen.App.Pages;

/// <summary>
/// Generell sida för att visa en cockpitpanel.
/// Samma sida ska kunna användas för flera flygplan,
/// helikoptrar och olika paneltyper.
/// </summary>
public partial class PanelPage : ContentPage
{
    private readonly string _moduleId;
    private readonly string _panelId;

    private AircraftModule? _module;
    private PanelDefinition? _panel;

    public PanelPage(
        string moduleId,
        string panelId)
    {
        InitializeComponent();

        _moduleId = moduleId;
        _panelId = panelId;

        LoadPanel();
    }

    /// <summary>
    /// Hämtar vald DCS-modul och vald panel från profilregistret.
    /// </summary>
    private void LoadPanel()
    {
        _module = ProfileCatalog.FindById(_moduleId);

        if (_module is null)
        {
            ShowPanelNotFound();
            return;
        }

        _panel = _module.Panels.FirstOrDefault(
            panel => string.Equals(
                panel.Id,
                _panelId,
                StringComparison.OrdinalIgnoreCase));

        if (_panel is null)
        {
            ShowPanelNotFound();
            return;
        }

        ModuleNameLabel.Text = _module.DisplayName;
        PanelNameLabel.Text = _panel.DisplayName;

        PlaceholderLabel.Text =
            $"{_panel.DisplayName}\n\nLIVE DCS VIEW";
    }

    /// <summary>
    /// Visar ett tydligt fel om modulen eller panelen inte kan hittas.
    /// </summary>
    private void ShowPanelNotFound()
    {
        ModuleNameLabel.Text = "Okänd modul";
        PanelNameLabel.Text = "Panelen kunde inte hittas";
        PlaceholderLabel.Text = "PANEL NOT FOUND";
    }
}