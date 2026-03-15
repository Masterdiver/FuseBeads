using FuseBeads.Domain.Entities;

namespace FuseBeads.Domain.Interfaces;

/// <summary>
/// Provides the available bead color palette.
/// </summary>
public interface IBeadColorPalette
{
    IReadOnlyList<BeadColor> Colors { get; }
    BeadColor FindClosestColor(byte r, byte g, byte b);
}
