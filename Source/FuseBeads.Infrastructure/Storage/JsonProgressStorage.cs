using System.Text.Json;
using System.Text.Json.Serialization;
using FuseBeads.Domain.Interfaces;

namespace FuseBeads.Infrastructure.Storage;

public class JsonProgressStorage : IProgressStorage
{
    private static readonly string FilePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "bead_progress.json");

    public async Task SaveProgressAsync(IReadOnlyCollection<(int Row, int Column)> checkedCells)
    {
        var dto = new ProgressDto(
            checkedCells.Select(c => new CellPositionDto(c.Row, c.Column)).ToList());
        await using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, dto, ProgressStorageJsonContext.Default.ProgressDto);
    }

    public async Task<HashSet<(int Row, int Column)>> LoadProgressAsync()
    {
        if (!File.Exists(FilePath))
            return [];

        await using var stream = File.OpenRead(FilePath);
        var dto = await JsonSerializer.DeserializeAsync(stream, ProgressStorageJsonContext.Default.ProgressDto);
        if (dto is null)
            return [];

        return dto.CheckedCells.Select(c => (c.Row, c.Column)).ToHashSet();
    }

    public Task ClearProgressAsync()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        return Task.CompletedTask;
    }
}

internal record ProgressDto(List<CellPositionDto> CheckedCells);
internal record CellPositionDto(int Row, int Column);

[JsonSerializable(typeof(ProgressDto))]
[JsonSerializable(typeof(CellPositionDto))]
internal partial class ProgressStorageJsonContext : JsonSerializerContext { }
