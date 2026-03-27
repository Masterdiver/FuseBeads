using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Palettes;

public class BeadColorPaletteFactory : IBeadColorPaletteFactory
{
    private readonly Dictionary<PaletteType, IBeadColorPalette> _palettes = new()
    {
        [PaletteType.Standard] = new StandardBeadColorPalette(),
        [PaletteType.Hama] = new HamaBeadColorPalette(),
        [PaletteType.Artkal] = new ArtkalBeadColorPalette(),
        [PaletteType.Perler] = new PerlerBeadColorPalette(),
    };

    public IReadOnlyList<PaletteType> AvailablePalettes =>
        _palettes.Keys.ToList().AsReadOnly();

    public IBeadColorPalette GetPalette(PaletteType type) =>
        _palettes.TryGetValue(type, out var palette)
            ? palette
            : _palettes[PaletteType.Standard];
}
