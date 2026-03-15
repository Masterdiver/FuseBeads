namespace FuseBeads.Domain.Entities;

/// <summary>
/// A single cell in the bead pattern grid.
/// </summary>
public record BeadCell(int Row, int Column, BeadColor Color);
