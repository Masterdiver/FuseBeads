using FuseBeads.Application.DTOs;

namespace FuseBeads.Application.Services;

/// <summary>
/// Application service for converting images to bead patterns.
/// </summary>
public interface IPatternService
{
    Task<PatternResult> GeneratePatternAsync(Stream imageStream, PatternSettings settings);
}
