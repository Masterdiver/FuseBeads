using FuseBeads.Domain.Entities;

namespace FuseBeads.Application.DTOs;

public class PatternSettings
{
    public int Width { get; set; } = 29;
    public int Height { get; set; } = 0;
    public int BeadSizePx { get; set; } = 20;

    // Feature 1: Palette selection
    public PaletteType PaletteType { get; set; } = PaletteType.Standard;

    // Feature 2: Max color limit (0 = no limit)
    public int MaxColors { get; set; } = 0;

    // Feature 3: Image pre-processing
    public float Brightness { get; set; } = 0f;   // -1.0 to 1.0
    public float Contrast { get; set; } = 0f;     // -1.0 to 1.0
    public float Saturation { get; set; } = 0f;   // -1.0 to 1.0

    // Feature 4: Board grid overlay
    public bool ShowBoardGrid { get; set; } = false;
    public int BoardWidth { get; set; } = 29;
    public int BoardHeight { get; set; } = 29;

    // Feature 11: Dithering
    public bool EnableDithering { get; set; } = false;
}
