using Astro.Core;

namespace Astro.App.Models;

/// <summary>
/// Trajectory builders: the code that turns the orbital mechanics of a sample into an
/// animatable path through space.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the propulsion book, where only a couple of samples integrate equations of motion,
/// almost everything here IS a trajectory - that is what astrodynamics is about. These
/// builders cover the four whose shape is most worth seeing move: a ballistic missile's
/// free-flight arc, an eccentric orbit obeying Kepler's second law, a Hohmann transfer, and
/// an interplanetary cruise.
/// </para>
/// <para>
/// <b>A note on sampling.</b> Every builder samples UNIFORMLY IN TIME, never uniformly in
/// true anomaly. That distinction is the entire visual point of Kepler's second law: sampled
/// in time, the spacecraft visibly races through periapsis and crawls through apoapsis. Had
/// the sampling been uniform in angle the animation would show a body moving at constant
/// speed around its orbit, which is exactly the misconception the picture exists to correct.
/// </para>
/// </remarks>
public static partial class SampleCatalog
{
    /// <summary>Number of points generated for each animated path.</summary>
    /// <remarks>
    /// 480 points is roughly eight seconds at sixty frames a second, so the animation advances
    /// about one sample per frame - smooth, without generating data nobody will ever see.
    /// </remarks>
    private const int TrajectoryResolution = 480;

    /// <summary>
    /// A raw trajectory sample before the vehicle heading has been worked out.
    /// </summary>
    /// <param name="Time">Mission elapsed time, s.</param>
    /// <param name="X">World X coordinate in display units.</param>
    /// <param name="Y">World Y coordinate in display units.</param>
    /// <param name="Powered">Whether the vehicle is thrusting.</param>
    /// <param name="Channels">Telemetry values for the heads-up display.</param>
    private readonly record struct RawPoint(
        double Time,
        double X,
        double Y,
        bool Powered,
        double[] Channels);

    /// <summary>
    /// Converts raw points into <see cref="TrajectoryPoint"/>s, deriving the heading of the
    /// vehicle glyph from the direction of travel.
    /// </summary>
    /// <param name="raw">The points in time order.</param>
    /// <returns>Points ready to hand to the renderer.</returns>
    /// <remarks>
    /// The heading is taken as a central finite difference of position, which for an
    /// equal-aspect plot is exactly the direction of the velocity vector. Doing it here, once,
    /// means no individual builder has to work out which way its spacecraft is pointing - and
    /// it is automatically right for orbits, transfers and ballistic arcs alike.
    /// </remarks>
    private static List<TrajectoryPoint> WithHeadings(IReadOnlyList<RawPoint> raw)
    {
        List<TrajectoryPoint> points = new(raw.Count);

        for (int i = 0; i < raw.Count; i++)
        {
            // Central difference in the interior, one-sided at the two ends.
            int previous = Math.Max(0, i - 1);
            int next = Math.Min(raw.Count - 1, i + 1);

            double dx = raw[next].X - raw[previous].X;
            double dy = raw[next].Y - raw[previous].Y;

            // A stationary point (or a single-point path) has no defined direction; pointing
            // along +X is as good a convention as any and never actually occurs in practice.
            double heading = dx == 0.0 && dy == 0.0
                ? 0.0
                : Math.Atan2(dy, dx) * Constants.RadToDeg;

            points.Add(new TrajectoryPoint(
                raw[i].Time,
                raw[i].X,
                raw[i].Y,
                heading,
                raw[i].Powered,
                raw[i].Channels));
        }

        return points;
    }

    /// <summary>
    /// Creates a ring: a circle drawn as an outline only, used for reference orbits.
    /// </summary>
    /// <param name="radius">Ring radius in world units.</param>
    /// <param name="strokeHex">Outline colour.</param>
    /// <returns>A body that renders as an unfilled circle.</returns>
    /// <remarks>
    /// A fully transparent fill turns the body renderer into a ring renderer, which is why
    /// reference orbits need no dedicated type. The name is left empty so no label is drawn at
    /// the ring's centre, where it would collide with the central body.
    /// </remarks>
    private static TrajectoryBody Ring(double radius, string strokeHex) =>
        new(string.Empty, 0.0, 0.0, radius, "#00000000", strokeHex);

    // ====================================================================================
    // Chapter 6 - Ballistic missile free-flight trajectory
    // ====================================================================================

    /// <summary>
    /// Builds the free-flight arc of a ballistic missile, from burnout to impact.
    /// </summary>
    /// <param name="inputs">Burnout altitude, speed and flight-path angle.</param>
    /// <returns>The animatable trajectory.</returns>
    /// <remarks>
    /// <para>
    /// This is the sample where drawing the path really pays off, because the free-flight
    /// portion of a ballistic missile trajectory is not a parabola - it is a piece of an
    /// ELLIPSE with the centre of the Earth at one focus, and it only looks like the parabola
    /// of school physics when the range is short enough that the planet's curvature and the
    /// variation of gravity with altitude can both be ignored. Drawn against a round Earth at
    /// true scale, the difference is impossible to miss.
    /// </para>
    /// <para>
    /// The trajectory is symmetric about apogee, which is what lets the geometry be set up so
    /// simply: if the free-flight range angle is Psi, then burnout sits at true anomaly
    /// 180 - Psi/2 and impact at 180 + Psi/2.
    /// </para>
    /// </remarks>
    private static Trajectory BuildBallisticTrajectory(SampleInputs inputs)
    {
        double burnoutRadius = Constants.EarthRadiusKm + inputs["altitude"];
        double burnoutSpeed = inputs["speed"];
        double phi = Angles.ToRadians(inputs["phi"]);

        BallisticTrajectory result = BallisticMissile.Compute(burnoutRadius, burnoutSpeed, phi);

        double eccentricity = result.Eccentricity;
        double semiMajorAxis = result.SemiMajorAxisKm;
        double semiLatusRectum = semiMajorAxis * (1.0 - eccentricity * eccentricity);

        // Symmetry about apogee fixes the true anomalies of the two ends of the arc.
        double halfRange = result.FreeFlightRangeAngleRad / 2.0;
        double burnoutTrueAnomaly = Math.PI - halfRange;

        // Time since periapsis at burnout, so the sweep can be driven by the clock.
        double meanMotion = TwoBody.MeanMotion(semiMajorAxis);
        double burnoutTimeSincePeriapsis =
            KeplerSolver.TrueToMeanAnomaly(burnoutTrueAnomaly, eccentricity) / meanMotion;

        double timeOfFlight = result.TimeOfFlightSec;

        List<RawPoint> raw = new(TrajectoryResolution);

        for (int i = 0; i < TrajectoryResolution; i++)
        {
            double time = timeOfFlight * i / (TrajectoryResolution - 1.0);

            double trueAnomaly = KeplerSolver.TrueAnomalyAfterTime(
                burnoutTimeSincePeriapsis + time, eccentricity, semiMajorAxis);

            double radius = TwoBody.RadiusAtTrueAnomaly(
                semiLatusRectum, eccentricity, trueAnomaly);

            // Rotate so that apogee (true anomaly 180) points straight up the screen. The
            // launch site then sits down and to the left, the target down and to the right,
            // which is how every textbook draws it.
            double theta = trueAnomaly - Math.PI;

            double speed = TwoBody.VisViva(radius, semiMajorAxis);
            double flightPathAngle = TwoBody.FlightPathAngle(eccentricity, trueAnomaly);

            raw.Add(new RawPoint(
                Time: time,
                X: radius * Math.Sin(theta),
                Y: radius * Math.Cos(theta),
                Powered: false,
                Channels:
                [
                    radius - Constants.EarthRadiusKm,
                    speed,
                    Angles.ToDegrees(flightPathAngle),
                    Constants.EarthRadiusKm * (theta + halfRange),
                    time / 60.0
                ]));
        }

        List<TrajectoryPoint> points = WithHeadings(raw);

        // Apogee is the midpoint in time, because the arc is symmetric about it.
        int apogeeIndex = TrajectoryResolution / 2;

        return new Trajectory
        {
            Title = "Ballistic Missile Free-Flight Trajectory",
            Subtitle =
                $"Burnout at {inputs["altitude"]:N0} km, {burnoutSpeed:F2} km/s, " +
                $"flight-path angle {inputs["phi"]:F1} deg  |  " +
                $"surface range {result.SurfaceRangeKm:N0} km, " +
                $"time of flight {timeOfFlight / 60.0:F1} min",
            XAxisLabel = "Distance from Earth's centre, X (km)",
            YAxisLabel = "Distance from Earth's centre, Y (km)",
            Points = points,

            // Non-negotiable here. The whole lesson is that the arc is an ellipse around a
            // round planet; unequal axes would distort both the planet and the conic and
            // destroy the point of the picture.
            EqualAspect = true,

            PlaybackSeconds = 13.0,
            Bodies =
            [
                new TrajectoryBody("Earth", 0.0, 0.0, Constants.EarthRadiusKm, "#14324B", "#4C87B8")
            ],
            Channels =
            [
                new TrajectoryChannel("Altitude", "km", "N0"),
                new TrajectoryChannel("Speed", "km/s", "F3"),
                new TrajectoryChannel("Path angle", "deg", "F2"),
                new TrajectoryChannel("Ground range", "km", "N0"),
                new TrajectoryChannel("Elapsed", "min", "F2")
            ],
            Markers =
            [
                new TrajectoryMarker("Burnout", 0, "#FF8A3D"),
                new TrajectoryMarker("Apogee", apogeeIndex, "#4FC3F7"),
                new TrajectoryMarker("Impact", points.Count - 1, "#FF5C5C")
            ],
            StateLabel = "Phase",
            PoweredText = "POWERED",
            CoastingText = "FREE FLIGHT",
            Notes =
                "Only the FREE-FLIGHT phase is drawn: the powered boost from the launch site up " +
                "to the burnout point is not modelled here. The path is a genuine Keplerian " +
                "ellipse with the Earth's centre at one focus, sampled uniformly in TIME - which " +
                "is why the vehicle visibly slows near apogee. Model: spherical non-rotating " +
                "Earth, point mass, no atmosphere and no drag, so the re-entry end of the arc is " +
                "idealised."
        };
    }

    // ====================================================================================
    // Chapter 4 - Kepler's equation, seen as motion
    // ====================================================================================

    /// <summary>
    /// Builds one complete revolution of an eccentric orbit, sampled uniformly in time.
    /// </summary>
    /// <param name="inputs">Eccentricity, semi-major axis and the mean anomaly of interest.</param>
    /// <returns>The animatable trajectory.</returns>
    /// <remarks>
    /// The Kepler's-equation sample computes a single answer: the true anomaly corresponding to
    /// one mean anomaly. Animating the whole revolution turns that isolated number into the
    /// thing it is actually part of - and makes Kepler's second law directly visible, because
    /// equal times sweep equal areas and the spacecraft therefore visibly hurries through
    /// periapsis and loiters at apoapsis.
    /// </remarks>
    private static Trajectory BuildKeplerOrbitTrajectory(SampleInputs inputs)
    {
        double eccentricity = inputs["ecc"];
        double semiMajorAxis = inputs["sma"];
        double meanAnomaly = Angles.ToRadians(inputs["M"]);

        if (eccentricity >= 1.0)
        {
            throw new ArgumentException(
                "Only closed (elliptical) orbits can be animated as a full revolution. " +
                "Eccentricity must be below 1.",
                "ecc");
        }

        double semiLatusRectum = semiMajorAxis * (1.0 - eccentricity * eccentricity);
        double period = TwoBody.Period(semiMajorAxis);

        List<RawPoint> raw = new(TrajectoryResolution);

        for (int i = 0; i < TrajectoryResolution; i++)
        {
            double time = period * i / (TrajectoryResolution - 1.0);

            double trueAnomaly = KeplerSolver.TrueAnomalyAfterTime(
                time, eccentricity, semiMajorAxis);

            double radius = TwoBody.RadiusAtTrueAnomaly(
                semiLatusRectum, eccentricity, trueAnomaly);

            raw.Add(new RawPoint(
                Time: time,
                X: radius * Math.Cos(trueAnomaly),
                Y: radius * Math.Sin(trueAnomaly),
                Powered: false,
                Channels:
                [
                    radius,
                    radius - Constants.EarthRadiusKm,
                    TwoBody.VisViva(radius, semiMajorAxis),
                    Angles.ToDegrees(Angles.Normalize(trueAnomaly)),
                    Angles.ToDegrees(Angles.Normalize(Constants.TwoPi * time / period)),
                    time / 3600.0
                ]));
        }

        List<TrajectoryPoint> points = WithHeadings(raw);

        // The point the Kepler sample actually solves for: mean anomaly maps linearly to time,
        // so its index is simply its fraction of the period.
        int solvedIndex = (int)Math.Round(
            Angles.Normalize(meanAnomaly) / Constants.TwoPi * (TrajectoryResolution - 1));
        solvedIndex = Math.Clamp(solvedIndex, 0, points.Count - 1);

        double periapsis = TwoBody.PeriapsisRadius(semiMajorAxis, eccentricity);
        double apoapsis = TwoBody.ApoapsisRadius(semiMajorAxis, eccentricity);

        List<TrajectoryBody> bodies =
        [
            new TrajectoryBody("Earth", 0.0, 0.0, Constants.EarthRadiusKm, "#14324B", "#4C87B8")
        ];

        return new Trajectory
        {
            Title = "Kepler's Equation as Motion",
            Subtitle =
                $"a = {semiMajorAxis:N0} km, e = {eccentricity:F3}  |  " +
                $"periapsis {periapsis:N0} km, apoapsis {apoapsis:N0} km, " +
                $"period {period / 3600.0:F2} h",
            XAxisLabel = "X (km, periapsis direction)",
            YAxisLabel = "Y (km)",
            Points = points,
            EqualAspect = true,
            PlaybackSeconds = 15.0,
            Bodies = bodies,
            Channels =
            [
                new TrajectoryChannel("Radius", "km", "N0"),
                new TrajectoryChannel("Altitude", "km", "N0"),
                new TrajectoryChannel("Speed", "km/s", "F3"),
                new TrajectoryChannel("True anom", "deg", "F1"),
                new TrajectoryChannel("Mean anom", "deg", "F1"),
                new TrajectoryChannel("Elapsed", "h", "F2")
            ],
            Markers =
            [
                new TrajectoryMarker("Periapsis", 0, "#FF8A3D"),
                new TrajectoryMarker("Apoapsis", TrajectoryResolution / 2, "#4FC3F7"),
                new TrajectoryMarker($"M = {inputs["M"]:F0} deg", solvedIndex, "#B39DDB")
            ],
            StateLabel = "Motion",
            PoweredText = "THRUSTING",
            CoastingText = "COASTING",
            Notes =
                "Sampled uniformly in TIME, not in angle - which is precisely why the spacecraft " +
                "races through periapsis and crawls through apoapsis. That is Kepler's second law " +
                "made visible, and it is the reason Kepler's equation has to be solved " +
                "iteratively in the first place. If the drawn orbit passes through the Earth, the " +
                "periapsis radius you entered is genuinely below the surface: a = " +
                "26600 km with e = 0.8 gives a periapsis of 5320 km, which is 1058 km underground. " +
                "That is a property of the numbers, not of the drawing."
        };
    }
}
