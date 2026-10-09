// Path: Skyscreen.Server/Streaming/PanelStreamingDiagnosticService.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skyscreen.Server.Capture;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Kör ett explicit diagnostiskt engångstest av
/// capture + JPEG-encoding.
///
/// Tjänsten gör ingenting om diagnostiken inte
/// har aktiverats i konfigurationen.
/// </summary>
public sealed class PanelStreamingDiagnosticService
    : BackgroundService
{
    private readonly IPanelCaptureService _captureService;
    private readonly IPanelFrameEncoder _frameEncoder;
    private readonly PanelStreamingDiagnosticOptions _options;
    private readonly ILogger<PanelStreamingDiagnosticService> _logger;

    public PanelStreamingDiagnosticService(
        IPanelCaptureService captureService,
        IPanelFrameEncoder frameEncoder,
        IOptions<PanelStreamingDiagnosticOptions> options,
        ILogger<PanelStreamingDiagnosticService> logger)
    {
        ArgumentNullException.ThrowIfNull(captureService);
        ArgumentNullException.ThrowIfNull(frameEncoder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _captureService = captureService;
        _frameEncoder = frameEncoder;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        ValidateOptions();

        string moduleId =
            _options.ModuleId!.Trim();

        string panelId =
            _options.PanelId!.Trim();

        string outputPath =
            Path.GetFullPath(
                _options.OutputPath!.Trim());

        _logger.LogInformation(
            "Streaming-diagnostik startar. ModuleId: {ModuleId}, PanelId: {PanelId}",
            moduleId,
            panelId);

        PanelCaptureFrame? capturedFrame =
            await _captureService.CaptureAsync(
                moduleId,
                panelId,
                stoppingToken);

        if (capturedFrame is null)
        {
            _logger.LogWarning(
                "Streaming-diagnostik kunde inte hitta någon capture-region för {ModuleId}/{PanelId}.",
                moduleId,
                panelId);

            return;
        }

        stoppingToken.ThrowIfCancellationRequested();

        EncodedPanelFrame encodedFrame =
            await _frameEncoder.EncodeAsync(
                capturedFrame,
                stoppingToken);

        stoppingToken.ThrowIfCancellationRequested();

        if (encodedFrame.Data.Length == 0)
        {
            throw new InvalidOperationException(
                "JPEG-encodern returnerade en tom bildbuffer.");
        }

        string? outputDirectory =
            Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(
                outputDirectory);
        }

        await File.WriteAllBytesAsync(
            outputPath,
            encodedFrame.Data,
            stoppingToken);

        double compressionRatio =
            (double)encodedFrame.Data.Length /
            capturedFrame.PixelData.Length;

        _logger.LogInformation(
            "Streaming-diagnostik sparade {Width}x{Height} {ContentType} till {OutputPath}. " +
            "Rådata: {RawBytes} byte. Kodad data: {EncodedBytes} byte. Andel av rådata: {CompressionPercent:F1} %.",
            encodedFrame.Width,
            encodedFrame.Height,
            encodedFrame.ContentType,
            outputPath,
            capturedFrame.PixelData.Length,
            encodedFrame.Data.Length,
            compressionRatio * 100.0);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(
            _options.ModuleId))
        {
            throw new InvalidOperationException(
                "StreamingDiagnostic är aktiverad men ModuleId saknas.");
        }

        if (string.IsNullOrWhiteSpace(
            _options.PanelId))
        {
            throw new InvalidOperationException(
                "StreamingDiagnostic är aktiverad men PanelId saknas.");
        }

        if (string.IsNullOrWhiteSpace(
            _options.OutputPath))
        {
            throw new InvalidOperationException(
                "StreamingDiagnostic är aktiverad men OutputPath saknas.");
        }
    }
}