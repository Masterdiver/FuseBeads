using FuseBeads.Domain.Entities;

namespace FuseBeads.Application.DTOs;

/// <summary>
/// Result of converting an image to a bead pattern.
/// </summary>
public class PatternResult
{
    public required BeadPattern Pattern { get; init; }
    public required byte[] PatternImage { get; init; }
    public required IReadOnlyList<ColorInfo> ColorInfos { get; init; }
    public int TotalBeads => Pattern.TotalBeads;
}

public class ColorInfo
{
    public required string ColorName { get; init; }
    public required string HexCode { get; init; }
    public required int Count { get; init; }
    public double Percentage { get; init; }
}
