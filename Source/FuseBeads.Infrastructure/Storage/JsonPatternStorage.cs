using System.Text.Json;
using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Storage;

public class JsonPatternStorage : IPatternStorage
{
    public async Task SavePatternAsync(string filePath, BeadPattern pattern)
    {
        var dto = new PatternDto
        {
            Rows = pattern.Rows,
            Columns = pattern.Columns,
            Cells = new List<CellDto>()
        };

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

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<BeadPattern> LoadPatternAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var dto = JsonSerializer.Deserialize<PatternDto>(json)
            ?? throw new InvalidOperationException("Invalid pattern file.");

        var pattern = new BeadPattern(dto.Rows, dto.Columns);
        foreach (var cell in dto.Cells)
        {
            var color = new BeadColor(cell.ColorName, cell.R, cell.G, cell.B);
            pattern.SetCell(cell.Row, cell.Column, color);
        }

        return pattern;
    }

    private class PatternDto
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<CellDto> Cells { get; set; } = [];
    }

    private class CellDto
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }
}
