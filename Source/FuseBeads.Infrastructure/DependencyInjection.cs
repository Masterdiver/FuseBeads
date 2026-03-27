using FuseBeads.Application.Services;
using FuseBeads.Domain.Interfaces;
using FuseBeads.Infrastructure.ImageProcessing;
using FuseBeads.Infrastructure.Palettes;
using FuseBeads.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FuseBeads.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IBeadColorPalette, StandardBeadColorPalette>();
        services.AddSingleton<IBeadColorPaletteFactory, BeadColorPaletteFactory>();
        services.AddSingleton<IImageLoader, SkiaImageLoader>();
        services.AddSingleton<IPatternRenderer, SkiaPatternRenderer>();
        services.AddSingleton<IPrintRenderer, SkiaPrintRenderer>();
        services.AddSingleton<IPatternStorage, JsonPatternStorage>();
        services.AddSingleton<IProgressStorage, JsonProgressStorage>();
        services.AddTransient<IPatternService, PatternService>();
        return services;
    }
}
