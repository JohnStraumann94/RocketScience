using Propulsion.App.ViewModels;

namespace Propulsion.App.Views;

/// <summary>
/// Code-behind for <c>MainPage.xaml</c>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately almost empty. Every behaviour of the page - which samples exist, which one is
/// selected, parsing the inputs, running the calculation and formatting the result - lives in
/// <see cref="MainViewModel"/>. The only job of the page class is to load the XAML and attach
/// the view-model that the DI container handed it.
/// </para>
/// <para>
/// Keeping it this thin is what allows the whole application's logic to be exercised without a
/// UI: a test can construct <see cref="MainViewModel"/>, select a sample, poke values into the
/// parameters and await <c>RunCommand</c>.
/// </para>
/// </remarks>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Creates the page.
    /// </summary>
    /// <param name="viewModel">
    /// The view-model, supplied by the DI container configured in <see cref="MauiProgram"/>.
    /// Constructor injection is used rather than creating the view-model inline so that the
    /// catalog and the Azure placeholder service are single, shared instances.
    /// </param>
    public MainPage(MainViewModel viewModel)
    {
        // Generated from the XAML; builds the visual tree.
        InitializeComponent();

        // Every {Binding ...} in the XAML resolves against this object.
        BindingContext = viewModel;
    }
}
