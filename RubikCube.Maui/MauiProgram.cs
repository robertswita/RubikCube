using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace RubikCube.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if MACCATALYST
                handlers.AddHandler<Platforms.MacCatalyst.Rendering.MetalCubeView,
                    Platforms.MacCatalyst.Rendering.MetalCubeViewHandler>();
#elif WINDOWS
                handlers.AddHandler<Platforms.Windows.Rendering.OpenGLCubeView,
                    Platforms.Windows.Rendering.OpenGLCubeViewHandler>();
#endif
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
