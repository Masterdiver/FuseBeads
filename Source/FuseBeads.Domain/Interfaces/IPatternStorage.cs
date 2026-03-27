using FuseBeads.Domain.Entities;

namespace FuseBeads.Domain.Interfaces;

public interface IPatternStorage
{
    Task SavePatternAsync(string filePath, BeadPattern pattern);
    Task<BeadPattern> LoadPatternAsync(string filePath);
}
