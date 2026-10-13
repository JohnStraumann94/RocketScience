using Astro.Core;
using System.Text;

namespace Astro.App.Models;

/// <summary>
/// The complete table of contents: every runnable sample, grouped by the chapter of
/// <i>Fundamentals of Astrodynamics</i> that it illustrates.
/// </summary>
/// <remarks>
/// <para>
/// This is the only file that needs to change to add, remove or re-order samples. Each entry
/// declares its inputs, its explanatory text, the equivalent C# source, and a delegate that
/// calls into <c>Astro.Core</c> and formats the answer.
/// </para>
/// <para>
/// <b>Azure integration note.</b> Several samples would benefit from cloud services — storing
/// a user's saved scenarios, fetching live TLE catalogues, or offloading a porkchop-plot grid
/// search to Azure Functions. Those integration points are marked with
/// <c>AZURE PLACEHOLDER</c> comments in <c>Services/AzurePlaceholderService.cs</c> and are
/// intentionally not wired up yet.
/// </para>
/// </remarks>
public static partial class SampleCatalog
{
    /// <summary>Convenience factory that keeps the catalogue entries readable.</summary>
    private static SampleParameter P(string key, string label, double value, string units = "", string help = "")
        => new(key, label, value, units, help);

    /// <summary>
    /// Builds the full chapter-grouped catalogue. Called once at start-up by the view-model.
    /// </summary>
    public static IReadOnlyList<ChapterGroup> Build() =>
    [
        new ChapterGroup("Chapter 1", "Two-Body Orbital Mechanics",
        [
            OrbitEnergyExplorer(),
            SpeedVersusAltitudeTable(),
            ConicGeometryExplorer()
        ]),

        new ChapterGroup("Chapter 2", "Orbit Determination from Observation",
        [
            StateVectorToElements(),
            ElementsToStateVector()
        ]),

        new ChapterGroup("Chapter 3", "Basic Orbital Manoeuvres",
        [
            HohmannTransferSample(),
            BiEllipticComparison(),
            PlaneChangeSample(),
            PhasingSample()
        ]),

        new ChapterGroup("Chapter 4", "Position and Velocity as a Function of Time",
        [
            KeplerEquationSample(),
            UniversalPropagationSample()
        ]),

        new ChapterGroup("Chapter 5", "Orbit Determination from Two Positions",
        [
            LambertSample()
        ]),

        new ChapterGroup("Chapter 6", "Ballistic Missile Trajectories",
        [
            BallisticTrajectorySample()
        ]),

        new ChapterGroup("Chapter 7", "Lunar Trajectories",
        [
            LunarPatchedConicSample()
        ]),

        new ChapterGroup("Chapter 8", "Interplanetary Trajectories",
        [
            InterplanetaryHohmannSample(),
            GravityAssistSample()
        ]),

        new ChapterGroup("Chapter 9", "Perturbations",
        [
            J2PerturbationSample(),
            SunSynchronousDesignSample()
        ])
    ];

    // =====================================================================
    // CHAPTER 1 — TWO-BODY ORBITAL MECHANICS
    // =====================================================================

    private static CodeSample OrbitEnergyExplorer() => new()
    {
        Id = "ch1-energy",
        Chapter = "Chapter 1",
        Title = "Vis-viva and orbital energy",
        Summary = "Speed, energy, period and escape speed for any point on any orbit.",
        Theory =
            """
            The two-body equation of motion has two conserved quantities: specific angular
            momentum h = r x v, and specific mechanical energy eps = v^2/2 - mu/r.

            Because energy is constant, the speed at any radius depends only on that radius
            and on the size of the orbit. Rearranging gives the vis-viva equation:

                v = sqrt( mu ( 2/r - 1/a ) )

            The sign of eps classifies the orbit outright: negative means bound (circle or
            ellipse), zero means parabolic escape, positive means hyperbolic.

            Try setting the radius equal to the semi-major axis - you get a circular orbit.
            Then push the semi-major axis very large and watch the speed climb towards the
            escape value.
            """,
        Parameters =
        [
            P("altitude", "Altitude above the surface", 400.0, "km", "Radius is measured from the centre, so r = 6378.137 + altitude."),
            P("sma", "Semi-major axis a", 6778.137, "km", "Set equal to the radius for a circular orbit; larger for an ellipse.")
        ],
        CodeSnippet =
            """
            double r = Constants.EarthRadiusKm + altitudeKm;

            double speed   = TwoBody.VisViva(r, semiMajorAxisKm);
            double energy  = TwoBody.SpecificEnergy(r, speed);
            double period  = TwoBody.Period(semiMajorAxisKm);
            double escape  = TwoBody.EscapeSpeed(r);
            """,
        Run = inputs =>
        {
            double radius = Constants.EarthRadiusKm + inputs["altitude"];
            double sma = inputs["sma"];

            double speed = TwoBody.VisViva(radius, sma);
            double energy = TwoBody.SpecificEnergy(radius, speed);
            double circular = TwoBody.CircularSpeed(radius);
            double escape = TwoBody.EscapeSpeed(radius);

            StringBuilder report = new();
            report.AppendLine($"Radius r                  = {radius,12:F3} km");
            report.AppendLine($"Semi-major axis a         = {sma,12:F3} km");
            report.AppendLine();
            report.AppendLine($"Speed (vis-viva)          = {speed,12:F6} km/s");
            report.AppendLine($"Circular speed at r       = {circular,12:F6} km/s");
            report.AppendLine($"Escape speed at r         = {escape,12:F6} km/s");
            report.AppendLine();
            report.AppendLine($"Specific energy eps       = {energy,12:F6} km^2/s^2");

            if (energy < 0.0)
            {
                double period = TwoBody.Period(sma);
                report.AppendLine($"Orbit is BOUND (elliptical or circular).");
                report.AppendLine($"Period T                  = {period,12:F2} s  ({period / 60.0:F2} min)");
                report.AppendLine($"Mean motion n             = {TwoBody.MeanMotion(sma),12:E6} rad/s");
            }
            else if (Math.Abs(energy) < 1e-9)
            {
                report.AppendLine("Orbit is PARABOLIC - exactly at escape speed.");
            }
            else
            {
                report.AppendLine("Orbit is HYPERBOLIC - the vehicle escapes with energy to spare.");
                report.AppendLine($"v_infinity                = {Math.Sqrt(2.0 * energy),12:F6} km/s");
            }

            return report.ToString();
        }
    };

    private static CodeSample SpeedVersusAltitudeTable() => new()
    {
        Id = "ch1-speed-table",
        Chapter = "Chapter 1",
        Title = "Circular speed and period vs altitude",
        Summary = "A sweep from LEO to beyond geostationary, showing how slowly high orbits move.",
        Theory =
            """
            For a circular orbit the vis-viva equation collapses to v = sqrt(mu/r), and
            Kepler's third law gives T = 2 pi sqrt(a^3/mu).

            Both are worth internalising as a table rather than a formula. Notice that:

              * speed falls only as the square root of radius, so a satellite six times
                further out is still moving 40% as fast;
              * period grows as radius to the 3/2 power, so it climbs far more dramatically;
              * geostationary altitude (35,786 km) is exactly where the period reaches one
                sidereal day, 23h 56m 04s - not 24 hours.
            """,
        Parameters =
        [
            P("min", "Lowest altitude", 200.0, "km"),
            P("max", "Highest altitude", 40000.0, "km"),
            P("steps", "Number of rows", 12.0, "", "How many altitudes to tabulate between the limits.")
        ],
        CodeSnippet =
            """
            for (int i = 0; i < steps; i++)
            {
                double altitude = min + (max - min) * i / (steps - 1);
                double r        = Constants.EarthRadiusKm + altitude;

                double v = TwoBody.CircularSpeed(r);
                double T = TwoBody.Period(r);
            }
            """,
        Run = inputs =>
        {
            double min = inputs["min"];
            double max = inputs["max"];
            int steps = Math.Clamp(inputs.Integer("steps"), 2, 60);

            StringBuilder report = new();
            report.AppendLine("  Altitude       Radius        Speed        Period       Period");
            report.AppendLine("      (km)         (km)       (km/s)         (min)      (hours)");
            report.AppendLine("  ---------------------------------------------------------------");

            for (int i = 0; i < steps; i++)
            {
                double altitude = min + (max - min) * i / (steps - 1);
                double radius = Constants.EarthRadiusKm + altitude;

                double speed = TwoBody.CircularSpeed(radius);
                double period = TwoBody.Period(radius);

                report.AppendLine(
                    $"  {altitude,9:F0}  {radius,11:F1}  {speed,11:F4}  {period / 60.0,11:F2}  {period / 3600.0,11:F3}");
            }

            report.AppendLine();
            report.AppendLine($"  For reference, geostationary radius = 42164.17 km " +
                              $"(altitude {42164.17 - Constants.EarthRadiusKm:F0} km),");
            report.AppendLine($"  where the period equals one sidereal day = {Constants.SiderealDaySeconds / 3600.0:F4} hours.");

            return report.ToString();
        }
    };

    private static CodeSample ConicGeometryExplorer() => new()
    {
        Id = "ch1-conic",
        Chapter = "Chapter 1",
        Title = "The orbit equation and conic geometry",
        Summary = "Sweep true anomaly around an orbit and watch radius, speed and flight-path angle change.",
        Theory =
            """
            All four conic sections are contained in one polar equation:

                r = p / (1 + e cos(nu))

            where p = a(1 - e^2) is the semi-latus rectum and nu is the true anomaly measured
            from periapsis. Eccentricity alone decides the shape: e = 0 circle, 0 < e < 1
            ellipse, e = 1 parabola, e > 1 hyperbola.

            The flight-path angle gamma - the angle between the velocity vector and the local
            horizontal - follows from tan(gamma) = e sin(nu) / (1 + e cos(nu)). It is zero at
            both apsides, which is precisely why tangential burns there are so efficient.
            """,
        Parameters =
        [
            P("sma", "Semi-major axis a", 26600.0, "km"),
            P("ecc", "Eccentricity e", 0.74, "", "A Molniya-like orbit by default. Try 0 for a circle."),
            P("steps", "Number of rows", 13.0, "", "True anomaly is swept from 0 to 360 degrees.")
        ],
        CodeSnippet =
            """
            double p = sma * (1 - ecc * ecc);

            double r     = TwoBody.RadiusAtTrueAnomaly(p, ecc, nuRad);
            double v     = TwoBody.VisViva(r, sma);
            double gamma = TwoBody.FlightPathAngle(ecc, nuRad);
            """,
        Run = inputs =>
        {
            double sma = inputs["sma"];
            double ecc = inputs["ecc"];
            int steps = Math.Clamp(inputs.Integer("steps"), 2, 73);

            double p = sma * (1.0 - ecc * ecc);

            StringBuilder report = new();
            report.AppendLine($"Semi-latus rectum p = {p:F3} km");
            report.AppendLine($"Periapsis radius    = {TwoBody.PeriapsisRadius(sma, ecc):F3} km " +
                              $"(altitude {TwoBody.PeriapsisRadius(sma, ecc) - Constants.EarthRadiusKm:F1} km)");

            if (ecc < 1.0)
            {
                report.AppendLine($"Apoapsis radius     = {TwoBody.ApoapsisRadius(sma, ecc):F3} km " +
                                  $"(altitude {TwoBody.ApoapsisRadius(sma, ecc) - Constants.EarthRadiusKm:F1} km)");
                report.AppendLine($"Period              = {TwoBody.Period(sma) / 3600.0:F4} hours");
            }

            report.AppendLine();
            report.AppendLine("       nu        Radius        Speed   Flight-path");
            report.AppendLine("    (deg)          (km)       (km/s)     angle (deg)");
            report.AppendLine("    ------------------------------------------------");

            for (int i = 0; i < steps; i++)
            {
                double nuDeg = 360.0 * i / (steps - 1);
                double nu = Angles.ToRadians(nuDeg);

                double radius = TwoBody.RadiusAtTrueAnomaly(p, ecc, nu);
                double speed = TwoBody.VisViva(radius, sma);
                double gamma = Angles.ToDegrees(TwoBody.FlightPathAngle(ecc, nu));

                report.AppendLine($"    {nuDeg,6:F1}  {radius,12:F1}  {speed,11:F5}  {gamma,14:F4}");
            }

            return report.ToString();
        }
    };

    // =====================================================================
    // CHAPTER 2 — ORBIT DETERMINATION FROM OBSERVATION
    // =====================================================================

    private static CodeSample StateVectorToElements() => new()
    {
        Id = "ch2-rv2coe",
        Chapter = "Chapter 2",
        Title = "State vector to orbital elements (rv2coe)",
        Summary = "Turn a position and velocity measurement into the six classical elements.",
        Theory =
            """
            Radar and GPS produce position and velocity. Catalogues, mission plans and human
            intuition all work in orbital elements. This conversion is the bridge.

            The algorithm builds three vectors and then reads the angles off them:

              h = r x v                                    (angular momentum, fixes the plane)
              n = K x h                                    (node vector, points at the ascending node)
              e = [(v^2 - mu/r) r - (r.v) v] / mu          (eccentricity vector, points at periapsis)

            Each angle then comes from an arccosine plus a quadrant test, because arccos alone
            cannot distinguish 60 degrees from 300 degrees.

            The defaults below are the standard worked example from Vallado (Example 2-5), so
            you can check the output against the textbook: a = 36127.343 km, e = 0.832853,
            i = 87.870 deg, RAAN = 227.898 deg, argp = 53.38 deg, nu = 92.335 deg.
            """,
        Parameters =
        [
            P("rx", "Position r_x", 6524.834, "km"),
            P("ry", "Position r_y", 6862.875, "km"),
            P("rz", "Position r_z", 6448.296, "km"),
            P("vx", "Velocity v_x", 4.901327, "km/s"),
            P("vy", "Velocity v_y", 5.533756, "km/s"),
            P("vz", "Velocity v_z", -1.976341, "km/s")
        ],
        CodeSnippet =
            """
            StateVector state = new(
                new Vector3(rx, ry, rz),
                new Vector3(vx, vy, vz));

            OrbitalElements elements = OrbitConversions.ToOrbitalElements(state);
            """,
        Run = inputs =>
        {
            StateVector state = new(
                new Vector3(inputs["rx"], inputs["ry"], inputs["rz"]),
                new Vector3(inputs["vx"], inputs["vy"], inputs["vz"]));

            OrbitalElements elements = OrbitConversions.ToOrbitalElements(state);

            StringBuilder report = new();
            report.AppendLine("INPUT STATE");
            report.AppendLine(state.ToString());
            report.AppendLine();
            report.AppendLine("CLASSICAL ORBITAL ELEMENTS");
            report.AppendLine(elements.ToString());

            if (elements.Eccentricity < 1.0)
            {
                report.AppendLine();
                report.AppendLine($"Period                    = {TwoBody.Period(elements.SemiMajorAxisKm) / 60.0,14:F3} min");
                report.AppendLine($"Perigee altitude          = {elements.PeriapsisRadiusKm - Constants.EarthRadiusKm,14:F3} km");
                report.AppendLine($"Apogee altitude           = {elements.ApoapsisRadiusKm - Constants.EarthRadiusKm,14:F3} km");
            }

            // Round-trip check: converting straight back must reproduce the input exactly.
            StateVector recovered = OrbitConversions.ToStateVector(elements);
            double positionError = (recovered.PositionKm - state.PositionKm).Magnitude;
            double velocityError = (recovered.VelocityKmPerSec - state.VelocityKmPerSec).Magnitude;

            report.AppendLine();
            report.AppendLine("ROUND-TRIP VERIFICATION (elements converted back to a state vector)");
            report.AppendLine($"  position error            = {positionError:E3} km");
            report.AppendLine($"  velocity error            = {velocityError:E3} km/s");

            return report.ToString();
        }
    };

    private static CodeSample ElementsToStateVector() => new()
    {
        Id = "ch2-coe2rv",
        Chapter = "Chapter 2",
        Title = "Orbital elements to state vector (coe2rv)",
        Summary = "The inverse conversion, via the perifocal frame and a 3-1-3 rotation.",
        Theory =
            """
            Going the other way is a two-stage process.

            First, write the state in the perifocal (PQW) frame, where the P axis points at
            periapsis. In that frame the algebra is almost trivial:

                r_pqw = ( p / (1 + e cos nu) ) * [ cos nu, sin nu, 0 ]
                v_pqw = sqrt(mu/p)             * [ -sin nu, e + cos nu, 0 ]

            Second, rotate into the inertial frame with the classical 3-1-3 Euler sequence
            R3(-RAAN) R1(-i) R3(-argp). Those three rotations are exactly the three orienting
            elements, applied in the order that undoes how they were defined.
            """,
        Parameters =
        [
            P("sma", "Semi-major axis a", 36127.343, "km"),
            P("ecc", "Eccentricity e", 0.832853, ""),
            P("inc", "Inclination i", 87.870, "deg"),
            P("raan", "RAAN", 227.898, "deg"),
            P("argp", "Argument of periapsis", 53.38, "deg"),
            P("nu", "True anomaly", 92.335, "deg")
        ],
        CodeSnippet =
            """
            OrbitalElements elements = new(
                sma, ecc,
                Angles.ToRadians(incDeg),
                Angles.ToRadians(raanDeg),
                Angles.ToRadians(argpDeg),
                Angles.ToRadians(nuDeg));

            StateVector state = OrbitConversions.ToStateVector(elements);
            """,
        Run = inputs =>
        {
            OrbitalElements elements = new(
                inputs["sma"],
                inputs["ecc"],
                inputs.Radians("inc"),
                inputs.Radians("raan"),
                inputs.Radians("argp"),
                inputs.Radians("nu"));

            StateVector state = OrbitConversions.ToStateVector(elements);

            StringBuilder report = new();
            report.AppendLine("INPUT ELEMENTS");
            report.AppendLine(elements.ToString());
            report.AppendLine();
            report.AppendLine("RESULTING STATE VECTOR");
            report.AppendLine(state.ToString());
            report.AppendLine();

            // Show the derived quantities that make the answer easy to sanity-check.
            Vector3 h = Vector3.Cross(state.PositionKm, state.VelocityKmPerSec);
            report.AppendLine($"Angular momentum h        = {h.ToString("F4")}  |h| = {h.Magnitude:F4} km^2/s");
            report.AppendLine($"Flight-path angle         = {Angles.ToDegrees(TwoBody.FlightPathAngle(elements.Eccentricity, elements.TrueAnomalyRad)):F5} deg");
            report.AppendLine($"Specific energy           = {TwoBody.SpecificEnergy(state.RadiusKm, state.SpeedKmPerSec):F6} km^2/s^2");

            return report.ToString();
        }
    };

    // =====================================================================
    // CHAPTER 3 — BASIC ORBITAL MANOEUVRES
    // =====================================================================

    private static CodeSample HohmannTransferSample() => new()
    {
        Id = "ch3-hohmann",
        Chapter = "Chapter 3",
        Title = "Hohmann transfer",
        Summary = "The classic two-burn minimum-energy transfer between circular orbits.",
        Theory =
            """
            Walter Hohmann worked this out in 1925, decades before anything flew. Connect two
            circular orbits with an ellipse that is tangent to both: its periapsis sits on the
            inner circle and its apoapsis on the outer one, so a_transfer = (r1 + r2)/2.

            Both burns are purely tangential - no direction change at all - which is exactly
            why the manoeuvre is so efficient. Each burn is just the difference of two
            vis-viva evaluations, and the coast is half the transfer ellipse's period.

            The defaults transfer from a 300 km parking orbit to geostationary altitude, which
            is the single most-flown manoeuvre in the history of spaceflight.
            """,
        Parameters =
        [
            P("r1", "Initial circular radius r1", 6678.0, "km", "6678 km is roughly a 300 km altitude parking orbit."),
            P("r2", "Final circular radius r2", 42164.0, "km", "42164 km is geostationary. Make this smaller than r1 to lower an orbit.")
        ],
        CodeSnippet =
            """
            TransferResult result = Maneuvers.HohmannTransfer(r1Km, r2Km);

            double dv1   = result.BurnsKmPerSec[0];
            double dv2   = result.BurnsKmPerSec[1];
            double total = result.TotalDeltaVKmPerSec;
            double hours = result.TransferTimeHours;
            """,
        Run = inputs =>
        {
            double r1 = inputs["r1"];
            double r2 = inputs["r2"];

            TransferResult result = Maneuvers.HohmannTransfer(r1, r2);
            double aTransfer = (r1 + r2) / 2.0;

            StringBuilder report = new();
            report.AppendLine(result.Description);
            report.AppendLine();
            report.AppendLine($"Initial circular speed    = {TwoBody.CircularSpeed(r1),12:F6} km/s");
            report.AppendLine($"Final circular speed      = {TwoBody.CircularSpeed(r2),12:F6} km/s");
            report.AppendLine();
            report.AppendLine($"Burn 1 (at r1)            = {result.BurnsKmPerSec[0],12:F6} km/s  " +
                              $"({(result.BurnsKmPerSec[0] >= 0 ? "prograde" : "retrograde")})");
            report.AppendLine($"Burn 2 (at r2)            = {result.BurnsKmPerSec[1],12:F6} km/s  " +
                              $"({(result.BurnsKmPerSec[1] >= 0 ? "prograde" : "retrograde")})");
            report.AppendLine($"TOTAL delta-v             = {result.TotalDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine();
            report.AppendLine($"Transfer ellipse a        = {aTransfer,12:F3} km");
            report.AppendLine($"Transfer time             = {result.TransferTimeSec,12:F1} s");
            report.AppendLine($"                          = {result.TransferTimeHours,12:F4} hours");

            // The bi-elliptic comparison is the natural follow-up question.
            double ratio = r2 / r1;
            report.AppendLine();
            report.AppendLine($"Radius ratio r2/r1        = {ratio,12:F4}");
            report.AppendLine(ratio > Maneuvers.BiEllipticBreakEvenRatio
                ? $"  This exceeds the break-even ratio of {Maneuvers.BiEllipticBreakEvenRatio:F3},\n" +
                  "  so a three-burn bi-elliptic transfer may be cheaper. See the next sample."
                : $"  This is below the break-even ratio of {Maneuvers.BiEllipticBreakEvenRatio:F3},\n" +
                  "  so Hohmann is the optimal two-impulse transfer here.");

            return report.ToString();
        },

        // The subject of this sample IS a trajectory, so it can be animated:
        // the tangent transfer ellipse between two circular orbits.
        BuildTrajectory = BuildHohmannTrajectory
    };

    private static CodeSample BiEllipticComparison() => new()
    {
        Id = "ch3-bielliptic",
        Chapter = "Chapter 3",
        Title = "Bi-elliptic vs Hohmann",
        Summary = "When flying further actually costs less: the three-burn transfer.",
        Theory =
            """
            The counter-intuitive result of Chapter 3. Instead of transferring directly, fling
            the vehicle out to a very high intermediate apoapsis r_b, do the shape change out
            there where it is moving slowly and therefore cheaply, then drop down to the
            target radius.

            This is the Oberth effect running in reverse. Because a burn's effect on orbital
            energy is proportional to the speed at which it is performed, changing the orbit
            far away - where speed is low - costs very little delta-v.

            The break-even radius ratio is about 11.94. Below it, Hohmann always wins. Above
            about 15.58, bi-elliptic always wins given a large enough r_b. In between it
            depends on how far out you are willing to go, and the price is always time:
            bi-elliptic transfers can take many times longer.
            """,
        Parameters =
        [
            P("r1", "Initial circular radius r1", 7000.0, "km"),
            P("rb", "Intermediate apoapsis r_b", 210000.0, "km", "Must be larger than both r1 and r2. Push it higher and watch the cost fall."),
            P("r2", "Final circular radius r2", 140000.0, "km", "Default gives a ratio of 20, above the break-even point.")
        ],
        CodeSnippet =
            """
            TransferResult hohmann   = Maneuvers.HohmannTransfer(r1, r2);
            TransferResult biElliptic = Maneuvers.BiEllipticTransfer(r1, rb, r2);

            bool biEllipticWins =
                biElliptic.TotalDeltaVKmPerSec < hohmann.TotalDeltaVKmPerSec;
            """,
        Run = inputs =>
        {
            double r1 = inputs["r1"];
            double rb = inputs["rb"];
            double r2 = inputs["r2"];

            TransferResult hohmann = Maneuvers.HohmannTransfer(r1, r2);
            TransferResult biElliptic = Maneuvers.BiEllipticTransfer(r1, rb, r2);

            StringBuilder report = new();
            report.AppendLine($"Radius ratio r2/r1 = {r2 / r1:F4}   " +
                              $"(break-even is {Maneuvers.BiEllipticBreakEvenRatio:F3})");
            report.AppendLine();

            report.AppendLine("HOHMANN (2 burns)");
            report.AppendLine($"  burn 1                  = {hohmann.BurnsKmPerSec[0],12:F6} km/s");
            report.AppendLine($"  burn 2                  = {hohmann.BurnsKmPerSec[1],12:F6} km/s");
            report.AppendLine($"  TOTAL                   = {hohmann.TotalDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine($"  time                    = {hohmann.TransferTimeHours,12:F3} hours " +
                              $"({hohmann.TransferTimeHours / 24.0:F3} days)");
            report.AppendLine();

            report.AppendLine("BI-ELLIPTIC (3 burns)");
            report.AppendLine($"  burn 1 (at r1)          = {biElliptic.BurnsKmPerSec[0],12:F6} km/s");
            report.AppendLine($"  burn 2 (at rb)          = {biElliptic.BurnsKmPerSec[1],12:F6} km/s");
            report.AppendLine($"  burn 3 (at r2)          = {biElliptic.BurnsKmPerSec[2],12:F6} km/s");
            report.AppendLine($"  TOTAL                   = {biElliptic.TotalDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine($"  time                    = {biElliptic.TransferTimeHours,12:F3} hours " +
                              $"({biElliptic.TransferTimeHours / 24.0:F3} days)");
            report.AppendLine();

            double saving = hohmann.TotalDeltaVKmPerSec - biElliptic.TotalDeltaVKmPerSec;
            double timeCost = biElliptic.TransferTimeHours - hohmann.TransferTimeHours;

            report.AppendLine("VERDICT");
            if (saving > 0.0)
            {
                report.AppendLine($"  Bi-elliptic WINS by {saving:F6} km/s ({saving / hohmann.TotalDeltaVKmPerSec * 100.0:F2}% cheaper),");
                report.AppendLine($"  at a cost of {timeCost:F1} extra hours ({timeCost / 24.0:F2} days) of flight time.");
            }
            else
            {
                report.AppendLine($"  Hohmann WINS by {-saving:F6} km/s, and is {-timeCost / 24.0:F2} days faster as well.");
            }

            return report.ToString();
        }
    };

    private static CodeSample PlaneChangeSample() => new()
    {
        Id = "ch3-plane-change",
        Chapter = "Chapter 3",
        Title = "Plane changes, simple and combined",
        Summary = "Why changing inclination is the most expensive thing you can do in orbit.",
        Theory =
            """
            Rotating an orbital plane without changing the orbit's size requires

                dv = 2 v sin( di / 2 )

            because the before and after velocity vectors form an isosceles triangle. The
            speed v appears as a direct multiplier, and that is devastating: in low Earth
            orbit v is about 7.7 km/s, so even a 30-degree plane change costs roughly 4 km/s -
            more than the entire transfer to geostationary orbit.

            There are two escapes. First, do it where v is small: at apoapsis of a highly
            elliptical orbit. Second, fold the plane change into a burn you were going to make
            anyway, using the law of cosines:

                dv = sqrt( v_i^2 + v_f^2 - 2 v_i v_f cos(di) )

            This is exactly what a geostationary apogee kick motor does - it circularises the
            orbit and removes the launch site's inclination in a single burn.
            """,
        Parameters =
        [
            P("vi", "Speed before the burn v_i", 1.6079, "km/s", "Default is the apogee speed of a standard geostationary transfer orbit."),
            P("vf", "Speed after the burn v_f", 3.0747, "km/s", "Default is the geostationary circular speed."),
            P("di", "Plane change angle", 28.5, "deg", "28.5 deg is the latitude of Cape Canaveral, and so the minimum launch inclination."),
            P("vleo", "LEO speed for comparison", 7.7258, "km/s", "Used to show how much worse the same plane change would be in LEO.")
        ],
        CodeSnippet =
            """
            double combined = Maneuvers.CombinedManeuver(vi, vf, Angles.ToRadians(diDeg));

            double separate = Math.Abs(vf - vi)
                            + Maneuvers.SimplePlaneChange(vf, Angles.ToRadians(diDeg));
            """,
        Run = inputs =>
        {
            double vi = inputs["vi"];
            double vf = inputs["vf"];
            double di = inputs.Radians("di");
            double vleo = inputs["vleo"];

            double combined = Maneuvers.CombinedManeuver(vi, vf, di);
            double speedOnly = Math.Abs(vf - vi);
            double planeOnly = Maneuvers.SimplePlaneChange(vf, di);
            double separate = speedOnly + planeOnly;

            StringBuilder report = new();
            report.AppendLine($"Plane change angle        = {inputs["di"],12:F4} deg");
            report.AppendLine();
            report.AppendLine("SEPARATE BURNS");
            report.AppendLine($"  speed change only       = {speedOnly,12:F6} km/s");
            report.AppendLine($"  plane change only       = {planeOnly,12:F6} km/s  (at v = {vf:F4} km/s)");
            report.AppendLine($"  TOTAL                   = {separate,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("COMBINED SINGLE BURN");
            report.AppendLine($"  TOTAL                   = {combined,12:F6} km/s");
            report.AppendLine($"  saving                  = {separate - combined,12:F6} km/s " +
                              $"({(separate - combined) / separate * 100.0:F2}%)");
            report.AppendLine();
            report.AppendLine("WHERE YOU DO IT MATTERS ENORMOUSLY");
            report.AppendLine($"  same plane change in LEO (v = {vleo:F4} km/s)");
            report.AppendLine($"                            = {Maneuvers.SimplePlaneChange(vleo, di),12:F6} km/s");
            report.AppendLine($"  same plane change at apogee (v = {vi:F4} km/s)");
            report.AppendLine($"                            = {Maneuvers.SimplePlaneChange(vi, di),12:F6} km/s");
            report.AppendLine();
            report.AppendLine("  ... a factor of " +
                              $"{Maneuvers.SimplePlaneChange(vleo, di) / Maneuvers.SimplePlaneChange(vi, di):F2} " +
                              "difference for exactly the same geometric result.");

            return report.ToString();
        }
    };

    private static CodeSample PhasingSample() => new()
    {
        Id = "ch3-phasing",
        Chapter = "Chapter 3",
        Title = "Phasing manoeuvre (rendezvous)",
        Summary = "The great paradox: to catch up with something ahead of you, slow down.",
        Theory =
            """
            Suppose a target is 30 degrees ahead of you in the same circular orbit. Firing
            your engine forwards to chase it is exactly wrong: thrusting prograde raises your
            orbit, lengthens your period, and you fall further behind.

            The correct answer is to brake. That drops you into a smaller, faster-period
            orbit; after one lap you arrive back at the same point earlier than the target,
            having gained on it. A second burn, equal and opposite, restores the original
            orbit - now in the right place.

            This is the fundamental manoeuvre of every rendezvous ever flown, from Gemini VI
            to routine ISS resupply.
            """,
        Parameters =
        [
            P("radius", "Circular orbit radius", 6778.137, "km", "Roughly the ISS orbit (400 km altitude)."),
            P("phase", "Phase angle to make up", 30.0, "deg", "Positive means the target is ahead of you."),
            P("revs", "Revolutions to spread it over", 1.0, "", "More revolutions means a gentler, cheaper manoeuvre.")
        ],
        CodeSnippet =
            """
            TransferResult result = Maneuvers.PhasingManeuver(
                orbitRadiusKm: radiusKm,
                phaseAngleRad: Angles.ToRadians(phaseDeg),
                numberOfRevolutions: revs);
            """,
        Run = inputs =>
        {
            double radius = inputs["radius"];
            double phase = inputs.Radians("phase");
            int revs = Math.Max(1, inputs.Integer("revs"));

            TransferResult result = Maneuvers.PhasingManeuver(radius, phase, revs);

            StringBuilder report = new();
            report.AppendLine(result.Description);
            report.AppendLine();
            report.AppendLine($"Original circular period  = {TwoBody.Period(radius) / 60.0,12:F4} min");
            report.AppendLine($"Original circular speed   = {TwoBody.CircularSpeed(radius),12:F6} km/s");
            report.AppendLine();
            report.AppendLine($"Burn 1 (enter phasing)    = {result.BurnsKmPerSec[0],12:F6} km/s");
            report.AppendLine($"Burn 2 (restore orbit)    = {result.BurnsKmPerSec[1],12:F6} km/s");
            report.AppendLine($"TOTAL delta-v             = {result.TotalDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine();
            report.AppendLine($"Total manoeuvre time      = {result.TransferTimeHours,12:F4} hours");
            report.AppendLine();
            report.AppendLine(result.BurnsKmPerSec[0] < 0.0
                ? "The first burn is RETROGRADE: you slow down to catch up with a target ahead."
                : "The first burn is PROGRADE: you speed up to drop back towards a target behind.");

            return report.ToString();
        }
    };

    // =====================================================================
    // CHAPTER 4 — POSITION AND VELOCITY AS A FUNCTION OF TIME
    // =====================================================================

    private static CodeSample KeplerEquationSample() => new()
    {
        Id = "ch4-kepler",
        Chapter = "Chapter 4",
        Title = "Kepler's equation solver",
        Summary = "Solve M = E - e sin E by Newton's method and watch convergence degrade with eccentricity.",
        Theory =
            """
            The true anomaly does not advance uniformly - a satellite races through periapsis
            and crawls through apoapsis. Kepler introduced the mean anomaly M, which does
            advance uniformly, and tied it to the geometry through

                M = E - e sin(E)

            Going from E to M is trivial. Going from M to E is not: the equation is
            transcendental and has occupied numerical analysts since 1650. Newton's method
            converges quickly, but the derivative 1 - e cos(E) approaches zero near periapsis
            of a highly eccentric orbit, which is why the solver switches its starting guess
            to E = pi once e exceeds 0.8.

            Push the eccentricity towards 0.99 and watch the iteration count climb.
            """,
        Parameters =
        [
            P("M", "Mean anomaly M", 60.0, "deg"),
            P("ecc", "Eccentricity e", 0.8, "", "Must be below 1. Try 0.99 to stress the solver."),
            P("sma", "Semi-major axis a", 26600.0, "km", "Used only to convert the answer into a time since periapsis.")
        ],
        CodeSnippet =
            """
            var solution = KeplerSolver.SolveEccentricAnomaly(
                Angles.ToRadians(meanAnomalyDeg), eccentricity);

            double trueAnomaly = KeplerSolver.EccentricToTrueAnomaly(
                solution.AnomalyRad, eccentricity);
            """,
        Run = inputs =>
        {
            double m = inputs.Radians("M");
            double ecc = inputs["ecc"];
            double sma = inputs["sma"];

            var solution = KeplerSolver.SolveEccentricAnomaly(m, ecc);
            double nu = KeplerSolver.EccentricToTrueAnomaly(solution.AnomalyRad, ecc);

            double radius = sma * (1.0 - ecc * Math.Cos(solution.AnomalyRad));
            double timeSincePeriapsis = KeplerSolver.TimeSincePeriapsis(nu, ecc, sma);

            StringBuilder report = new();
            report.AppendLine($"Mean anomaly M            = {inputs["M"],12:F6} deg");
            report.AppendLine($"Eccentricity e            = {ecc,12:F6}");
            report.AppendLine();
            report.AppendLine("SOLUTION");
            report.AppendLine($"  Eccentric anomaly E     = {Angles.ToDegrees(solution.AnomalyRad),12:F6} deg");
            report.AppendLine($"  True anomaly nu         = {Angles.ToDegrees(nu),12:F6} deg");
            report.AppendLine();
            report.AppendLine("CONVERGENCE");
            report.AppendLine($"  Newton iterations       = {solution.Iterations,12}");
            report.AppendLine($"  Final residual          = {solution.FinalResidual,12:E4} rad");
            report.AppendLine($"  Starting guess used     = {(ecc < 0.8 ? "E0 = M (low eccentricity)" : "E0 = pi (high eccentricity)")}");
            report.AppendLine();
            report.AppendLine("DERIVED QUANTITIES");
            report.AppendLine($"  Radius r = a(1-e cos E) = {radius,12:F3} km");
            report.AppendLine($"  Time since periapsis    = {timeSincePeriapsis,12:F2} s  ({timeSincePeriapsis / 60.0:F3} min)");
            report.AppendLine($"  Orbital period          = {TwoBody.Period(sma),12:F2} s  ({TwoBody.Period(sma) / 60.0:F3} min)");
            report.AppendLine();
            report.AppendLine("VERIFICATION (substituting E back into Kepler's equation)");
            report.AppendLine($"  E - e sin(E)            = {Angles.ToDegrees(KeplerSolver.EccentricToMeanAnomaly(solution.AnomalyRad, ecc)),12:F9} deg");

            return report.ToString();
        },

        // The subject of this sample IS a trajectory, so it can be animated:
        // one full revolution, sampled uniformly in time.
        BuildTrajectory = BuildKeplerOrbitTrajectory
    };

    private static CodeSample UniversalPropagationSample() => new()
    {
        Id = "ch4-universal",
        Chapter = "Chapter 4",
        Title = "Universal variable propagation",
        Summary = "One equation that propagates any conic - circle, ellipse, parabola or hyperbola.",
        Theory =
            """
            The classical approach needs a different equation for each conic type, and all of
            them degrade badly near e = 1 - exactly where escape trajectories and transfer
            arcs live.

            Sundman's universal anomaly chi fixes this. By changing the independent variable
            via dt = (r / sqrt(mu)) dchi, a single equation covers every conic continuously:

                sqrt(mu) dt = chi^3 S(psi) + (r0.v0/sqrt(mu)) chi^2 C(psi) + r0 chi (1 - psi C(psi))

            where psi = chi^2/a and C and S are the Stumpff functions. Once chi is found, the
            new state comes from the Lagrange f and g coefficients.

            Try an initial speed of 12 km/s at 7000 km - that is hyperbolic, and the same code
            handles it without modification.
            """,
        Parameters =
        [
            P("rx", "Position r_x", -6045.0, "km"),
            P("ry", "Position r_y", -3490.0, "km"),
            P("rz", "Position r_z", 2500.0, "km"),
            P("vx", "Velocity v_x", -3.457, "km/s"),
            P("vy", "Velocity v_y", 6.618, "km/s"),
            P("vz", "Velocity v_z", 2.533, "km/s"),
            P("dt", "Propagation time", 3600.0, "s", "May be negative to propagate backwards in time.")
        ],
        CodeSnippet =
            """
            StateVector start = new(
                new Vector3(rx, ry, rz),
                new Vector3(vx, vy, vz));

            StateVector end = UniversalVariables.Propagate(start, deltaTimeSec);
            """,
        Run = inputs =>
        {
            StateVector start = new(
                new Vector3(inputs["rx"], inputs["ry"], inputs["rz"]),
                new Vector3(inputs["vx"], inputs["vy"], inputs["vz"]));

            double dt = inputs["dt"];
            StateVector end = UniversalVariables.Propagate(start, dt);

            OrbitalElements elements = OrbitConversions.ToOrbitalElements(start);

            StringBuilder report = new();
            report.AppendLine($"ORBIT TYPE: {elements.Type}   (e = {elements.Eccentricity:F6}, a = {elements.SemiMajorAxisKm:F3} km)");
            report.AppendLine();
            report.AppendLine("INITIAL STATE");
            report.AppendLine(start.ToString());
            report.AppendLine();
            report.AppendLine($"PROPAGATED BY {dt:F1} s ({dt / 60.0:F3} min)");
            report.AppendLine(end.ToString());
            report.AppendLine();

            // Energy must be conserved exactly under two-body motion; showing it proves the
            // propagator is not quietly drifting.
            double energyStart = TwoBody.SpecificEnergy(start.RadiusKm, start.SpeedKmPerSec);
            double energyEnd = TwoBody.SpecificEnergy(end.RadiusKm, end.SpeedKmPerSec);

            report.AppendLine("CONSERVATION CHECKS (these must not change under two-body motion)");
            report.AppendLine($"  energy before           = {energyStart,14:F9} km^2/s^2");
            report.AppendLine($"  energy after            = {energyEnd,14:F9} km^2/s^2");
            report.AppendLine($"  drift                   = {Math.Abs(energyEnd - energyStart),14:E4}");

            Vector3 hStart = Vector3.Cross(start.PositionKm, start.VelocityKmPerSec);
            Vector3 hEnd = Vector3.Cross(end.PositionKm, end.VelocityKmPerSec);
            report.AppendLine($"  |h| before              = {hStart.Magnitude,14:F9} km^2/s");
            report.AppendLine($"  |h| after               = {hEnd.Magnitude,14:F9} km^2/s");

            // Round-trip: propagating back must land exactly where we started.
            StateVector back = UniversalVariables.Propagate(end, -dt);
            report.AppendLine();
            report.AppendLine("REVERSIBILITY CHECK (propagate forward then back)");
            report.AppendLine($"  position error          = {(back.PositionKm - start.PositionKm).Magnitude,14:E4} km");

            return report.ToString();
        }
    };

    // =====================================================================
    // CHAPTER 5 — ORBIT DETERMINATION FROM TWO POSITIONS AND TIME
    // =====================================================================

    private static CodeSample LambertSample() => new()
    {
        Id = "ch5-lambert",
        Chapter = "Chapter 5",
        Title = "Lambert's problem",
        Summary = "Given two positions and a flight time, find the orbit that connects them.",
        Theory =
            """
            Lambert's problem is the targeting engine of spaceflight. Given a departure point,
            an arrival point, and how long you are willing to take, what velocity do you need?

            Lambert's theorem (1761) states that the transfer time depends only on the
            semi-major axis, the chord between the two points, and the sum r1 + r2 - and
            remarkably, on nothing else about the arc's shape.

            The solver below uses the universal-variable formulation with bisection on psi.
            Bisection is slower than Newton's method but cannot diverge, which matters because
            the time-of-flight function has an awkward derivative near the parabolic boundary.

            Every interplanetary porkchop plot is thousands of these solves on a grid of
            departure and arrival dates.

            Set "Long way" to 1 to sweep the reflex angle instead - occasionally cheaper, and
            sometimes the only option when the short way would fly through the Earth.
            """,
        Parameters =
        [
            P("r1x", "Departure r1_x", 5000.0, "km"),
            P("r1y", "Departure r1_y", 10000.0, "km"),
            P("r1z", "Departure r1_z", 2100.0, "km"),
            P("r2x", "Arrival r2_x", -14600.0, "km"),
            P("r2y", "Arrival r2_y", 2500.0, "km"),
            P("r2z", "Arrival r2_z", 7000.0, "km"),
            P("tof", "Time of flight", 3600.0, "s"),
            P("longway", "Long way? (0 = no, 1 = yes)", 0.0, "", "Sweeps more than 180 degrees around the Earth.")
        ],
        CodeSnippet =
            """
            LambertSolution solution = Lambert.Solve(
                position1Km: new Vector3(r1x, r1y, r1z),
                position2Km: new Vector3(r2x, r2y, r2z),
                timeOfFlightSec: tof,
                direction: longWay ? TransferDirection.LongWay
                                   : TransferDirection.ShortWay);
            """,
        Run = inputs =>
        {
            Vector3 r1 = new(inputs["r1x"], inputs["r1y"], inputs["r1z"]);
            Vector3 r2 = new(inputs["r2x"], inputs["r2y"], inputs["r2z"]);
            double tof = inputs["tof"];
            bool longWay = inputs.Flag("longway");

            LambertSolution solution = Lambert.Solve(
                r1, r2, tof,
                longWay ? TransferDirection.LongWay : TransferDirection.ShortWay);

            double sweep = Angles.ToDegrees(Vector3.AngleBetween(r1, r2));
            if (longWay)
            {
                sweep = 360.0 - sweep;
            }

            StringBuilder report = new();
            report.AppendLine($"Departure r1              = {r1.ToString("F3")}   |r1| = {r1.Magnitude:F3} km");
            report.AppendLine($"Arrival   r2              = {r2.ToString("F3")}   |r2| = {r2.Magnitude:F3} km");
            report.AppendLine($"Swept angle               = {sweep,12:F4} deg  ({(longWay ? "long way" : "short way")})");
            report.AppendLine($"Time of flight            = {tof,12:F1} s  ({tof / 60.0:F3} min)");
            report.AppendLine();
            report.AppendLine("SOLUTION");
            report.AppendLine($"  departure velocity v1   = {solution.DepartureVelocityKmPerSec.ToString("F6")} km/s");
            report.AppendLine($"                    |v1|  = {solution.DepartureVelocityKmPerSec.Magnitude,12:F6} km/s");
            report.AppendLine($"  arrival velocity   v2   = {solution.ArrivalVelocityKmPerSec.ToString("F6")} km/s");
            report.AppendLine($"                    |v2|  = {solution.ArrivalVelocityKmPerSec.Magnitude,12:F6} km/s");
            report.AppendLine($"  bisection iterations    = {solution.Iterations,12}");
            report.AppendLine();

            // Describe the transfer orbit that the solver found.
            OrbitalElements transfer = OrbitConversions.ToOrbitalElements(
                new StateVector(r1, solution.DepartureVelocityKmPerSec));

            report.AppendLine("TRANSFER ORBIT");
            report.AppendLine(transfer.ToString());
            report.AppendLine();

            // Independent verification: propagate the answer and see if it actually arrives.
            StateVector propagated = UniversalVariables.Propagate(
                new StateVector(r1, solution.DepartureVelocityKmPerSec), tof);

            report.AppendLine("VERIFICATION (propagating the solution forward by the time of flight)");
            report.AppendLine($"  arrived at              = {propagated.PositionKm.ToString("F3")} km");
            report.AppendLine($"  target was              = {r2.ToString("F3")} km");
            report.AppendLine($"  miss distance           = {(propagated.PositionKm - r2).Magnitude,12:E4} km");

            return report.ToString();
        }
    };

    // =====================================================================
    // CHAPTER 6 — BALLISTIC MISSILE TRAJECTORIES
    // =====================================================================

    private static CodeSample BallisticTrajectorySample() => new()
    {
        Id = "ch6-ballistic",
        Chapter = "Chapter 6",
        Title = "Ballistic missile free-flight trajectory",
        Summary = "Range, apogee and flight time from burnout conditions.",
        Theory =
            """
            A ballistic missile trajectory is simply an elliptical orbit whose perigee happens
            to lie inside the Earth. Everything from Chapters 1 to 4 applies unchanged.

            The one new ingredient is the free-flight range equation, which relates the burnout
            state directly to the ground range achieved:

                cos(Psi/2) = (1 - Q cos^2(phi)) / sqrt(1 + Q(Q-2) cos^2(phi))

            where Q = v_bo^2 r_bo / mu. Note that Q = 1 with a horizontal burnout is a circular
            orbit and Q = 2 is escape speed, so a missile always has Q < 2 - otherwise it is a
            spacecraft.

            The optimal loft angle for maximum range is beautifully simple: phi = (pi - Psi)/4.
            For a short-range shot that is the familiar 45 degrees; for a half-globe shot it
            falls towards zero.
            """,
        Parameters =
        [
            P("altitude", "Burnout altitude", 100.0, "km"),
            P("speed", "Burnout speed v_bo", 6.5, "km/s", "Must give Q < 2, i.e. below escape speed."),
            P("phi", "Burnout flight-path angle", 25.0, "deg", "Measured from the local horizontal.")
        ],
        CodeSnippet =
            """
            BallisticTrajectory result = BallisticMissile.Compute(
                burnoutRadiusKm: Constants.EarthRadiusKm + altitudeKm,
                burnoutSpeedKmPerSec: speedKmPerSec,
                burnoutFlightPathAngleRad: Angles.ToRadians(phiDeg));
            """,
        Run = inputs =>
        {
            double radius = Constants.EarthRadiusKm + inputs["altitude"];
            double speed = inputs["speed"];
            double phi = inputs.Radians("phi");

            BallisticTrajectory result = BallisticMissile.Compute(radius, speed, phi);

            StringBuilder report = new();
            report.AppendLine("BURNOUT CONDITIONS");
            report.AppendLine($"  radius r_bo             = {radius,12:F3} km  (altitude {inputs["altitude"]:F1} km)");
            report.AppendLine($"  speed  v_bo             = {speed,12:F4} km/s");
            report.AppendLine($"  flight-path angle phi   = {inputs["phi"],12:F4} deg");
            report.AppendLine();
            report.AppendLine("FREE-FLIGHT TRAJECTORY");
            report.AppendLine($"  Q parameter             = {result.QParameter,12:F6}   (Q = 2 is escape speed)");
            report.AppendLine($"  eccentricity e          = {result.Eccentricity,12:F6}");
            report.AppendLine($"  semi-major axis a       = {result.SemiMajorAxisKm,12:F3} km");
            report.AppendLine($"  perigee radius          = {TwoBody.PeriapsisRadius(result.SemiMajorAxisKm, result.Eccentricity),12:F3} km " +
                              "(inside the Earth, as expected)");
            report.AppendLine();
            report.AppendLine("PERFORMANCE");
            report.AppendLine($"  range angle Psi         = {result.FreeFlightRangeAngleDeg,12:F4} deg");
            report.AppendLine($"  ground range            = {result.SurfaceRangeKm,12:F1} km");
            report.AppendLine($"  maximum altitude        = {result.MaximumAltitudeKm,12:F1} km");
            report.AppendLine($"  free-flight time        = {result.TimeOfFlightSec,12:F1} s  ({result.TimeOfFlightMinutes:F2} min)");
            report.AppendLine();

            // Compare against the optimal-angle solution for the same achieved range.
            double optimalAngle = BallisticMissile.OptimalFlightPathAngle(result.FreeFlightRangeAngleRad);
            double minimumSpeed = BallisticMissile.MinimumBurnoutSpeed(result.FreeFlightRangeAngleRad, radius);

            report.AppendLine("OPTIMALITY");
            report.AppendLine($"  optimal angle for this range = {Angles.ToDegrees(optimalAngle),8:F4} deg");
            report.AppendLine($"  minimum speed needed         = {minimumSpeed,8:F4} km/s");
            report.AppendLine($"  you are using                = {speed,8:F4} km/s");
            report.AppendLine(speed > minimumSpeed + 1e-6
                ? $"  ... {speed - minimumSpeed:F4} km/s more than the minimum. Two flight-path angles\n" +
                  "      (a lofted 'high road' and a depressed 'low road') reach this same range."
                : "  ... this is essentially the minimum-energy trajectory for this range.");

            return report.ToString();
        },

        // The subject of this sample IS a trajectory, so it can be animated:
        // the free-flight ellipse from burnout to impact.
        BuildTrajectory = BuildBallisticTrajectory
    };

    // =====================================================================
    // CHAPTER 7 — LUNAR TRAJECTORIES
    // =====================================================================

    private static CodeSample LunarPatchedConicSample() => new()
    {
        Id = "ch7-lunar",
        Chapter = "Chapter 7",
        Title = "Lunar transfer by patched conics",
        Summary = "Trans-lunar injection, sphere of influence, and lunar orbit insertion.",
        Theory =
            """
            The Earth-Moon-spacecraft system is a three-body problem with no closed-form
            solution. The patched-conic approximation divides space into regions where one
            body dominates, solves a simple two-body problem in each, and stitches the answers
            together at the boundary.

            That boundary is the Moon's sphere of influence, r_SOI = R (m_moon/m_earth)^(2/5),
            which works out at about 66,200 km - roughly one sixth of the way back to Earth.

            Outside it, the vehicle flies a geocentric ellipse. Inside it, a selenocentric
            hyperbola. The lunar orbit insertion burn then uses the Oberth effect: the deeper
            you dive before burning, the less of your speed you have to shed.

            Every Apollo mission was first sketched exactly this way.
            """,
        Parameters =
        [
            P("parking", "Parking orbit altitude", 185.0, "km", "185 km was the Apollo parking orbit."),
            P("vinf", "Arrival v_infinity at the Moon", 0.8, "km/s", "Selenocentric speed on entering the sphere of influence."),
            P("lunarorbit", "Target lunar orbit altitude", 110.0, "km", "110 km was the Apollo command module orbit.")
        ],
        CodeSnippet =
            """
            double soi = LunarTrajectory.MoonSphereOfInfluenceKm;

            var leg = LunarTrajectory.ComputeGeocentricLeg(
                parkingOrbitRadiusKm: Constants.EarthRadiusKm + parkingAltitudeKm,
                arrivalRadiusKm: Constants.EarthMoonDistanceKm - soi);

            double loi = LunarTrajectory.LunarOrbitInsertionDeltaV(
                hyperbolicExcessSpeedKmPerSec: vInf,
                periapsisRadiusKm: Constants.MoonRadiusKm + lunarAltitudeKm);
            """,
        Run = inputs =>
        {
            double parkingRadius = Constants.EarthRadiusKm + inputs["parking"];
            double soi = LunarTrajectory.MoonSphereOfInfluenceKm;
            double arrivalRadius = Constants.EarthMoonDistanceKm - soi;

            var leg = LunarTrajectory.ComputeGeocentricLeg(parkingRadius, arrivalRadius);

            double lunarOrbitRadius = Constants.MoonRadiusKm + inputs["lunarorbit"];
            double loi = LunarTrajectory.LunarOrbitInsertionDeltaV(inputs["vinf"], lunarOrbitRadius);

            StringBuilder report = new();
            report.AppendLine("GEOMETRY");
            report.AppendLine($"  Earth-Moon distance     = {Constants.EarthMoonDistanceKm,12:F0} km");
            report.AppendLine($"  Moon's sphere of influence = {soi,9:F0} km  " +
                              $"({soi / Constants.EarthMoonDistanceKm * 100.0:F1}% of the distance)");
            report.AppendLine($"  patch point radius      = {arrivalRadius,12:F0} km");
            report.AppendLine();
            report.AppendLine("PHASE 1 - GEOCENTRIC ELLIPSE (trans-lunar injection)");
            report.AppendLine($"  parking orbit radius    = {parkingRadius,12:F1} km");
            report.AppendLine($"  parking orbit speed     = {TwoBody.CircularSpeed(parkingRadius),12:F6} km/s");
            report.AppendLine($"  injection speed         = {leg.InjectionSpeedKmPerSec,12:F6} km/s");
            report.AppendLine($"  TLI delta-v             = {leg.InjectionDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine($"  transfer a              = {leg.TransferSemiMajorAxisKm,12:F0} km");
            report.AppendLine($"  transfer e              = {leg.TransferEccentricity,12:F6}");
            report.AppendLine($"  arrival speed (geo)     = {leg.ArrivalSpeedKmPerSec,12:F6} km/s");
            report.AppendLine($"  coast time              = {leg.TimeOfFlightDays,12:F4} days");
            report.AppendLine();
            report.AppendLine("PHASE 2 - SELENOCENTRIC HYPERBOLA (lunar orbit insertion)");
            report.AppendLine($"  arrival v_infinity      = {inputs["vinf"],12:F6} km/s");
            report.AppendLine($"  target orbit radius     = {lunarOrbitRadius,12:F1} km  (altitude {inputs["lunarorbit"]:F0} km)");
            report.AppendLine($"  speed at periapsis      = {Math.Sqrt(inputs["vinf"] * inputs["vinf"] + 2.0 * Constants.MuMoon / lunarOrbitRadius),12:F6} km/s");
            report.AppendLine($"  lunar circular speed    = {TwoBody.CircularSpeed(lunarOrbitRadius, Constants.MuMoon),12:F6} km/s");
            report.AppendLine($"  LOI delta-v             = {loi,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("MISSION TOTAL");
            report.AppendLine($"  TLI + LOI               = {leg.InjectionDeltaVKmPerSec + loi,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("  For comparison, Apollo's actual TLI was about 3.05-3.10 km/s and");
            report.AppendLine("  its LOI about 0.9 km/s, over a 3-day (higher energy) trajectory.");

            return report.ToString();
        }
    };

    // =====================================================================
    // CHAPTER 8 — INTERPLANETARY TRAJECTORIES
    // =====================================================================

    private static CodeSample InterplanetaryHohmannSample() => new()
    {
        Id = "ch8-interplanetary",
        Chapter = "Chapter 8",
        Title = "Interplanetary Hohmann transfer",
        Summary = "Full Earth-to-Mars budget: C3, injection, capture, flight time and launch window.",
        Theory =
            """
            Patched conics again, now on the scale of the solar system, and with three conics
            instead of two: a departure hyperbola relative to Earth, a heliocentric transfer
            ellipse, and an arrival hyperbola at the target planet.

            The planetary spheres of influence are so small compared with an astronomical unit
            (Earth's is 0.6% of 1 AU) that the patch points can be treated as coincident with
            the planets themselves.

            Two numbers dominate mission planning:

              C3 = v_infinity^2 is the "characteristic energy" that launch providers quote in
              their performance curves - it is what actually determines how much mass you can
              send.

              The synodic period sets the launch windows. For Earth and Mars it is about 780
              days, which is why Mars missions launch in clusters roughly every 26 months.

            The departure phase angle is the other half of the window: Mars must be about 44
            degrees ahead of Earth at launch so that it arrives at the rendezvous point at the
            same time you do.
            """,
        Parameters =
        [
            P("r1", "Departure planet orbit radius", 1.0, "AU", "Earth = 1.0 AU."),
            P("r2", "Target planet orbit radius", 1.523679, "AU", "Mars = 1.5237, Venus = 0.7233, Jupiter = 5.2026."),
            P("park1", "Departure parking altitude", 200.0, "km", "Above the Earth's surface."),
            P("mu2", "Target planet mu", 42828.375214, "km^3/s^2", "Mars = 42828.4, Venus = 324858.6, Jupiter = 1.26687e8."),
            P("radius2", "Target planet radius", 3396.19, "km", "Mars = 3396.19, Venus = 6051.8, Jupiter = 71492."),
            P("park2", "Arrival parking altitude", 500.0, "km", "Above the target planet's surface.")
        ],
        CodeSnippet =
            """
            var transfer = Interplanetary.ComputeHohmannTransfer(
                departureOrbitRadiusKm:   r1Au * Constants.AstronomicalUnitKm,
                arrivalOrbitRadiusKm:     r2Au * Constants.AstronomicalUnitKm,
                departurePlanetMu:        Constants.MuEarth,
                departureParkingRadiusKm: Constants.EarthRadiusKm + parkAlt1,
                arrivalPlanetMu:          targetMu,
                arrivalParkingRadiusKm:   targetRadius + parkAlt2);
            """,
        Run = inputs =>
        {
            var transfer = Interplanetary.ComputeHohmannTransfer(
                departureOrbitRadiusKm: inputs["r1"] * Constants.AstronomicalUnitKm,
                arrivalOrbitRadiusKm: inputs["r2"] * Constants.AstronomicalUnitKm,
                departurePlanetMu: Constants.MuEarth,
                departureParkingRadiusKm: Constants.EarthRadiusKm + inputs["park1"],
                arrivalPlanetMu: inputs["mu2"],
                arrivalParkingRadiusKm: inputs["radius2"] + inputs["park2"]);

            StringBuilder report = new();
            report.AppendLine("HELIOCENTRIC TRANSFER ELLIPSE");
            report.AppendLine($"  departure radius        = {inputs["r1"],12:F6} AU");
            report.AppendLine($"  arrival radius          = {inputs["r2"],12:F6} AU");
            report.AppendLine($"  transfer speed at departure = {transfer.HeliocentricDepartureSpeedKmPerSec,8:F6} km/s");
            report.AppendLine($"  flight time             = {transfer.TransferTimeDays,12:F2} days  " +
                              $"({transfer.TransferTimeDays / 365.25:F3} years)");
            report.AppendLine();
            report.AppendLine("DEPARTURE (Earth escape hyperbola)");
            report.AppendLine($"  v_infinity required     = {transfer.DepartureVInfinityKmPerSec,12:F6} km/s");
            report.AppendLine($"  C3 (= v_inf^2)          = {transfer.DepartureC3,12:F4} km^2/s^2");
            report.AppendLine($"  injection delta-v       = {transfer.InjectionDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("ARRIVAL (capture hyperbola)");
            report.AppendLine($"  v_infinity on arrival   = {transfer.ArrivalVInfinityKmPerSec,12:F6} km/s");
            report.AppendLine($"  capture delta-v         = {transfer.CaptureDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("MISSION TOTAL");
            report.AppendLine($"  total delta-v           = {transfer.TotalDeltaVKmPerSec,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("LAUNCH WINDOW");
            report.AppendLine($"  required phase angle    = {transfer.DeparturePhaseAngleDeg,12:F3} deg");
            report.AppendLine(transfer.DeparturePhaseAngleDeg > 0.0
                ? "    (the target must LEAD the departure planet by this angle at launch)"
                : "    (the target must TRAIL the departure planet by this angle at launch)");
            report.AppendLine($"  synodic period          = {transfer.SynodicPeriodDays,12:F2} days  " +
                              $"({transfer.SynodicPeriodDays / 365.25:F3} years between windows)");

            return report.ToString();
        },

        // The subject of this sample IS a trajectory, so it can be animated:
        // the heliocentric cruise between two planetary orbits.
        BuildTrajectory = BuildInterplanetaryTrajectory
    };

    private static CodeSample GravityAssistSample() => new()
    {
        Id = "ch8-gravity-assist",
        Chapter = "Chapter 8",
        Title = "Gravity assist (planetary flyby)",
        Summary = "Stealing momentum from a planet: turning angle and heliocentric velocity gain.",
        Theory =
            """
            In the planet's own reference frame a flyby changes nothing but direction: the
            vehicle arrives with speed v_infinity and leaves with speed v_infinity, rotated
            through a turning angle delta given by

                sin(delta / 2) = 1 / e

            But the planet is itself moving around the Sun. In the heliocentric frame that
            rotation adds vectorially to the planet's orbital velocity, and the spacecraft can
            gain up to 2 v_infinity sin(delta/2) of heliocentric speed - for free.

            The energy comes from the planet's orbital motion. Jupiter lost an utterly
            immeasurable amount of orbital energy giving Voyager 2 about 10 km/s.

            Notice the trade-off: a close, slow flyby (low v_infinity, high e is achieved by
            flying deep) turns you further, but there is less speed to redirect. The optimum
            is a real design problem.
            """,
        Parameters =
        [
            P("vinf", "Hyperbolic excess speed v_inf", 5.6, "km/s", "Speed relative to the planet, far from it."),
            P("periapsis", "Flyby periapsis radius", 100000.0, "km", "Distance of closest approach from the planet's centre."),
            P("mu", "Planet mu", 1.26686534e8, "km^3/s^2", "Jupiter by default. Earth = 398600.4, Venus = 324858.6.")
        ],
        CodeSnippet =
            """
            // Eccentricity of the flyby hyperbola from its periapsis and v_infinity.
            double e = 1.0 + periapsisRadiusKm * vInf * vInf / planetMu;

            double turning = Interplanetary.FlybyTurningAngle(e);
            double gain    = Interplanetary.GravityAssistDeltaV(vInf, turning);
            """,
        Run = inputs =>
        {
            double vInf = inputs["vinf"];
            double rp = inputs["periapsis"];
            double mu = inputs["mu"];

            // e = 1 + r_p v_inf^2 / mu, from the definition of the hyperbola's periapsis.
            double e = 1.0 + rp * vInf * vInf / mu;

            double turning = Interplanetary.FlybyTurningAngle(e);
            double gain = Interplanetary.GravityAssistDeltaV(vInf, turning);
            double vPeriapsis = Math.Sqrt(vInf * vInf + 2.0 * mu / rp);

            StringBuilder report = new();
            report.AppendLine("FLYBY HYPERBOLA");
            report.AppendLine($"  v_infinity              = {vInf,12:F6} km/s");
            report.AppendLine($"  periapsis radius        = {rp,12:F0} km");
            report.AppendLine($"  eccentricity e          = {e,12:F6}");
            report.AppendLine($"  semi-major axis a       = {-mu / (vInf * vInf),12:F0} km  (negative, as required)");
            report.AppendLine($"  speed at periapsis      = {vPeriapsis,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("RESULT");
            report.AppendLine($"  turning angle delta     = {Angles.ToDegrees(turning),12:F4} deg");
            report.AppendLine($"  max heliocentric gain   = {gain,12:F6} km/s");
            report.AppendLine();
            report.AppendLine("  This is the magnitude of the change in the heliocentric velocity");
            report.AppendLine("  vector. Whether it is a speed-up or a slow-down depends on which");
            report.AppendLine("  side of the planet you pass - trailing edge to gain, leading edge");
            report.AppendLine("  to lose (which is how missions to Mercury shed energy).");
            report.AppendLine();
            report.AppendLine("SENSITIVITY TO FLYBY ALTITUDE");
            report.AppendLine("    Periapsis (km)          e     Turning (deg)     Gain (km/s)");
            report.AppendLine("    ---------------------------------------------------------------");

            foreach (double factor in new[] { 0.25, 0.5, 1.0, 2.0, 4.0, 8.0 })
            {
                double testRp = rp * factor;
                double testE = 1.0 + testRp * vInf * vInf / mu;
                double testTurn = Interplanetary.FlybyTurningAngle(testE);
                double testGain = Interplanetary.GravityAssistDeltaV(vInf, testTurn);

                report.AppendLine($"    {testRp,13:F0}  {testE,9:F4}  {Angles.ToDegrees(testTurn),15:F4}  {testGain,14:F6}");
            }

            report.AppendLine();
            report.AppendLine("  Closer is always better for turning - which is why flyby design is");
            report.AppendLine("  a fight between trajectory gain and atmospheric/radiation limits.");

            return report.ToString();
        }
    };

    // =====================================================================
    // CHAPTER 9 — PERTURBATIONS
    // =====================================================================

    private static CodeSample J2PerturbationSample() => new()
    {
        Id = "ch9-j2",
        Chapter = "Chapter 9",
        Title = "J2 nodal regression and apsidal rotation",
        Summary = "How the Earth's equatorial bulge makes every orbit precess.",
        Theory =
            """
            The Earth is not a sphere: centrifugal flattening makes it about 21 km wider at
            the equator than at the poles. That bulge exerts a torque on an inclined orbit,
            and the orbit responds like a gyroscope - it precesses.

            The effect is captured by J2 = 1.0826e-3, and it produces two secular drifts:

                dRAAN/dt = -(3/2) n J2 (Re/p)^2 cos(i)
                dargp/dt =  (3/4) n J2 (Re/p)^2 (5 cos^2(i) - 1)

            Both are exploited rather than merely tolerated. Set the inclination so that the
            node drifts at exactly one revolution per year and you have a sun-synchronous
            orbit. Set cos^2(i) = 1/5 - that is i = 63.435 degrees - and perigee stops moving
            entirely, which is why every Molniya satellite flies at that inclination.

            Try the default Molniya orbit, then change the inclination away from 63.4 and
            watch the apsidal drift appear.
            """,
        Parameters =
        [
            P("sma", "Semi-major axis a", 26600.0, "km", "Molniya by default."),
            P("ecc", "Eccentricity e", 0.74, ""),
            P("inc", "Inclination i", 63.4349, "deg", "Try 51.6 (ISS) or 98.6 (sun-synchronous)."),
            P("days", "Time span to project", 365.0, "days")
        ],
        CodeSnippet =
            """
            double nodeRate = Perturbations.NodalRegressionRate(sma, ecc, Angles.ToRadians(incDeg));
            double apsisRate = Perturbations.ApsidalRotationRate(sma, ecc, Angles.ToRadians(incDeg));

            double nodeDriftPerDay = Angles.ToDegrees(nodeRate) * Constants.SolarDaySeconds;
            """,
        Run = inputs =>
        {
            double sma = inputs["sma"];
            double ecc = inputs["ecc"];
            double inc = inputs.Radians("inc");
            double days = inputs["days"];

            double nodeRate = Perturbations.NodalRegressionRate(sma, ecc, inc);
            double apsisRate = Perturbations.ApsidalRotationRate(sma, ecc, inc);

            double nodePerDay = Angles.ToDegrees(nodeRate) * Constants.SolarDaySeconds;
            double apsisPerDay = Angles.ToDegrees(apsisRate) * Constants.SolarDaySeconds;

            StringBuilder report = new();
            report.AppendLine("ORBIT");
            report.AppendLine($"  semi-major axis a       = {sma,12:F1} km");
            report.AppendLine($"  eccentricity e          = {ecc,12:F6}");
            report.AppendLine($"  inclination i           = {inputs["inc"],12:F4} deg");
            report.AppendLine($"  perigee altitude        = {TwoBody.PeriapsisRadius(sma, ecc) - Constants.EarthRadiusKm,12:F1} km");
            report.AppendLine($"  apogee altitude         = {TwoBody.ApoapsisRadius(sma, ecc) - Constants.EarthRadiusKm,12:F1} km");
            report.AppendLine($"  period                  = {TwoBody.Period(sma) / 3600.0,12:F4} hours");
            report.AppendLine();
            report.AppendLine("SECULAR J2 DRIFT RATES");
            report.AppendLine($"  nodal regression        = {nodePerDay,12:F6} deg/day");
            report.AppendLine($"                          = {nodePerDay * 365.25,12:F3} deg/year");
            report.AppendLine($"  apsidal rotation        = {apsisPerDay,12:F6} deg/day");
            report.AppendLine($"                          = {apsisPerDay * 365.25,12:F3} deg/year");
            report.AppendLine();
            report.AppendLine($"OVER {days:F0} DAYS");
            report.AppendLine($"  RAAN moves by           = {nodePerDay * days,12:F3} deg");
            report.AppendLine($"  argument of perigee by  = {apsisPerDay * days,12:F3} deg");
            report.AppendLine();
            report.AppendLine("INTERPRETATION");
            report.AppendLine(nodePerDay < -0.001
                ? "  The node drifts WESTWARD (prograde orbit)."
                : nodePerDay > 0.001
                    ? "  The node drifts EASTWARD (retrograde orbit)."
                    : "  The node is essentially STATIONARY (polar orbit).");

            double criticalDeg = Angles.ToDegrees(Perturbations.CriticalInclinationRad);
            report.AppendLine(Math.Abs(Math.Abs(inputs["inc"]) - criticalDeg) < 0.01 ||
                              Math.Abs(Math.Abs(inputs["inc"]) - (180.0 - criticalDeg)) < 0.01
                ? $"  You are at the CRITICAL INCLINATION ({criticalDeg:F4} deg): perigee is frozen.\n" +
                  "  This is the Molniya trick - apogee stays parked over the same hemisphere."
                : $"  Perigee is drifting. The critical inclination that would freeze it is\n" +
                  $"  {criticalDeg:F4} deg (or {180.0 - criticalDeg:F4} deg retrograde).");

            return report.ToString();
        }
    };

    private static CodeSample SunSynchronousDesignSample() => new()
    {
        Id = "ch9-sunsync",
        Chapter = "Chapter 9",
        Title = "Sun-synchronous orbit design",
        Summary = "Choose an altitude, get the inclination that keeps the lighting constant.",
        Theory =
            """
            A sun-synchronous orbit precesses its node eastward at exactly the rate the Earth
            orbits the Sun: 360 degrees per tropical year, or 1.99106e-7 rad/s. The orbital
            plane then holds a constant angle to the Sun, so the satellite crosses the equator
            at the same local solar time on every single pass.

            That consistency is everything for Earth observation: images taken weeks apart
            have the same shadows and can be compared directly. Landsat, Sentinel, and every
            operational weather satellite in polar orbit uses it.

            Setting the nodal regression formula equal to the required rate and solving for
            inclination gives a value slightly above 90 degrees - the orbit must be retrograde,
            because cos(i) has to be negative to make the node drift eastward. At 800 km the
            answer is about 98.6 degrees.

            Notice the constraint at the bottom of the sweep: above roughly 5,975 km altitude
            no sun-synchronous orbit exists at all, because J2 simply cannot supply enough
            precession.
            """,
        Parameters =
        [
            P("altitude", "Orbit altitude", 800.0, "km"),
            P("ecc", "Eccentricity e", 0.0, "", "Sun-synchronous orbits are usually near-circular."),
            P("min", "Sweep table: lowest altitude", 300.0, "km"),
            P("max", "Sweep table: highest altitude", 1500.0, "km")
        ],
        CodeSnippet =
            """
            double a = Constants.EarthRadiusKm + altitudeKm;

            double inclination = Angles.ToDegrees(
                Perturbations.SunSynchronousInclination(a, eccentricity));
            """,
        Run = inputs =>
        {
            double altitude = inputs["altitude"];
            double ecc = inputs["ecc"];
            double sma = Constants.EarthRadiusKm + altitude;

            double inclination = Perturbations.SunSynchronousInclination(sma, ecc);
            double period = TwoBody.Period(sma);
            double nodeRate = Perturbations.NodalRegressionRate(sma, ecc, inclination);

            StringBuilder report = new();
            report.AppendLine("DESIGN POINT");
            report.AppendLine($"  altitude                = {altitude,12:F1} km");
            report.AppendLine($"  semi-major axis a       = {sma,12:F3} km");
            report.AppendLine($"  eccentricity e          = {ecc,12:F6}");
            report.AppendLine();
            report.AppendLine($"  REQUIRED INCLINATION    = {Angles.ToDegrees(inclination),12:F5} deg  (retrograde)");
            report.AppendLine();
            report.AppendLine($"  orbital period          = {period / 60.0,12:F4} min");
            report.AppendLine($"  orbits per day          = {Constants.SolarDaySeconds / period,12:F4}");
            report.AppendLine($"  resulting node drift    = {Angles.ToDegrees(nodeRate) * Constants.SolarDaySeconds,12:F6} deg/day");
            report.AppendLine($"  required for sun-sync   = {360.0 / 365.2422,12:F6} deg/day");
            report.AppendLine();
            report.AppendLine("ALTITUDE SWEEP");
            report.AppendLine("     Altitude    Inclination      Period    Orbits/day");
            report.AppendLine("         (km)          (deg)       (min)");
            report.AppendLine("     ------------------------------------------------");

            double min = inputs["min"];
            double max = inputs["max"];

            for (int i = 0; i < 13; i++)
            {
                double h = min + (max - min) * i / 12.0;
                double a = Constants.EarthRadiusKm + h;

                try
                {
                    double inc = Angles.ToDegrees(Perturbations.SunSynchronousInclination(a, ecc));
                    double t = TwoBody.Period(a);

                    report.AppendLine($"     {h,8:F0}  {inc,13:F5}  {t / 60.0,10:F3}  {Constants.SolarDaySeconds / t,12:F4}");
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Above roughly 5975 km no solution exists: cos(i) would exceed 1.
                    report.AppendLine($"     {h,8:F0}   -- no sun-synchronous solution exists at this altitude --");
                }
            }

            return report.ToString();
        }
    };
}
