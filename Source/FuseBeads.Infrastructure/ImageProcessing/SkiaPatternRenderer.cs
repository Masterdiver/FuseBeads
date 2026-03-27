using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;
using SkiaSharp;

namespace FuseBeads.Infrastructure.ImageProcessing;

public class SkiaPatternRenderer : IPatternRenderer
{
    public byte[] RenderPattern(BeadPattern pattern, int beadSizePx = 20)
    {
        return RenderPatternInternal(pattern, beadSizePx, 0, 0);
    }

    public byte[] RenderPattern(BeadPattern pattern, int beadSizePx, int boardWidth, int boardHeight)
    {
        return RenderPatternInternal(pattern, beadSizePx, boardWidth, boardHeight);
    }

    private static byte[] RenderPatternInternal(BeadPattern pattern, int beadSizePx, int boardWidth, int boardHeight)
    {
        int width = pattern.Columns * beadSizePx;
        int height = pattern.Rows * beadSizePx;

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var beadPaint = new SKPaint();
        beadPaint.IsAntialias = true;
        using var gridPaint = new SKPaint();
        gridPaint.Color = new SKColor(200, 200, 200);
        gridPaint.Style = SKPaintStyle.Stroke;
        gridPaint.StrokeWidth = 0.5f;
        gridPaint.IsAntialias = true;
        using var holePaint = new SKPaint();
        holePaint.Color = new SKColor(255, 255, 255, 80);
        holePaint.Style = SKPaintStyle.Fill;
        holePaint.IsAntialias = true;

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

                beadPaint.Color = new SKColor(cell.Color.R, cell.Color.G, cell.Color.B);
                beadPaint.Style = SKPaintStyle.Fill;
                canvas.DrawCircle(cx, cy, beadRadius, beadPaint);
                canvas.DrawCircle(cx, cy, beadRadius, gridPaint);
                canvas.DrawCircle(cx, cy, holeRadius, holePaint);
            }
        }

        // Draw light grid lines
        using var boardPaint = new SKPaint();
        boardPaint.Color = new SKColor(180, 180, 180, 100);
        boardPaint.Style = SKPaintStyle.Stroke;
        boardPaint.StrokeWidth = 0.3f;
        for (int i = 0; i <= pattern.Columns; i++)
            canvas.DrawLine(i * beadSizePx, 0, i * beadSizePx, height, boardPaint);
        for (int i = 0; i <= pattern.Rows; i++)
            canvas.DrawLine(0, i * beadSizePx, width, i * beadSizePx, boardPaint);

        // Draw board grid overlay
        if (boardWidth > 0 && boardHeight > 0)
        {
            using var boardGridPaint = new SKPaint();
            boardGridPaint.Color = new SKColor(255, 50, 50, 180);
            boardGridPaint.Style = SKPaintStyle.Stroke;
            boardGridPaint.StrokeWidth = 2.5f;
            boardGridPaint.IsAntialias = true;
            boardGridPaint.PathEffect = SKPathEffect.CreateDash([6f, 3f], 0);

            for (int col = boardWidth; col < pattern.Columns; col += boardWidth)
            {
                float x = col * beadSizePx;
                canvas.DrawLine(x, 0, x, height, boardGridPaint);
            }

            for (int row = boardHeight; row < pattern.Rows; row += boardHeight)
            {
                float y = row * beadSizePx;
                canvas.DrawLine(0, y, width, y, boardGridPaint);
            }
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
