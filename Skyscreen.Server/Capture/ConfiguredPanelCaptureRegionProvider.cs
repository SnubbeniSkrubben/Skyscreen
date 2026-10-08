// Path: Skyscreen.Server/Capture/ConfiguredPanelCaptureRegionProvider.cs

using Microsoft.Extensions.Options;

namespace Skyscreen.Server.Capture;

/// <summary>
/// Hämtar maskinspecifika capture-regioner från
/// PanelCaptureOptions.
///
/// Matchning av ModuleId och PanelId är inte
/// skiftlägeskänslig.
/// </summary>
public sealed class ConfiguredPanelCaptureRegionProvider
    : IPanelCaptureRegionProvider
{
    private readonly Dictionary<string, PanelCaptureRegion> _regions =
        new(StringComparer.OrdinalIgnoreCase);

    public ConfiguredPanelCaptureRegionProvider(
        IOptions<PanelCaptureOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (PanelCaptureRegionConfiguration configuration
                 in options.Value.Regions)
        {
            ValidateConfiguration(configuration);

            string key =
                CreateKey(
                    configuration.ModuleId,
                    configuration.PanelId);

            if (!_regions.TryAdd(
                    key,
                    configuration.Region))
            {
                throw new InvalidOperationException(
                    $"Capture-regionen för modul " +
                    $"'{configuration.ModuleId}' och panel " +
                    $"'{configuration.PanelId}' är konfigurerad flera gånger.");
            }
        }
    }

    /// <inheritdoc />
    public bool TryGetRegion(
        string moduleId,
        string panelId,
        out PanelCaptureRegion? region)
    {
        if (string.IsNullOrWhiteSpace(moduleId) ||
            string.IsNullOrWhiteSpace(panelId))
        {
            region = null;
            return false;
        }

        string key =
            CreateKey(
                moduleId,
                panelId);

        return _regions.TryGetValue(
            key,
            out region);
    }

    private static string CreateKey(
        string moduleId,
        string panelId)
    {
        return
            $"{moduleId.Trim()}\0{panelId.Trim()}";
    }

    private static void ValidateConfiguration(
        PanelCaptureRegionConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration.ModuleId))
        {
            throw new InvalidOperationException(
                "PanelCapture-konfigurationen innehåller ett tomt ModuleId.");
        }

        if (string.IsNullOrWhiteSpace(configuration.PanelId))
        {
            throw new InvalidOperationException(
                "PanelCapture-konfigurationen innehåller ett tomt PanelId.");
        }

        ArgumentNullException.ThrowIfNull(configuration.Region);

        if (configuration.Region.Width <= 0)
        {
            throw new InvalidOperationException(
                $"Capture-regionen för " +
                $"'{configuration.ModuleId}/{configuration.PanelId}' " +
                "har ogiltig Width.");
        }

        if (configuration.Region.Height <= 0)
        {
            throw new InvalidOperationException(
                $"Capture-regionen för " +
                $"'{configuration.ModuleId}/{configuration.PanelId}' " +
                "har ogiltig Height.");
        }
    }
}