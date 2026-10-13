namespace Propulsion.App.Models;

/// <summary>
/// The parsed parameter values handed to a sample when the user presses <b>Run</b>.
/// </summary>
/// <remarks>
/// A thin wrapper over a dictionary. Its only real job is to turn a missing key into a
/// meaningful exception, and to offer a degrees-to-radians accessor so that sample code can
/// present friendly degree inputs to the user while the physics layer keeps working in
/// radians throughout.
/// </remarks>
public sealed class SampleInputs
{
    private readonly IReadOnlyDictionary<string, double> _values;

    /// <summary>Creates an input set from already-parsed values.</summary>
    public SampleInputs(IReadOnlyDictionary<string, double> values) => _values = values;

    /// <summary>Fetches a raw parameter value by key.</summary>
    /// <exception cref="KeyNotFoundException">Thrown when the sample asks for a parameter it never declared.</exception>
    public double this[string key] => _values.TryGetValue(key, out double value)
        ? value
        : throw new KeyNotFoundException(
            $"The sample requested a parameter named '{key}' that was not declared in its parameter list.");

    /// <summary>Fetches a parameter that the user entered in degrees and returns it in radians.</summary>
    public double Radians(string key) => this[key] * Propulsion.Core.Constants.DegToRad;

    /// <summary>Fetches a parameter and rounds it to the nearest integer.</summary>
    public int Integer(string key) => (int)Math.Round(this[key]);

    /// <summary>
    /// Fetches a parameter and interprets it as a boolean flag: any non-zero value is true.
    /// Used for the handful of samples that need a yes/no switch.
    /// </summary>
    public bool Flag(string key) => Math.Abs(this[key]) > 0.5;
}
