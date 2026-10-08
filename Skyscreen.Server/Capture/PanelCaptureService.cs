// Path: Skyscreen.Server/Capture/PanelCaptureService.cs

namespace Skyscreen.Server.Capture;

/// <summary>
/// Standardimplementation av IPanelCaptureService.
///
/// Tjänsten översätter en logisk Skyscreen-panel till dess
/// maskinspecifika capture-region och använder därefter den
/// registrerade capture-källan för att fånga bildrutan.
/// </summary>
public sealed class PanelCaptureService
    : IPanelCaptureService
{
    private readonly IPanelCaptureRegionProvider _regionProvider;
    private readonly IPanelCaptureSource _captureSource;

    public PanelCaptureService(
        IPanelCaptureRegionProvider regionProvider,
        IPanelCaptureSource captureSource)
    {
        ArgumentNullException.ThrowIfNull(regionProvider);
        ArgumentNullException.ThrowIfNull(captureSource);

        _regionProvider = regionProvider;
        _captureSource = captureSource;
    }

    /// <inheritdoc />
    public async Task<PanelCaptureFrame?> CaptureAsync(
        string moduleId,
        string panelId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new ArgumentException(
                "ModuleId får inte vara tomt.",
                nameof(moduleId));
        }

        if (string.IsNullOrWhiteSpace(panelId))
        {
            throw new ArgumentException(
                "PanelId får inte vara tomt.",
                nameof(panelId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        bool found =
            _regionProvider.TryGetRegion(
                moduleId,
                panelId,
                out PanelCaptureRegion? region);

        if (!found || region is null)
        {
            return null;
        }

        return await _captureSource.CaptureAsync(
            region,
            cancellationToken);
    }
}