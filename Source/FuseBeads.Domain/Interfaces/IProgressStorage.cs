namespace FuseBeads.Domain.Interfaces;

public interface IProgressStorage
{
    Task SaveProgressAsync(IReadOnlyCollection<(int Row, int Column)> checkedCells);
    Task<HashSet<(int Row, int Column)>> LoadProgressAsync();
    Task ClearProgressAsync();
}
