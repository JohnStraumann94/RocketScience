namespace Astro.Core;

/// <summary>
/// Chapter 4 (advanced) — the <b>universal variable formulation</b> of the Kepler problem.
/// </summary>
/// <remarks>
/// <para>
/// The classical approach needs a different equation for each conic: Kepler's equation for
/// ellipses, Barker's for parabolas, the hyperbolic Kepler equation for hyperbolas. Worse,
/// all three degrade badly near e = 1, which is exactly where interesting missions live
/// (escape trajectories, transfer arcs, comet orbits).
/// </para>
/// <para>
/// Sundman's <b>universal anomaly</b> chi removes the problem entirely. By changing the
/// independent variable via dt = (r / sqrt(mu)) dchi, a <i>single</i> equation covers every
/// conic section continuously, with no special cases and no loss of accuracy at e = 1:
/// </para>
/// <code>
///     sqrt(mu) dt = chi^3 C(psi) + (r0 · v0 / sqrt(mu)) chi^2 S(psi) + r0 chi (1 − psi C(psi))
/// </code>
/// <para>
/// where psi = chi^2 / a and C, S are the Stumpff functions. This is the formulation used by
/// essentially every production astrodynamics package, and it is what makes
/// <see cref="Lambert"/> possible.
/// </para>
/// </remarks>
public static class UniversalVariables
{
    /// <summary>
    /// <b>Stumpff function C(psi)</b>, defined by the entire series
    /// C(psi) = 1/2! − psi/4! + psi^2/6! − ...
    /// </summary>
    /// <remarks>
    /// <para>Closed forms exist for each sign of psi:</para>
    /// <list type="bullet">
    ///   <item><description>psi &gt; 0 (ellipse): C = (1 − cos(sqrt(psi))) / psi</description></item>
    ///   <item><description>psi &lt; 0 (hyperbola): C = (cosh(sqrt(−psi)) − 1) / (−psi)</description></item>
    ///   <item><description>psi = 0 (parabola): C = 1/2</description></item>
    /// </list>
    /// <para>
    /// Both closed forms are 0/0 indeterminate at psi = 0, so near the origin we fall back on
    /// the truncated power series. Without that guard the parabolic case — precisely the one
    /// universal variables were invented to handle — would produce NaN.
    /// </para>
    /// </remarks>
    public static double StumpffC(double psi)
    {
        const double nearZero = 1e-6;

        if (psi > nearZero)
        {
            double sqrtPsi = Math.Sqrt(psi);
            return (1.0 - Math.Cos(sqrtPsi)) / psi;
        }

        if (psi < -nearZero)
        {
            double sqrtNegPsi = Math.Sqrt(-psi);
            return (Math.Cosh(sqrtNegPsi) - 1.0) / -psi;
        }

        // Series expansion around psi = 0: 1/2 - psi/24 + psi^2/720 - ...
        return 0.5 - psi / 24.0 + psi * psi / 720.0;
    }

    /// <summary>
    /// <b>Stumpff function S(psi)</b>, defined by S(psi) = 1/3! − psi/5! + psi^2/7! − ...
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>psi &gt; 0: S = (sqrt(psi) − sin(sqrt(psi))) / psi^(3/2)</description></item>
    ///   <item><description>psi &lt; 0: S = (sinh(sqrt(−psi)) − sqrt(−psi)) / (−psi)^(3/2)</description></item>
    ///   <item><description>psi = 0: S = 1/6</description></item>
    /// </list>
    /// The same near-zero series guard applies, and here it matters even more: for small psi
    /// the numerator is the difference of two nearly equal quantities, so the closed form
    /// suffers catastrophic cancellation well before psi actually reaches zero.
    /// </remarks>
    public static double StumpffS(double psi)
    {
        const double nearZero = 1e-6;

        if (psi > nearZero)
        {
            double sqrtPsi = Math.Sqrt(psi);
            return (sqrtPsi - Math.Sin(sqrtPsi)) / Math.Pow(psi, 1.5);
        }

        if (psi < -nearZero)
        {
            double sqrtNegPsi = Math.Sqrt(-psi);
            return (Math.Sinh(sqrtNegPsi) - sqrtNegPsi) / Math.Pow(-psi, 1.5);
        }

        // Series expansion around psi = 0: 1/6 - psi/120 + psi^2/5040 - ...
        return 1.0 / 6.0 - psi / 120.0 + psi * psi / 5040.0;
    }

    /// <summary>
    /// Propagates a state vector forward or backward in time by solving the universal
    /// Kepler equation — valid for <i>every</i> conic section.
    /// </summary>
    /// <param name="initial">The state at the initial epoch.</param>
    /// <param name="deltaTimeSec">Elapsed time [s]; negative propagates into the past.</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <param name="tolerance">Convergence tolerance on the universal anomaly.</param>
    /// <param name="maxIterations">Iteration cap.</param>
    /// <returns>The state at the new epoch.</returns>
    /// <remarks>
    /// <para>
    /// After chi is found, the new state is reconstructed with the <b>Lagrange f and g
    /// coefficients</b>:
    /// </para>
    /// <code>
    ///     r = f r0 + g v0
    ///     v = f' r0 + g' v0
    /// </code>
    /// <para>
    /// These express the new state as a linear combination of the old position and velocity —
    /// which is possible because the motion is planar, so r0 and v0 span the whole orbit plane.
    /// </para>
    /// </remarks>
    public static StateVector Propagate(
        StateVector initial,
        double deltaTimeSec,
        double mu = Constants.MuEarth,
        double tolerance = 1e-10,
        int maxIterations = 200)
    {
        Guard.Positive(mu, nameof(mu));

        Vector3 r0 = initial.PositionKm;
        Vector3 v0 = initial.VelocityKmPerSec;

        double r0Mag = r0.Magnitude;
        double v0Mag = v0.Magnitude;
        double sqrtMu = Math.Sqrt(mu);

        // alpha = 1/a, obtained from the energy. Its sign is the conic classifier:
        // alpha > 0 ellipse, alpha = 0 parabola, alpha < 0 hyperbola.
        double alpha = 2.0 / r0Mag - v0Mag * v0Mag / mu;

        double rDotV = Vector3.Dot(r0, v0);

        // ---- Initial guess for chi, chosen by conic type (Vallado, Algorithm 8) ----
        double chi;
        if (alpha > 1e-12)
        {
            // Elliptical: chi advances roughly linearly with time.
            chi = sqrtMu * deltaTimeSec * alpha;
        }
        else if (Math.Abs(alpha) <= 1e-12)
        {
            // Parabolic: solve Barker's equation for the starting value.
            Vector3 h = Vector3.Cross(r0, v0);
            double p = h.MagnitudeSquared / mu;
            double s = 0.5 * Math.Atan(1.0 / (3.0 * Math.Sqrt(mu / (p * p * p)) * deltaTimeSec));
            double w = Math.Atan(Math.Cbrt(Math.Tan(s)));
            chi = Math.Sqrt(p) * 2.0 / Math.Tan(2.0 * w);
        }
        else
        {
            // Hyperbolic: a logarithmic guess, because chi grows only logarithmically with dt.
            double a = 1.0 / alpha;   // negative
            chi = Math.Sign(deltaTimeSec) * Math.Sqrt(-a) * Math.Log(
                -2.0 * mu * alpha * deltaTimeSec /
                (rDotV + Math.Sign(deltaTimeSec) * Math.Sqrt(-mu * a) * (1.0 - r0Mag * alpha)));
        }

        // ---- Newton iteration on the universal Kepler equation ----
        double psi = 0.0;
        double c2 = 0.5;
        double c3 = 1.0 / 6.0;
        double r = r0Mag;
        int iteration = 0;

        for (; iteration < maxIterations; iteration++)
        {
            psi = chi * chi * alpha;
            c2 = StumpffC(psi);
            c3 = StumpffS(psi);

            // r(chi) — needed both as the derivative dt/dchi and for the final f,g terms.
            r = chi * chi * c2
              + (rDotV / sqrtMu) * chi * (1.0 - psi * c3)
              + r0Mag * (1.0 - psi * c2);

            // The universal Kepler equation residual, expressed in units of sqrt(mu)*time.
            double residual = sqrtMu * deltaTimeSec
                            - chi * chi * chi * c3
                            - (rDotV / sqrtMu) * chi * chi * c2
                            - r0Mag * chi * (1.0 - psi * c3);

            double chiNew = chi + residual / r;

            if (Math.Abs(chiNew - chi) < tolerance)
            {
                chi = chiNew;

                // Recompute the auxiliaries at the converged chi so f/g are consistent.
                psi = chi * chi * alpha;
                c2 = StumpffC(psi);
                c3 = StumpffS(psi);
                r = chi * chi * c2
                  + (rDotV / sqrtMu) * chi * (1.0 - psi * c3)
                  + r0Mag * (1.0 - psi * c2);
                break;
            }

            chi = chiNew;
        }

        if (iteration >= maxIterations)
        {
            throw new InvalidOperationException(
                $"Universal Kepler propagation failed to converge in {maxIterations} iterations.");
        }

        // ---- Lagrange f and g coefficients ----
        double f = 1.0 - (chi * chi / r0Mag) * c2;
        double g = deltaTimeSec - (chi * chi * chi / sqrtMu) * c3;
        double gDot = 1.0 - (chi * chi / r) * c2;
        double fDot = (sqrtMu / (r * r0Mag)) * chi * (psi * c3 - 1.0);

        Vector3 rNew = f * r0 + g * v0;
        Vector3 vNew = fDot * r0 + gDot * v0;

        return new StateVector(rNew, vNew);
    }
}
