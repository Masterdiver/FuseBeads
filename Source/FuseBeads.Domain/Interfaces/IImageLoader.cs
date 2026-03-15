namespace FuseBeads.Domain.Interfaces;

/// <summary>
/// Abstraction for loading image pixel data from a stream.
/// </summary>
public interface IImageLoader
{
    /// <summary>
    /// Loads an image and returns its pixel data as RGBA byte array, width, and height.
    /// </summary>
    (byte[] Pixels, int Width, int Height) LoadImage(Stream stream);

    /// <summary>
    /// Resizes pixel data to the target dimensions.
    /// </summary>
    (byte[] Pixels, int Width, int Height) Resize(byte[] pixels, int srcWidth, int srcHeight, int targetWidth, int targetHeight);
}
