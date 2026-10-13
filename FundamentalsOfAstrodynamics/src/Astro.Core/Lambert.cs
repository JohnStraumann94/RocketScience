namespace Astro.Core;

/// <summary>
/// Which way round the central body the transfer arc travels.
/// </summary>
public enum TransferDirection
{
    /// <summary>
    /// "Short way" — the swept angle is less than 180 degrees. Prograde in the sense that
    /// the arc follows the direction implied by r1 x r2.
    /// </summary>
    ShortWay,

    /// <summary>
    /// "Long way" — the swept angle exceeds 180 degrees. Occasionally cheaper, and sometimes
    /// the only option when the short-way solution would fly through the central body.
    /// </summary>
    LongWay
}

/// <summary>
/// The solution of Lambert's problem: the two velocity vectors that connect the given
/// positions in the given time.
/// </summary>
/// <param name="DepartureVelocityKmPerSec">v1 — the velocity required at r1 [km/s].</param>
/// <param name="ArrivalVelocityKmPerSec">v2 — the velocity you will have on arrival at r2 [km/s].</param>
/// <param name="Iterations">Number of bisection steps used.</param>
public readonly record struct LambertSolution(
    Vector3 DepartureVelocityKmPerSec,
    Vector3 ArrivalVelocityKmPerSec,
    int Iterations);

/// <summary>
/// Chapter 5 — <b>Orbit Determination from Two Positions and Time</b>, universally known as
/// <b>Lambert's problem</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The question.</b> Given a departure point r1, an arrival point r2, and the time of
/// flight between them, what orbit connects the two? Equivalently: what velocity must I have
/// at r1 to arrive at r2 exactly dt seconds later?
/// </para>
/// <para>
/// <b>Why it matters.</b> Lambert's problem is the targeting engine of spaceflight. Every
/// interplanetary porkchop plot is thousands of Lambert solutions on a grid of departure and
/// arrival dates. Every rendezvous burn, every intercept, every trajectory correction is
/// posed as a Lambert problem.
/// </para>
/// <para>
/// <b>Lambert's theorem</b> (1761) states that the transfer time depends only on the
/// semi-major axis, the chord length between the two points, and the sum r1 + r2 — remarkably,
/// not on the shape of the arc in any other way.
/// </para>
/// <para>
/// The implementation below is the universal-variable formulation with bisection on psi
/// (Bate/Mueller/White Chapter 5, refined by Vallado). Bisection is used in preference to
/// Newton's method because the time-of-flight function is monotonic in psi but has an
/// awkward derivative near the parabolic boundary; bisection is slower but cannot diverge.
/// </para>
/// </remarks>
public static class Lambert
{
    /// <summary>
    /// Solves Lambert's problem for a single-revolution transfer.
    /// </summary>
    /// <param name="position1Km">r1 — departure position vector [km].</param>
    /// <param name="position2Km">r2 — arrival position vector [km].</param>
    /// <param name="timeOfFlightSec">dt — time of flight [s]; must be positive.</param>
    /// <param name="direction">Short way (&lt; 180 deg) or long way (&gt; 180 deg).</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <param name="tolerance">Convergence tolerance on the time of flight [s].</param>
    /// <param name="maxIterations">Bisection iteration cap.</param>
    /// <returns>The departure and arrival velocity vectors.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the geometry is degenerate — in particular when r1 and r2 are collinear
    /// (a swept angle of exactly 0 or 180 degrees), for which the transfer plane is undefined.
    /// </exception>
    public static LambertSolution Solve(
        Vector3 position1Km,
        Vector3 position2Km,
        double timeOfFlightSec,
        TransferDirection direction = TransferDirection.ShortWay,
        double mu = Constants.MuEarth,
        double tolerance = 1e-6,
        int maxIterations = 200)
    {
        Guard.Positive(timeOfFlightSec, nameof(timeOfFlightSec));
        Guard.Positive(mu, nameof(mu));

        double r1 = position1Km.Magnitude;
        double r2 = position2Km.Magnitude;
        Guard.Positive(r1, nameof(position1Km));
        Guard.Positive(r2, nameof(position2Km));

        // Swept angle between the two position vectors.
        double cosDeltaNu = Math.Clamp(Vector3.Dot(position1Km, position2Km) / (r1 * r2), -1.0, 1.0);
        double deltaNu = Math.Acos(cosDeltaNu);

        // The long-way solution sweeps the reflex angle instead.
        if (direction == TransferDirection.LongWay)
        {
            deltaNu = Constants.TwoPi - deltaNu;
        }

        // 'A' is a geometry constant combining the chord and the radii. It is the term that
        // couples the two ends of the arc together.
        double sinDeltaNu = Math.Sin(deltaNu);
        double a = sinDeltaNu * Math.Sqrt(r1 * r2 / (1.0 - Math.Cos(deltaNu)));

        if (double.IsNaN(a) || Math.Abs(a) < 1e-12)
        {
            throw new ArgumentException(
                "Degenerate Lambert geometry: the position vectors are collinear (swept angle of 0 or 180 degrees), " +
                "so the transfer plane is undefined. Perturb one of the positions slightly.");
        }

        // ---- Bisection bracket on psi = chi^2 / a ----
        // Upper bound 4*pi^2 corresponds to one full revolution; the lower bound is pushed
        // down as needed to keep the auxiliary variable y positive.
        double psi = 0.0;
        double psiUpper = 4.0 * Math.PI * Math.PI;
        double psiLower = -4.0 * Math.PI;

        double c2 = 0.5;
        double c3 = 1.0 / 6.0;
        double y = 0.0;
        int iteration = 0;

        for (; iteration < maxIterations; iteration++)
        {
            c2 = UniversalVariables.StumpffC(psi);
            c3 = UniversalVariables.StumpffS(psi);

            // y is the "effective chord" term; it must stay positive for sqrt(y) to be real.
            y = r1 + r2 + a * (psi * c3 - 1.0) / Math.Sqrt(c2);

            // If A > 0 the geometry can still drive y negative for very small psi. Raise the
            // lower bracket until y becomes positive again, as prescribed by Vallado.
            if (a > 0.0 && y < 0.0)
            {
                int rescue = 0;
                while (y < 0.0 && rescue < 100)
                {
                    psiLower += 0.1;
                    psi = psiLower;
                    c2 = UniversalVariables.StumpffC(psi);
                    c3 = UniversalVariables.StumpffS(psi);
                    y = r1 + r2 + a * (psi * c3 - 1.0) / Math.Sqrt(c2);
                    rescue++;
                }
            }

            double chi = Math.Sqrt(y / c2);

            // Time of flight implied by the current psi.
            double timeOfFlightComputed = (chi * chi * chi * c3 + a * Math.Sqrt(y)) / Math.Sqrt(mu);

            if (Math.Abs(timeOfFlightComputed - timeOfFlightSec) < tolerance)
            {
                break;
            }

            // Time of flight increases monotonically with psi, so the update is a plain bisection.
            if (timeOfFlightComputed <= timeOfFlightSec)
            {
                psiLower = psi;
            }
            else
            {
                psiUpper = psi;
            }

            psi = (psiUpper + psiLower) / 2.0;
        }

        if (iteration >= maxIterations)
        {
            throw new InvalidOperationException(
                $"Lambert solver failed to converge in {maxIterations} iterations. " +
                "The requested time of flight may be physically impossible for this geometry.");
        }

        // ---- Recover the velocities from the Lagrange coefficients ----
        // Note f, g and gDot follow directly from y; fDot is not needed.
        double f = 1.0 - y / r1;
        double g = a * Math.Sqrt(y / mu);
        double gDot = 1.0 - y / r2;

        Vector3 v1 = (position2Km - f * position1Km) / g;
        Vector3 v2 = (gDot * position2Km - position1Km) / g;

        return new LambertSolution(v1, v2, iteration);
    }
}
