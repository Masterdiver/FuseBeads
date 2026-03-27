using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using FuseBeads.Infrastructure;
using FuseBeads.ViewModels;
using Microsoft.Extensions.Logging;

namespace FuseBeads
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Onion Architecture: Infrastructure wires up Domain + Application services
            builder.Services.AddInfrastructure();
            builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);

            // Presentation layer registrations
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<InstructionViewModel>();
            builder.Services.AddTransient<InstructionPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
