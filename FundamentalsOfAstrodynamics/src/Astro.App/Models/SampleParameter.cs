using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace Astro.App.Models;

/// <summary>
/// A single editable numeric input for a code sample.
/// </summary>
/// <remarks>
/// <para>
/// The value is held as a <see cref="string"/> rather than a <see cref="double"/> on purpose.
/// Two-way binding an <c>Entry</c> straight to a double is a well-known source of misery in
/// MAUI: the moment the user deletes the last character, or types "-" or "1.", the text is
/// not a valid number and the binding either throws or silently reverts, yanking the cursor
/// around. Keeping the raw text and parsing only when the sample is actually run lets the
/// user edit freely and lets us produce a friendly error message instead of a crash.
/// </para>
/// <para>
/// It derives from <see cref="ObservableObject"/> (CommunityToolkit.Mvvm) so that
/// <see cref="Value"/> raises <c>INotifyPropertyChanged</c> for the UI.
/// </para>
/// </remarks>
public sealed partial class SampleParameter : ObservableObject
{
    /// <summary>
    /// Creates a parameter definition.
    /// </summary>
    /// <param name="key">The lookup key the sample's Run delegate uses to fetch this value.</param>
    /// <param name="label">The caption shown next to the input box.</param>
    /// <param name="defaultValue">The initial value, pre-loaded so every sample runs meaningfully out of the box.</param>
    /// <param name="units">Physical units, shown to the right of the box (may be empty for dimensionless values).</param>
    /// <param name="help">A one-line explanation shown beneath the box.</param>
    public SampleParameter(string key, string label, double defaultValue, string units = "", string help = "")
    {
        Key = key;
        Label = label;
        Units = units;
        Help = help;
        DefaultValue = defaultValue;

        // "R" round-trips the double exactly; InvariantCulture keeps the decimal separator a
        // point regardless of the machine's regional settings.
        _value = defaultValue.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>The dictionary key by which the sample retrieves this parameter.</summary>
    public string Key { get; }

    /// <summary>The human-readable caption.</summary>
    public string Label { get; }

    /// <summary>The units string displayed beside the entry box.</summary>
    public string Units { get; }

    /// <summary>Explanatory help text displayed beneath the entry box.</summary>
    public string Help { get; }

    /// <summary>The value the parameter starts with, used by the "Reset" command.</summary>
    public double DefaultValue { get; }

    /// <summary>True when this parameter carries a units suffix worth displaying.</summary>
    public bool HasUnits => !string.IsNullOrWhiteSpace(Units);

    /// <summary>True when this parameter has help text worth displaying.</summary>
    public bool HasHelp => !string.IsNullOrWhiteSpace(Help);

    /// <summary>The raw text currently in the entry box.</summary>
    [ObservableProperty]
    private string _value;

    /// <summary>
    /// Parses the current text as a finite double.
    /// </summary>
    /// <exception cref="FormatException">
    /// Thrown with a message naming the offending field, so the UI can tell the user exactly
    /// which box is wrong rather than just "input string was not in a correct format".
    /// </exception>
    /// <remarks>
    /// The finiteness check is not redundant. Since .NET Core 3.0 <c>double.TryParse</c>
    /// SUCCEEDS on a value too large to represent, returning positive or negative infinity
    /// instead of failing - so typing <c>1e400</c> into a box would otherwise sail past this
    /// method and through any guard that only tests for a positive value, since infinity is
    /// greater than zero. The result is a report full of Infinity and NaN rather than an error
    /// message. Rejecting it here fixes it once for every parameter of every sample.
    /// </remarks>
    public double ToDouble()
    {
        if (double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            if (double.IsNaN(parsed) || double.IsInfinity(parsed))
            {
                throw new FormatException(
                    $"'{Label}' is too large to represent as a number (you entered \"{Value}\"). " +
                    "Values must be finite; the largest usable magnitude is about 1.8e308.");
            }

            return parsed;
        }

        throw new FormatException($"'{Label}' is not a valid number (you entered \"{Value}\").");
    }

    /// <summary>Restores the parameter to the value it was constructed with.</summary>
    public void Reset() => Value = DefaultValue.ToString("R", CultureInfo.InvariantCulture);
}
