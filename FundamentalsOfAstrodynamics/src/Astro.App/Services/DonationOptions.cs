namespace Astro.App.Services;

/// <summary>
/// Holds the single piece of configuration behind the <b>Donate</b> button: the address of the
/// donation page to open in the user's browser.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why there is no payment code anywhere in this application.</b> The Donate button does not
/// take a card number, does not talk to a payment processor, and never sees a currency amount.
/// All it does is hand a URL to the operating system's default browser. This is the same design
/// Paint.NET uses, and it is deliberate for three reasons:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>Compliance.</b> The moment an application touches cardholder data it falls inside the
///     scope of PCI DSS. By redirecting to a hosted page run by PayPal, Stripe, Ko-fi or similar,
///     the payment happens entirely on the processor's own infrastructure and this program stays
///     completely outside that scope. There is nothing here to audit and nothing here to leak.
///   </description></item>
///   <item><description>
///     <b>Trust.</b> Users are - correctly - suspicious of a desktop program that asks for card
///     details in its own window. Sending them to a familiar, HTTPS, processor branded page that
///     they can inspect in their browser's address bar is far more likely to be completed.
///   </description></item>
///   <item><description>
///     <b>Maintenance.</b> Payment APIs change, credentials expire and processors get swapped.
///     Because the only coupling here is a string, changing processor never requires touching
///     application code, and - see below - need not even require a rebuild.
///   </description></item>
/// </list>
/// <para>
/// <b>The URL is a placeholder until an account exists.</b> This follows the same convention as
/// the Azure settings elsewhere in the solution: ship a clearly marked placeholder, detect it at
/// runtime, and degrade into an explanation rather than a broken link. Until it is replaced the
/// button still appears and is still clickable, but it opens an in-app note explaining that
/// donations are not live yet instead of launching the browser at a dead address.
/// </para>
/// </remarks>
public static class DonationOptions
{
    /// <summary>
    /// The placeholder value. <see cref="IsConfigured"/> compares against this exact string to
    /// decide whether a real donation page has been supplied yet.
    /// </summary>
    public const string Placeholder = "https://example.invalid/donate";

    /// <summary>
    /// Name of the optional plain text file, looked for beside the executable, whose first
    /// non-empty, non-comment line overrides <see cref="Url"/>.
    /// </summary>
    /// <remarks>
    /// This exists so the donation address can be corrected on a copy that has already been
    /// published, zipped and sent out, without recompiling and re-distributing a 230 MB folder.
    /// Lines beginning with <c>#</c> are treated as comments so the shipped file can document
    /// itself.
    /// </remarks>
    public const string OverrideFileName = "donate.url";

    /// <summary>
    /// The compiled in donation address.
    /// </summary>
    /// <remarks>
    /// This points at a page on a domain we control rather than straight at a payment processor.
    /// That indirection is deliberate: the processor behind the page can be changed - or several
    /// can be offered side by side - without this address ever changing, so no copy of this
    /// program already in someone's hands can be left pointing somewhere dead.
    /// </remarks>
    public const string DefaultUrl = "https://johnstraumann.com/Donate";

    private static string? _cached;

    /// <summary>
    /// The donation address actually in force: the contents of the override file if one is
    /// present and valid, otherwise <see cref="DefaultUrl"/>.
    /// </summary>
    public static string Url => _cached ??= ResolveUrl();

    /// <summary>
    /// True when a real donation page has been configured, i.e. the resolved address is an
    /// absolute http/https URL and is not still the placeholder.
    /// </summary>
    public static bool IsConfigured =>
        !string.Equals(Url, Placeholder, StringComparison.OrdinalIgnoreCase)
        && Uri.TryCreate(Url, UriKind.Absolute, out Uri? parsed)
        && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// Reads the override file if it exists, falling back to the compiled in default.
    /// </summary>
    /// <remarks>
    /// Every failure path here returns the default rather than throwing. A missing, locked,
    /// empty or malformed override file is not worth taking the application down for; the worst
    /// consequence of ignoring it is that the button explains donations are not configured,
    /// which is the same state the program shipped in.
    /// </remarks>
    private static string ResolveUrl()
    {
        try
        {
            string directory = AppContext.BaseDirectory;
            string path = Path.Combine(directory, OverrideFileName);

            if (!File.Exists(path))
            {
                return DefaultUrl;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();

                // Skip blank lines and comments, so the shipped template can explain itself.
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }

                // Only accept something that is genuinely a web address. This stops a typo in
                // the file from turning the button into a launcher for an arbitrary local
                // program, which is the one genuinely dangerous thing an unchecked "open this
                // string" could otherwise be talked into doing.
                if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? candidate)
                    && (candidate.Scheme == Uri.UriSchemeHttp || candidate.Scheme == Uri.UriSchemeHttps))
                {
                    return trimmed;
                }

                // First meaningful line was not a usable URL; do not keep scanning, because
                // silently using a later line would hide the mistake.
                break;
            }
        }
        catch (Exception)
        {
            // Fall through to the default - see the remarks above.
        }

        return DefaultUrl;
    }
}
