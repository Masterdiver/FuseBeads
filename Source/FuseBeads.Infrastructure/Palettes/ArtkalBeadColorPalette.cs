using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Palettes;

public class ArtkalBeadColorPalette : IBeadColorPalette
{
    public IReadOnlyList<BeadColor> Colors { get; } = new List<BeadColor>
    {
        new("Weiß", 248, 248, 248),
        new("Elfenbein", 235, 220, 195),
        new("Zitronengelb", 255, 235, 0),
        new("Sonnengelb", 255, 200, 0),
        new("Hellorange", 255, 165, 50),
        new("Orange", 245, 110, 30),
        new("Korallenrot", 240, 75, 65),
        new("Rot", 200, 15, 15),
        new("Dunkelrot", 140, 12, 8),
        new("Weinrot", 115, 10, 40),
        new("Pink", 235, 80, 125),
        new("Hellpink", 255, 185, 200),
        new("Babyrosa", 255, 210, 220),
        new("Magenta", 195, 25, 115),
        new("Lila", 115, 55, 140),
        new("Violett", 85, 35, 120),
        new("Flieder", 185, 150, 205),
        new("Lavendel", 155, 120, 175),
        new("Dunkelblau", 10, 40, 105),
        new("Blau", 20, 60, 175),
        new("Mittelblau", 55, 110, 190),
        new("Hellblau", 60, 160, 215),
        new("Himmelblau", 130, 195, 235),
        new("Babyblau", 175, 215, 240),
        new("Türkis", 0, 160, 155),
        new("Aquamarin", 80, 195, 190),
        new("Dunkelgrün", 0, 90, 45),
        new("Grün", 0, 140, 65),
        new("Hellgrün", 95, 200, 100),
        new("Lindgrün", 170, 220, 80),
        new("Mintgrün", 150, 225, 185),
        new("Olive", 115, 125, 60),
        new("Gelbgrün", 200, 215, 0),
        new("Braun", 105, 65, 25),
        new("Hellbraun", 185, 130, 65),
        new("Schokobraun", 65, 40, 20),
        new("Hautfarbe", 242, 195, 160),
        new("Pfirsich", 250, 175, 145),
        new("Karamell", 200, 150, 95),
        new("Grau", 150, 150, 150),
        new("Hellgrau", 195, 195, 195),
        new("Dunkelgrau", 85, 85, 90),
        new("Anthrazit", 50, 50, 55),
        new("Schwarz", 12, 12, 12),
        new("Neongelb", 230, 255, 0),
        new("Neonorange", 255, 125, 0),
        new("Neongrün", 35, 220, 65),
        new("Neonpink", 255, 55, 145),
        new("Neonblau", 0, 165, 255),
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
