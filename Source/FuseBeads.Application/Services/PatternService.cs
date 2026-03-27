using FuseBeads.Application.DTOs;
using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Application.Services;

public class PatternService : IPatternService
{
    private readonly IImageLoader _imageLoader;
    private readonly IBeadColorPalette _palette;
    private readonly IPatternRenderer _renderer;

    public PatternService(IImageLoader imageLoader, IBeadColorPalette palette, IPatternRenderer renderer)
    {
        _imageLoader = imageLoader;
        _palette = palette;
        _renderer = renderer;
    }

    public Task<PatternResult> GeneratePatternAsync(Stream imageStream, PatternSettings settings)
    {
        return Task.Run(() =>
        {
            // 1. Load the source image
            var (pixels, width, height) = _imageLoader.LoadImage(imageStream);

            // 2. Auto-calculate grid height from aspect ratio if not specified
            int gridHeight = settings.Height > 0
                ? settings.Height
                : (int)Math.Round((double)settings.Width * height / width);
            if (gridHeight < 1) gridHeight = 1;

            // 3. Resize to bead grid dimensions
            var (resized, _, _) = _imageLoader.Resize(pixels, width, height, settings.Width, gridHeight);

            // 4. Map each pixel to the closest bead color
            var pattern = new BeadPattern(gridHeight, settings.Width);
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < settings.Width; col++)
                {
                    int idx = (row * settings.Width + col) * 4;
                    byte r = resized[idx];
                    byte g = resized[idx + 1];
                    byte b = resized[idx + 2];

                    var closestColor = _palette.FindClosestColor(r, g, b);
                    pattern.SetCell(row, col, closestColor);
                }
            }

            // 5. Render the pattern image
            byte[] patternImage = _renderer.RenderPattern(pattern, settings.BeadSizePx);

            // 6. Build color info list
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
}
