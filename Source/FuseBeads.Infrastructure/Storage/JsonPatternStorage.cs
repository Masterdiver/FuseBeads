using System.Text.Json;
using System.Text.Json.Serialization;
using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Storage;

public class JsonPatternStorage : IPatternStorage
{
    public async Task SavePatternAsync(string filePath, BeadPattern pattern)
    {
        await using var stream = File.Create(filePath);
        await SavePatternToStreamAsync(stream, pattern);
    }

    public async Task SavePatternToStreamAsync(Stream stream, BeadPattern pattern)
    {
        var dto = new PatternDto { Rows = pattern.Rows, Columns = pattern.Columns };

        for (int r = 0; r < pattern.Rows; r++)
        {
            for (int c = 0; c < pattern.Columns; c++)
            {
                var cell = pattern.Grid[r, c];
                if (cell is null) continue;
                dto.Cells.Add(new CellDto
                {
                    Row = r,
                    Column = c,
                    ColorName = cell.Color.Name,
                    R = cell.Color.R,
                    G = cell.Color.G,
                    B = cell.Color.B
                });
            }
        }

        await JsonSerializer.SerializeAsync(stream, dto, PatternStorageJsonContext.Default.PatternDto);
    }

    public async Task<BeadPattern> LoadPatternAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var dto = await JsonSerializer.DeserializeAsync(stream, PatternStorageJsonContext.Default.PatternDto)
            ?? throw new InvalidOperationException("Invalid pattern file.");

        var pattern = new BeadPattern(dto.Rows, dto.Columns);
        foreach (var cell in dto.Cells)
        {
            var color = new BeadColor(cell.ColorName, cell.R, cell.G, cell.B);
            pattern.SetCell(cell.Row, cell.Column, color);
        }

        return pattern;
    }
}

internal class PatternDto
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public List<CellDto> Cells { get; set; } = [];
}

internal class CellDto
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
}

[JsonSerializable(typeof(PatternDto))]
[JsonSerializable(typeof(CellDto))]
internal partial class PatternStorageJsonContext : JsonSerializerContext { }
