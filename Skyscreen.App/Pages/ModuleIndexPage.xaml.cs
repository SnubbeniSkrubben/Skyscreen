// Path: Skyscreen.App/Pages/ModuleIndexPage.xaml.cs

using Skyscreen.App.Services;
using Skyscreen.Core.Models;
using Skyscreen.Profiles;

namespace Skyscreen.App.Pages;

/// <summary>
/// Startsida där användaren väljer vilken DCS-modul som ska användas.
/// </summary>
public partial class ModuleIndexPage : ContentPage
{
    private readonly ISkyscreenClientService? _clientService;

    /// <summary>
    /// Parameterlös konstruktor som används av nuvarande Shell-konfiguration.
    ///
    /// Den behålls tillfälligt medan navigationskedjan kopplas till
    /// klienttjänsten stegvis.
    /// </summary>
    public ModuleIndexPage()
        : this(clientService: null)
    {
    }

    /// <summary>
    /// Skapar modulsidan med appens gemensamma klienttjänst så att
    /// samma klientanslutning kan följa med genom navigationskedjan.
    /// </summary>
    public ModuleIndexPage(
        ISkyscreenClientService? clientService)
    {
        InitializeComponent();

        _clientService = clientService;

        LoadModules();
    }

    /// <summary>
    /// Hämtar alla aktiverade DCS-moduler från profilregistret
    /// och visar dem på indexsidan.
    /// </summary>
    private void LoadModules()
    {
        ModulesCollectionView.ItemsSource =
            ProfileCatalog.GetEnabled();
    }

    /// <summary>
    /// Hanterar val av flygplan eller helikopter och öppnar
    /// sidan där användaren väljer cockpitpanel.
    /// </summary>
    private async void OnModuleSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault()
            is not AircraftModule module)
        {
            return;
        }

        ModulesCollectionView.SelectedItem = null;

        await Navigation.PushAsync(
            new PanelIndexPage(
                module.Id,
                _clientService));
    }
}