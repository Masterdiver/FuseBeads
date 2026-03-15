using FuseBeads.Domain.Entities;

namespace FuseBeads.Domain.Interfaces;

/// <summary>
/// Renders a bead pattern to a displayable image.
/// </summary>
public interface IPatternRenderer
{
    /// <summary>
    /// Renders the pattern as a PNG image byte array.
    /// </summary>
    byte[] RenderPattern(BeadPattern pattern, int beadSizePx = 20);
}
