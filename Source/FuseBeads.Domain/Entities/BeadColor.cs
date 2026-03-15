namespace FuseBeads.Domain.Entities;

/// <summary>
/// Represents a single fuse bead color with its RGB values and name.
/// </summary>
public record BeadColor(string Name, byte R, byte G, byte B)
{
    public string HexCode => $"#{R:X2}{G:X2}{B:X2}";

    public double DistanceTo(byte r, byte g, byte b)
    {
        double dr = R - r;
        double dg = G - g;
        double db = B - b;
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }
}
