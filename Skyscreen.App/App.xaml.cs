// Path: Skyscreen.App/App.xaml.cs

using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Skyscreen.App.Services;

namespace Skyscreen.App
{
    public partial class App : Application
    {
        private static readonly TimeSpan DiscoveryRetryDelay =
            TimeSpan.FromSeconds(2);

        private readonly ISkyscreenClientService _clientService;
        private readonly IServerDiscoveryClient _serverDiscoveryClient;
        private readonly IServerEndpointProvider _serverEndpointProvider;
        private readonly ILogger<App> _logger;

        private CancellationTokenSource? _windowLifetimeCancellation;

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
            _windowLifetimeCancellation?.Dispose();
            _windowLifetimeCancellation =
                new CancellationTokenSource();

            Window window =
                new(new AppShell(_clientService));

            window.Created += OnWindowCreated;
            window.Destroying += OnWindowDestroying;

            return window;
        }

        /// <summary>
        /// Startar serverupptäckt när appfönstret har skapats.
        ///
        /// På en fysisk enhet upprepas discovery tills en server
        /// hittas eller appfönstret förstörs.
        ///
        /// Android-emulatorn får i Debug använda den särskilda
        /// utvecklingsendpointen som fallback.
        /// </summary>
        private async void OnWindowCreated(
            object? sender,
            EventArgs e)
        {
            CancellationTokenSource? lifetimeCancellation =
                _windowLifetimeCancellation;

            if (lifetimeCancellation is null)
            {
                return;
            }

            await ConnectToServerAsync(
                lifetimeCancellation.Token);
        }

        /// <summary>
        /// Avbryter pågående discovery när appfönstret förstörs.
        /// </summary>
        private void OnWindowDestroying(
            object? sender,
            EventArgs e)
        {
            CancellationTokenSource? lifetimeCancellation =
                _windowLifetimeCancellation;

            _windowLifetimeCancellation = null;

            if (lifetimeCancellation is null)
            {
                return;
            }

            lifetimeCancellation.Cancel();
            lifetimeCancellation.Dispose();
        }

        /// <summary>
        /// Försöker hitta Skyscreen.Server och etablera
        /// klientanslutningen.
        ///
        /// Om ingen server hittas på en fysisk enhet väntar appen
        /// en kort stund och gör sedan ett nytt discovery-försök.
        ///
        /// När en endpoint väl har hittats lämnas fortsatt
        /// återanslutning till SkyscreenClientService.
        /// </summary>
        private async Task ConnectToServerAsync(
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Uri? endpoint = null;

                try
                {
                    endpoint =
                        await _serverDiscoveryClient.DiscoverAsync(
                            cancellationToken);

                    if (endpoint is not null)
                    {
                        _logger.LogInformation(
                            "Skyscreen.Server hittades via discovery: {Endpoint}",
                            endpoint);
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Server discovery misslyckades.");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
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
                    _logger.LogInformation(
                        "Ingen Skyscreen.Server kunde hittas. Nytt discovery-försök görs om {DelaySeconds} sekunder.",
                        DiscoveryRetryDelay.TotalSeconds);

                    try
                    {
                        await Task.Delay(
                            DiscoveryRetryDelay,
                            cancellationToken);
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    continue;
                }

                try
                {
                    await _clientService.ConnectAsync(
                        endpoint,
                        cancellationToken);

                    _logger.LogInformation(
                        "Skyscreen.App ansluten till servern {Endpoint}. ClientId: {ClientId}",
                        endpoint,
                        _clientService.ClientId);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Skyscreen.App kunde inte ansluta till servern {Endpoint}. Klientens reconnect-loop tar över.",
                        endpoint);
                }

                return;
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