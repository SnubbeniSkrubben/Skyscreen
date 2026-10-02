// Path: Skyscreen.App/Pages/ModuleIndexPage.xaml.cs

using Skyscreen.Core.Models;
using Skyscreen.Profiles;

namespace Skyscreen.App.Pages;

/// <summary>
/// Startsida där användaren väljer vilken DCS-modul som ska användas.
/// </summary>
public partial class ModuleIndexPage : ContentPage
{
    public ModuleIndexPage()
    {
        InitializeComponent();

        LoadModules();
    }

    /// <summary>
    /// Hämtar alla aktiverade DCS-moduler från profilregistret
    /// och visar dem på indexsidan.
    /// </summary>
    private void LoadModules()
    {
        ModulesCollectionView.ItemsSource = ProfileCatalog.GetEnabled();
    }

    /// <summary>
    /// Hanterar val av flygplan eller helikopter och öppnar
    /// sidan där användaren väljer cockpitpanel.
    /// </summary>
    private async void OnModuleSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not AircraftModule module)
        {
            return;
        }

        ModulesCollectionView.SelectedItem = null;

        await Navigation.PushAsync(
            new PanelIndexPage(module.Id));
    }
}