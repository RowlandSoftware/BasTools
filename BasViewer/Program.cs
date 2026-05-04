using Avalonia;
using System;

namespace BasViewer.GUI
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Avalonia.UseSkiaTextRendering", true);

            bool skia = AppContext.TryGetSwitch("Avalonia.UseSkiaTextRendering", out bool enabled) && enabled;
            System.Diagnostics.Debug.WriteLine($"Skia switch: {AppContext.TryGetSwitch("Avalonia.UseSkiaTextRendering", out bool skiaenabled) && skiaenabled}");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}
