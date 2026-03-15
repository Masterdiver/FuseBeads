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

            // 2. Resize to bead grid dimensions
            var (resized, _, _) = _imageLoader.Resize(pixels, width, height, settings.Width, settings.Height);

            // 3. Map each pixel to the closest bead color
            var pattern = new BeadPattern(settings.Height, settings.Width);
            for (int row = 0; row < settings.Height; row++)
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

            // 4. Render the pattern image
            byte[] patternImage = _renderer.RenderPattern(pattern, settings.BeadSizePx);

            // 5. Build color info list
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
