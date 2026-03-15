using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;
using SkiaSharp;

namespace FuseBeads.Infrastructure.ImageProcessing;

public class SkiaPatternRenderer : IPatternRenderer
{
    public byte[] RenderPattern(BeadPattern pattern, int beadSizePx = 20)
    {
        int width = pattern.Columns * beadSizePx;
        int height = pattern.Rows * beadSizePx;

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var beadPaint = new SKPaint { IsAntialias = true };
        using var gridPaint = new SKPaint
        {
            Color = new SKColor(200, 200, 200),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.5f,
            IsAntialias = true
        };
        using var holePaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 80),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float margin = beadSizePx * 0.08f;
        float beadRadius = (beadSizePx - margin * 2) / 2f;
        float holeRadius = beadRadius * 0.2f;

        for (int row = 0; row < pattern.Rows; row++)
        {
            for (int col = 0; col < pattern.Columns; col++)
            {
                var cell = pattern.Grid[row, col];
                if (cell is null) continue;

                float cx = col * beadSizePx + beadSizePx / 2f;
                float cy = row * beadSizePx + beadSizePx / 2f;

                // Draw filled bead circle
                beadPaint.Color = new SKColor(cell.Color.R, cell.Color.G, cell.Color.B);
                beadPaint.Style = SKPaintStyle.Fill;
                canvas.DrawCircle(cx, cy, beadRadius, beadPaint);

                // Draw bead border
                canvas.DrawCircle(cx, cy, beadRadius, gridPaint);

                // Draw center hole
                canvas.DrawCircle(cx, cy, holeRadius, holePaint);
            }
        }

        // Draw grid lines for pegboard feel
        using var boardPaint = new SKPaint
        {
            Color = new SKColor(180, 180, 180, 100),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.3f
        };
        for (int i = 0; i <= pattern.Columns; i++)
            canvas.DrawLine(i * beadSizePx, 0, i * beadSizePx, height, boardPaint);
        for (int i = 0; i <= pattern.Rows; i++)
            canvas.DrawLine(0, i * beadSizePx, width, i * beadSizePx, boardPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
