using Astro.App.Views;

namespace Astro.App;

/// <summary>
/// The application root. Owns the merged resource dictionaries declared in
/// <c>App.xaml</c> and creates the single main window.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Creates the application.
    /// </summary>
    /// <param name="services">
    /// The dependency-injection container built in <see cref="MauiProgram"/>. It is captured
    /// here so that <see cref="CreateWindow"/> can resolve the main page together with its
    /// view-model, rather than the page having to new up its own dependencies.
    /// </param>
    public App(IServiceProvider services)
    {
        _services = services;

        InitializeComponent();
    }

    /// <summary>
    /// Builds the application's single window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default MAUI template wraps everything in a <c>Shell</c>, which provides flyout and
    /// tab navigation. This app has exactly one page whose left-hand pane <i>is</i> the
    /// navigation, so Shell would only add redundant chrome and an extra layer between the
    /// page and the DI container. We therefore host the page directly.
    /// </para>
    /// <para>
    /// A generous default size is preferred because the detail pane shows fixed-width tabular
    /// output that wraps badly in a narrow window. That preference is only a preference,
    /// though: the requested size is clamped to what the display can actually show, because an
    /// unconditional 1500 x 950 opens partly off-screen on a 1366 x 768 laptop, and a minimum
    /// size larger than the screen leaves the user unable to shrink the window back into view.
    /// </para>
    /// </remarks>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        MainPage page = _services.GetRequiredService<MainPage>();

        (double width, double height) = PreferredWindowSize();

        return new Window(page)
        {
            Title = "Fundamentals of Astrodynamics - Interactive Code Samples",
            Width = width,
            Height = height,

            // The floor is deliberately well below the preferred size. It exists to stop the
            // two-pane layout collapsing into unreadability, not to dictate a screen size.
            MinimumWidth = 900,
            MinimumHeight = 600
        };
    }

    /// <summary>
    /// The window size to request: the preferred size, reduced if the display is too small.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>MainDisplayInfo</c> reports physical pixels, whereas <c>Window.Width</c> and
    /// <c>Window.Height</c> are in device-independent units, so the raw values have to be
    /// divided by the display density before they can be compared. Skipping that conversion is
    /// the classic way to get a window sized for a 4K monitor on a 150% scaled laptop.
    /// </para>
    /// <para>
    /// Ninety-two per cent of the display is used as the ceiling rather than the whole thing, so
    /// that the title bar and the Windows taskbar do not push the bottom of the window off the
    /// bottom of the screen.
    /// </para>
    /// </remarks>
    private static (double Width, double Height) PreferredWindowSize()
    {
        const double preferredWidth = 1500;
        const double preferredHeight = 950;

        try
        {
            DisplayInfo display = DeviceDisplay.Current.MainDisplayInfo;

            // Density is 0 before the display is known, and dividing by it would produce
            // infinity; fall back to the preferred size in that case rather than guessing.
            if (display.Density <= 0 || display.Width <= 0 || display.Height <= 0)
            {
                return (preferredWidth, preferredHeight);
            }

            double usableWidth = display.Width / display.Density * 0.92;
            double usableHeight = display.Height / display.Density * 0.92;

            return (Math.Min(preferredWidth, usableWidth), Math.Min(preferredHeight, usableHeight));
        }
        catch
        {
            // Display information is a platform query and is not worth crashing the app over.
            return (preferredWidth, preferredHeight);
        }
    }
}
