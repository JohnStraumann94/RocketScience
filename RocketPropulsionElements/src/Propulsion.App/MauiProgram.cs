using Propulsion.App.Services;
using Propulsion.App.ViewModels;
using Propulsion.App.Views;
using Microsoft.Extensions.Logging;

namespace Propulsion.App;

/// <summary>
/// Composition root of the application: builds the .NET MAUI host, registers every service in
/// the dependency-injection container and returns the configured <see cref="MauiApp"/>.
/// </summary>
/// <remarks>
/// The platform entry points (for Windows, <c>Platforms/Windows/App.xaml.cs</c>) call
/// <see cref="CreateMauiApp"/> exactly once at start-up.
/// </remarks>
public static class MauiProgram
{
    /// <summary>
    /// Builds and configures the application host.
    /// </summary>
    /// <returns>The fully configured <see cref="MauiApp"/> instance.</returns>
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder
            // Nominates App as the Application subclass. MAUI resolves App from the container,
            // which is how App receives the IServiceProvider it uses to build the main window.
            .UseMauiApp<App>()

            // Register the fonts shipped in Resources/Fonts. The alias on the right is what
            // FontFamily="..." in XAML refers to. Code and numeric output in this app use
            // "Consolas", which is a system font on Windows and therefore needs no entry here.
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ---------------------------------------------------------------------------------
        // Dependency injection
        // ---------------------------------------------------------------------------------
        //
        // AZURE PLACEHOLDER
        // AzurePlaceholderService is registered as a singleton so that, when real Azure
        // resources are provisioned later, this single registration is the only line that has
        // to change - for example to an interface plus a concrete implementation configured
        // from appsettings.json / Key Vault. Nothing else in the app news it up directly.
        builder.Services.AddSingleton<AzurePlaceholderService>();

        // ContentLibrary reads every embedded documentation and Python resource once, in its
        // constructor. Registering it as a singleton means that scan happens exactly once per
        // process rather than each time a Help or Instructions window is opened.
        builder.Services.AddSingleton<ContentLibrary>();

        // The view-model is a singleton because it owns the sample catalog and the user's
        // in-progress input values. With only one window, a singleton means the user's edits
        // and last result survive if the page is ever recreated.
        builder.Services.AddSingleton<MainViewModel>();

        // The page is likewise a singleton; App.CreateWindow resolves it once.
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        // Route logging to the debugger output window during development only.
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
