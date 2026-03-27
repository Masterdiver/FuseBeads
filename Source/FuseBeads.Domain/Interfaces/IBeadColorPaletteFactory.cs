using FuseBeads.Domain.Entities;

namespace FuseBeads.Domain.Interfaces;

public interface IBeadColorPaletteFactory
{
    IBeadColorPalette GetPalette(PaletteType type);
    IReadOnlyList<PaletteType> AvailablePalettes { get; }
}
