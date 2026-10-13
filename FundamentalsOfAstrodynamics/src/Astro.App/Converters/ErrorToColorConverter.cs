using System.Globalization;

namespace Astro.App.Converters;

/// <summary>
/// Converts the view-model's <c>HasError</c> flag into a colour for the output pane.
/// </summary>
/// <remarks>
/// <para>
/// A converter is used rather than exposing a <c>Color</c> property on the view-model because
/// colour is a presentation concern: the view-model should not know that failure is red. This
/// also keeps the view-model free of any dependency on the MAUI graphics types, which is what
/// makes it unit-testable in isolation.
/// </para>
/// <para>
/// The <c>ConverterParameter</c> selects which pair of colours to use, so a single converter
/// instance serves both the border stroke and the text.
/// </para>
/// </remarks>
public sealed class ErrorToColorConverter : IValueConverter
{
    /// <summary>Colour of the output border when the last run failed.</summary>
    private static readonly Color ErrorStroke = Color.FromArgb("#C64545");

    /// <summary>Colour of the output border when the last run succeeded.</summary>
    private static readonly Color NormalStroke = Color.FromArgb("#3A3F4B");

    /// <summary>Text colour used for an error report.</summary>
    private static readonly Color ErrorText = Color.FromArgb("#FF8080");

    /// <summary>Text colour used for a normal result.</summary>
    private static readonly Color NormalText = Color.FromArgb("#D7DCE5");

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasError = value is true;
        bool wantText = parameter as string == "Text";

        return wantText
            ? (hasError ? ErrorText : NormalText)
            : (hasError ? ErrorStroke : NormalStroke);
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// Always thrown: this converter is display-only and the binding is one-way.
    /// </exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("ErrorToColorConverter is one-way only.");
}
