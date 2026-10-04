// Path: Skyscreen.App/MauiProgram.cs

using Microsoft.Extensions.Logging;
using Skyscreen.App.Services;
using Skyscreen.App.Transport;
using Skyscreen.Core.Protocol;

namespace Skyscreen.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

            builder.Services.AddSingleton<
                ISkyscreenMessageSerializer,
                JsonSkyscreenMessageSerializer>();

            builder.Services.AddSingleton<
                IClientConnectionFactory,
                WebSocketClientConnectionFactory>();

            builder.Services.AddSingleton<
                IClientIdentityProvider,
                ClientIdentityProvider>();

            builder.Services.AddSingleton<
                IServerEndpointProvider,
                DevelopmentServerEndpointProvider>();

            builder.Services.AddSingleton<
                ISkyscreenClientService,
                SkyscreenClientService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}