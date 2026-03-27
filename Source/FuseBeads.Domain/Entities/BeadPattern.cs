namespace FuseBeads.Domain.Entities;

/// <summary>
/// Represents a complete fuse bead pattern with its grid of cells and color summary.
/// </summary>
public class BeadPattern
{
    public int Rows { get; }
    public int Columns { get; }
    public BeadCell?[,] Grid { get; }

    public BeadPattern(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
        Grid = new BeadCell?[rows, columns];
    }

    public HashSet<(int Row, int Column)> CheckedCells { get; } = [];

    public void ToggleChecked(int row, int column)
    {
        var key = (row, column);
        if (!CheckedCells.Remove(key))
            CheckedCells.Add(key);
    }

    public bool IsChecked(int row, int column) => CheckedCells.Contains((row, column));

    public int CheckedBeadsCount => CheckedCells.Count;

    public void SetCell(int row, int column, BeadColor color)
    {
        Grid[row, column] = new BeadCell(row, column, color);
    }

    /// <summary>
    /// Gets a summary of how many beads of each color are needed.
    /// </summary>
    public Dictionary<BeadColor, int> GetColorSummary()
    {
        var summary = new Dictionary<BeadColor, int>();
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                var cell = Grid[r, c];
                if (cell is null) continue;

                if (summary.ContainsKey(cell.Color))
                    summary[cell.Color]++;
                else
                    summary[cell.Color] = 1;
            }
        }
        return summary.OrderByDescending(x => x.Value)
                       .ToDictionary(x => x.Key, x => x.Value);
    }

    public int TotalBeads => Rows * Columns;

    public string ToShoppingList()
    {
        var summary = GetColorSummary();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Shopping List - {Columns}×{Rows} ({TotalBeads} beads)");
        sb.AppendLine(new string('-', 40));
        foreach (var kvp in summary)
        {
            double pct = Math.Round(100.0 * kvp.Value / TotalBeads, 1);
            sb.AppendLine($"{kvp.Key.Name,-20} {kvp.Value,6}  ({pct:F1}%)");
        }
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"{"Total",-20} {TotalBeads,6}");
        return sb.ToString();
    }
}
