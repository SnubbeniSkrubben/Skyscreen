// Path: Skyscreen.App/App.xaml.cs

using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Skyscreen.App.Services;

namespace Skyscreen.App
{
    public partial class App : Application
    {
        private readonly ISkyscreenClientService _clientService;
        private readonly IServerDiscoveryClient _serverDiscoveryClient;
        private readonly IServerEndpointProvider _serverEndpointProvider;
        private readonly ILogger<App> _logger;

        public App(
            ISkyscreenClientService clientService,
            IServerDiscoveryClient serverDiscoveryClient,
            IServerEndpointProvider serverEndpointProvider,
            ILogger<App> logger)
        {
            ArgumentNullException.ThrowIfNull(clientService);
            ArgumentNullException.ThrowIfNull(serverDiscoveryClient);
            ArgumentNullException.ThrowIfNull(serverEndpointProvider);
            ArgumentNullException.ThrowIfNull(logger);

            InitializeComponent();

            _clientService = clientService;
            _serverDiscoveryClient = serverDiscoveryClient;
            _serverEndpointProvider = serverEndpointProvider;
            _logger = logger;
        }

        protected override Window CreateWindow(
            IActivationState? activationState)
        {
            Window window =
                new(new AppShell(_clientService));

            window.Created += OnWindowCreated;

            return window;
        }

        /// <summary>
        /// Försöker hitta Skyscreen.Server via lokal discovery och
        /// etablerar därefter klientanslutningen.
        ///
        /// Android-emulatorn får i Debug använda den särskilda
        /// utvecklingsendpointen som fallback.
        ///
        /// En fysisk enhet använder inte emulatoradressen 10.0.2.2.
        /// </summary>
        private async void OnWindowCreated(
            object? sender,
            EventArgs e)
        {
            Uri? endpoint = null;

            try
            {
                endpoint =
                    await _serverDiscoveryClient.DiscoverAsync(
                        CancellationToken.None);

                if (endpoint is not null)
                {
                    _logger.LogInformation(
                        "Skyscreen.Server hittades via discovery: {Endpoint}",
                        endpoint);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Server discovery misslyckades.");
            }

            if (endpoint is null)
            {
                endpoint =
                    GetDevelopmentFallbackEndpoint();

                if (endpoint is not null)
                {
                    _logger.LogInformation(
                        "Discovery hittade ingen server. Emulatorns utvecklingsendpoint används: {Endpoint}",
                        endpoint);
                }
            }

            if (endpoint is null)
            {
                _logger.LogWarning(
                    "Ingen Skyscreen.Server kunde hittas. Appen fortsätter utan serveranslutning.");

                return;
            }

            try
            {
                await _clientService.ConnectAsync(
                    endpoint,
                    CancellationToken.None);

                _logger.LogInformation(
                    "Skyscreen.App ansluten till servern {Endpoint}. ClientId: {ClientId}",
                    endpoint,
                    _clientService.ClientId);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skyscreen.App kunde inte ansluta till servern {Endpoint}.",
                    endpoint);
            }
        }

        /// <summary>
        /// Returnerar emulatorns särskilda utvecklingsendpoint endast
        /// vid Debug-körning på en virtuell enhet.
        /// </summary>
        private Uri? GetDevelopmentFallbackEndpoint()
        {
#if DEBUG
            if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
            {
                return _serverEndpointProvider.GetServerEndpoint();
            }
#endif

            return null;
        }
    }
}