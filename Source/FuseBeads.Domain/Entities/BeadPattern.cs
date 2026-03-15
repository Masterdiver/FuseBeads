namespace FuseBeads.Domain.Entities;

/// <summary>
/// Represents a complete fuse bead pattern with its grid of cells and color summary.
/// </summary>
public class BeadPattern
{
    public int Rows { get; }
    public int Columns { get; }
    public BeadCell[,] Grid { get; }

    public BeadPattern(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
        Grid = new BeadCell[rows, columns];
    }

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
}
