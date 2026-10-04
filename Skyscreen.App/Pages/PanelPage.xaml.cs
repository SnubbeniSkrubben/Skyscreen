// Path: Skyscreen.App/Pages/PanelPage.xaml.cs

using System.Diagnostics;
using Skyscreen.App.Services;
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
    private readonly string _subscriptionId;
    private readonly ISkyscreenClientService? _clientService;

    private AircraftModule? _module;
    private PanelDefinition? _panel;
    private bool _isSubscribed;

    /// <summary>
    /// Befintlig konstruktor som behålls tillfälligt medan
    /// navigationskedjan kopplas till klienttjänsten stegvis.
    /// </summary>
    public PanelPage(
        string moduleId,
        string panelId)
        : this(
            moduleId,
            panelId,
            clientService: null)
    {
    }

    /// <summary>
    /// Skapar panelsidan med tillgång till appens gemensamma
    /// klienttjänst för panelprenumerationen.
    /// </summary>
    public PanelPage(
        string moduleId,
        string panelId,
        ISkyscreenClientService? clientService)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(panelId);

        InitializeComponent();

        _moduleId = moduleId;
        _panelId = panelId;
        _clientService = clientService;

        // Samma logiska panel på samma klient ska använda samma
        // SubscriptionId. Det gör prenumerationen möjlig att
        // uppdatera och avsluta på ett deterministiskt sätt.
        _subscriptionId =
            $"panel:{_moduleId}:{_panelId}";

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
    /// Startar panelens logiska prenumeration när sidan blir aktiv.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_panel is null
            || _clientService is null
            || _isSubscribed
            || !_clientService.IsConnected)
        {
            return;
        }

        try
        {
            await _clientService.SubscribePanelAsync(
                _subscriptionId,
                _moduleId,
                _panelId,
                ShouldReceiveVideo(_panel),
                ShouldEnableInput(_panel),
                CancellationToken.None);

            _isSubscribed = true;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(
                $"Skyscreen: panelprenumerationen kunde inte startas. {exception}");
        }
    }

    /// <summary>
    /// Avslutar panelens logiska prenumeration när sidan
    /// inte längre är aktiv.
    /// </summary>
    protected override async void OnDisappearing()
    {
        if (_clientService is not null
            && _isSubscribed)
        {
            try
            {
                if (_clientService.IsConnected)
                {
                    await _clientService.UnsubscribePanelAsync(
                        _subscriptionId,
                        CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(
                    $"Skyscreen: panelprenumerationen kunde inte avslutas. {exception}");
            }
            finally
            {
                _isSubscribed = false;
            }
        }

        base.OnDisappearing();
    }

    /// <summary>
    /// Anger om paneltypen innehåller en videodisplay.
    /// </summary>
    private static bool ShouldReceiveVideo(
        PanelDefinition panel)
    {
        return panel.Type is
            PanelType.Display
            or PanelType.Hybrid;
    }

    /// <summary>
    /// Anger om paneltypen kan innehålla interaktiva kontroller.
    /// </summary>
    private static bool ShouldEnableInput(
        PanelDefinition panel)
    {
        return panel.Type is
            PanelType.ControlPanel
            or PanelType.Hybrid;
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