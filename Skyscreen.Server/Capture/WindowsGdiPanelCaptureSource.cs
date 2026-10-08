// Path: Skyscreen.Server/Capture/WindowsGdiPanelCaptureSource.cs

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Skyscreen.Server.Capture;

/// <summary>
/// Windows-specifik capturekälla som använder GDI
/// för att fånga ett rektangulärt område av det virtuella skrivbordet.
/// </summary>
public sealed class WindowsGdiPanelCaptureSource
    : IPanelCaptureSource
{
    private const int Srccopy = 0x00CC0020;
    private const uint DibRgbColors = 0;
    private const uint BiRgb = 0;

    public Task<PanelCaptureFrame> CaptureAsync(
        PanelCaptureRegion region,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(region);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "WindowsGdiPanelCaptureSource kräver Windows.");
        }

        ValidateRegion(region);

        PanelCaptureFrame frame =
            Capture(
                region,
                cancellationToken);

        return Task.FromResult(frame);
    }

    private static PanelCaptureFrame Capture(
        PanelCaptureRegion region,
        CancellationToken cancellationToken)
    {
        IntPtr screenDc = IntPtr.Zero;
        IntPtr memoryDc = IntPtr.Zero;
        IntPtr bitmap = IntPtr.Zero;
        IntPtr previousObject = IntPtr.Zero;

        try
        {
            screenDc = GetDC(IntPtr.Zero);

            if (screenDc == IntPtr.Zero)
            {
                ThrowLastWin32Error(
                    "Kunde inte hämta device context för skrivbordet.");
            }

            memoryDc =
                CreateCompatibleDC(screenDc);

            if (memoryDc == IntPtr.Zero)
            {
                ThrowLastWin32Error(
                    "Kunde inte skapa memory device context.");
            }

            int stride =
                checked(region.Width * 4);

            int pixelDataLength =
                checked(stride * region.Height);

            BitmapInfo bitmapInfo =
                new()
                {
                    Header = new BitmapInfoHeader
                    {
                        Size =
                            (uint)Marshal.SizeOf<BitmapInfoHeader>(),

                        Width = region.Width,

                        // Negativ höjd skapar en top-down DIB.
                        Height = -region.Height,

                        Planes = 1,
                        BitCount = 32,
                        Compression = BiRgb,
                        SizeImage = (uint)pixelDataLength
                    }
                };

            bitmap =
                CreateDIBSection(
                    screenDc,
                    ref bitmapInfo,
                    DibRgbColors,
                    out IntPtr bitmapBits,
                    IntPtr.Zero,
                    0);

            if (bitmap == IntPtr.Zero ||
                bitmapBits == IntPtr.Zero)
            {
                ThrowLastWin32Error(
                    "Kunde inte skapa DIB-section för capture.");
            }

            previousObject =
                SelectObject(
                    memoryDc,
                    bitmap);

            if (previousObject == IntPtr.Zero ||
                previousObject == new IntPtr(-1))
            {
                ThrowLastWin32Error(
                    "Kunde inte välja capture-bitmap i memory device context.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            bool copied =
                BitBlt(
                    memoryDc,
                    0,
                    0,
                    region.Width,
                    region.Height,
                    screenDc,
                    region.X,
                    region.Y,
                    Srccopy);

            if (!copied)
            {
                ThrowLastWin32Error(
                    "Kunde inte kopiera skärmområdet med BitBlt.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            byte[] pixelData =
                new byte[pixelDataLength];

            Marshal.Copy(
                bitmapBits,
                pixelData,
                0,
                pixelData.Length);

            return new PanelCaptureFrame
            {
                Width = region.Width,
                Height = region.Height,
                PixelFormat =
                    PanelCapturePixelFormat.Bgrx32,
                Stride = stride,
                PixelData = pixelData,
                CapturedAtUtc =
                    DateTimeOffset.UtcNow
            };
        }
        finally
        {
            if (previousObject != IntPtr.Zero &&
                previousObject != new IntPtr(-1) &&
                memoryDc != IntPtr.Zero)
            {
                SelectObject(
                    memoryDc,
                    previousObject);
            }

            if (bitmap != IntPtr.Zero)
            {
                DeleteObject(bitmap);
            }

            if (memoryDc != IntPtr.Zero)
            {
                DeleteDC(memoryDc);
            }

            if (screenDc != IntPtr.Zero)
            {
                ReleaseDC(
                    IntPtr.Zero,
                    screenDc);
            }
        }
    }

    private static void ValidateRegion(
        PanelCaptureRegion region)
    {
        if (region.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(region),
                "Capture-regionens Width måste vara större än 0.");
        }

        if (region.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(region),
                "Capture-regionens Height måste vara större än 0.");
        }

        checked
        {
            _ = region.Width * 4;
            _ = region.Width *
                region.Height *
                4;
        }
    }

    private static void ThrowLastWin32Error(
        string message)
    {
        int errorCode =
            Marshal.GetLastWin32Error();

        throw new Win32Exception(
            errorCode,
            message);
    }

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern IntPtr GetDC(
        IntPtr windowHandle);

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern int ReleaseDC(
        IntPtr windowHandle,
        IntPtr deviceContext);

    [DllImport(
        "gdi32.dll",
        SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(
        IntPtr deviceContext);

    [DllImport(
        "gdi32.dll",
        SetLastError = true)]
    private static extern bool DeleteDC(
        IntPtr deviceContext);

    [DllImport(
        "gdi32.dll",
        SetLastError = true)]
    private static extern IntPtr CreateDIBSection(
        IntPtr deviceContext,
        ref BitmapInfo bitmapInfo,
        uint usage,
        out IntPtr bits,
        IntPtr section,
        uint offset);

    [DllImport(
        "gdi32.dll",
        SetLastError = true)]
    private static extern IntPtr SelectObject(
        IntPtr deviceContext,
        IntPtr graphicsObject);

    [DllImport(
        "gdi32.dll",
        SetLastError = true)]
    private static extern bool DeleteObject(
        IntPtr graphicsObject);

    [DllImport(
        "gdi32.dll",
        SetLastError = true)]
    private static extern bool BitBlt(
        IntPtr destinationDc,
        int destinationX,
        int destinationY,
        int width,
        int height,
        IntPtr sourceDc,
        int sourceX,
        int sourceY,
        int rasterOperation);

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }
}