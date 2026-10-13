using System.Globalization;

namespace Astro.App.Converters;

/// <summary>
/// Turns a boolean into one of two colours, chosen by the converter parameter.
/// </summary>
/// <remarks>
/// <para>
/// Written for the C# / Python language toggle, where two buttons sit side by side and
/// exactly one of them must look "pressed". Each button binds its background to a boolean
/// that is true when that language is showing, and this converter maps true to the accent
/// colour and false to the inactive chrome colour.
/// </para>
/// <para>
/// The alternative - a pair of <c>DataTrigger</c>s per button in XAML - needs eight lines of
/// markup on each button and has to be repeated for the text colour too. One converter with
/// a parameter is both shorter and easier to keep consistent.
/// </para>
/// <para>
/// The parameter selects which pair of colours to use:
/// </para>
/// <list type="bullet">
///   <item><description><c>null</c> or anything else - button background: accent when on, dark chrome when off.</description></item>
///   <item><description><c>"Text"</c> - button foreground: near black when on (for contrast against the accent), normal body text when off.</description></item>
/// </list>
/// </remarks>
public sealed class BoolToToggleColorConverter : IValueConverter
{
    /// <summary>Background of the selected toggle: the app's accent blue.</summary>
    private static readonly Color ActiveBackground = Color.FromArgb("#4EA8FF");

    /// <summary>Background of the unselected toggle: the same neutral used by secondary buttons.</summary>
    private static readonly Color InactiveBackground = Color.FromArgb("#2E3440");

    /// <summary>Foreground of the selected toggle, dark enough to read against the accent.</summary>
    private static readonly Color ActiveText = Color.FromArgb("#0C1017");

    /// <summary>Foreground of the unselected toggle: ordinary body text.</summary>
    private static readonly Color InactiveText = Color.FromArgb("#E7ECF4");

    /// <summary>Maps the boolean to a colour.</summary>
    /// <param name="value">Expected to be a <see cref="bool"/>; anything else is treated as false.</param>
    /// <param name="targetType">Ignored.</param>
    /// <param name="parameter">Pass <c>"Text"</c> for the foreground pair, anything else for the background pair.</param>
    /// <param name="culture">Ignored.</param>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is true;
        bool wantsText = parameter is string text &&
                         text.Equals("Text", StringComparison.OrdinalIgnoreCase);

        if (wantsText)
        {
            return isActive ? ActiveText : InactiveText;
        }

        return isActive ? ActiveBackground : InactiveBackground;
    }

    /// <summary>Not supported: a colour cannot be turned back into a selection state.</summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException(
            $"{nameof(BoolToToggleColorConverter)} is one-way: it is bound to a read-only visual property.");
}
