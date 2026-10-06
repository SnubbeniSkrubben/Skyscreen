// Path: Skyscreen.App/Pages/ModuleIndexPage.xaml.cs

using Skyscreen.App.Services;
using Skyscreen.Core.Models;
using Skyscreen.Core.Protocol;
using Skyscreen.Profiles;

namespace Skyscreen.App.Pages;

/// <summary>
/// Startsida där användaren väljer vilken DCS-modul som ska användas.
///
/// Sidan visar även appens aktuella anslutningsstatus mot servern
/// samt senast mottagna ServerStatus.
/// </summary>
public partial class ModuleIndexPage : ContentPage
{
    private readonly ISkyscreenClientService? _clientService;

    private bool _statusEventsSubscribed;

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
        UpdateStatusLabels();
    }

    /// <summary>
    /// Registrerar sidan för statusändringar när den blir synlig.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        SubscribeToStatusEvents();
        UpdateStatusLabels();
    }

    /// <summary>
    /// Avregistrerar sidan från statusändringar när användaren
    /// navigerar bort från sidan.
    /// </summary>
    protected override void OnDisappearing()
    {
        UnsubscribeFromStatusEvents();

        base.OnDisappearing();
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
    /// Registrerar sidan för klienttjänstens statusändringar.
    /// </summary>
    private void SubscribeToStatusEvents()
    {
        if (_clientService is null
            || _statusEventsSubscribed)
        {
            return;
        }

        _clientService.ConnectionStateChanged +=
            OnConnectionStateChanged;

        _clientService.ServerStatusChanged +=
            OnServerStatusChanged;

        _statusEventsSubscribed = true;
    }

    /// <summary>
    /// Tar bort sidans statusprenumerationer.
    /// </summary>
    private void UnsubscribeFromStatusEvents()
    {
        if (_clientService is null
            || !_statusEventsSubscribed)
        {
            return;
        }

        _clientService.ConnectionStateChanged -=
            OnConnectionStateChanged;

        _clientService.ServerStatusChanged -=
            OnServerStatusChanged;

        _statusEventsSubscribed = false;
    }

    /// <summary>
    /// Hanterar ändrad anslutningsstatus från klienttjänsten.
    /// </summary>
    private void OnConnectionStateChanged(
        object? sender,
        EventArgs e)
    {
        Dispatcher.Dispatch(
            UpdateStatusLabels);
    }

    /// <summary>
    /// Hanterar ny eller rensad ServerStatus från klienttjänsten.
    /// </summary>
    private void OnServerStatusChanged(
        object? sender,
        EventArgs e)
    {
        Dispatcher.Dispatch(
            UpdateStatusLabels);
    }

    /// <summary>
    /// Uppdaterar statusytan med klientens aktuella
    /// anslutningstillstånd och senast mottagna serverstatus.
    /// </summary>
    private void UpdateStatusLabels()
    {
        if (_clientService is null)
        {
            ConnectionStatusLabel.Text =
                "Anslutningsstatus: Frånkopplad";

            ServerStatusLabel.Text =
                "Serverstatus: Ingen status mottagen";

            DcsStatusLabel.Text =
                "DCS-status: Okänd";

            return;
        }

        ConnectionStatusLabel.Text =
            $"Anslutningsstatus: {GetConnectionStateText(_clientService.ConnectionState)}";

        ServerStatusMessage? serverStatus =
            _clientService.ServerStatus;

        if (serverStatus is null)
        {
            ServerStatusLabel.Text =
                "Serverstatus: Ingen status mottagen";

            DcsStatusLabel.Text =
                "DCS-status: Okänd";

            return;
        }

        ServerStatusLabel.Text =
            $"Serverstatus: Version {serverStatus.ServerVersion}";

        DcsStatusLabel.Text =
            GetDcsStatusText(serverStatus);
    }

    /// <summary>
    /// Översätter klientens interna anslutningstillstånd
    /// till text för användargränssnittet.
    /// </summary>
    private static string GetConnectionStateText(
        SkyscreenConnectionState connectionState)
    {
        return connectionState switch
        {
            SkyscreenConnectionState.Disconnected =>
                "Frånkopplad",

            SkyscreenConnectionState.Connecting =>
                "Ansluter",

            SkyscreenConnectionState.Connected =>
                "Ansluten",

            SkyscreenConnectionState.Reconnecting =>
                "Återansluter",

            _ =>
                "Okänd"
        };
    }

    /// <summary>
    /// Skapar UI-text för serverns rapporterade DCS-status.
    /// </summary>
    private static string GetDcsStatusText(
        ServerStatusMessage serverStatus)
    {
        if (serverStatus.IsDcsRunning is null)
        {
            return "DCS-status: Okänd";
        }

        if (!serverStatus.IsDcsRunning.Value)
        {
            return "DCS-status: Körs inte";
        }

        if (string.IsNullOrWhiteSpace(
                serverStatus.ActiveModuleId))
        {
            return "DCS-status: Körs";
        }

        return
            $"DCS-status: Körs – aktiv modul: {serverStatus.ActiveModuleId}";
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