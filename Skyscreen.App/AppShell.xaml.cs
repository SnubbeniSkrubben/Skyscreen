// Path: Skyscreen.App/AppShell.xaml.cs

using Skyscreen.App.Pages;
using Skyscreen.App.Services;

namespace Skyscreen.App;

/// <summary>
/// Applikationens Shell och övergripande navigationsskal.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Parameterlös konstruktor som behålls tillfälligt medan
    /// klienttjänsten kopplas in stegvis från App.
    /// </summary>
    public AppShell()
        : this(clientService: null)
    {
    }

    /// <summary>
    /// Skapar Shell med appens gemensamma klienttjänst.
    ///
    /// När klienttjänsten finns tillgänglig skapas startsidan
    /// explicit så att samma klientinstans kan föras vidare genom
    /// hela navigationskedjan.
    /// </summary>
    public AppShell(
        ISkyscreenClientService? clientService)
    {
        InitializeComponent();

        if (clientService is null)
        {
            return;
        }

        ModuleIndexShellContent.ContentTemplate =
            new DataTemplate(
                () => new ModuleIndexPage(clientService));
    }
}