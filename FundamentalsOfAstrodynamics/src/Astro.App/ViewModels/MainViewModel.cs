using Astro.App.Models;
using Astro.App.Services;
using Astro.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Astro.App.ViewModels;

/// <summary>
/// The single view-model behind <c>MainPage</c>: it owns the table of contents, the currently
/// selected sample, and the run/reset commands.
/// </summary>
/// <remarks>
/// <para>
/// The app deliberately uses one view-model rather than one per sample. Because every sample
/// is described entirely by data (<see cref="CodeSample"/>), the detail pane can be built
/// generically, and there is nothing sample-specific for a dedicated view-model to hold.
/// </para>
/// <para>
/// Source generators from CommunityToolkit.Mvvm do the boilerplate: <c>[ObservableProperty]</c>
/// on a private field generates the public property plus change notification, and
/// <c>[RelayCommand]</c> on a method generates an <c>ICommand</c>.
/// </para>
/// </remarks>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly AzurePlaceholderService _azure;

    /// <summary>
    /// The embedded documentation and Python translations. Injected rather than newed up so
    /// that the (slightly) expensive one-off resource scan happens once for the process, at
    /// the moment the DI container builds the singleton, and not every time a window opens.
    /// </summary>
    private readonly ContentLibrary _content;

    /// <summary>Creates the view-model and loads the catalogue.</summary>
    /// <param name="azure">The (currently stubbed) Azure integration service.</param>
    /// <param name="content">The embedded help, per-sample guides and Python translations.</param>
    public MainViewModel(AzurePlaceholderService azure, ContentLibrary content)
    {
        _azure = azure;
        _content = content;

        Chapters = new ObservableCollection<ChapterGroup>(SampleCatalog.Build());

        // Open on the first sample of the first chapter so the app never shows an empty pane.
        SelectedSample = Chapters.FirstOrDefault()?.FirstOrDefault();
    }

    /// <summary>The chapter-grouped table of contents shown in the left navigation pane.</summary>
    public ObservableCollection<ChapterGroup> Chapters { get; }

    /// <summary>
    /// The status line shown in the footer, confirming that all computation is local.
    /// </summary>
    public string RuntimeStatus => _azure.StatusMessage;

    /// <summary>
    /// A short version and build-date string shown in the footer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read from the assembly's informational version, with the build date taken from the last
    /// write time of the assembly file. Nothing has to be bumped by hand: rebuilding the project
    /// changes the stamp automatically.
    /// </para>
    /// <para>
    /// This exists for test feedback. The single most common wasted exchange in a bug report is
    /// establishing which build the reporter was actually running, and a stamp on screen that
    /// they can read out removes it entirely.
    /// </para>
    /// </remarks>
    public string BuildStamp
    {
        get
        {
            try
            {
                Assembly assembly = typeof(MainViewModel).Assembly;

                string version =
                    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? assembly.GetName().Version?.ToString()
                    ?? "unknown";

                // The informational version often carries a source-control suffix such as
                // "1.0.0+3f2a1c9". The hash is noise on a footer, so keep only the version.
                int plus = version.IndexOf('+');

                if (plus > 0)
                {
                    version = version[..plus];
                }

                string location = assembly.Location;

                string built = !string.IsNullOrEmpty(location) && File.Exists(location)
                    ? File.GetLastWriteTime(location).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : "unknown";

                return $"v{version}  build {built}";
            }
            catch
            {
                // A cosmetic footer label must never be the reason the window fails to load.
                return "v unknown";
            }
        }
    }

    /// <summary>Total number of samples, shown in the header.</summary>
    public int SampleCount => Chapters.Sum(chapter => chapter.Count);

    /// <summary>
    /// The footer summary line: how many samples there are in total, and how many of those can
    /// be animated.
    /// </summary>
    /// <remarks>
    /// The animated count is derived by asking the catalog rather than being written down
    /// anywhere, so it cannot fall out of step when a trajectory builder is added to or removed
    /// from a sample. It is stated in the footer because a badge on individual rows tells you
    /// which ones qualify but not how many there are to look for.
    /// </remarks>
    public string SampleCountCaption
    {
        get
        {
            int animated = Chapters.Sum(chapter => chapter.Count(sample => sample.CanVisualize));

            return animated == 0
                ? $"{SampleCount} runnable samples"
                : $"{SampleCount} runnable samples  -  {animated} with an animated trajectory (marked ANIMATED)";
        }
    }

    /// <summary>
    /// The sample currently displayed in the detail pane. Setting it clears any previous
    /// output so stale results from another sample can never be mistaken for current ones.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(SelectedTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedChapter))]
    [NotifyPropertyChangedFor(nameof(SelectedTheory))]
    [NotifyPropertyChangedFor(nameof(SelectedCode))]
    [NotifyPropertyChangedFor(nameof(SelectedParameters))]
    [NotifyPropertyChangedFor(nameof(CanVisualize))]
    [NotifyPropertyChangedFor(nameof(HasPython))]
    [NotifyPropertyChangedFor(nameof(CodeLanguageCaption))]
    private CodeSample? _selectedSample;

    /// <summary>
    /// Which language the source listing is currently showing: <c>false</c> for C#,
    /// <c>true</c> for Python.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A boolean rather than an enum because there are exactly two languages and the UI is a
    /// two-state toggle. A bool binds directly to the <c>IsChecked</c>-style visual states of
    /// the two toggle buttons without needing a converter per value.
    /// </para>
    /// <para>
    /// The choice is deliberately STICKY across samples. Somebody reading the catalogue in
    /// Python wants to stay in Python as they move down the table of contents; forcing them
    /// back to C# on every selection would be maddening.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCode))]
    [NotifyPropertyChangedFor(nameof(ShowingCSharp))]
    [NotifyPropertyChangedFor(nameof(CodeLanguageCaption))]
    private bool _showingPython;

    /// <summary>The text shown in the output pane — either results or an error report.</summary>
    [ObservableProperty]
    private string _output = "Select a sample from the left, adjust the inputs, then press Run.";

    /// <summary>True when the last run threw; the view uses it to colour the output red.</summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>How long the last run took, shown beneath the output.</summary>
    [ObservableProperty]
    private string _executionSummary = string.Empty;

    /// <summary>True when a sample is selected, used to show or hide the detail pane.</summary>
    public bool HasSelection => SelectedSample is not null;

    /// <summary>Title of the selected sample.</summary>
    public string SelectedTitle => SelectedSample?.Title ?? string.Empty;

    /// <summary>Chapter of the selected sample.</summary>
    public string SelectedChapter => SelectedSample?.Chapter ?? string.Empty;

    /// <summary>Explanatory theory text for the selected sample.</summary>
    public string SelectedTheory => SelectedSample?.Theory ?? string.Empty;

    /// <summary>
    /// The source listing for the selected sample, in whichever language is currently
    /// selected.
    /// </summary>
    /// <remarks>
    /// Falls back to C# if a sample has no Python translation, so the pane can never be blank.
    /// In practice every sample has both, and <see cref="HasPython"/> hides the toggle if one
    /// ever does not.
    /// </remarks>
    public string SelectedCode
    {
        get
        {
            if (SelectedSample is not { } sample)
            {
                return string.Empty;
            }

            if (ShowingPython && _content.GetPython(sample.Id) is { } python)
            {
                return python;
            }

            return sample.CodeSnippet;
        }
    }

    /// <summary>True when the C# listing is showing; the inverse of <see cref="ShowingPython"/>.</summary>
    /// <remarks>
    /// Exists purely so the two toggle buttons can each bind a background colour to a plain
    /// boolean, without an inverting converter on one of them.
    /// </remarks>
    public bool ShowingCSharp => !ShowingPython;

    /// <summary>
    /// True when the selected sample has a Python translation, used to show or hide the
    /// language toggle.
    /// </summary>
    public bool HasPython => SelectedSample is { } sample && _content.HasPython(sample.Id);

    /// <summary>
    /// The caption printed beneath the source listing, naming the language on screen and
    /// where its file lives on disk.
    /// </summary>
    public string CodeLanguageCaption
    {
        get
        {
            if (SelectedSample is not { } sample)
            {
                return string.Empty;
            }

            return ShowingPython && _content.HasPython(sample.Id)
                ? $"Python - a standalone, runnable script. Source: python\\examples\\{sample.Id}.py"
                : "C# - the exact Astro.Core calls this sample makes when you press Run.";
        }
    }

    /// <summary>The editable parameters of the selected sample.</summary>
    public IReadOnlyList<SampleParameter> SelectedParameters =>
        SelectedSample?.Parameters ?? [];

    /// <summary>
    /// True when the selected sample can produce an animated flight path. Bound to the
    /// visibility of the "Visualize Trajectory" button, so the button simply is not there for
    /// the samples where a trajectory would be meaningless.
    /// </summary>
    public bool CanVisualize => SelectedSample?.CanVisualize == true;

    /// <summary>Switches the source listing to C#.</summary>
    [RelayCommand]
    private void ShowCSharp() => ShowingPython = false;

    /// <summary>Switches the source listing to Python.</summary>
    [RelayCommand]
    private void ShowPython() => ShowingPython = true;

    /// <summary>
    /// Opens the general help document - what the application is, how it is laid out, and how
    /// to work with it - in its own window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A WINDOW, not a modal dialog, and for the same reason the trajectory visualiser is a
    /// window: help that covers up the thing it is describing is close to useless. This one
    /// can be parked on a second monitor and left open for a whole session.
    /// </para>
    /// <para>
    /// The document is Markdown held in <c>docs/help.md</c> and embedded at build time. It is
    /// deliberately a real file rather than a string constant, so it can be read, printed and
    /// reviewed outside the application.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void ShowHelp()
    {
        DocumentPage page = new(
            eyebrow: "HELP",
            title: "Fundamentals of Astrodynamics - Interactive Code Samples",
            markdown: _content.GetHelpDocument(),
            sourceHint: "Source: docs\\help.md  -  edit that file to change this document.");

        Window window = new(page)
        {
            Title = "Help - Fundamentals of Astrodynamics",
            Width = FitWidth(1000),
            Height = FitHeight(860),
            MinimumWidth = 640,
            MinimumHeight = 480
        };

        Application.Current?.OpenWindow(window);
    }

    /// <summary>
    /// Opens the project's donation page in the user's default web browser.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This method deliberately contains no payment logic at all.</b> It hands a URL to the
    /// operating system and stops. The entire transaction - the amount, the card details, the
    /// receipt - happens on the payment processor's own hosted page, inside the user's browser,
    /// where they can see the padlock and the domain name. See <see cref="DonationOptions"/> for
    /// why that separation matters.
    /// </para>
    /// <para>
    /// If no donation page has been configured yet, the button does not open a dead link.
    /// Instead it opens an explanatory window, so that a build handed to testers before the
    /// donation account exists behaves sensibly rather than appearing broken.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task DonateAsync()
    {
        if (!DonationOptions.IsConfigured)
        {
            ShowDonationNotConfigured();
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(DonationOptions.Url);
        }
        catch (Exception ex)
        {
            // Launching a browser can fail on a locked down machine, or if no browser is
            // registered for http at all. Rather than swallow it, show the address so the user
            // can copy it out by hand - the donation is still possible, just less convenient.
            ShowDonationDocument(
                "The donation page could not be opened",
                $"This computer would not open a web browser automatically.\n\n" +
                $"Reason reported by Windows: `{ex.GetType().Name}: {ex.Message}`\n\n" +
                "You can still donate by typing this address into any browser:\n\n" +
                $"```\n{DonationOptions.Url}\n```\n");
        }
    }

    /// <summary>
    /// Explains that donations are not switched on yet, and - for whoever is building this -
    /// exactly how to switch them on.
    /// </summary>
    private void ShowDonationNotConfigured() => ShowDonationDocument(
        "Donations are not set up yet",
        "Thank you for wanting to support this project. The donation page is not live yet, so " +
        "there is nothing to send you to.\n\n" +
        "> This build was handed out before a donation account existed. That is expected, and " +
        "nothing is wrong with your copy.\n\n" +
        "## If this is your copy to configure\n\n" +
        "There are two ways to point this button at a real donation page.\n\n" +
        "**Without rebuilding** - create a plain text file named `" + DonationOptions.OverrideFileName +
        "` in the same folder as the program's `.exe`, containing one line: the full address of " +
        "the donation page. Lines starting with `#` are ignored, so you can leave yourself a " +
        "note in the file. Restart the program and the button will use it. This works on a copy " +
        "that has already been published and sent out.\n\n" +
        "**In the source** - change the `DefaultUrl` constant in " +
        "`Services/DonationOptions.cs` and rebuild. This is the right option for the version you " +
        "intend to keep.\n\n" +
        "Only `http` and `https` addresses are accepted, so a typo cannot turn this button into a " +
        "way of launching some other program.\n");

    /// <summary>
    /// Shared plumbing for the two donation related messages, so both look like every other
    /// document window in the application rather than like a raw dialog box.
    /// </summary>
    private void ShowDonationDocument(string title, string markdown)
    {
        DocumentPage page = new(
            eyebrow: "SUPPORT",
            title: title,
            markdown: markdown,
            sourceHint: "Source: Services\\DonationOptions.cs");

        Window window = new(page)
        {
            Title = "Support this project",
            Width = FitWidth(720),
            Height = FitHeight(620),
            MinimumWidth = 520,
            MinimumHeight = 400
        };

        Application.Current?.OpenWindow(window);
    }
    /// <remarks>
    /// <para>
    /// The document is assembled by <see cref="GuideComposer"/> from two halves: generated
    /// sections (how to run it, and a reference table of every input with its units and
    /// default) that are derived from the sample's own definition and therefore can never
    /// drift out of date, plus a hand-written half loaded from <c>docs/guides/&lt;id&gt;.md</c>
    /// containing the background and the experiments worth trying.
    /// </para>
    /// <para>
    /// Because the generated half needs nothing but the <see cref="CodeSample"/> itself, the
    /// button works for every sample - including any added later that has no hand-written
    /// guide yet.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void ShowInstructions()
    {
        if (SelectedSample is not { } sample)
        {
            return;
        }

        string document = GuideComposer.Compose(
            sample,
            _content.GetGuide(sample.Id),
            _content.HasPython(sample.Id));

        DocumentPage page = new(
            eyebrow: $"{sample.Chapter} - INSTRUCTIONS",
            title: sample.Title,
            markdown: document,
            sourceHint: $"Hand-written notes: docs\\guides\\{sample.Id}.md  -  " +
                        "the steps and input table above are generated from the sample itself.");

        Window window = new(page)
        {
            Title = $"Instructions - {sample.Title}",
            Width = FitWidth(980),
            Height = FitHeight(840),
            MinimumWidth = 640,
            MinimumHeight = 480
        };

        Application.Current?.OpenWindow(window);
    }

    /// <summary>
    /// Builds the selected sample's flight path and opens it in an animated visualisation
    /// window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A SEPARATE WINDOW rather than a modal page or an in-place panel, for three reasons: the
    /// animation can be watched while the parameters that produced it are still visible in the
    /// main window; several trajectories can be opened side by side to compare, say, a Hohmann
    /// transfer against a bi-elliptic one; and an orbit drawn to scale genuinely needs the room.
    /// </para>
    /// <para>
    /// The trajectory is recomputed from the CURRENT parameter values rather than cached from
    /// the last Run, so what is animated always matches what is on screen.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task VisualizeTrajectoryAsync()
    {
        if (SelectedSample is not { BuildTrajectory: { } builder } sample)
        {
            return;
        }

        try
        {
            Dictionary<string, double> values = [];
            foreach (SampleParameter parameter in sample.Parameters)
            {
                values[parameter.Key] = parameter.ToDouble();
            }

            SampleInputs inputs = new(values);

            Trajectory trajectory = await Task.Run(() => builder(inputs));

            if (trajectory.Points.Count < 2)
            {
                HasError = true;
                Output = "The trajectory could not be visualised: the propagation produced " +
                         "fewer than two points. Check the inputs and try again.";
                return;
            }

            TrajectoryPage page = new(trajectory);

            Window window = new(page)
            {
                Title = $"Trajectory - {sample.Title}",
                Width = FitWidth(1180),
                Height = FitHeight(820),
                MinimumWidth = 780,
                MinimumHeight = 560
            };

            Application.Current?.OpenWindow(window);

            ExecutionSummary =
                $"Visualising {trajectory.Points.Count:N0} trajectory points " +
                $"spanning {trajectory.Duration:N0} s of flight.";
        }
        catch (Exception exception)
        {
            // Same philosophy as RunAsync: a rejected input is a teaching moment, not a crash.
            HasError = true;

            StringBuilder message = new();
            message.AppendLine("The sample could not be run.");
            message.AppendLine();
            message.AppendLine("The trajectory could not be computed with these inputs.");
            message.AppendLine();
            message.AppendLine($"{exception.GetType().Name}: {exception.Message}");

            if (exception is ArgumentException { ParamName: { } parameterName })
            {
                message.AppendLine();
                message.AppendLine($"Offending input: {parameterName}");
            }

            Output = message.ToString();
            ExecutionSummary = "Failed.";
        }
    }

    /// <summary>
    /// Called by the generated <c>SelectedSample</c> setter after the value changes.
    /// Resets the output pane so results always belong to the sample on screen.
    /// </summary>
    partial void OnSelectedSampleChanged(CodeSample? value)
    {
        HasError = false;
        ExecutionSummary = string.Empty;
        Output = value is null
            ? "Select a sample from the left."
            : $"Ready. Press Run to execute \"{value.Title}\".";
    }

    /// <summary>
    /// Executes the selected sample with the current parameter values and displays the result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All exceptions are caught deliberately. Sample code validates its inputs aggressively
    /// (asking for a bi-elliptic transfer with too small an intermediate radius, or a Kepler
    /// solve with e &gt; 1, will throw), and surfacing that message in the output pane is a
    /// perfectly good teaching outcome. Crashing the app is not.
    /// </para>
    /// <para>
    /// The work is done on a background thread via <see cref="Task.Run(Action)"/> so the UI
    /// stays responsive: most samples finish in microseconds, but the table-generating ones
    /// with a large row count, and Lambert with a tight tolerance, can take noticeably longer.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task RunAsync()
    {
        if (SelectedSample is not { } sample)
        {
            return;
        }

        HasError = false;
        ExecutionSummary = "Running...";

        try
        {
            // Parse every parameter first, so a typo is reported before any physics runs.
            Dictionary<string, double> values = [];
            foreach (SampleParameter parameter in sample.Parameters)
            {
                values[parameter.Key] = parameter.ToDouble();
            }

            SampleInputs inputs = new(values);

            Stopwatch stopwatch = Stopwatch.StartNew();
            string result = await Task.Run(() => sample.Run(inputs));
            stopwatch.Stop();

            Output = result;
            ExecutionSummary = $"Completed in {stopwatch.Elapsed.TotalMilliseconds:F3} ms.";
        }
        catch (Exception exception)
        {
            HasError = true;

            StringBuilder message = new();
            message.AppendLine("The sample could not be run.");
            message.AppendLine();
            message.AppendLine($"{exception.GetType().Name}: {exception.Message}");

            // Argument exceptions carry the offending parameter name, which is the single
            // most useful piece of information for the user.
            if (exception is ArgumentException { ParamName: { } parameterName })
            {
                message.AppendLine();
                message.AppendLine($"Offending input: {parameterName}");
            }

            message.AppendLine();
            message.AppendLine("Adjust the inputs above and try again, or press Reset to restore the defaults.");

            Output = message.ToString();
            ExecutionSummary = "Failed.";
        }
    }

    /// <summary>Restores every parameter of the selected sample to its default value.</summary>
    [RelayCommand]
    private void ResetParameters()
    {
        if (SelectedSample is not { } sample)
        {
            return;
        }

        foreach (SampleParameter parameter in sample.Parameters)
        {
            parameter.Reset();
        }

        HasError = false;
        ExecutionSummary = string.Empty;
        Output = "Inputs restored to their default values. Press Run to execute.";
    }

    /// <summary>Copies the current output to the system clipboard.</summary>
    [RelayCommand]
    private async Task CopyOutputAsync()
    {
        if (!string.IsNullOrWhiteSpace(Output))
        {
            await Clipboard.Default.SetTextAsync(Output);
            ExecutionSummary = "Output copied to the clipboard.";
        }
    }

    // ==================================================================================
    // WINDOW SIZING
    // ==================================================================================

    /// <summary>
    /// Reduces a preferred window width to something the display can actually show.
    /// </summary>
    /// <remarks>
    /// The Help, Instructions and Trajectory windows all ask for a comfortable reading size.
    /// On a 1366 x 768 laptop those requests are wider and taller than the screen, so the
    /// window opens with its edges outside the desktop and the transport controls along the
    /// bottom of the trajectory window become unreachable. Clamping costs nothing on a large
    /// monitor and prevents that entirely on a small one.
    /// </remarks>
    private static double FitWidth(double preferred) => Fit(preferred, horizontal: true);

    /// <summary>
    /// Reduces a preferred window height to something the display can actually show.
    /// </summary>
    private static double FitHeight(double preferred) => Fit(preferred, horizontal: false);

    /// <summary>
    /// Shared implementation of <see cref="FitWidth"/> and <see cref="FitHeight"/>.
    /// </summary>
    /// <remarks>
    /// <c>MainDisplayInfo</c> is expressed in physical pixels while window sizes are in
    /// device-independent units, so the display extent must be divided by the density before
    /// the two can be compared. The 0.92 factor leaves room for the title bar and the taskbar.
    /// Any failure to read the display falls back to the caller's preference, because a
    /// slightly oversized window is a far better outcome than a crash on startup.
    /// </remarks>
    private static double Fit(double preferred, bool horizontal)
    {
        try
        {
            DisplayInfo display = DeviceDisplay.Current.MainDisplayInfo;

            if (display.Density <= 0)
            {
                return preferred;
            }

            double extent = horizontal ? display.Width : display.Height;

            if (extent <= 0)
            {
                return preferred;
            }

            return Math.Min(preferred, extent / display.Density * 0.92);
        }
        catch
        {
            return preferred;
        }
    }
}
