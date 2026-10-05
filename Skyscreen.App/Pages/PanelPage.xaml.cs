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
    private bool _isSubscriptionDesired;

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
    /// Registrerar att panelens prenumeration önskas när sidan
    /// blir aktiv.
    ///
    /// Klienttjänsten ansvarar för om prenumerationen kan skickas
    /// direkt eller ska sparas tills serveranslutningen är tillgänglig.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_panel is null
            || _clientService is null
            || _isSubscriptionDesired)
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

            _isSubscriptionDesired = true;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(
                $"Skyscreen: panelprenumerationen kunde inte registreras som önskad. {exception}");
        }
    }

    /// <summary>
    /// Tar bort panelens önskade prenumeration när sidan
    /// inte längre är aktiv.
    ///
    /// Detta görs oavsett om serveranslutningen för tillfället
    /// är tillgänglig eller inte.
    /// </summary>
    protected override async void OnDisappearing()
    {
        if (_clientService is not null
            && _isSubscriptionDesired)
        {
            try
            {
                await _clientService.UnsubscribePanelAsync(
                    _subscriptionId,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(
                    $"Skyscreen: panelprenumerationen kunde inte tas bort från önskat tillstånd. {exception}");
            }
            finally
            {
                _isSubscriptionDesired = false;
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