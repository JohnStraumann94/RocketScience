using System.Text;

namespace Propulsion.App.Views;

/// <summary>
/// A deliberately small Markdown renderer that turns a documentation string into MAUI views.
/// </summary>
/// <remarks>
/// <para>
/// WHY NOT A MARKDOWN NUGET PACKAGE? Because the whole application has, by design, no
/// third-party dependencies beyond MAUI itself and the MVVM toolkit, and because the
/// documentation it has to render is written by us. We therefore only need the handful of
/// constructs we actually use, and supporting exactly those in about two hundred lines is
/// simpler to reason about - and far smaller - than taking a general-purpose parser plus a
/// rendering layer.
/// </para>
/// <para>
/// THE SUPPORTED SUBSET, and how each piece is drawn:
/// </para>
/// <list type="table">
///   <listheader><term>Markup</term><description>Rendered as</description></listheader>
///   <item><term><c># Title</c></term><description>Large bold heading.</description></item>
///   <item><term><c>## Section</c></term><description>Accent-coloured section heading with space above.</description></item>
///   <item><term><c>### Subsection</c></term><description>Small bold heading.</description></item>
///   <item><term><c>- item</c> or <c>* item</c></term><description>Bulleted line, indented, with a hanging bullet glyph.</description></item>
///   <item><term><c>  - item</c></term><description>Nested bullet - two leading spaces per level.</description></item>
///   <item><term><c>1. item</c></term><description>Numbered step. The number in the source is preserved, so steps can be renumbered by editing the file alone.</description></item>
///   <item><term><c>```</c> fenced block</term><description>Monospaced card on the code background colour.</description></item>
///   <item><term><c>| a | b |</c></term><description>Consecutive table rows are gathered and drawn as one monospaced, column-aligned block. Separator rows (<c>|---|---|</c>) are dropped.</description></item>
///   <item><term><c>&gt; note</c></term><description>Callout with an accent-coloured left edge.</description></item>
///   <item><term><c>---</c></term><description>Horizontal rule.</description></item>
///   <item><term>anything else</term><description>Body paragraph.</description></item>
/// </list>
/// <para>
/// INLINE MARKUP. Within any non-code line, <c>**bold**</c> and <c>`code`</c> are honoured by
/// building a <see cref="FormattedString"/> of spans. Everything else is passed through
/// literally, which is what we want: an equation such as <c>v^2 = mu*(2/r - 1/a)</c> must not
/// be mangled by a parser trying to be clever about asterisks.
/// </para>
/// <para>
/// PERFORMANCE. A document is parsed once, when its window opens, and produces a static tree
/// of labels. There is no re-flow, no virtualisation and no incremental update, because the
/// documents are a few hundred lines at most and are never edited at runtime.
/// </para>
/// </remarks>
public static class DocumentRenderer
{
    // ==================================================================================
    // PALETTE
    // These mirror the colours in Resources/Styles/Colors.xaml. They are repeated here as
    // constants rather than looked up from the resource dictionary because this class
    // builds views in code, and a hard failure at startup from a mistyped resource key
    // would be far worse than a duplicated hex string.
    // ==================================================================================

    private static readonly Color PrimaryText = Color.FromArgb("#E7ECF4");
    private static readonly Color SecondaryText = Color.FromArgb("#9AA4B5");
    private static readonly Color Accent = Color.FromArgb("#4EA8FF");
    private static readonly Color CodeText = Color.FromArgb("#C8D3E4");
    private static readonly Color CodeBackground = Color.FromArgb("#12151C");
    private static readonly Color CardBackground = Color.FromArgb("#1E222C");
    private static readonly Color RuleColor = Color.FromArgb("#2E3440");

    /// <summary>
    /// Parses a Markdown document and returns a layout containing the rendered views.
    /// </summary>
    /// <param name="markdown">The document source. May be null or empty.</param>
    /// <returns>
    /// A <see cref="VerticalStackLayout"/> ready to be dropped into a <c>ScrollView</c>.
    /// </returns>
    public static VerticalStackLayout Render(string? markdown)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            layout.Add(BodyLabel("No documentation is available for this item."));
            return layout;
        }

        // Normalise line endings so the parser only ever has to think about '\n'.
        string[] rawLines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        // Fold hard-wrapped source lines back into whole paragraphs before parsing.
        List<string> lines = Fold(rawLines);

        int index = 0;

        while (index < lines.Count)
        {
            string line = lines[index];
            string trimmed = line.TrimStart();

            // ---- Fenced code block -------------------------------------------------
            // Everything up to the closing fence is taken verbatim: no inline parsing,
            // no trimming, so indentation - which is syntax in Python - survives intact.
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                StringBuilder code = new();
                index++;

                while (index < lines.Count &&
                       !lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    code.AppendLine(lines[index]);
                    index++;
                }

                // Step past the closing fence (or off the end, if the document forgot one).
                index++;

                layout.Add(CodeCard(code.ToString().TrimEnd()));
                continue;
            }

            // ---- Table -------------------------------------------------------------
            // Gathered greedily: every consecutive line starting with '|' belongs to the
            // same table, and the whole thing is rendered as one aligned monospaced block.
            if (trimmed.StartsWith("|", StringComparison.Ordinal))
            {
                List<string> rows = [];

                while (index < lines.Count &&
                       lines[index].TrimStart().StartsWith("|", StringComparison.Ordinal))
                {
                    rows.Add(lines[index].Trim());
                    index++;
                }

                layout.Add(TableCard(rows));
                continue;
            }

            index++;

            // ---- Blank line --------------------------------------------------------
            if (trimmed.Length == 0)
            {
                continue;
            }

            // ---- Horizontal rule ---------------------------------------------------
            if (trimmed is "---" or "***" or "___")
            {
                layout.Add(new BoxView
                {
                    Color = RuleColor,
                    HeightRequest = 1,
                    Margin = new Thickness(0, 14, 0, 14)
                });
                continue;
            }

            // ---- Headings ----------------------------------------------------------
            if (trimmed.StartsWith("### ", StringComparison.Ordinal))
            {
                layout.Add(Heading(trimmed[4..], 15, PrimaryText, top: 14, bottom: 4));
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                layout.Add(Heading(trimmed[3..], 17, Accent, top: 20, bottom: 6));
                continue;
            }

            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                layout.Add(Heading(trimmed[2..], 22, PrimaryText, top: 4, bottom: 8));
                continue;
            }

            // ---- Callout -----------------------------------------------------------
            if (trimmed.StartsWith("> ", StringComparison.Ordinal))
            {
                layout.Add(Callout(trimmed[2..]));
                continue;
            }

            // ---- Bullet ------------------------------------------------------------
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                // Indentation depth: two source spaces per nesting level.
                int depth = (line.Length - trimmed.Length) / 2;
                layout.Add(Bullet(trimmed[2..], depth));
                continue;
            }

            // ---- Numbered step -----------------------------------------------------
            if (IsNumberedStep(trimmed))
            {
                int dot = trimmed.IndexOf(". ", StringComparison.Ordinal);
                layout.Add(NumberedStep(trimmed[..dot], trimmed[(dot + 2)..]));
                continue;
            }

            // ---- Ordinary paragraph ------------------------------------------------
            layout.Add(BodyLabel(trimmed));
        }

        return layout;
    }

    // ==================================================================================
    // LINE FOLDING
    // ==================================================================================

    /// <summary>
    /// Joins hard-wrapped source lines back into whole logical lines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The documentation is written wrapped at about ninety-five columns, because that is how
    /// prose stays reviewable in a diff. Markdown treats consecutive non-blank lines as ONE
    /// paragraph, and this pass is what implements that rule. Without it every source line
    /// would become its own label, which produces visibly wrong leading, and - worse - would
    /// break any <c>**bold**</c> or <c>`code`</c> span that happened to straddle a line
    /// break, leaving the raw asterisks on screen.
    /// </para>
    /// <para>
    /// What is folded, and what is not:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Never folded:</b> anything inside a fenced code block, table rows, headings,
    ///     horizontal rules and blank lines. Code in particular must survive byte for byte,
    ///     because in Python the indentation is the syntax.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Folded into the previous line:</b> a plain continuation line, i.e. one that does
    ///     not itself begin a new construct. This is what carries a wrapped paragraph, and
    ///     also what carries the second and subsequent lines of a wrapped bullet.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Folded quote-to-quote:</b> consecutive <c>&gt;</c> lines become one callout
    ///     rather than a stack of separate boxes each with its own accent bar.
    ///   </description></item>
    /// </list>
    /// <para>
    /// The distinction that makes wrapped bullets work is simple: an indented line that starts
    /// with <c>- </c> is a NESTED bullet, and an indented line that does not is a
    /// CONTINUATION of the bullet above it. That is exactly the convention the documents are
    /// written in.
    /// </para>
    /// <para>
    /// The leading whitespace of the FIRST line of each block is preserved, because the bullet
    /// renderer derives its nesting depth from it.
    /// </para>
    /// </remarks>
    private static List<string> Fold(string[] lines)
    {
        List<string> folded = [];
        bool insideFence = false;

        foreach (string raw in lines)
        {
            string trimmed = raw.TrimStart();

            // Fenced code: toggle on the fence itself, and pass everything through verbatim.
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                insideFence = !insideFence;
                folded.Add(raw);
                continue;
            }

            if (insideFence)
            {
                folded.Add(raw);
                continue;
            }

            // Structural lines are emitted as-is and can never absorb a continuation.
            bool isStructural =
                trimmed.Length == 0 ||
                trimmed.StartsWith('|') ||
                trimmed.StartsWith('#') ||
                trimmed is "---" or "***" or "___";

            if (isStructural)
            {
                folded.Add(raw);
                continue;
            }

            bool isBullet = trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                            trimmed.StartsWith("* ", StringComparison.Ordinal);
            bool isQuote = trimmed.StartsWith("> ", StringComparison.Ordinal);
            bool isStep = IsNumberedStep(trimmed);

            string previous = folded.Count > 0 ? folded[^1] : string.Empty;
            string previousTrimmed = previous.TrimStart();

            // A quote line following another quote line extends the same callout.
            if (isQuote && previousTrimmed.StartsWith("> ", StringComparison.Ordinal))
            {
                folded[^1] = previous.TrimEnd() + " " + trimmed[2..];
                continue;
            }

            // A plain line following any foldable line continues it.
            if (!isBullet && !isQuote && !isStep && folded.Count > 0)
            {
                bool previousIsFoldable =
                    previousTrimmed.Length > 0 &&
                    !previousTrimmed.StartsWith('|') &&
                    !previousTrimmed.StartsWith('#') &&
                    !previousTrimmed.StartsWith("```", StringComparison.Ordinal) &&
                    previousTrimmed is not ("---" or "***" or "___");

                if (previousIsFoldable)
                {
                    folded[^1] = previous.TrimEnd() + " " + trimmed;
                    continue;
                }
            }

            folded.Add(raw);
        }

        return folded;
    }

    /// <summary>
    /// True when a line begins with a short run of digits followed by ". ", e.g. "12. Press Run".
    /// </summary>
    /// <remarks>
    /// Matched by hand rather than with a regular expression: the pattern is trivial, this
    /// runs once per line of every document, and it avoids paying for regex construction at
    /// window-open time. The length cap of three digits stops a decimal number at the start of
    /// a sentence from being mistaken for a list marker.
    /// </remarks>
    private static bool IsNumberedStep(string trimmed)
    {
        int dot = trimmed.IndexOf(". ", StringComparison.Ordinal);

        return dot is > 0 and <= 3 && trimmed[..dot].All(char.IsDigit);
    }

    // ==================================================================================
    // BLOCK BUILDERS
    // ==================================================================================

    /// <summary>Builds a heading label.</summary>
    private static Label Heading(string text, double size, Color colour, double top, double bottom) =>
        new()
        {
            FormattedText = Inline(text, size, colour, bold: true),
            Margin = new Thickness(0, top, 0, bottom)
        };

    /// <summary>Builds a body paragraph.</summary>
    private static Label BodyLabel(string text) =>
        new()
        {
            FormattedText = Inline(text, 14, PrimaryText, bold: false),
            LineHeight = 1.45,
            Margin = new Thickness(0, 0, 0, 8)
        };

    /// <summary>
    /// Builds one bullet: a hanging glyph in a fixed-width column, then the wrapped text.
    /// </summary>
    /// <remarks>
    /// A <see cref="Grid"/> rather than a horizontal stack, because the text must wrap under
    /// itself rather than under the bullet - the only way to get a proper hanging indent.
    /// </remarks>
    private static View Bullet(string text, int depth)
    {
        // Nested levels get a smaller, hollow glyph so the hierarchy reads at a glance.
        string glyph = depth == 0 ? "\u2022" : "\u25E6";

        Grid grid = new()
        {
            ColumnDefinitions = [new ColumnDefinition(18), new ColumnDefinition(GridLength.Star)],
            Margin = new Thickness(6 + (depth * 18), 0, 0, 6)
        };

        Label glyphLabel = new()
        {
            Text = glyph,
            FontSize = 14,
            TextColor = Accent,
            VerticalOptions = LayoutOptions.Start
        };

        Label textLabel = new()
        {
            FormattedText = Inline(text, 14, PrimaryText, bold: false),
            LineHeight = 1.4
        };

        grid.Add(glyphLabel, 0);
        grid.Add(textLabel, 1);

        return grid;
    }

    /// <summary>Builds one numbered step, with the number in the accent colour.</summary>
    private static View NumberedStep(string number, string text)
    {
        Grid grid = new()
        {
            ColumnDefinitions = [new ColumnDefinition(26), new ColumnDefinition(GridLength.Star)],
            Margin = new Thickness(6, 0, 0, 8)
        };

        Label numberLabel = new()
        {
            Text = number + ".",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Accent,
            VerticalOptions = LayoutOptions.Start
        };

        Label textLabel = new()
        {
            FormattedText = Inline(text, 14, PrimaryText, bold: false),
            LineHeight = 1.45
        };

        grid.Add(numberLabel, 0);
        grid.Add(textLabel, 1);

        return grid;
    }

    /// <summary>Builds a monospaced card for a fenced code block.</summary>
    private static View CodeCard(string code) =>
        new Border
        {
            BackgroundColor = CodeBackground,
            Stroke = RuleColor,
            StrokeThickness = 1,
            Padding = new Thickness(14, 12),
            Margin = new Thickness(0, 4, 0, 12),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Content = new Label
            {
                Text = code,
                FontFamily = "Consolas",
                FontSize = 12.5,
                LineHeight = 1.35,
                TextColor = CodeText
            }
        };

    /// <summary>
    /// Builds a table as a single column-aligned monospaced block.
    /// </summary>
    /// <remarks>
    /// A real MAUI <see cref="Grid"/> would give prettier cell borders, but it also needs a
    /// star-sizing decision per column that no document can express. Padding the cells to a
    /// common width in a monospaced font gives perfect alignment for free, always fits, and
    /// is what a terminal would do - which suits a developer-facing tool.
    /// </remarks>
    private static View TableCard(List<string> rows)
    {
        // Split each row into cells, dropping the leading and trailing pipe.
        List<string[]> cells = [];

        foreach (string row in rows)
        {
            string body = row.Trim();

            if (body.StartsWith('|'))
            {
                body = body[1..];
            }

            if (body.EndsWith('|'))
            {
                body = body[..^1];
            }

            string[] parts = body.Split('|').Select(part => part.Trim()).ToArray();

            // Drop the |---|---| separator row: it carries no information once the header is
            // going to be rendered in bold anyway.
            bool isSeparator = parts.All(part =>
                part.Length > 0 && part.All(character => character is '-' or ':'));

            if (!isSeparator)
            {
                cells.Add(parts);
            }
        }

        if (cells.Count == 0)
        {
            return new BoxView { Color = Colors.Transparent, HeightRequest = 0 };
        }

        // Column widths: the widest cell in each column, so nothing is ever truncated.
        int columnCount = cells.Max(row => row.Length);
        int[] widths = new int[columnCount];

        foreach (string[] row in cells)
        {
            for (int column = 0; column < row.Length; column++)
            {
                widths[column] = Math.Max(widths[column], StripInline(row[column]).Length);
            }
        }

        StringBuilder text = new();

        for (int rowIndex = 0; rowIndex < cells.Count; rowIndex++)
        {
            string[] row = cells[rowIndex];

            for (int column = 0; column < columnCount; column++)
            {
                string cell = column < row.Length ? StripInline(row[column]) : string.Empty;

                // The final column is not padded, which avoids a ragged block of trailing
                // spaces that would look wrong if the user copied the text out.
                text.Append(column == columnCount - 1 ? cell : cell.PadRight(widths[column] + 2));
            }

            text.AppendLine();

            // Underline the header row with box-drawing characters.
            if (rowIndex == 0)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    text.Append(new string('\u2500', widths[column]));
                    text.Append(column == columnCount - 1 ? string.Empty : "  ");
                }

                text.AppendLine();
            }
        }

        return new Border
        {
            BackgroundColor = CodeBackground,
            Stroke = RuleColor,
            StrokeThickness = 1,
            Padding = new Thickness(14, 12),
            Margin = new Thickness(0, 4, 0, 12),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Content = new Label
            {
                Text = text.ToString().TrimEnd(),
                FontFamily = "Consolas",
                FontSize = 12.5,
                LineHeight = 1.4,
                TextColor = CodeText
            }
        };
    }

    /// <summary>Builds a callout: an accent bar on the left, tinted body text on the right.</summary>
    private static View Callout(string text)
    {
        Grid grid = new()
        {
            ColumnDefinitions = [new ColumnDefinition(3), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 12,
            Margin = new Thickness(0, 4, 0, 12)
        };

        grid.Add(new BoxView { Color = Accent, WidthRequest = 3 }, 0);

        grid.Add(new Label
        {
            FormattedText = Inline(text, 13.5, SecondaryText, bold: false),
            LineHeight = 1.45,
            VerticalOptions = LayoutOptions.Center
        }, 1);

        return new Border
        {
            BackgroundColor = CardBackground,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = new Thickness(12, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Content = grid
        };
    }

    // ==================================================================================
    // INLINE MARKUP
    // ==================================================================================

    /// <summary>
    /// Builds a <see cref="FormattedString"/> honouring <c>**bold**</c> and <c>`code`</c>.
    /// </summary>
    /// <remarks>
    /// A single left-to-right scan. When a marker is found, the text before it is emitted as
    /// a normal span and the text up to the matching closing marker is emitted as a styled
    /// one. An unmatched marker is emitted literally rather than swallowing the rest of the
    /// line - documentation containing a stray asterisk should still be readable.
    /// </remarks>
    private static FormattedString Inline(string text, double size, Color colour, bool bold)
    {
        FormattedString formatted = new();
        StringBuilder plain = new();

        void FlushPlain()
        {
            if (plain.Length == 0)
            {
                return;
            }

            formatted.Spans.Add(new Span
            {
                Text = plain.ToString(),
                FontSize = size,
                TextColor = colour,
                FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None
            });

            plain.Clear();
        }

        int position = 0;

        while (position < text.Length)
        {
            // ---- **bold** ----------------------------------------------------------
            if (position + 1 < text.Length && text[position] == '*' && text[position + 1] == '*')
            {
                int close = text.IndexOf("**", position + 2, StringComparison.Ordinal);

                if (close > 0)
                {
                    FlushPlain();

                    // The inner text is emitted through the italic splitter rather than as a
                    // single span, so that a title inside a bold citation - which is written
                    // as **Author, *Title*** - keeps both attributes instead of showing the
                    // inner asterisks literally. A MAUI Span cannot contain another Span, so
                    // nesting has to be flattened into a run of differently attributed spans.
                    AddEmphasised(formatted, text[(position + 2)..close], size, colour, bold: true);

                    position = close + 2;
                    continue;
                }
            }

            // ---- *italic* ----------------------------------------------------------
            // Guarded on the character after the marker being non-blank, which is the
            // standard rule and is what stops a multiplication sign in prose ("v * t") from
            // being mistaken for the start of an emphasis run.
            if (text[position] == '*' &&
                position + 1 < text.Length &&
                !char.IsWhiteSpace(text[position + 1]))
            {
                int close = text.IndexOf('*', position + 1);

                if (close > 0 && !char.IsWhiteSpace(text[close - 1]))
                {
                    FlushPlain();

                    formatted.Spans.Add(new Span
                    {
                        Text = text[(position + 1)..close],
                        FontSize = size,
                        TextColor = colour,
                        FontAttributes = bold ? FontAttributes.Bold | FontAttributes.Italic : FontAttributes.Italic
                    });

                    position = close + 1;
                    continue;
                }
            }

            // ---- `code` ------------------------------------------------------------
            if (text[position] == '`')
            {
                int close = text.IndexOf('`', position + 1);

                if (close > 0)
                {
                    FlushPlain();

                    formatted.Spans.Add(new Span
                    {
                        Text = text[(position + 1)..close],
                        FontFamily = "Consolas",
                        FontSize = size - 0.5,
                        TextColor = CodeText
                    });

                    position = close + 1;
                    continue;
                }
            }

            plain.Append(text[position]);
            position++;
        }

        FlushPlain();

        // A Label with an empty FormattedString renders nothing at all, which would silently
        // drop a blank cell; one empty span keeps the layout honest.
        if (formatted.Spans.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = string.Empty, FontSize = size, TextColor = colour });
        }

        return formatted;
    }

    /// <summary>
    /// Appends <paramref name="text"/> as bold spans, switching in italic across any
    /// <c>*...*</c> runs it contains.
    /// </summary>
    /// <remarks>
    /// This exists only to flatten one level of nesting. Markdown allows emphasis inside
    /// strong emphasis, but a MAUI <see cref="Span"/> is a leaf: it carries attributes and
    /// text and nothing else. Splitting the run into alternating spans produces the same
    /// visual result without needing a tree.
    /// </remarks>
    private static void AddEmphasised(FormattedString formatted, string text, double size, Color colour, bool bold)
    {
        FontAttributes baseAttributes = bold ? FontAttributes.Bold : FontAttributes.None;
        int position = 0;

        while (position < text.Length)
        {
            int open = text.IndexOf('*', position);

            // No further marker: the rest of the run is plain.
            if (open < 0)
            {
                AddRun(text[position..], baseAttributes);
                break;
            }

            int close = open + 1 < text.Length ? text.IndexOf('*', open + 1) : -1;

            // An unmatched marker is emitted verbatim rather than swallowed, so that a
            // malformed document shows what it actually says instead of losing characters.
            if (close < 0)
            {
                AddRun(text[position..], baseAttributes);
                break;
            }

            AddRun(text[position..open], baseAttributes);
            AddRun(text[(open + 1)..close], baseAttributes | FontAttributes.Italic);
            position = close + 1;
        }

        void AddRun(string run, FontAttributes attributes)
        {
            if (run.Length == 0)
            {
                return;
            }

            formatted.Spans.Add(new Span
            {
                Text = run,
                FontSize = size,
                TextColor = colour,
                FontAttributes = attributes
            });
        }
    }

    /// <summary>
    /// Removes inline markers from a string.
    /// </summary>
    /// <remarks>
    /// Used for table cells, which are rendered inside a single monospaced label and so
    /// cannot carry per-span formatting. Stripping the markers is better than showing them:
    /// the reader sees clean text, and the column alignment is computed from what is actually
    /// drawn.
    /// </remarks>
    private static string StripInline(string text) => text.Replace("**", string.Empty).Replace("`", string.Empty);
}
