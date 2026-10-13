using Propulsion.App.Models;

namespace Propulsion.App.Models;

/// <summary>
/// The complete table of contents: every runnable code sample in the application, grouped by
/// the chapter of Sutton's <i>Rocket Propulsion Elements</i> that it comes from.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the only file you need to touch to add, remove or reorder a sample.</b> The UI is
/// entirely data-driven: <c>MainPage.xaml</c> renders whatever <see cref="CodeSample"/> objects
/// this class hands it, so there is no per-sample view, view-model or XAML to write.
/// </para>
/// <para>
/// The catalogue is split across several <c>partial</c> files — one per chapter grouping — purely
/// so that no single file becomes unreadable:
/// </para>
/// <list type="bullet">
///   <item><description><c>SampleCatalog.cs</c> — this file: <see cref="Build"/> and shared formatting helpers.</description></item>
///   <item><description><c>SampleCatalog.Definitions.cs</c> — Chapter 2, definitions and performance parameters.</description></item>
///   <item><description><c>SampleCatalog.Nozzles.cs</c> — Chapter 3, nozzle theory and thermodynamic relations.</description></item>
///   <item><description><c>SampleCatalog.Flight.cs</c> — Chapter 4, flight performance and staging.</description></item>
///   <item><description><c>SampleCatalog.Propellants.cs</c> — Chapters 5, 7 and 12, thermochemistry and propellants.</description></item>
///   <item><description><c>SampleCatalog.LiquidEngines.cs</c> — Chapters 6, 8, 9 and 10, feed systems, injectors and cooling.</description></item>
///   <item><description><c>SampleCatalog.SolidMotors.cs</c> — Chapters 11 and 12, solid propellant motors.</description></item>
///   <item><description><c>SampleCatalog.Electric.cs</c> — Chapters 17 and 19, electric propulsion.</description></item>
/// </list>
/// <para>
/// Every sample delegates its actual arithmetic to <c>Propulsion.Core</c>, which is a plain
/// class library with no UI dependencies and is covered by the xUnit suite in
/// <c>tests\Propulsion.Core.Tests</c>. Nothing in this file computes physics itself; it only
/// gathers inputs, calls the library and formats the answer. That separation is what makes the
/// numbers on screen trustworthy — they are the same numbers the tests assert against.
/// </para>
/// </remarks>
public static partial class SampleCatalog
{
    /// <summary>
    /// Builds the full grouped table of contents in reading order.
    /// </summary>
    /// <returns>
    /// One <see cref="ChapterGroup"/> per chapter grouping, each containing its samples in the
    /// order they should appear in the left-hand navigation pane.
    /// </returns>
    public static IReadOnlyList<ChapterGroup> Build() =>
    [
        new ChapterGroup("Chapter 2", "Definitions and Fundamentals",
        [
            ThrustAndSpecificImpulse(),
            TotalImpulseAndMassFractions()
        ]),

        new ChapterGroup("Chapter 3", "Nozzle Theory and Thermodynamic Relations",
        [
            IdealExhaustVelocityExplorer(),
            CharacteristicVelocityAndThrustCoefficient(),
            FullEngineAnalysis(),
            AltitudeSweep(),
            NozzleSizingAndGeometry(),
            NozzleLossBudget()
        ]),

        new ChapterGroup("Chapter 4", "Flight Performance",
        [
            RocketEquationExplorer(),
            PayloadFractionAndTheSingleStageLimit(),
            MultiStageVehicleAnalysis(),
            OptimumStageCountSweep(),
            VerticalAscentSimulation(),
            GravityTurnAscentTrajectory()
        ]),

        new ChapterGroup("Chapters 5, 7 and 12", "Thermochemistry and Propellants",
        [
            ThermochemistryAndStagnationRelations(),
            PropellantComparisonTable(),
            TankVolumeAndMixtureRatio()
        ]),

        new ChapterGroup("Chapters 6, 8, 9 and 10", "Liquid Propellant Engines",
        [
            InjectorSizing(),
            FeedSystemPressureBudget(),
            TurbopumpSizing(),
            RegenerativeCoolingBalance()
        ]),

        new ChapterGroup("Chapters 11 and 12", "Solid Propellant Motors",
        [
            SolidMotorEquilibriumPressure(),
            BatesGrainThrustCurve()
        ]),

        new ChapterGroup("Chapters 17 and 19", "Electric Propulsion",
        [
            ElectricThrusterPowerBalance(),
            ElectricMissionOptimisation()
        ])
    ];

    // ====================================================================================
    // SHARED FORMATTING HELPERS
    // ------------------------------------------------------------------------------------
    // The output pane is a fixed-pitch text box, so every report is built from aligned
    // label/value lines. These helpers keep that alignment consistent across all samples
    // rather than scattering magic column widths through two dozen string builders.
    // ====================================================================================

    /// <summary>The column width used for the label part of every report line.</summary>
    private const int LabelWidth = 38;

    /// <summary>
    /// Formats one aligned "label .... value unit" line for the output pane.
    /// </summary>
    /// <param name="label">The quantity name.</param>
    /// <param name="value">The already-formatted value.</param>
    /// <param name="units">Optional units string appended after the value.</param>
    private static string Line(string label, string value, string units = "") =>
        $"  {label.PadRight(LabelWidth)}{value}{(units.Length > 0 ? " " + units : "")}";

    /// <summary>
    /// Formats one aligned line from a numeric value using a general-purpose format string.
    /// </summary>
    /// <param name="label">The quantity name.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="units">Optional units string.</param>
    /// <param name="format">A .NET numeric format string; defaults to four significant decimals.</param>
    private static string Line(string label, double value, string units = "", string format = "N4") =>
        Line(label, value.ToString(format), units);

    /// <summary>
    /// Produces a section heading with an underline, used to break long reports into blocks.
    /// </summary>
    /// <param name="text">The heading text.</param>
    private static string Heading(string text) =>
        $"{text}{Environment.NewLine}  {new string('-', Math.Max(text.Length, 40))}";
}
