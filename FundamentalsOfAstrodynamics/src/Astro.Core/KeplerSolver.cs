namespace Astro.Core;

/// <summary>
/// Chapter 4 — <b>Position and Velocity as a Function of Time</b>: Kepler's equation and
/// the conversions between the three "anomalies" (true, eccentric and mean).
/// </summary>
/// <remarks>
/// <para>
/// The orbit equation gives r as a function of the true anomaly nu, but nu does not advance
/// uniformly — a satellite races through periapsis and crawls through apoapsis. Kepler
/// solved this by introducing two auxiliary angles:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Eccentric anomaly E</b> — a geometric construction that projects the ellipse onto
///     its circumscribing circle.
///   </description></item>
///   <item><description>
///     <b>Mean anomaly M</b> — a fictitious angle that <i>does</i> advance uniformly:
///     M = n(t − t_p), where n is the mean motion and t_p the time of periapsis passage.
///   </description></item>
/// </list>
/// <para>
/// They are tied together by <b>Kepler's equation</b>, M = E − e sin E. Going from E to M
/// is trivial. Going the other way is not: the equation is transcendental and has no
/// closed-form solution, which is why the iterative solver below exists and why it has
/// occupied numerical analysts since 1650.
/// </para>
/// </remarks>
public static class KeplerSolver
{
    /// <summary>Default convergence tolerance on the Kepler residual [rad].</summary>
    public const double DefaultTolerance = 1e-12;

    /// <summary>Default iteration cap; Newton's method normally converges in 3–6 steps.</summary>
    public const int DefaultMaxIterations = 100;

    /// <summary>
    /// The outcome of a Kepler-equation solve, including diagnostics that the UI displays so
    /// you can watch the convergence behaviour change with eccentricity.
    /// </summary>
    /// <param name="AnomalyRad">The eccentric anomaly E (elliptic) or H (hyperbolic) [rad].</param>
    /// <param name="Iterations">How many Newton steps were actually taken.</param>
    /// <param name="FinalResidual">|M_computed − M_target| at the returned solution [rad].</param>
    public readonly record struct KeplerSolution(double AnomalyRad, int Iterations, double FinalResidual);

    /// <summary>
    /// Solves <b>Kepler's equation M = E − e sin E</b> for the eccentric anomaly E, given the
    /// mean anomaly M and eccentricity e (elliptical orbits only, 0 &lt;= e &lt; 1).
    /// </summary>
    /// <param name="meanAnomalyRad">M — mean anomaly [rad]. Any value; it is wrapped internally.</param>
    /// <param name="eccentricity">e — eccentricity, 0 &lt;= e &lt; 1.</param>
    /// <param name="tolerance">Absolute convergence tolerance on the residual [rad].</param>
    /// <param name="maxIterations">Safety cap on the iteration count.</param>
    /// <returns>The eccentric anomaly and convergence diagnostics.</returns>
    /// <remarks>
    /// <para>
    /// <b>Method: Newton–Raphson.</b> Define f(E) = E − e sin E − M. Then f'(E) = 1 − e cos E,
    /// and the update is
    /// </para>
    /// <code>
    ///     E_{k+1} = E_k − f(E_k) / f'(E_k)
    ///             = E_k + (M − E_k + e sin E_k) / (1 − e cos E_k)
    /// </code>
    /// <para>
    /// <b>Why the starting guess matters.</b> f'(E) = 1 − e cos E approaches zero near
    /// periapsis of a highly eccentric orbit, so a careless initial guess can send the first
    /// Newton step wildly off. The classic remedy, used here, is due to Smith: start from
    /// E_0 = M for moderate eccentricity, but from E_0 = pi (the far side) when e &gt; 0.8, so
    /// that the iteration approaches the root from the well-conditioned direction.
    /// </para>
    /// </remarks>
    public static KeplerSolution SolveEccentricAnomaly(
        double meanAnomalyRad,
        double eccentricity,
        double tolerance = DefaultTolerance,
        int maxIterations = DefaultMaxIterations)
    {
        Guard.InRange(eccentricity, 0.0, 1.0 - 1e-12, nameof(eccentricity));

        double m = Angles.Normalize(meanAnomalyRad);

        // Smith's starting guess (see remarks above).
        double e0 = eccentricity < 0.8
            ? m
            : Math.PI;

        double eAnom = e0;
        int iteration = 0;
        double residual = double.MaxValue;

        while (iteration < maxIterations)
        {
            double f = eAnom - eccentricity * Math.Sin(eAnom) - m;   // Kepler residual
            double fPrime = 1.0 - eccentricity * Math.Cos(eAnom);    // derivative

            residual = Math.Abs(f);
            if (residual < tolerance)
            {
                break;
            }

            // fPrime can only vanish if e == 1, which the guard above already excluded,
            // but a defensive check costs nothing and documents the assumption.
            if (Math.Abs(fPrime) < 1e-15)
            {
                throw new InvalidOperationException(
                    "Kepler solver stalled: derivative vanished (eccentricity too close to 1).");
            }

            eAnom -= f / fPrime;
            iteration++;
        }

        if (residual > tolerance)
        {
            throw new InvalidOperationException(
                $"Kepler's equation failed to converge in {maxIterations} iterations (residual {residual:E3}).");
        }

        return new KeplerSolution(Angles.Normalize(eAnom), iteration, residual);
    }

    /// <summary>
    /// Solves the <b>hyperbolic</b> Kepler equation M = e sinh H − H for the hyperbolic
    /// anomaly H.
    /// </summary>
    /// <param name="meanAnomalyRad">M — hyperbolic mean anomaly [rad]; NOT wrapped, since a hyperbola never repeats.</param>
    /// <param name="eccentricity">e — eccentricity, must be &gt; 1.</param>
    /// <param name="tolerance">Absolute convergence tolerance.</param>
    /// <param name="maxIterations">Safety cap on the iteration count.</param>
    /// <remarks>
    /// Same Newton scheme, with f(H) = e sinh H − H − M and f'(H) = e cosh H − 1. The
    /// starting guess uses the asymptotic inverse of sinh, H_0 ≈ asinh(M/e) — sinh grows
    /// exponentially, so starting at M itself would overshoot catastrophically for large M.
    /// </remarks>
    public static KeplerSolution SolveHyperbolicAnomaly(
        double meanAnomalyRad,
        double eccentricity,
        double tolerance = DefaultTolerance,
        int maxIterations = DefaultMaxIterations)
    {
        if (eccentricity <= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eccentricity), eccentricity, "The hyperbolic Kepler equation requires e > 1.");
        }

        // Asymptotic starting guess: for large |M|, e sinh H dominates, so H ~ asinh(M/e).
        double h = Math.Asinh(meanAnomalyRad / eccentricity);

        int iteration = 0;
        double residual = double.MaxValue;

        while (iteration < maxIterations)
        {
            double f = eccentricity * Math.Sinh(h) - h - meanAnomalyRad;
            double fPrime = eccentricity * Math.Cosh(h) - 1.0;

            residual = Math.Abs(f);
            if (residual < tolerance)
            {
                break;
            }

            h -= f / fPrime;
            iteration++;
        }

        if (residual > tolerance)
        {
            throw new InvalidOperationException(
                $"Hyperbolic Kepler equation failed to converge in {maxIterations} iterations (residual {residual:E3}).");
        }

        return new KeplerSolution(h, iteration, residual);
    }

    // -------------------------------------------------------------------------
    // Anomaly conversions (elliptical orbits)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Eccentric anomaly E → true anomaly nu, using the half-angle relation
    /// tan(nu/2) = sqrt((1+e)/(1−e)) · tan(E/2).
    /// </summary>
    /// <remarks>
    /// The half-angle form is used rather than <c>cos nu = (cos E − e)/(1 − e cos E)</c>
    /// because <see cref="Math.Atan2"/> resolves the quadrant automatically, removing the
    /// need for an error-prone sign test.
    /// </remarks>
    public static double EccentricToTrueAnomaly(double eccentricAnomalyRad, double eccentricity)
    {
        Guard.InRange(eccentricity, 0.0, 1.0 - 1e-12, nameof(eccentricity));

        double factor = Math.Sqrt((1.0 + eccentricity) / (1.0 - eccentricity));
        double nu = 2.0 * Math.Atan2(
            factor * Math.Sin(eccentricAnomalyRad / 2.0),
            Math.Cos(eccentricAnomalyRad / 2.0));

        return Angles.Normalize(nu);
    }

    /// <summary>True anomaly nu → eccentric anomaly E (the inverse of <see cref="EccentricToTrueAnomaly"/>).</summary>
    public static double TrueToEccentricAnomaly(double trueAnomalyRad, double eccentricity)
    {
        Guard.InRange(eccentricity, 0.0, 1.0 - 1e-12, nameof(eccentricity));

        double factor = Math.Sqrt((1.0 - eccentricity) / (1.0 + eccentricity));
        double e = 2.0 * Math.Atan2(
            factor * Math.Sin(trueAnomalyRad / 2.0),
            Math.Cos(trueAnomalyRad / 2.0));

        return Angles.Normalize(e);
    }

    /// <summary>Eccentric anomaly E → mean anomaly M. This direction is Kepler's equation applied directly.</summary>
    public static double EccentricToMeanAnomaly(double eccentricAnomalyRad, double eccentricity)
        => Angles.Normalize(eccentricAnomalyRad - eccentricity * Math.Sin(eccentricAnomalyRad));

    /// <summary>True anomaly nu → mean anomaly M (composition of the two conversions above).</summary>
    public static double TrueToMeanAnomaly(double trueAnomalyRad, double eccentricity)
        => EccentricToMeanAnomaly(TrueToEccentricAnomaly(trueAnomalyRad, eccentricity), eccentricity);

    /// <summary>
    /// Mean anomaly M → true anomaly nu. Solves Kepler's equation, then converts.
    /// This is the operation you need to answer "where will the satellite be in 40 minutes?".
    /// </summary>
    public static double MeanToTrueAnomaly(double meanAnomalyRad, double eccentricity)
    {
        KeplerSolution solution = SolveEccentricAnomaly(meanAnomalyRad, eccentricity);
        return EccentricToTrueAnomaly(solution.AnomalyRad, eccentricity);
    }

    // -------------------------------------------------------------------------
    // Anomaly conversions (hyperbolic orbits)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Hyperbolic anomaly H → true anomaly nu, via
    /// tan(nu/2) = sqrt((e+1)/(e−1)) · tanh(H/2).
    /// </summary>
    public static double HyperbolicToTrueAnomaly(double hyperbolicAnomalyRad, double eccentricity)
    {
        if (eccentricity <= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(eccentricity), eccentricity, "Requires e > 1.");
        }

        double factor = Math.Sqrt((eccentricity + 1.0) / (eccentricity - 1.0));
        return 2.0 * Math.Atan(factor * Math.Tanh(hyperbolicAnomalyRad / 2.0));
    }

    /// <summary>True anomaly nu → hyperbolic anomaly H.</summary>
    public static double TrueToHyperbolicAnomaly(double trueAnomalyRad, double eccentricity)
    {
        if (eccentricity <= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(eccentricity), eccentricity, "Requires e > 1.");
        }

        double factor = Math.Sqrt((eccentricity - 1.0) / (eccentricity + 1.0));
        return 2.0 * Math.Atanh(factor * Math.Tan(trueAnomalyRad / 2.0));
    }

    // -------------------------------------------------------------------------
    // Time <-> anomaly
    // -------------------------------------------------------------------------

    /// <summary>
    /// Time since periapsis passage for a given true anomaly on an elliptical orbit [s].
    /// </summary>
    /// <remarks>t − t_p = M / n, with M from Kepler's equation and n = sqrt(mu/a^3).</remarks>
    public static double TimeSincePeriapsis(
        double trueAnomalyRad,
        double eccentricity,
        double semiMajorAxisKm,
        double mu = Constants.MuEarth)
    {
        double m = TrueToMeanAnomaly(trueAnomalyRad, eccentricity);
        double n = TwoBody.MeanMotion(semiMajorAxisKm, mu);
        return m / n;
    }

    /// <summary>
    /// The inverse: true anomaly reached a given time after periapsis passage [rad].
    /// </summary>
    public static double TrueAnomalyAfterTime(
        double timeSincePeriapsisSec,
        double eccentricity,
        double semiMajorAxisKm,
        double mu = Constants.MuEarth)
    {
        double n = TwoBody.MeanMotion(semiMajorAxisKm, mu);
        double m = n * timeSincePeriapsisSec;
        return MeanToTrueAnomaly(m, eccentricity);
    }

    /// <summary>
    /// Propagates a full element set forward (or backward) in time by advancing only the
    /// true anomaly — which is legitimate because, in the unperturbed two-body problem, the
    /// other five elements are constant.
    /// </summary>
    /// <param name="elements">Starting elements.</param>
    /// <param name="deltaTimeSec">Elapsed time [s]; may be negative to propagate backwards.</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <returns>The elements at the new epoch.</returns>
    public static OrbitalElements Propagate(
        OrbitalElements elements,
        double deltaTimeSec,
        double mu = Constants.MuEarth)
    {
        if (elements.Eccentricity >= 1.0)
        {
            throw new NotSupportedException(
                "Use UniversalVariables.Propagate for open (parabolic/hyperbolic) trajectories.");
        }

        double n = TwoBody.MeanMotion(elements.SemiMajorAxisKm, mu);
        double m0 = TrueToMeanAnomaly(elements.TrueAnomalyRad, elements.Eccentricity);
        double m1 = m0 + n * deltaTimeSec;   // M advances linearly — that is the whole point of M
        double nu1 = MeanToTrueAnomaly(m1, elements.Eccentricity);

        return elements.AtTrueAnomaly(nu1);
    }
}
