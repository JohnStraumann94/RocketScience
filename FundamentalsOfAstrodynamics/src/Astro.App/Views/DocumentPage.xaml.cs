namespace Astro.App.Views;

/// <summary>
/// A window that displays one Markdown document: either the general Help file or the
/// step-by-step Instructions for a single sample.
/// </summary>
/// <remarks>
/// <para>
/// ONE PAGE, TWO FEATURES. The Help button and the per-topic Instructions button both open
/// this page. They differ only in the text handed to the constructor, so there is a single
/// place to fix a layout problem, a single place to change the reading width, and a single
/// place that knows how to render Markdown.
/// </para>
/// <para>
/// WHY A SEPARATE WINDOW rather than a modal or a tab. Instructions are meant to be followed
/// while working: the reader needs to see step 3 and the input boxes at the same time. A
/// modal would hide the very thing it is describing. A separate window can be dragged onto a
/// second monitor, left open across sample changes, and closed independently.
/// </para>
/// </remarks>
public partial class DocumentPage : ContentPage
{
    /// <summary>
    /// The raw Markdown, kept so the "Copy as text" button can put the original source on
    /// the clipboard rather than a lossy reconstruction of the rendered views.
    /// </summary>
    private readonly string _markdown;

    /// <summary>
    /// Creates a document window.
    /// </summary>
    /// <param name="eyebrow">
    /// The small upper-case line above the title, e.g. "HELP" or "CHAPTER 3 - INSTRUCTIONS".
    /// </param>
    /// <param name="title">The large heading, e.g. the sample's name.</param>
    /// <param name="markdown">The document body, in the Markdown subset DocumentRenderer supports.</param>
    /// <param name="sourceHint">
    /// A short note naming the file on disk this document was written in, shown in the footer
    /// so the text can be corrected at source rather than reverse-engineered.
    /// </param>
    public DocumentPage(string eyebrow, string title, string markdown, string sourceHint)
    {
        InitializeComponent();

        _markdown = markdown ?? string.Empty;

        EyebrowLabel.Text = eyebrow;
        TitleLabel.Text = title;
        SourceLabel.Text = sourceHint;

        // Parse once and drop the resulting view tree into the scrolling host. Parsing in the
        // constructor rather than in OnAppearing means the window is never shown empty and
        // then visibly populated a frame later.
        BodyHost.Add(DocumentRenderer.Render(_markdown));
    }

    /// <summary>
    /// Copies the document's Markdown source to the clipboard.
    /// </summary>
    /// <remarks>
    /// The Markdown source is copied rather than the rendered text because it pastes usefully
    /// into an issue, a pull request or a notebook, where the headings and bullets will be
    /// re-rendered. The button's caption is briefly changed to confirm the copy happened,
    /// which is a cheaper and less intrusive acknowledgement than a dialog.
    /// </remarks>
    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(_markdown);

        if (sender is Button button)
        {
            string original = button.Text;
            button.Text = "Copied";

            await Task.Delay(1400);

            button.Text = original;
        }
    }
}
