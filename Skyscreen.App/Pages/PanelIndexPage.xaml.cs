// Path: Skyscreen.App/Pages/PanelIndexPage.xaml.cs

using Skyscreen.App.Services;
using Skyscreen.Core.Models;
using Skyscreen.Profiles;

namespace Skyscreen.App.Pages;

/// <summary>
/// Sida där användaren väljer vilken cockpitpanel som ska öppnas
/// för den valda DCS-modulen.
/// </summary>
public partial class PanelIndexPage : ContentPage
{
    private readonly string _moduleId;
    private readonly ISkyscreenClientService? _clientService;

    /// <summary>
    /// Befintlig konstruktor som behålls tillfälligt medan
    /// navigationskedjan kopplas till klienttjänsten stegvis.
    /// </summary>
    public PanelIndexPage(string moduleId)
        : this(
            moduleId,
            clientService: null)
    {
    }

    /// <summary>
    /// Skapar sidan för vald modul och behåller appens gemensamma
    /// klienttjänst så att den kan skickas vidare till panelsidan.
    /// </summary>
    public PanelIndexPage(
        string moduleId,
        ISkyscreenClientService? clientService)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        InitializeComponent();

        _moduleId = moduleId;
        _clientService = clientService;

        LoadModule();
    }

    /// <summary>
    /// Hämtar vald modul och visar dess tillgängliga paneler.
    /// </summary>
    private void LoadModule()
    {
        AircraftModule? module = ProfileCatalog.FindById(_moduleId);

        if (module is null)
        {
            ModuleNameLabel.Text = "Okänd modul";
            PanelsCollectionView.ItemsSource =
                Array.Empty<PanelDefinition>();

            return;
        }

        ModuleNameLabel.Text = module.DisplayName;

        PanelsCollectionView.ItemsSource = module.Panels
            .Where(panel => panel.IsEnabled)
            .ToArray();
    }

    /// <summary>
    /// Hanterar val av cockpitpanel och öppnar den generella
    /// panelsidan med samma klienttjänst som navigationskedjan
    /// mottagit.
    /// </summary>
    private async void OnPanelSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault()
            is not PanelDefinition panel)
        {
            return;
        }

        PanelsCollectionView.SelectedItem = null;

        await Navigation.PushAsync(
            new PanelPage(
                _moduleId,
                panel.Id,
                _clientService));
    }
}