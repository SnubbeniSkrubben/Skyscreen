// Path: Skyscreen.Server/Capture/PanelCaptureDiagnosticService.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Skyscreen.Server.Capture;

/// <summary>
/// Kör ett explicit diagnostiskt engångstest av panelcapture.
///
/// Tjänsten gör ingenting om diagnostiken inte har aktiverats
/// i konfigurationen.
///
/// Vid aktivering fångas en enda bildruta för angiven logisk
/// panel och sparas som en okomprimerad 32-bitars BMP-fil.
/// </summary>
public sealed class PanelCaptureDiagnosticService
    : BackgroundService
{
    private const int BitmapFileHeaderSize = 14;
    private const int BitmapInfoHeaderSize = 40;
    private const int BitmapPixelDataOffset =
        BitmapFileHeaderSize + BitmapInfoHeaderSize;

    private readonly IPanelCaptureService _captureService;
    private readonly PanelCaptureDiagnosticOptions _options;
    private readonly ILogger<PanelCaptureDiagnosticService> _logger;

    public PanelCaptureDiagnosticService(
        IPanelCaptureService captureService,
        IOptions<PanelCaptureDiagnosticOptions> options,
        ILogger<PanelCaptureDiagnosticService> logger)
    {
        ArgumentNullException.ThrowIfNull(captureService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _captureService = captureService;
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
            "Panelcapture-diagnostik startar. ModuleId: {ModuleId}, PanelId: {PanelId}",
            moduleId,
            panelId);

        PanelCaptureFrame? frame =
            await _captureService.CaptureAsync(
                moduleId,
                panelId,
                stoppingToken);

        if (frame is null)
        {
            _logger.LogWarning(
                "Panelcapture-diagnostik kunde inte hitta någon capture-region för {ModuleId}/{PanelId}.",
                moduleId,
                panelId);

            return;
        }

        stoppingToken.ThrowIfCancellationRequested();

        SaveBitmap(
            frame,
            outputPath);

        _logger.LogInformation(
            "Panelcapture-diagnostik sparade {Width}x{Height} {PixelFormat} till {OutputPath}.",
            frame.Width,
            frame.Height,
            frame.PixelFormat,
            outputPath);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ModuleId))
        {
            throw new InvalidOperationException(
                "PanelCaptureDiagnostic är aktiverad men ModuleId saknas.");
        }

        if (string.IsNullOrWhiteSpace(_options.PanelId))
        {
            throw new InvalidOperationException(
                "PanelCaptureDiagnostic är aktiverad men PanelId saknas.");
        }

        if (string.IsNullOrWhiteSpace(_options.OutputPath))
        {
            throw new InvalidOperationException(
                "PanelCaptureDiagnostic är aktiverad men OutputPath saknas.");
        }
    }

    private static void SaveBitmap(
        PanelCaptureFrame frame,
        string outputPath)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Width <= 0)
        {
            throw new InvalidOperationException(
                "Bildrutans Width är ogiltig.");
        }

        if (frame.Height <= 0)
        {
            throw new InvalidOperationException(
                "Bildrutans Height är ogiltig.");
        }

        if (frame.PixelFormat !=
            PanelCapturePixelFormat.Bgrx32)
        {
            throw new InvalidOperationException(
                $"Bildrutans pixel-format {frame.PixelFormat} stöds inte " +
                "av BMP-diagnostiken.");
        }

        int expectedStride =
            checked(frame.Width * 4);

        if (frame.Stride != expectedStride)
        {
            throw new InvalidOperationException(
                $"Bildrutans stride är {frame.Stride}, men " +
                $"{expectedStride} förväntades för BGRX32.");
        }

        int expectedDataLength =
            checked(frame.Stride * frame.Height);

        if (frame.PixelData.Length != expectedDataLength)
        {
            throw new InvalidOperationException(
                $"Bildrutans pixelbuffer innehåller " +
                $"{frame.PixelData.Length} byte, men " +
                $"{expectedDataLength} förväntades.");
        }

        string? outputDirectory =
            Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        int fileSize =
            checked(
                BitmapPixelDataOffset +
                frame.PixelData.Length);

        using FileStream stream =
            new(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

        using BinaryWriter writer =
            new(stream);

        // BITMAPFILEHEADER
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write(BitmapPixelDataOffset);

        // BITMAPINFOHEADER
        writer.Write(BitmapInfoHeaderSize);
        writer.Write(frame.Width);

        // Negativ höjd betyder att pixelraderna är lagrade
        // uppifrån och ned, vilket matchar capture-formatet.
        writer.Write(-frame.Height);

        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(0);
        writer.Write(frame.PixelData.Length);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        writer.Write(frame.PixelData);
    }
}