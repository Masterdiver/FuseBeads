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
}
