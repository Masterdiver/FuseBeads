using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Palettes;

public class HamaBeadColorPalette : IBeadColorPalette
{
    public IReadOnlyList<BeadColor> Colors { get; } = new List<BeadColor>
    {
        new("Weiß", 245, 245, 245),
        new("Creme", 224, 200, 170),
        new("Gelb", 255, 222, 0),
        new("Orange", 240, 105, 25),
        new("Rot", 210, 10, 10),
        new("Pink", 238, 85, 130),
        new("Hellpink", 255, 180, 200),
        new("Lila", 110, 50, 135),
        new("Flieder", 180, 145, 200),
        new("Blau", 15, 55, 170),
        new("Hellblau", 55, 155, 210),
        new("Türkis", 0, 155, 150),
        new("Grün", 0, 135, 60),
        new("Hellgrün", 90, 195, 95),
        new("Pastell-Gelb", 255, 240, 160),
        new("Pastell-Blau", 155, 195, 230),
        new("Pastell-Grün", 160, 215, 175),
        new("Pastell-Rosa", 255, 200, 210),
        new("Pastell-Lila", 200, 180, 215),
        new("Braun", 100, 60, 25),
        new("Hellbraun", 180, 120, 60),
        new("Dunkelbraun", 60, 35, 15),
        new("Grau", 145, 145, 145),
        new("Dunkelgrau", 80, 80, 85),
        new("Schwarz", 15, 15, 15),
        new("Hautfarbe", 240, 190, 155),
        new("Dunkelrot", 130, 15, 5),
        new("Dunkelblau", 5, 35, 100),
        new("Dunkelgrün", 0, 85, 40),
        new("Olive", 110, 120, 60),
        new("Lachsfarben", 250, 140, 115),
        new("Magenta", 190, 20, 115),
        new("Neongelb", 225, 255, 0),
        new("Neonorange", 255, 130, 0),
        new("Neongrün", 40, 215, 70),
        new("Neonpink", 255, 60, 150),
        new("Transparent", 220, 220, 220),
        new("Mittelblau", 50, 100, 180),
        new("Warmgrau", 165, 155, 145),
        new("Bordeaux", 105, 5, 30),
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
