using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Palettes;

/// <summary>
/// Standard Hama/Perler bead color palette with common colors.
/// </summary>
public class StandardBeadColorPalette : IBeadColorPalette
{
    public IReadOnlyList<BeadColor> Colors { get; } = new List<BeadColor>
    {
        new("Weiß", 241, 241, 241),
        new("Creme", 217, 187, 155),
        new("Gelb", 236, 216, 0),
        new("Orange", 237, 97, 32),
        new("Rot", 190, 12, 4),
        new("Pink", 228, 72, 117),
        new("Lila", 100, 46, 122),
        new("Blau", 0, 56, 166),
        new("Hellblau", 50, 153, 204),
        new("Türkis", 0, 147, 143),
        new("Grün", 0, 128, 56),
        new("Hellgrün", 86, 186, 89),
        new("Braun", 96, 57, 19),
        new("Hellbraun", 175, 117, 57),
        new("Grau", 138, 141, 143),
        new("Dunkelgrau", 77, 79, 82),
        new("Schwarz", 20, 20, 20),
        new("Hautfarbe", 238, 186, 151),
        new("Pfirsich", 248, 170, 140),
        new("Dunkelrot", 124, 10, 2),
        new("Dunkelblau", 2, 32, 96),
        new("Dunkelgrün", 0, 79, 36),
        new("Olive", 106, 115, 55),
        new("Lavendel", 150, 111, 168),
        new("Magenta", 183, 16, 107),
        new("Hellgelb", 255, 246, 133),
        new("Hellrosa", 249, 175, 195),
        new("Neongelb", 221, 255, 2),
        new("Neonorange", 255, 119, 0),
        new("Neongrün", 33, 206, 63),
        new("Neonpink", 255, 57, 145),
    };

    public BeadColor FindClosestColor(byte r, byte g, byte b)
    {
        BeadColor closest = Colors[0];
        double minDistance = double.MaxValue;

        foreach (var color in Colors)
        {
            double distance = color.DistanceTo(r, g, b);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = color;
            }
        }

        return closest;
    }
}
