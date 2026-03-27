using FuseBeads.Domain.Entities;

namespace FuseBeads.Domain.Interfaces;

/// <summary>
/// Renders a printable page containing the bead pattern, color legend, and instructions.
/// </summary>
public interface IPrintRenderer
{
    byte[] RenderPrintPage(BeadPattern pattern, int beadSizePx = 20);
    byte[] RenderPrintPageAsPdf(BeadPattern pattern, int beadSizePx = 20);
}
