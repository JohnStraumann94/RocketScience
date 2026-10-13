using System.Reflection;
using System.Text;

namespace Propulsion.App.Services;

/// <summary>
/// Loads the application's written documentation - the general help file, and the
/// per-sample instruction guides - together with the Python translation of every sample.
/// </summary>
/// <remarks>
/// <para>
/// WHY EMBEDDED RESOURCES RATHER THAN C# STRING LITERALS?
/// </para>
/// <para>
/// All of this content is long-form prose and long-form Python. Holding it in C# string
/// literals would mean escaping every quote and backslash, would make the catalogue file
/// enormous, and would mean a full rebuild to fix a typo in a sentence. Holding it in real
/// <c>.md</c> and <c>.py</c> files instead means:
/// </para>
/// <list type="bullet">
///   <item><description>
///     The Python examples are genuinely runnable. They live in <c>&lt;solution&gt;\python\</c>,
///     they import a real <c>propulsion_core</c> package that sits next to them, and you can run
///     them from a terminal without the app being involved at all.
///   </description></item>
///   <item><description>
///     The guides are readable on GitHub, in any editor, or printed - they are just Markdown.
///   </description></item>
///   <item><description>
///     Nothing has to be escaped, so what you write is exactly what the user sees.
///   </description></item>
/// </list>
/// <para>
/// The files are compiled into the assembly as embedded resources (see the
/// <c>&lt;EmbeddedResource&gt;</c> items in <c>Propulsion.App.csproj</c>), which gives the best of
/// both worlds: one self-contained executable, but source files that are still ordinary text
/// files on disk.
/// </para>
/// <para>
/// LOGICAL NAMES. The csproj assigns each file a flat logical name so that lookups are a
/// simple string concatenation and never depend on folder layout:
/// </para>
/// <list type="bullet">
///   <item><description><c>doc.help</c> - the general help document.</description></item>
///   <item><description><c>guide.&lt;sample id&gt;</c> - one instruction guide per sample.</description></item>
///   <item><description><c>py.&lt;sample id&gt;</c> - the Python translation of one sample.</description></item>
/// </list>
/// <para>
/// Everything is read once, eagerly, in the constructor. The total payload is a few hundred
/// kilobytes of text, so the cost is negligible and it makes every accessor synchronous -
/// which in turn means the view-model properties the UI binds to can be plain properties
/// rather than async-loaded ones.
/// </para>
/// </remarks>
public sealed class ContentLibrary
{
    /// <summary>Logical-name prefix for the per-sample instruction guides.</summary>
    private const string GuidePrefix = "guide.";

    /// <summary>Logical-name prefix for the per-sample Python translations.</summary>
    private const string PythonPrefix = "py.";

    /// <summary>Logical name of the single general help document.</summary>
    private const string HelpName = "doc.help";

    /// <summary>Every embedded text resource, keyed by its logical name.</summary>
    private readonly Dictionary<string, string> _content = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Reads every embedded documentation resource out of this assembly.
    /// </summary>
    public ContentLibrary()
    {
        Assembly assembly = typeof(ContentLibrary).Assembly;

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            // Only take the resources this class owns. The assembly also contains MAUI's own
            // generated resources (XAML, styles, images), which must be left alone.
            bool isOurs =
                resourceName.Equals(HelpName, StringComparison.OrdinalIgnoreCase) ||
                resourceName.StartsWith(GuidePrefix, StringComparison.OrdinalIgnoreCase) ||
                resourceName.StartsWith(PythonPrefix, StringComparison.OrdinalIgnoreCase);

            if (!isOurs)
            {
                continue;
            }

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                continue;
            }

            using StreamReader reader = new(stream, Encoding.UTF8);
            _content[resourceName] = reader.ReadToEnd();
        }
    }

    /// <summary>
    /// True when at least one documentation resource was found. Used only by diagnostics; if
    /// this is ever false the csproj's EmbeddedResource globs have stopped matching.
    /// </summary>
    public bool HasContent => _content.Count > 0;

    /// <summary>The number of loaded resources, shown in the help window's footer.</summary>
    public int ResourceCount => _content.Count;

    /// <summary>
    /// The general "how this application works" document, in Markdown.
    /// </summary>
    /// <remarks>
    /// Returns a short built-in placeholder rather than throwing if the resource is missing,
    /// because a missing help file should never be able to stop the app opening a window.
    /// </remarks>
    public string GetHelpDocument() =>
        _content.TryGetValue(HelpName, out string? text)
            ? text
            : "# Help unavailable\n\nThe help document was not embedded in this build.";

    /// <summary>
    /// The step-by-step instruction guide for one sample, in Markdown, or <c>null</c> when the
    /// sample has no authored guide.
    /// </summary>
    /// <param name="sampleId">The sample's stable id, e.g. <c>ch3-hohmann</c>.</param>
    public string? GetGuide(string sampleId) =>
        _content.TryGetValue(GuidePrefix + sampleId, out string? text) ? text : null;

    /// <summary>
    /// The Python translation of one sample, or <c>null</c> when the sample has no Python
    /// version.
    /// </summary>
    /// <param name="sampleId">The sample's stable id, e.g. <c>ch3-hohmann</c>.</param>
    public string? GetPython(string sampleId) =>
        _content.TryGetValue(PythonPrefix + sampleId, out string? text) ? text : null;

    /// <summary>True when the given sample has an authored instruction guide.</summary>
    public bool HasGuide(string sampleId) => _content.ContainsKey(GuidePrefix + sampleId);

    /// <summary>True when the given sample has a Python translation.</summary>
    public bool HasPython(string sampleId) => _content.ContainsKey(PythonPrefix + sampleId);
}
