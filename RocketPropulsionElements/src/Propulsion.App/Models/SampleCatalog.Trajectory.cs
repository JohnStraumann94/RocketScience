using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapter 4, continued: the two-dimensional gravity-turn ascent, which is the sample that
/// produces a genuine flight path through space rather than a performance number.
/// </summary>
public static partial class SampleCatalog
{
    /// <summary>
    /// Integrates a real two-dimensional powered ascent and animates the resulting flight path.
    /// </summary>
    private static CodeSample GravityTurnAscentTrajectory() => new()
    {
        Id = "ch4-gravity-turn",
        Chapter = "Chapter 4",
        Title = "Gravity Turn Ascent Trajectory",
        Summary = "The real two-dimensional flight path of a launch vehicle, animated.",

        Theory =
            """
            Every other ascent calculation in this book answers "how much velocity?". This one
            answers "what shape is the flight?" - and the answer is one of the more elegant
            results in flight mechanics.

            THE PROBLEM. A launch vehicle cannot fly straight up. Orbit is not about altitude,
            it is about horizontal speed: at 200 km you need roughly 7.8 km/s SIDEWAYS, and a
            vehicle that flies vertically arrives with all its velocity pointing exactly the
            wrong way. But nor can it simply steer, because pulling a turn with aerodynamic
            lift while passing through max-Q would apply bending moments no light-weight
            airframe could survive.

            THE GRAVITY TURN. The resolution is to let gravity do the steering. A few seconds
            after lift-off, once clear of the tower and still slow, the vehicle performs a small
            PITCH-OVER: it tips one to five degrees away from vertical. From that instant it
            holds ZERO ANGLE OF ATTACK - it points exactly along its own velocity vector - and
            simply flies. Because the velocity is now slightly off vertical, a component of
            gravity acts perpendicular to the flight path and rotates it. The turn continues,
            smoothly and by itself, until the vehicle is flying nearly horizontally at burnout.

            No lift. No side loads. No control authority spent on steering. The whole ascent is
            set up by one small nudge in the first minute.

            THE EQUATIONS. With gamma the flight path angle above the local horizontal:

                dv/dt     = (T - D)/m - g*sin(gamma)
                dgamma/dt = -( g/v - v/(R+h) ) * cos(gamma)
                dh/dt     =  v*sin(gamma)
                dx/dt     =  v*cos(gamma) * R/(R+h)

            Look at the second equation. The v/(R+h) term is centrifugal relief from flying
            around a curved planet. When v^2/(R+h) equals g the whole bracket vanishes, gamma
            stops changing, and the vehicle is in orbit. The steering equation contains the
            definition of orbital velocity as a special case.

            WHAT TO EXPERIMENT WITH. The pitch-over angle is the single most sensitive input.
            Too small and the vehicle never turns over, arriving at burnout still climbing
            steeply with far too little horizontal speed. Too large and it pitches over
            aggressively, builds up dynamic pressure low in the atmosphere and can fly itself
            back into the ground. Somewhere between those failures is a narrow band that works
            - and finding it by hand is exactly how ascent trajectories were designed before
            optimisation software.

            Press "Visualize Trajectory" to watch the path being flown.
            """,

        CodeSnippet =
            """
            // Fourth-order Runge-Kutta integration of the two-dimensional, zero-lift
            // equations of motion, with an exponential atmosphere and inverse-square gravity.
            GravityTurnResult ascent = GravityTurn.Ascend(
                initialMass:            m0,
                propellantMass:         mp,
                vacuumThrust:           vacuumThrust,
                specificImpulseSeconds: isp,
                exitArea:               exitArea,
                exitPressure:           exitPressure,
                dragCoefficient:        cd,
                referenceArea:          frontalArea,
                pitchOverTime:          pitchTime,      // when to tip over
                pitchOverAngleDegrees:  pitchAngle,     // how far to tip
                coastSeconds:           coast,          // ballistic arc after burnout
                timeStep:               dt);

            // Every sample carries a full state: where it is, how fast, and which way it points.
            foreach (GravityTurnSample s in ascent.Samples)
            {
                double downrangeKm = s.Downrange / 1000.0;
                double altitudeKm  = s.Altitude  / 1000.0;
                double gamma       = s.FlightPathAngle;   // degrees above local horizontal
            }
            """,

        Parameters =
        [
            new SampleParameter("m0", "Initial mass", 50000.0, "kg", "Gross lift-off mass."),
            new SampleParameter("mp", "Propellant mass", 42000.0, "kg", "Usable propellant."),
            new SampleParameter("thrust", "Vacuum thrust", 900.0, "kN", "Needs T/W > 1 to leave the pad."),
            new SampleParameter("isp", "Vacuum specific impulse", 310.0, "s", "Engine performance."),
            new SampleParameter("aexit", "Nozzle exit area", 0.8, "m^2", "Sets the back-pressure loss."),
            new SampleParameter("pexit", "Nozzle exit pressure", 0.5, "bar", "From the area ratio."),
            new SampleParameter("cd", "Drag coefficient", 0.3, "", "0.2-0.5 for a slender launcher."),
            new SampleParameter("aref", "Frontal reference area", 7.0, "m^2", "Cross-sectional area."),
            new SampleParameter("tpitch", "Pitch-over time", 12.0, "s", "How long to fly vertically first."),
            new SampleParameter("apitch", "Pitch-over angle", 3.5, "deg", "THE critical input. Try 1 to 8."),
            new SampleParameter("coast", "Coast after burnout", 200.0, "s", "Ballistic arc to draw after the burn."),
            new SampleParameter("dt", "Integration time step", 0.1, "s", "0.05 to 0.2 is sensible.")
        ],

        Run = inputs =>
        {
            GravityTurnResult ascent = Integrate(inputs);

            StringBuilder report = new();

            report.AppendLine(Heading("VEHICLE AND STEERING"));
            report.AppendLine(Line("Gross lift-off mass", inputs["m0"], "kg", "N1"));
            report.AppendLine(Line("Propellant mass", inputs["mp"], "kg", "N1"));
            report.AppendLine(Line("Vacuum thrust", inputs["thrust"], "kN", "N2"));
            report.AppendLine(Line("Pitch-over at", inputs["tpitch"], "s", "N2"));
            report.AppendLine(Line("Pitch-over angle", inputs["apitch"], "deg", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("FLIGHT PATH"));
            report.AppendLine("   t(s)  Downrng(km)   Alt(km)   Vel(m/s)  Gamma(deg)   q(kPa)");
            report.AppendLine("  " + new string('-', 66));

            // About 18 evenly spaced rows: enough to see the turn develop, few enough to read.
            int count = ascent.Samples.Count;
            int stride = Math.Max(1, count / 18);
            for (int i = 0; i < count; i += stride)
            {
                GravityTurnSample s = ascent.Samples[i];
                report.AppendLine(string.Format(
                    "  {0,5:F1}  {1,10:F3}  {2,8:F3}  {3,9:F1}  {4,10:F2}  {5,7:F2}{6}",
                    s.Time,
                    s.Downrange / 1000.0,
                    s.Altitude / 1000.0,
                    s.Velocity,
                    s.FlightPathAngle,
                    s.DynamicPressure / 1000.0,
                    s.Powered ? "" : "  (coast)"));
            }
            report.AppendLine();

            report.AppendLine(Heading("BURNOUT STATE"));
            report.AppendLine(Line("Burnout time", ascent.BurnoutTime, "s", "N2"));
            report.AppendLine(Line("Burnout altitude", ascent.BurnoutAltitude / 1000.0, "km", "N3"));
            report.AppendLine(Line("Burnout downrange", ascent.BurnoutDownrange / 1000.0, "km", "N3"));
            report.AppendLine(Line("Burnout speed", ascent.BurnoutVelocity, "m/s", "N2"));
            report.AppendLine(Line("Burnout flight path angle", ascent.BurnoutFlightPathAngle, "deg", "N2"));

            // Decomposing the burnout velocity is the clearest way to show whether the turn
            // actually did its job: only the horizontal component counts towards orbit.
            double gammaRad = ascent.BurnoutFlightPathAngle * Constants.DegToRad;
            double horizontal = ascent.BurnoutVelocity * Math.Cos(gammaRad);
            double vertical = ascent.BurnoutVelocity * Math.Sin(gammaRad);

            report.AppendLine(Line("  Horizontal component", horizontal, "m/s", "N2"));
            report.AppendLine(Line("  Vertical component", vertical, "m/s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("LOSS ACCOUNTING"));
            report.AppendLine(Line("Ideal delta-v (rocket equation)", ascent.IdealDeltaV, "m/s", "N2"));
            report.AppendLine(Line("Gravity loss", ascent.GravityLoss, "m/s", "N2"));
            report.AppendLine(Line("Drag loss", ascent.DragLoss, "m/s", "N2"));
            report.AppendLine(Line("Speed actually achieved", ascent.BurnoutVelocity, "m/s", "N2"));
            report.AppendLine(Line("Efficiency",
                100.0 * ascent.BurnoutVelocity / ascent.IdealDeltaV, "%", "N2"));
            report.AppendLine();
            report.AppendLine(Line("Vertical-flight gravity loss (g * tb)",
                Constants.StandardGravity * ascent.BurnoutTime, "m/s", "N2"));
            report.AppendLine(Line("Saved by turning over",
                Constants.StandardGravity * ascent.BurnoutTime - ascent.GravityLoss, "m/s", "N2"));
            report.AppendLine("  The gravity turn's entire purpose, in one number: the vehicle");
            report.AppendLine("  spends less of its thrust holding itself up and more of it going");
            report.AppendLine("  sideways, which is the direction that actually buys orbit.");
            report.AppendLine();

            report.AppendLine(Heading("MAX-Q AND APOGEE"));
            report.AppendLine(Line("Max dynamic pressure", ascent.MaxDynamicPressure / 1000.0, "kPa", "N3"));
            report.AppendLine(Line("Time of max-Q", ascent.MaxDynamicPressureTime, "s", "N2"));
            report.AppendLine(Line("Apogee altitude", ascent.ApogeeAltitude / 1000.0, "km", "N3"));
            report.AppendLine(Line("Time of apogee", ascent.ApogeeTime, "s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("ORBITAL CONTEXT"));
            double circular = FlightPerformance.CircularOrbitVelocity(ascent.BurnoutAltitude);
            report.AppendLine(Line("Circular speed at burnout altitude", circular, "m/s", "N2"));
            report.AppendLine(Line("Horizontal speed achieved", horizontal, "m/s", "N2"));
            report.AppendLine(Line("Shortfall", circular - horizontal, "m/s", "N2"));
            report.AppendLine();
            report.AppendLine(horizontal >= circular
                ? "  This single stage reached orbital horizontal speed. Check the structural"
                : "  A second stage would supply the shortfall. A single stage that reached");
            report.AppendLine(horizontal >= circular
                ? "  coefficient - real hardware rarely permits this mass ratio."
                : "  orbit is possible on paper and brutally marginal in practice.");
            report.AppendLine();
            report.AppendLine("  Press \"Visualize Trajectory\" to watch this flight path being flown.");

            return report.ToString();
        },

        // =============================================================================
        // TRAJECTORY VISUALISATION
        // =============================================================================
        //
        // This is the sample the visualisation feature exists for. The integration produces a
        // curve through real physical space - downrange distance against altitude - so the
        // animation is a faithful side-on view of the ascent, with the vehicle drawn at its
        // computed attitude and the plume shown only while the engine is actually burning.
        BuildTrajectory = inputs =>
        {
            GravityTurnResult ascent = Integrate(inputs);

            List<TrajectoryPoint> points = new(ascent.Samples.Count);

            foreach (GravityTurnSample s in ascent.Samples)
            {
                points.Add(new TrajectoryPoint(
                    Time: s.Time,
                    X: s.Downrange / 1000.0,
                    Y: s.Altitude / 1000.0,

                    // The flight path angle IS the vehicle attitude here, because a gravity
                    // turn is flown at zero angle of attack by definition. That equality is
                    // the whole idea of the manoeuvre, and it is pleasing that it falls
                    // straight out as the rendering parameter.
                    Heading: s.FlightPathAngle,

                    Powered: s.Powered,
                    Channels:
                    [
                        s.Altitude / 1000.0,
                        s.Downrange / 1000.0,
                        s.Velocity,
                        s.FlightPathAngle,
                        s.Mass,
                        s.Acceleration / Constants.StandardGravity,
                        s.DynamicPressure / 1000.0
                    ]));
            }

            List<TrajectoryMarker> markers =
            [
                new TrajectoryMarker("Lift-off", 0, "#7BD88F")
            ];

            // The pitch-over is the manoeuvre that makes the whole trajectory work, so it is
            // worth calling out explicitly on the plot.
            double pitchTime = inputs["tpitch"];
            int pitchIndex = 0;
            for (int i = 0; i < ascent.Samples.Count; i++)
            {
                if (ascent.Samples[i].Time <= pitchTime)
                {
                    pitchIndex = i;
                }
            }

            markers.Add(new TrajectoryMarker("Pitch-over", pitchIndex, "#B39DDB"));
            markers.Add(new TrajectoryMarker("Max-Q", ascent.MaxDynamicPressureIndex, "#FFB347"));
            markers.Add(new TrajectoryMarker("Burnout", ascent.BurnoutIndex, "#FF5C5C"));

            // Only label apogee when it is a distinct event; for a steep flight that is still
            // climbing at the end of the coast, apogee IS the last point and the label would
            // just collide with the end of the path.
            if (ascent.ApogeeIndex > ascent.BurnoutIndex &&
                ascent.ApogeeIndex < ascent.Samples.Count - 2)
            {
                markers.Add(new TrajectoryMarker("Apogee", ascent.ApogeeIndex, "#4FC3F7"));
            }

            return new Trajectory
            {
                Title = "Gravity Turn Ascent",
                Subtitle =
                    $"{inputs["m0"]:N0} kg vehicle, {inputs["thrust"]:N0} kN thrust, " +
                    $"Isp {inputs["isp"]:N0} s, pitch-over {inputs["apitch"]:F1} deg " +
                    $"at T+{inputs["tpitch"]:F0} s",
                XAxisLabel = "Downrange distance (km)",
                YAxisLabel = "Altitude (km)",
                Points = points,
                Markers = markers,

                // Deliberately NOT equal aspect. A launch profile covers hundreds of
                // kilometres downrange against tens of kilometres of altitude; drawn to scale
                // it would be an invisible sliver along the bottom of the window. The axes are
                // labelled with their units so the distortion cannot mislead - and this is
                // exactly how every real ascent profile in the literature is plotted.
                EqualAspect = false,

                PlaybackSeconds = 14.0,
                Channels =
                [
                    new TrajectoryChannel("Altitude", "km", "F2"),
                    new TrajectoryChannel("Downrange", "km", "F2"),
                    new TrajectoryChannel("Speed", "m/s", "F1"),
                    new TrajectoryChannel("Path angle", "deg", "F2"),
                    new TrajectoryChannel("Mass", "kg", "F0"),
                    new TrajectoryChannel("Accel", "g", "F2"),
                    new TrajectoryChannel("Dyn press", "kPa", "F2")
                ],
                Notes =
                    "Orange path = engine burning; blue path = ballistic coast. The vertical and " +
                    "horizontal axes use DIFFERENT scales, as every published ascent profile does. " +
                    "Model: point mass in a plane, zero lift and zero angle of attack, RK4 " +
                    "integration, exponential atmosphere, inverse-square gravity, constant drag " +
                    "coefficient, constant vacuum thrust, no staging, no Earth rotation and no " +
                    "steering beyond the single pitch-over kick."
            };
        }
    };

    /// <summary>
    /// Reads the sample's parameters and runs the two-dimensional ascent integration.
    /// </summary>
    /// <param name="inputs">The user's current parameter values.</param>
    /// <returns>The integrated trajectory and its summary statistics.</returns>
    /// <remarks>
    /// Shared by <c>Run</c> and <c>BuildTrajectory</c> so that the numbers in the text report
    /// and the curve in the animation are guaranteed to come from the same computation. If the
    /// two called the integrator separately with subtly different argument order, they could
    /// silently disagree - and a picture that disagrees with the numbers beside it is worse
    /// than no picture at all.
    /// </remarks>
    private static GravityTurnResult Integrate(SampleInputs inputs) =>
        GravityTurn.Ascend(
            initialMass: inputs["m0"],
            propellantMass: inputs["mp"],
            vacuumThrust: inputs["thrust"] * 1000.0,
            specificImpulseSeconds: inputs["isp"],
            exitArea: inputs["aexit"],
            exitPressure: Constants.BarToPa(inputs["pexit"]),
            dragCoefficient: inputs["cd"],
            referenceArea: inputs["aref"],
            pitchOverTime: inputs["tpitch"],
            pitchOverAngleDegrees: inputs["apitch"],
            coastSeconds: inputs["coast"],
            timeStep: Math.Clamp(inputs["dt"], 0.02, 1.0));
}
