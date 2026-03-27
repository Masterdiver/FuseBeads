using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Palettes;

public class PerlerBeadColorPalette : IBeadColorPalette
{
    public IReadOnlyList<BeadColor> Colors { get; } = new List<BeadColor>
    {
        new("Weiß", 242, 242, 242),
        new("Creme", 220, 195, 160),
        new("Elfenbein", 235, 220, 190),
        new("Gelb", 248, 220, 0),
        new("Pastellgelb", 255, 240, 155),
        new("Hellorange", 255, 165, 45),
        new("Orange", 240, 108, 30),
        new("Rot", 205, 15, 10),
        new("Kirschrot", 170, 10, 15),
        new("Dunkelrot", 128, 12, 5),
        new("Rotbraun", 145, 45, 30),
        new("Pink", 232, 75, 120),
        new("Hellpink", 250, 175, 195),
        new("Blassrosa", 255, 205, 215),
        new("Magenta", 188, 20, 110),
        new("Pflaume", 120, 30, 95),
        new("Lila", 105, 48, 125),
        new("Lavendel", 155, 115, 170),
        new("Flieder", 185, 155, 205),
        new("Blau", 10, 60, 170),
        new("Dunkelblau", 8, 38, 100),
        new("Hellblau", 55, 160, 210),
        new("Pastellblau", 160, 200, 235),
        new("Himmelblau", 110, 180, 230),
        new("Türkis", 5, 150, 148),
        new("Aquamarin", 85, 195, 185),
        new("Grün", 0, 132, 58),
        new("Hellgrün", 88, 192, 92),
        new("Dunkelgrün", 0, 82, 38),
        new("Pastell-Grün", 155, 215, 170),
        new("Lindgrün", 180, 225, 80),
        new("Olive", 108, 118, 58),
        new("Braun", 98, 60, 22),
        new("Hellbraun", 178, 122, 60),
        new("Dunkelbraun", 62, 38, 18),
        new("Hautfarbe", 240, 188, 152),
        new("Pfirsich", 250, 172, 142),
        new("Karamell", 195, 145, 90),
        new("Grau", 140, 142, 145),
        new("Hellgrau", 188, 188, 188),
        new("Dunkelgrau", 78, 80, 84),
        new("Schwarz", 18, 18, 18),
        new("Neongelb", 222, 255, 5),
        new("Neonorange", 255, 120, 0),
        new("Neongrün", 35, 208, 65),
        new("Neonpink", 255, 58, 148),
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
