// Path: Skyscreen.App/App.xaml.cs

using Microsoft.Extensions.Logging;
using Skyscreen.App.Services;

namespace Skyscreen.App
{
    public partial class App : Application
    {
        private readonly ISkyscreenClientService _clientService;
        private readonly IServerEndpointProvider _serverEndpointProvider;
        private readonly ILogger<App> _logger;

        public App(
            ISkyscreenClientService clientService,
            IServerEndpointProvider serverEndpointProvider,
            ILogger<App> logger)
        {
            ArgumentNullException.ThrowIfNull(clientService);
            ArgumentNullException.ThrowIfNull(serverEndpointProvider);
            ArgumentNullException.ThrowIfNull(logger);

            InitializeComponent();

            _clientService = clientService;
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
        /// Försöker etablera anslutningen till Skyscreen.Server
        /// när appfönstret har skapats.
        ///
        /// Ett anslutningsfel får inte hindra appen från att starta.
        /// </summary>
        private async void OnWindowCreated(
            object? sender,
            EventArgs e)
        {
            Uri endpoint =
                _serverEndpointProvider.GetServerEndpoint();

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
    }
}