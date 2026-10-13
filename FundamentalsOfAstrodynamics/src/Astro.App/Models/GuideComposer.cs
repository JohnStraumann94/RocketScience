using System.Globalization;
using System.Text;

namespace Astro.App.Models;

/// <summary>
/// Assembles the Instructions document for one sample.
/// </summary>
/// <remarks>
/// <para>
/// The finished document is part generated and part written by hand, and the split is
/// deliberate:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Generated</b> - "how to run it" and the input reference table. These are mechanical
///     facts that are already stated precisely somewhere else in the program (the sample's
///     <see cref="SampleParameter"/> list, its <see cref="CodeSample.CanVisualize"/> flag).
///     Writing them out by hand for every sample would guarantee that some of them drift out
///     of date the first time a default value changes. Generating them means they are correct
///     by construction, for ever.
///   </description></item>
///   <item><description>
///     <b>Written by hand</b> - the background, the experiments worth trying, and the traps.
///     No generator can invent the observation that raising eccentricity past 0.8 puts
///     periapsis underground, or that the interesting thing about a bi-elliptic transfer only
///     appears beyond a radius ratio of about 11.94. That is the part a reader actually wants,
///     and it is written per sample in <c>docs/guides/&lt;id&gt;.md</c>.
///   </description></item>
/// </list>
/// <para>
/// If a sample has no hand-written guide yet, the generated half still produces a complete,
/// genuinely useful document - so the Instructions button is never dark and never apologetic.
/// </para>
/// </remarks>
public static class GuideComposer
{
    /// <summary>
    /// Builds the full Instructions document for a sample.
    /// </summary>
    /// <param name="sample">The sample being documented.</param>
    /// <param name="authoredGuide">
    /// The hand-written portion loaded from <c>docs/guides/&lt;id&gt;.md</c>, or <c>null</c> if
    /// this sample does not have one yet.
    /// </param>
    /// <param name="hasPython">
    /// Whether a Python translation exists, so the document only advertises the language
    /// toggle when there is something to toggle to.
    /// </param>
    /// <returns>A Markdown document in the subset <c>DocumentRenderer</c> understands.</returns>
    public static string Compose(CodeSample sample, string? authoredGuide, bool hasPython)
    {
        StringBuilder document = new();

        // ==============================================================================
        // 1. WHAT IT IS
        // Repeated from the TOC summary so the Instructions window stands on its own. A
        // reader who has dragged this window to a second monitor should not have to look
        // back at the main window to remember which sample it belongs to.
        // ==============================================================================
        document.AppendLine("## What this sample computes");
        document.AppendLine();
        document.AppendLine(sample.Summary);
        document.AppendLine();

        // ==============================================================================
        // 2. HOW TO RUN IT
        // The step numbering is built up rather than hard-coded, because two of the steps
        // are conditional. Hard-coding "5." next to a step that is sometimes the fourth
        // would be exactly the kind of quiet wrongness this class exists to avoid.
        // ==============================================================================
        document.AppendLine("## How to run it");
        document.AppendLine();

        int step = 1;

        document.AppendLine($"{step++}. This sample is already selected in the table of contents on the left. " +
                            "Every input below already holds a sensible default, so you can run it immediately " +
                            "without changing anything.");

        if (sample.Parameters.Count > 0)
        {
            document.AppendLine($"{step++}. Look at the **INPUTS** card in the main window. " +
                                $"There {(sample.Parameters.Count == 1 ? "is one box" : $"are {sample.Parameters.Count} boxes")} " +
                                "you can edit. Click into one and type a new number. " +
                                "The units are printed to the right of each box, and they matter: " +
                                "entering metres where kilometres are asked for will not be rejected, " +
                                "it will just give you an answer that is wrong by a factor of a thousand.");
        }

        document.AppendLine($"{step++}. Press **Run**. The result appears in the **OUTPUT** card as a formatted " +
                            "report, and the time the calculation took is printed underneath it.");

        if (sample.CanVisualize)
        {
            document.AppendLine($"{step++}. Press **Visualize Trajectory** to open an animated illustration of the " +
                                "flight path these numbers describe. It is recomputed from whatever is in the " +
                                "input boxes at that moment, so it always matches what you are looking at. " +
                                "You can leave that window open and keep changing inputs.");
        }

        document.AppendLine($"{step++}. Press **Reset inputs** at any time to put every box back to its default " +
                            "value. Nothing you type can permanently break the sample.");

        if (hasPython)
        {
            document.AppendLine($"{step++}. Scroll down to **THE CODE THAT RUNS** and use the **C#** / **Python** " +
                                "toggle to switch languages. Both versions implement the same mathematics and " +
                                "produce the same numbers; the Python one is a standalone script you can run " +
                                "outside this application.");
        }
        else
        {
            document.AppendLine($"{step++}. Scroll down to **THE CODE THAT RUNS** to see the exact library calls " +
                                "that produced the output above.");
        }

        document.AppendLine();

        // ==============================================================================
        // 3. THE INPUTS
        // Generated straight from the parameter definitions, so it cannot go stale.
        // ==============================================================================
        if (sample.Parameters.Count > 0)
        {
            document.AppendLine("## The inputs you can change");
            document.AppendLine();
            document.AppendLine("| Input | Units | Default | What it means |");
            document.AppendLine("| --- | --- | --- | --- |");

            foreach (SampleParameter parameter in sample.Parameters)
            {
                string units = string.IsNullOrWhiteSpace(parameter.Units) ? "-" : parameter.Units;
                string help = string.IsNullOrWhiteSpace(parameter.Help) ? "-" : parameter.Help;

                document.AppendLine(
                    $"| {parameter.Label} " +
                    $"| {units} " +
                    $"| {Format(parameter.DefaultValue)} " +
                    $"| {help} |");
            }

            document.AppendLine();
            document.AppendLine("> Every box takes a plain decimal number. Use a full stop for the decimal " +
                                "point regardless of your regional settings, and write very large or very " +
                                "small values in scientific notation if you prefer, for example `1.495979e8`.");
            document.AppendLine();
        }

        // ==============================================================================
        // 4. THE HAND-WRITTEN PART
        // Appended verbatim. It is Markdown already, and it is trusted content that ships
        // with the application, so there is nothing to sanitise.
        // ==============================================================================
        if (!string.IsNullOrWhiteSpace(authoredGuide))
        {
            document.AppendLine(authoredGuide.Trim());
            document.AppendLine();
        }

        // ==============================================================================
        // 5. TROUBLESHOOTING
        // Identical for every sample, because the failure modes are a property of the
        // shared runner rather than of any individual calculation.
        // ==============================================================================
        document.AppendLine("## If something goes wrong");
        document.AppendLine();
        document.AppendLine("- **The output turns red.** That is not a crash. The sample validated your inputs, " +
                            "rejected them, and told you why. The message names the offending value. This is a " +
                            "normal and useful outcome: asking for something physically impossible should say so.");
        document.AppendLine("- **\"... is not a valid number\".** One of the boxes contains something that will not " +
                            "parse - often an empty box, a stray letter, or a comma used as a decimal point.");
        document.AppendLine("- **The answer looks absurd.** Check the units column above first. It is by far the " +
                            "most common cause, and no program can detect it for you.");
        document.AppendLine("- **You have lost track of what you changed.** Press **Reset inputs**.");
        document.AppendLine();

        return document.ToString();
    }

    /// <summary>
    /// Formats a default value for the reference table.
    /// </summary>
    /// <remarks>
    /// Round-trip ("R") formatting would print <c>6378.137</c> correctly but also
    /// <c>0.0010826269</c> as <c>0.0010826269</c> and <c>149597870.7</c> as
    /// <c>149597870.7</c> - fine - yet a value such as <c>1E-06</c> would come out in a
    /// notation that looks like a typo in a prose table. Switching to scientific notation
    /// only for genuinely extreme magnitudes keeps the common case readable.
    /// </remarks>
    private static string Format(double value)
    {
        if (value == 0.0)
        {
            return "0";
        }

        double magnitude = Math.Abs(value);

        return magnitude is >= 1e-4 and < 1e7
            ? value.ToString("0.######", CultureInfo.InvariantCulture)
            : value.ToString("0.####e+0", CultureInfo.InvariantCulture);
    }
}
