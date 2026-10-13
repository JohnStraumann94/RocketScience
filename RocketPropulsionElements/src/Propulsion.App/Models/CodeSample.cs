namespace Propulsion.App.Models;

/// <summary>
/// One runnable code sample in the table of contents.
/// </summary>
/// <remarks>
/// <para>
/// A sample is deliberately just <i>data plus a delegate</i>. There is no per-sample view,
/// no per-sample view-model and no per-sample XAML: the single detail pane in
/// <c>MainPage.xaml</c> renders whatever parameters the sample declares, calls
/// <see cref="Run"/>, and displays the returned text. Adding a new sample therefore means
/// adding one entry to <see cref="SampleCatalog"/> and nothing else.
/// </para>
/// <para>
/// The trade-off is that samples can only take numeric inputs and can only return text.
/// For a teaching tool built around closed-form equations that is exactly the right shape,
/// and it keeps the whole catalogue readable in one file.
/// </para>
/// </remarks>
public sealed class CodeSample
{
    /// <summary>A short stable identifier, e.g. "ch3-hohmann".</summary>
    public required string Id { get; init; }

    /// <summary>The title shown in the table of contents and as the detail-pane heading.</summary>
    public required string Title { get; init; }

    /// <summary>The chapter this sample belongs to; used to group the table of contents.</summary>
    public required string Chapter { get; init; }

    /// <summary>A one-sentence description shown under the title in the TOC.</summary>
    public required string Summary { get; init; }

    /// <summary>
    /// A longer explanation of the underlying theory, shown in the detail pane above the
    /// inputs. This is where the book's content actually lives.
    /// </summary>
    public required string Theory { get; init; }

    /// <summary>
    /// The C# source the sample is equivalent to, displayed in the "Code" section so the
    /// reader can see exactly which library calls produced the output.
    /// </summary>
    public required string CodeSnippet { get; init; }

    /// <summary>The editable inputs this sample exposes. May be empty.</summary>
    public required IReadOnlyList<SampleParameter> Parameters { get; init; }

    /// <summary>
    /// The delegate that performs the calculation and returns the formatted report.
    /// </summary>
    /// <remarks>
    /// Exceptions thrown here are caught by the view-model and shown in the output pane, so
    /// sample code is free to validate aggressively — a clear error is a perfectly good
    /// teaching outcome (for instance, asking for a bi-elliptic transfer with too small an
    /// intermediate radius).
    /// </remarks>
    public required Func<SampleInputs, string> Run { get; init; }

    /// <summary>
    /// Optionally builds an animatable flight path from the same inputs <see cref="Run"/> uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Null for the great majority of samples, and deliberately so. Most of the book's
    /// mathematics is STEADY-STATE point performance - a thrust coefficient, an area ratio, a
    /// mixture ratio - and there is simply no path to draw. Only the samples that integrate
    /// equations of motion through time produce a genuine trajectory, and only those set this.
    /// </para>
    /// <para>
    /// Because it is nullable, the "Visualize Trajectory" button can be shown or hidden purely
    /// from data: <see cref="CanVisualize"/> drives its visibility, and no existing sample had
    /// to change when the feature was added.
    /// </para>
    /// </remarks>
    public Func<SampleInputs, Trajectory>? BuildTrajectory { get; init; }

    /// <summary>
    /// True when this sample can produce an animated flight path, i.e. when
    /// <see cref="BuildTrajectory"/> is set.
    /// </summary>
    public bool CanVisualize => BuildTrajectory is not null;
}

/// <summary>
/// A chapter's worth of samples, shaped for a grouped <c>CollectionView</c>.
/// </summary>
/// <remarks>
/// MAUI's grouping support binds to an <c>IEnumerable</c> of <c>IEnumerable</c>, so the
/// idiomatic pattern is to derive the group from <see cref="List{T}"/> and hang the header
/// properties off it. That is exactly what this type does.
/// </remarks>
public sealed class ChapterGroup : List<CodeSample>
{
    /// <summary>Creates a chapter group.</summary>
    /// <param name="chapterNumber">Display number, e.g. "Chapter 3".</param>
    /// <param name="title">Chapter title, e.g. "Basic Orbital Manoeuvres".</param>
    /// <param name="samples">The samples belonging to the chapter.</param>
    public ChapterGroup(string chapterNumber, string title, IEnumerable<CodeSample> samples)
        : base(samples)
    {
        ChapterNumber = chapterNumber;
        Title = title;
    }

    /// <summary>The chapter number shown in the TOC header, e.g. "Chapter 3".</summary>
    public string ChapterNumber { get; }

    /// <summary>The chapter title shown in the TOC header.</summary>
    public string Title { get; }
}
