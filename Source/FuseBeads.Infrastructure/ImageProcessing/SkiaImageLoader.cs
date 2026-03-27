using FuseBeads.Domain.Interfaces;
using SkiaSharp;

namespace FuseBeads.Infrastructure.ImageProcessing;

public class SkiaImageLoader : IImageLoader
{
    public (byte[] Pixels, int Width, int Height) LoadImage(Stream stream)
    {
        using var skStream = new SKManagedStream(stream);
        using var codec = SKCodec.Create(skStream)
            ?? throw new InvalidOperationException("Could not decode image.");

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);

        codec.GetPixels(info, bitmap.GetPixels());

        byte[] pixels = bitmap.Bytes;
        return (pixels, bitmap.Width, bitmap.Height);
    }

    public (byte[] Pixels, int Width, int Height) Resize(byte[] pixels, int srcWidth, int srcHeight, int targetWidth, int targetHeight)
    {
        var srcInfo = new SKImageInfo(srcWidth, srcHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var srcBitmap = new SKBitmap(srcInfo);

        var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            srcBitmap.InstallPixels(srcInfo, handle.AddrOfPinnedObject(), srcInfo.RowBytes);

            var dstInfo = new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var dstBitmap = srcBitmap.Resize(dstInfo, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

            if (dstBitmap is null)
                throw new InvalidOperationException("Could not resize image.");

            return (dstBitmap.Bytes, targetWidth, targetHeight);
        }
        finally
        {
            handle.Free();
        }
    }

    public byte[] AdjustImage(byte[] pixels, int width, int height, float brightness, float contrast, float saturation)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var srcBitmap = new SKBitmap(info);

        var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            srcBitmap.InstallPixels(info, handle.AddrOfPinnedObject(), info.RowBytes);

            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            using var paint = new SKPaint();

            // Brightness: offset in [-1, 1] (SKColorFilter matrix offsets are normalized)
            float bOffset = brightness;

            // Contrast: scale factor, offset centered at 0.5
            float cFactor = 1f + contrast;
            float cOffset = -0.5f * cFactor + 0.5f;

            // Saturation matrix
            var satMatrix = CreateSaturationMatrix(1f + saturation);

            // Brightness/contrast matrix
            float[] bcMatrix =
            {
                cFactor, 0, 0, 0, bOffset + cOffset,
                0, cFactor, 0, 0, bOffset + cOffset,
                0, 0, cFactor, 0, bOffset + cOffset,
                0, 0, 0, 1, 0
            };

            // Concatenate: saturation * brightness/contrast
            var combined = ConcatColorMatrices(satMatrix, bcMatrix);

            paint.ColorFilter = SKColorFilter.CreateColorMatrix(combined);
            canvas.DrawBitmap(srcBitmap, 0, 0, paint);

            using var snapshot = surface.Snapshot();
            using var resultBitmap = SKBitmap.FromImage(snapshot);
            return resultBitmap.Bytes;
        }
        finally
        {
            handle.Free();
        }
    }

    private static float[] CreateSaturationMatrix(float s)
    {
        const float lumR = 0.2126f;
        const float lumG = 0.7152f;
        const float lumB = 0.0722f;

        return
        [
            lumR * (1f - s) + s, lumG * (1f - s),     lumB * (1f - s),     0, 0,
            lumR * (1f - s),     lumG * (1f - s) + s, lumB * (1f - s),     0, 0,
            lumR * (1f - s),     lumG * (1f - s),     lumB * (1f - s) + s, 0, 0,
            0,                   0,                   0,                   1, 0
        ];
    }

    private static float[] ConcatColorMatrices(float[] a, float[] b)
    {
        // 4x5 matrix multiplication: result = a * b
        var result = new float[20];
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                float sum = 0;
                for (int k = 0; k < 4; k++)
                {
                    sum += a[row * 5 + k] * b[k * 5 + col];
                }
                if (col == 4)
                    sum += a[row * 5 + 4]; // add the translation component from a
                result[row * 5 + col] = sum;
            }
        }
        return result;
    }
}
