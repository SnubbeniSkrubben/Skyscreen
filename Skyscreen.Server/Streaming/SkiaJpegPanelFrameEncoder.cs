// Path: Skyscreen.Server/Streaming/SkiaJpegPanelFrameEncoder.cs

using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Skyscreen.Server.Capture;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Kodar råa BGRX32-panelbilder till JPEG med SkiaSharp.
/// </summary>
public sealed class SkiaJpegPanelFrameEncoder
    : IPanelFrameEncoder
{
    private const string JpegContentType = "image/jpeg";

    private readonly int _quality;

    public SkiaJpegPanelFrameEncoder(
        IOptions<JpegPanelFrameEncoderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Value.Quality is < 1 or > 100)
        {
            throw new InvalidOperationException(
                "JPEG-kvaliteten måste vara mellan 1 och 100.");
        }

        _quality =
            options.Value.Quality;
    }

    public Task<EncodedPanelFrame> EncodeAsync(
        PanelCaptureFrame frame,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(frame);

        cancellationToken.ThrowIfCancellationRequested();

        ValidateFrame(frame);

        SKImageInfo imageInfo =
            new(
                frame.Width,
                frame.Height,
                SKColorType.Bgra8888,
                SKAlphaType.Opaque);

        using SKBitmap bitmap =
            new(
                imageInfo,
                frame.Stride);

        IntPtr destination =
            bitmap.GetPixels();

        if (destination == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "SkiaSharp kunde inte allokera pixelbuffer för JPEG-kodning.");
        }

        Marshal.Copy(
            frame.PixelData,
            0,
            destination,
            frame.PixelData.Length);

        cancellationToken.ThrowIfCancellationRequested();

        using SKData? encodedData =
            bitmap.Encode(
                SKEncodedImageFormat.Jpeg,
                _quality);

        if (encodedData is null)
        {
            throw new InvalidOperationException(
                "SkiaSharp kunde inte koda panelbilden till JPEG.");
        }

        byte[] data =
            encodedData.ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        EncodedPanelFrame encodedFrame =
            new()
            {
                Width = frame.Width,
                Height = frame.Height,
                ContentType = JpegContentType,
                Data = data,
                CapturedAtUtc = frame.CapturedAtUtc
            };

        return Task.FromResult(
            encodedFrame);
    }

    private static void ValidateFrame(
        PanelCaptureFrame frame)
    {
        if (frame.PixelFormat !=
            PanelCapturePixelFormat.Bgrx32)
        {
            throw new InvalidOperationException(
                $"Pixel-formatet {frame.PixelFormat} stöds inte av JPEG-encodern.");
        }

        if (frame.Width <= 0)
        {
            throw new InvalidOperationException(
                "Bildrutans Width måste vara större än 0.");
        }

        if (frame.Height <= 0)
        {
            throw new InvalidOperationException(
                "Bildrutans Height måste vara större än 0.");
        }

        int minimumStride =
            checked(frame.Width * 4);

        if (frame.Stride < minimumStride)
        {
            throw new InvalidOperationException(
                $"Bildrutans stride är {frame.Stride}, men minst " +
                $"{minimumStride} krävs för BGRX32.");
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
    }
}