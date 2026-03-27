using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;
using SkiaSharp;

namespace FuseBeads.Infrastructure.ImageProcessing;

public class SkiaPrintRenderer : IPrintRenderer
{
    private const int PageWidth = 2480;  // A4 at 300 DPI
    private const int PageMargin = 80;
    private const int ContentWidth = PageWidth - 2 * PageMargin;

    public byte[] RenderPrintPage(BeadPattern pattern, int beadSizePx = 20)
    {
        var colorSummary = pattern.GetColorSummary();
        int totalBeads = pattern.TotalBeads;

        // Pre-calculate required height
        int pageHeight = CalculatePageHeight(colorSummary.Count);

        var info = new SKImageInfo(PageWidth, pageHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        float y = PageMargin;

        y = DrawTitle(canvas, y);
        y = DrawStats(canvas, y, pattern, colorSummary.Count);
        y = DrawPatternImage(canvas, y, pattern, beadSizePx);
        y = DrawColorLegend(canvas, y, colorSummary, totalBeads);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static int CalculatePageHeight(int colorCount)
    {
        // Title + stats + pattern image (max ~1200) + color legend + instructions + margin
        int estimatedPatternHeight = 1200 + 100; // pattern + header
        int colorLegendHeight = 80 + colorCount * 55;
        return PageMargin * 2 + 140 + 100 + estimatedPatternHeight + colorLegendHeight + 200;
    }

    private static float DrawTitle(SKCanvas canvas, float y)
    {
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 72,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        string title = "Bügelperlen-Muster";
        float titleWidth = titlePaint.MeasureText(title);
        canvas.DrawText(title, (PageWidth - titleWidth) / 2f, y + 72, titlePaint);
        y += 110;

        // Separator line
        using var linePaint = new SKPaint
        {
            Color = new SKColor(200, 200, 200),
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(PageMargin, y, PageWidth - PageMargin, y, linePaint);
        y += 30;

        return y;
    }

    private static float DrawStats(SKCanvas canvas, float y, BeadPattern pattern, int colorCount)
    {
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(100, 100, 100),
            TextSize = 36,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        using var valuePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 44,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        float colWidth = ContentWidth / 3f;

        // Row 1: Labels
        canvas.DrawText("Raster", PageMargin, y + 36, labelPaint);
        canvas.DrawText("Perlen gesamt", PageMargin + colWidth, y + 36, labelPaint);
        canvas.DrawText("Farben", PageMargin + colWidth * 2, y + 36, labelPaint);
        y += 50;

        // Row 2: Values
        canvas.DrawText($"{pattern.Columns} × {pattern.Rows}", PageMargin, y + 44, valuePaint);
        canvas.DrawText($"{pattern.TotalBeads}", PageMargin + colWidth, y + 44, valuePaint);
        canvas.DrawText($"{colorCount}", PageMargin + colWidth * 2, y + 44, valuePaint);
        y += 70;

        // Separator
        using var linePaint = new SKPaint
        {
            Color = new SKColor(220, 220, 220),
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(PageMargin, y, PageWidth - PageMargin, y, linePaint);
        y += 20;

        return y;
    }

    private static float DrawPatternImage(SKCanvas canvas, float y, BeadPattern pattern, int beadSizePx)
    {
        using var sectionPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 48,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        canvas.DrawText("Muster", PageMargin, y + 48, sectionPaint);
        y += 70;

        // Render the pattern inline
        int patternPixelWidth = pattern.Columns * beadSizePx;
        int patternPixelHeight = pattern.Rows * beadSizePx;

        // Scale to fit content width, max height 1200
        float scale = Math.Min((float)ContentWidth / patternPixelWidth, 1200f / patternPixelHeight);
        float drawWidth = patternPixelWidth * scale;
        float drawHeight = patternPixelHeight * scale;

        // Render pattern onto a temporary surface
        var patternInfo = new SKImageInfo(patternPixelWidth, patternPixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var patternSurface = SKSurface.Create(patternInfo);
        var pc = patternSurface.Canvas;
        pc.Clear(SKColors.White);

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

                beadPaint.Color = new SKColor(cell.Color.R, cell.Color.G, cell.Color.B);
                beadPaint.Style = SKPaintStyle.Fill;
                pc.DrawCircle(cx, cy, beadRadius, beadPaint);
                pc.DrawCircle(cx, cy, beadRadius, gridPaint);
                pc.DrawCircle(cx, cy, holeRadius, holePaint);
            }
        }

        using var patternImage = patternSurface.Snapshot();

        // Draw centered on page
        float offsetX = PageMargin + (ContentWidth - drawWidth) / 2f;
        var destRect = new SKRect(offsetX, y, offsetX + drawWidth, y + drawHeight);
        canvas.DrawImage(patternImage, destRect);

        // Border around pattern
        using var borderPaint = new SKPaint
        {
            Color = new SKColor(180, 180, 180),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(destRect, borderPaint);

        y += drawHeight + 30;

        // Separator
        using var linePaint = new SKPaint
        {
            Color = new SKColor(220, 220, 220),
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(PageMargin, y, PageWidth - PageMargin, y, linePaint);
        y += 20;

        return y;
    }

    private static float DrawColorLegend(SKCanvas canvas, float y, Dictionary<BeadColor, int> colorSummary, int totalBeads)
    {
        using var sectionPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 48,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        canvas.DrawText("Farbenliste", PageMargin, y + 48, sectionPaint);
        y += 70;

        // Column headers
        using var headerPaint = new SKPaint
        {
            Color = new SKColor(80, 80, 80),
            TextSize = 32,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        float colColor = PageMargin + 60;
        float colName = PageMargin + 300;
        float colCount = PageMargin + ContentWidth - 500;
        float colPercent = PageMargin + ContentWidth - 200;

        canvas.DrawText("Farbe", PageMargin + 60, y + 32, headerPaint);
        canvas.DrawText("Name", colName, y + 32, headerPaint);
        canvas.DrawText("Anzahl", colCount, y + 32, headerPaint);
        canvas.DrawText("%", colPercent, y + 32, headerPaint);
        y += 50;

        using var namePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 30,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        using var hexPaint = new SKPaint
        {
            Color = new SKColor(120, 120, 120),
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        using var countPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 30,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        foreach (var kvp in colorSummary)
        {
            var color = kvp.Key;
            int count = kvp.Value;
            double percentage = Math.Round(100.0 * count / totalBeads, 1);

            // Color swatch
            using var swatchPaint = new SKPaint
            {
                Color = new SKColor(color.R, color.G, color.B),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(PageMargin + 25, y + 18, 18, swatchPaint);

            using var swatchBorder = new SKPaint
            {
                Color = new SKColor(180, 180, 180),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };
            canvas.DrawCircle(PageMargin + 25, y + 18, 18, swatchBorder);

            // Color name + hex
            canvas.DrawText(color.Name, colName, y + 22, namePaint);
            canvas.DrawText(color.HexCode, colName, y + 48, hexPaint);

            // Count
            canvas.DrawText(count.ToString(), colCount, y + 30, countPaint);

            // Percentage
            canvas.DrawText($"{percentage:F1}%", colPercent, y + 30, countPaint);

            y += 55;
        }

        y += 10;
        // Separator
        using var linePaint = new SKPaint
        {
            Color = new SKColor(220, 220, 220),
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(PageMargin, y, PageWidth - PageMargin, y, linePaint);
        y += 20;

        return y;
    }
}
