using Astro.Core;

namespace Astro.App.Models;

/// <summary>
/// Trajectory builders for the two-impulse transfers: the Hohmann transfer between coplanar
/// circular orbits, and its heliocentric equivalent between two planets.
/// </summary>
/// <remarks>
/// Both are the same geometry at wildly different scales, so they share
/// <see cref="BuildTwoImpulseTransfer"/> and differ only in units, colours and labels. That
/// reuse is not merely convenient - it is the point Chapter 8 makes, that an interplanetary
/// Hohmann transfer is exactly the orbital Hohmann transfer with the Sun in place of the Earth
/// and planets in place of spacecraft.
/// </remarks>
public static partial class SampleCatalog
{
    /// <summary>
    /// How much of the initial and final circular orbits to fly before and after the transfer,
    /// as a fraction of a revolution.
    /// </summary>
    /// <remarks>
    /// Showing a lead-in and a run-out matters. Without them the animation begins abruptly at
    /// the first burn, and the viewer has no visual reference for the circular orbit the
    /// transfer departs from - the transfer ellipse would just be a curve floating in space.
    /// </remarks>
    private const double CoastFraction = 0.16;

    /// <summary>
    /// Builds a coast, burn, transfer-ellipse, burn, coast animation between two coplanar
    /// circular orbits.
    /// </summary>
    /// <param name="initialRadius">Radius of the starting circular orbit, in km.</param>
    /// <param name="finalRadius">Radius of the target circular orbit, in km.</param>
    /// <param name="mu">Gravitational parameter of the central body, km^3/s^2.</param>
    /// <param name="transferTimeSeconds">Time of flight along the transfer ellipse, s.</param>
    /// <param name="scale">Divisor converting km into the display unit (1 for km, AU in km for AU).</param>
    /// <param name="timeDivisor">Divisor converting seconds into the displayed time unit.</param>
    /// <returns>Ordered raw points plus the indices of the two burns.</returns>
    /// <remarks>
    /// <para>
    /// <b>Geometry.</b> The first burn is placed on the +X axis and motion is counter-clockwise.
    /// When raising an orbit the transfer ellipse has its PERIAPSIS at the first burn, so the
    /// spacecraft coasts from true anomaly 0 to 180. When lowering an orbit it has its APOAPSIS
    /// there instead, and the sweep runs from 180 to 360. Handling both is what lets the sample
    /// answer "what if I make r2 smaller than r1?" without special-casing anything upstream.
    /// </para>
    /// <para>
    /// <b>Impulsive burns.</b> A Hohmann burn is instantaneous in the model, which would make
    /// it invisible. A handful of points on either side are therefore flagged as powered so the
    /// exhaust plume flashes at each burn. That is a rendering convenience and nothing more -
    /// no finite burn duration is being modelled, and the delta-v is applied as a step change.
    /// </para>
    /// </remarks>
    private static (List<RawPoint> Points, int FirstBurn, int SecondBurn) BuildTwoImpulseTransfer(
        double initialRadius,
        double finalRadius,
        double mu,
        double transferTimeSeconds,
        double scale,
        double timeDivisor)
    {
        bool raising = finalRadius >= initialRadius;

        double transferSemiMajorAxis = (initialRadius + finalRadius) / 2.0;
        double transferEccentricity =
            Math.Abs(finalRadius - initialRadius) / (initialRadius + finalRadius);
        double semiLatusRectum =
            transferSemiMajorAxis * (1.0 - transferEccentricity * transferEccentricity);

        double initialPeriod = TwoBody.Period(initialRadius, mu);
        double finalPeriod = TwoBody.Period(finalRadius, mu);

        double leadInTime = initialPeriod * CoastFraction;
        double runOutTime = finalPeriod * CoastFraction;

        // Split the frame budget in proportion to each phase's duration so the animation runs
        // at a uniform apparent rate throughout rather than crawling through the short bits.
        double totalTime = leadInTime + transferTimeSeconds + runOutTime;
        int leadInSteps = Math.Max(20, (int)(TrajectoryResolution * leadInTime / totalTime));
        int transferSteps = Math.Max(60, (int)(TrajectoryResolution * transferTimeSeconds / totalTime));
        int runOutSteps = Math.Max(20, (int)(TrajectoryResolution * runOutTime / totalTime));

        List<RawPoint> raw = new(leadInSteps + transferSteps + runOutSteps);

        double clock = 0.0;

        // Which orbit the vehicle is currently on. Needed for the vis-viva speed readout, and
        // updated as each phase begins. Declared before Add because a local function may not
        // capture a variable that is declared after it.
        double phaseSemiMajorAxis = initialRadius;

        // Adds one point, converting to display units on the way in.
        void Add(double radiusKm, double angleRad, double time, bool powered)
        {
            double speed = TwoBody.VisViva(radiusKm, phaseSemiMajorAxis, mu);

            raw.Add(new RawPoint(
                Time: time,
                X: radiusKm * Math.Cos(angleRad) / scale,
                Y: radiusKm * Math.Sin(angleRad) / scale,
                Powered: powered,
                Channels:
                [
                    radiusKm / scale,
                    speed,
                    time / timeDivisor
                ]));
        }

        // ---- Phase 1: lead-in coast on the initial circular orbit ----------------------
        // Ends exactly on the +X axis, so the burn happens at angle zero.
        for (int i = 0; i < leadInSteps; i++)
        {
            double fraction = i / (double)leadInSteps;
            double angle = -Constants.TwoPi * CoastFraction * (1.0 - fraction);

            Add(initialRadius, angle, clock, powered: false);
            clock += leadInTime / leadInSteps;
        }

        int firstBurn = raw.Count;

        // ---- Phase 2: the transfer ellipse ---------------------------------------------
        phaseSemiMajorAxis = transferSemiMajorAxis;

        double startTrueAnomaly = raising ? 0.0 : Math.PI;
        double meanMotion = TwoBody.MeanMotion(transferSemiMajorAxis, mu);
        double startTimeSincePeriapsis =
            KeplerSolver.TrueToMeanAnomaly(startTrueAnomaly, transferEccentricity) / meanMotion;

        for (int i = 0; i <= transferSteps; i++)
        {
            double elapsed = transferTimeSeconds * i / transferSteps;

            double trueAnomaly = KeplerSolver.TrueAnomalyAfterTime(
                startTimeSincePeriapsis + elapsed,
                transferEccentricity,
                transferSemiMajorAxis,
                mu);

            double radius = TwoBody.RadiusAtTrueAnomaly(
                semiLatusRectum, transferEccentricity, trueAnomaly);

            // The sweep must start on the +X axis regardless of which end of the ellipse the
            // spacecraft departs from, hence subtracting the starting true anomaly.
            double angle = trueAnomaly - startTrueAnomaly;

            // Flash the plume for a few frames either side of each impulse.
            bool powered = i <= 2 || i >= transferSteps - 2;

            Add(radius, angle, clock + elapsed, powered);
        }

        int secondBurn = raw.Count - 1;
        clock += transferTimeSeconds;

        // ---- Phase 3: run-out coast on the final circular orbit -------------------------
        phaseSemiMajorAxis = finalRadius;

        for (int i = 1; i <= runOutSteps; i++)
        {
            double fraction = i / (double)runOutSteps;

            // Arrival is half a revolution round from the first burn, i.e. on the -X axis.
            double angle = Math.PI + Constants.TwoPi * CoastFraction * fraction;

            Add(finalRadius, angle, clock + runOutTime * fraction, powered: false);
        }

        return (raw, firstBurn, secondBurn);
    }

    // ====================================================================================
    // Chapter 3 - Hohmann transfer
    // ====================================================================================

    /// <summary>Builds the animated Hohmann transfer between two circular Earth orbits.</summary>
    /// <param name="inputs">Initial and final circular radii, in km.</param>
    /// <returns>The animatable trajectory.</returns>
    private static Trajectory BuildHohmannTrajectory(SampleInputs inputs)
    {
        double initialRadius = inputs["r1"];
        double finalRadius = inputs["r2"];

        TransferResult transfer = Maneuvers.HohmannTransfer(initialRadius, finalRadius);

        (List<RawPoint> raw, int firstBurn, int secondBurn) = BuildTwoImpulseTransfer(
            initialRadius,
            finalRadius,
            Constants.MuEarth,
            transfer.TransferTimeSec,
            scale: 1.0,
            timeDivisor: 3600.0);

        List<TrajectoryPoint> points = WithHeadings(raw);

        double firstDeltaV = transfer.BurnsKmPerSec[0];
        double secondDeltaV = transfer.BurnsKmPerSec[1];

        return new Trajectory
        {
            Title = "Hohmann Transfer",
            Subtitle =
                $"{initialRadius:N0} km to {finalRadius:N0} km  |  " +
                $"total delta-v {transfer.TotalDeltaVKmPerSec:F4} km/s, " +
                $"transfer time {transfer.TransferTimeHours:F2} h",
            XAxisLabel = "X (km)",
            YAxisLabel = "Y (km)",
            Points = points,
            EqualAspect = true,
            PlaybackSeconds = 15.0,
            Bodies =
            [
                new TrajectoryBody("Earth", 0.0, 0.0, Constants.EarthRadiusKm, "#14324B", "#4C87B8"),

                // Reference rings for the two circular orbits. Drawing them makes it obvious
                // that the transfer ellipse is tangent to both - which is the entire reason
                // the Hohmann transfer is the cheapest two-impulse option.
                Ring(initialRadius, "#37536B"),
                Ring(finalRadius, "#37536B")
            ],
            Channels =
            [
                new TrajectoryChannel("Radius", "km", "N0"),
                new TrajectoryChannel("Speed", "km/s", "F4"),
                new TrajectoryChannel("Elapsed", "h", "F3")
            ],
            Markers =
            [
                new TrajectoryMarker($"Burn 1: {firstDeltaV:+0.000;-0.000} km/s", firstBurn, "#FF8A3D"),
                new TrajectoryMarker($"Burn 2: {secondDeltaV:+0.000;-0.000} km/s", secondBurn, "#FF8A3D"),
                new TrajectoryMarker("Arrival", points.Count - 1, "#7BD88F")
            ],
            StateLabel = "Thrust",
            PoweredText = "BURN",
            CoastingText = "COAST",
            Notes =
                "The transfer ellipse is tangent to both circular orbits, which is what makes " +
                "this the cheapest two-impulse transfer between them. Both burns are IMPULSIVE " +
                "in this model - instantaneous changes of velocity - so the plume flash marks " +
                "where each burn happens, not how long it takes. Model: coplanar circular orbits, " +
                "two-body point-mass gravity, no J2, no drag, no finite burn duration."
        };
    }

    // ====================================================================================
    // Chapter 8 - Interplanetary Hohmann transfer
    // ====================================================================================

    /// <summary>Builds the animated heliocentric transfer between two planetary orbits.</summary>
    /// <param name="inputs">Departure and arrival orbit radii, in AU.</param>
    /// <returns>The animatable trajectory.</returns>
    /// <remarks>
    /// Identical geometry to the Earth-orbit Hohmann transfer, at roughly six orders of
    /// magnitude larger scale and with the Sun at the focus. Displaying it in AU rather than
    /// kilometres is what keeps the axis labels legible.
    /// </remarks>
    private static Trajectory BuildInterplanetaryTrajectory(SampleInputs inputs)
    {
        double departureRadiusKm = inputs["r1"] * Constants.AstronomicalUnitKm;
        double arrivalRadiusKm = inputs["r2"] * Constants.AstronomicalUnitKm;

        // Half the period of the transfer ellipse: the definition of a Hohmann time of flight.
        double transferSemiMajorAxis = (departureRadiusKm + arrivalRadiusKm) / 2.0;
        double transferTime = TwoBody.Period(transferSemiMajorAxis, Constants.MuSun) / 2.0;

        (List<RawPoint> raw, int firstBurn, int secondBurn) = BuildTwoImpulseTransfer(
            departureRadiusKm,
            arrivalRadiusKm,
            Constants.MuSun,
            transferTime,
            scale: Constants.AstronomicalUnitKm,
            timeDivisor: Constants.SolarDaySeconds);

        List<TrajectoryPoint> points = WithHeadings(raw);

        double synodic = Interplanetary.SynodicPeriod(
            TwoBody.Period(departureRadiusKm, Constants.MuSun),
            TwoBody.Period(arrivalRadiusKm, Constants.MuSun));

        return new Trajectory
        {
            Title = "Interplanetary Hohmann Transfer",
            Subtitle =
                $"{inputs["r1"]:F3} AU to {inputs["r2"]:F3} AU  |  " +
                $"cruise {transferTime / Constants.SolarDaySeconds:N0} days, " +
                $"launch window every {synodic / Constants.SolarDaySeconds:N0} days",
            XAxisLabel = "X (AU)",
            YAxisLabel = "Y (AU)",
            Points = points,
            EqualAspect = true,
            PlaybackSeconds = 16.0,
            Bodies =
            [
                // NOT to scale: the Sun's true radius is 0.00465 AU, which at this zoom is
                // smaller than the line width. It is drawn oversized purely as a visual anchor,
                // and the footer says so.
                new TrajectoryBody("Sun", 0.0, 0.0, 0.055, "#5A4213", "#E8B84B"),
                Ring(inputs["r1"], "#37536B"),
                Ring(inputs["r2"], "#6B4A37")
            ],
            Channels =
            [
                new TrajectoryChannel("Radius", "AU", "F4"),
                new TrajectoryChannel("Speed", "km/s", "F3"),
                new TrajectoryChannel("Elapsed", "days", "F1")
            ],
            Markers =
            [
                new TrajectoryMarker("Departure burn", firstBurn, "#FF8A3D"),
                new TrajectoryMarker("Arrival burn", secondBurn, "#FF8A3D"),
                new TrajectoryMarker("Target orbit", points.Count - 1, "#7BD88F")
            ],
            StateLabel = "Thrust",
            PoweredText = "BURN",
            CoastingText = "CRUISE",
            Notes =
                "The Sun is drawn far larger than its true size (0.00465 AU) so it is visible at " +
                "this zoom; the two rings ARE to scale. Only the heliocentric cruise is shown - " +
                "the patched-conic escape from the departure planet and the capture at the target " +
                "happen inside spheres of influence far too small to see here. This is why launch " +
                "windows exist: the target must be at the arrival point when the spacecraft gets " +
                "there, which only lines up once per synodic period. Model: coplanar circular " +
                "planetary orbits, impulsive burns, two-body patched conics."
        };
    }
}
