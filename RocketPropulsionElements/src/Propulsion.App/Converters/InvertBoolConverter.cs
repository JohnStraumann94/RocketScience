using System.Globalization;

namespace Propulsion.App.Converters;

/// <summary>
/// Returns the logical negation of a boolean binding.
/// </summary>
/// <remarks>
/// Needed because the view-model exposes a single <c>HasSelection</c> flag, and the page has to
/// show one element when it is true (the detail pane) and a different element when it is false
/// (the "pick a sample" prompt). Rather than duplicating state on the view-model with a second
/// <c>HasNoSelection</c> property that could drift out of sync, the view inverts the one
/// authoritative flag at the point of use.
/// </remarks>
public sealed class InvertBoolConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}
