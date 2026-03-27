namespace FuseBeads.Application.DTOs;

/// <summary>
/// Settings for pattern generation.
/// </summary>
public class PatternSettings
{
    /// <summary>Number of beads wide.</summary>
    public int Width { get; set; } = 29;

    /// <summary>Number of beads tall. 0 means auto-calculate from image aspect ratio.</summary>
    public int Height { get; set; } = 0;

    /// <summary>Size in pixels for each bead in the rendered image.</summary>
    public int BeadSizePx { get; set; } = 20;
}
