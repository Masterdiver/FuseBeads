using FuseBeads.Application.DTOs;
using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Application.Services;

public class PatternService : IPatternService
{
    private readonly IImageLoader _imageLoader;
    private readonly IBeadColorPaletteFactory _paletteFactory;
    private readonly IPatternRenderer _renderer;

    public PatternService(IImageLoader imageLoader, IBeadColorPaletteFactory paletteFactory, IPatternRenderer renderer)
    {
        _imageLoader = imageLoader;
        _paletteFactory = paletteFactory;
        _renderer = renderer;
    }

    public Task<PatternResult> GeneratePatternAsync(Stream imageStream, PatternSettings settings)
    {
        return Task.Run(() =>
        {
            var palette = _paletteFactory.GetPalette(settings.PaletteType);

            // 1. Load the source image
            var (pixels, width, height) = _imageLoader.LoadImage(imageStream);

            // 2. Apply image pre-processing (Feature 3)
            if (settings.Brightness != 0f || settings.Contrast != 0f || settings.Saturation != 0f)
            {
                pixels = _imageLoader.AdjustImage(pixels, width, height,
                    settings.Brightness, settings.Contrast, settings.Saturation);
            }

            // 3. Auto-calculate grid height from aspect ratio if not specified
            int gridHeight = settings.Height > 0
                ? settings.Height
                : (int)Math.Round((double)settings.Width * height / width);
            if (gridHeight < 1) gridHeight = 1;

            // 4. Resize to bead grid dimensions
            var (resized, _, _) = _imageLoader.Resize(pixels, width, height, settings.Width, gridHeight);

            // 5. Apply Floyd-Steinberg dithering if enabled (Feature 11)
            if (settings.EnableDithering)
            {
                resized = ApplyFloydSteinbergDithering(resized, settings.Width, gridHeight, palette);
            }

            // 6. Map each pixel to the closest bead color
            var pattern = new BeadPattern(gridHeight, settings.Width);
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < settings.Width; col++)
                {
                    int idx = (row * settings.Width + col) * 4;
                    byte r = resized[idx];
                    byte g = resized[idx + 1];
                    byte b = resized[idx + 2];

                    var closestColor = palette.FindClosestColor(r, g, b);
                    pattern.SetCell(row, col, closestColor);
                }
            }

            // 7. Apply color limiting (Feature 2)
            if (settings.MaxColors > 0)
            {
                ApplyColorLimit(pattern, settings.MaxColors);
            }

            // 8. Render the pattern image
            byte[] patternImage;
            if (settings.ShowBoardGrid)
            {
                patternImage = _renderer.RenderPattern(pattern, settings.BeadSizePx,
                    settings.BoardWidth, settings.BoardHeight);
            }
            else
            {
                patternImage = _renderer.RenderPattern(pattern, settings.BeadSizePx);
            }

            // 9. Build color info list
            var summary = pattern.GetColorSummary();
            var colorInfos = summary.Select(kvp => new ColorInfo
            {
                ColorName = kvp.Key.Name,
                HexCode = kvp.Key.HexCode,
                Count = kvp.Value,
                Percentage = Math.Round(100.0 * kvp.Value / pattern.TotalBeads, 1)
            }).ToList();

            return new PatternResult
            {
                Pattern = pattern,
                PatternImage = patternImage,
                ColorInfos = colorInfos
            };
        });
    }

    private static void ApplyColorLimit(BeadPattern pattern, int maxColors)
    {
        var summary = pattern.GetColorSummary();
        if (summary.Count <= maxColors) return;

        // Keep the top N most-used colors
        var allowedColors = summary.Keys.Take(maxColors).ToHashSet();

        // Re-map cells with removed colors to the nearest allowed color
        for (int row = 0; row < pattern.Rows; row++)
        {
            for (int col = 0; col < pattern.Columns; col++)
            {
                var cell = pattern.Grid[row, col];
                if (cell is null || allowedColors.Contains(cell.Color)) continue;

                var nearest = allowedColors
                    .OrderBy(c => cell.Color.DistanceTo(c.R, c.G, c.B))
                    .First();
                pattern.SetCell(row, col, nearest);
            }
        }
    }

    private static byte[] ApplyFloydSteinbergDithering(byte[] pixels, int width, int height, IBeadColorPalette palette)
    {
        // Work on a float copy to accumulate error diffusion
        var result = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            result[i] = pixels[i];

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int idx = (row * width + col) * 4;

                byte oldR = ClampToByte(result[idx]);
                byte oldG = ClampToByte(result[idx + 1]);
                byte oldB = ClampToByte(result[idx + 2]);

                var nearest = palette.FindClosestColor(oldR, oldG, oldB);

                float errR = oldR - nearest.R;
                float errG = oldG - nearest.G;
                float errB = oldB - nearest.B;

                // Set the pixel to the nearest palette color
                result[idx] = nearest.R;
                result[idx + 1] = nearest.G;
                result[idx + 2] = nearest.B;

                // Distribute error to neighboring pixels
                DistributeError(result, width, height, col + 1, row, errR, errG, errB, 7f / 16f);
                DistributeError(result, width, height, col - 1, row + 1, errR, errG, errB, 3f / 16f);
                DistributeError(result, width, height, col, row + 1, errR, errG, errB, 5f / 16f);
                DistributeError(result, width, height, col + 1, row + 1, errR, errG, errB, 1f / 16f);
            }
        }

        // Convert back to byte array
        var output = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            output[i] = ClampToByte(result[i]);

        return output;
    }

    private static void DistributeError(float[] pixels, int width, int height, int col, int row, float errR, float errG, float errB, float factor)
    {
        if (col < 0 || col >= width || row < 0 || row >= height) return;

        int idx = (row * width + col) * 4;
        pixels[idx] += errR * factor;
        pixels[idx + 1] += errG * factor;
        pixels[idx + 2] += errB * factor;
    }

    private static byte ClampToByte(float value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }
}
