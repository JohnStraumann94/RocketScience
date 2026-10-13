namespace Astro.Core;

/// <summary>
/// A position/velocity pair expressed in an inertial Cartesian frame (for Earth orbits,
/// ECI / IJK; for heliocentric orbits, the ecliptic frame).
/// </summary>
/// <param name="PositionKm">r — position vector [km].</param>
/// <param name="VelocityKmPerSec">v — inertial velocity vector [km/s].</param>
public readonly record struct StateVector(Vector3 PositionKm, Vector3 VelocityKmPerSec)
{
    /// <summary>r — scalar distance from the centre of the central body [km].</summary>
    public double RadiusKm => PositionKm.Magnitude;

    /// <summary>v — scalar inertial speed [km/s].</summary>
    public double SpeedKmPerSec => VelocityKmPerSec.Magnitude;

    /// <inheritdoc/>
    public override string ToString() =>
        $"""
         r = {PositionKm.ToString("F4")} km   |r| = {RadiusKm:F4} km
         v = {VelocityKmPerSec.ToString("F6")} km/s |v| = {SpeedKmPerSec:F6} km/s
         """;
}

/// <summary>
/// Chapter 2 — <b>Orbit Determination from a Single Observation</b>: the two conversions
/// between a Cartesian state vector (r, v) and the classical orbital elements.
/// </summary>
/// <remarks>
/// These two routines are the workhorses of any astrodynamics library. Everything else
/// (propagation, manoeuvre planning, targeting) tends to be a sandwich:
/// state → elements → do something clever → elements → state.
/// </remarks>
public static class OrbitConversions
{
    /// <summary>
    /// Tolerance used to decide whether an orbit is "circular" or "equatorial". A value of
    /// 1e-8 corresponds to roughly 2 milli-arcseconds of inclination — far below the noise
    /// floor of any real orbit determination, but comfortably above double-precision fuzz.
    /// </summary>
    private const double SingularityTolerance = 1e-8;

    /// <summary>
    /// <b>State vector → classical orbital elements</b> (the "rv2coe" algorithm).
    /// </summary>
    /// <param name="state">Inertial position and velocity.</param>
    /// <param name="mu">Gravitational parameter of the central body [km^3/s^2].</param>
    /// <returns>The six classical elements, angles in radians.</returns>
    /// <remarks>
    /// <para>The derivation, step by step:</para>
    /// <list type="number">
    ///   <item><description>
    ///     <b>h = r x v</b> — the specific angular momentum. It is normal to the orbit
    ///     plane, so its direction fixes both i and Omega.
    ///   </description></item>
    ///   <item><description>
    ///     <b>n = K x h</b> — the node vector, pointing at the ascending node (where the
    ///     satellite crosses the equator going north). K is the polar unit vector.
    ///   </description></item>
    ///   <item><description>
    ///     <b>e = [(v^2 − mu/r) r − (r·v) v] / mu</b> — the eccentricity vector, which
    ///     points from the focus towards periapsis and whose magnitude is e. This is a
    ///     third constant of the motion (the Laplace–Runge–Lenz vector, per unit mu).
    ///   </description></item>
    ///   <item><description>
    ///     Each angle then comes from a dot product plus a <i>quadrant check</i>, because
    ///     arccos only returns [0, pi] and cannot tell 60 degrees from 300 degrees.
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Degenerate cases.</b> When the orbit is equatorial the node vector vanishes, and
    /// when it is circular the eccentricity vector vanishes; the corresponding angles are
    /// then undefined. Rather than return NaN we fall back on the standard substitutions
    /// (true longitude / argument of latitude / longitude of periapsis) collapsed into the
    /// same six slots, so downstream code — and especially <see cref="ToStateVector"/> —
    /// still round-trips exactly.
    /// </para>
    /// </remarks>
    public static OrbitalElements ToOrbitalElements(StateVector state, double mu = Constants.MuEarth)
    {
        Guard.Positive(mu, nameof(mu));

        Vector3 r = state.PositionKm;
        Vector3 v = state.VelocityKmPerSec;

        double rMag = r.Magnitude;
        double vMag = v.Magnitude;
        if (rMag <= 0.0)
        {
            throw new ArgumentException("Position vector must be non-zero.", nameof(state));
        }

        // --- Step 1: angular momentum ------------------------------------------------
        Vector3 h = Vector3.Cross(r, v);
        double hMag = h.Magnitude;
        if (hMag <= 0.0)
        {
            // r and v parallel => a purely radial "orbit" (straight up or straight down).
            // It is a legitimate degenerate conic but has no orbital plane, so no elements.
            throw new ArgumentException(
                "Position and velocity are parallel (rectilinear trajectory); orbital elements are undefined.",
                nameof(state));
        }

        // --- Step 2: node vector, n = K x h ------------------------------------------
        // Written out by hand because K = (0,0,1) makes two of the three cross-product
        // terms vanish: K x h = (-h_y, h_x, 0).
        Vector3 n = new(-h.Y, h.X, 0.0);
        double nMag = n.Magnitude;

        // --- Step 3: eccentricity vector ---------------------------------------------
        double rDotV = Vector3.Dot(r, v);
        Vector3 eVec = ((vMag * vMag - mu / rMag) * r - rDotV * v) / mu;
        double e = eVec.Magnitude;

        // --- Step 4: size, from the energy -------------------------------------------
        double energy = vMag * vMag / 2.0 - mu / rMag;
        double a = Math.Abs(energy) < 1e-14
            ? double.PositiveInfinity   // parabolic
            : -mu / (2.0 * energy);

        // --- Step 5: inclination ------------------------------------------------------
        // i is the angle between h and the polar axis; it is always in [0, pi], so no
        // quadrant check is needed. i > 90 deg means the orbit is retrograde.
        double i = Angles.SafeAcos(h.Z / hMag);

        bool isEquatorial = nMag < SingularityTolerance;
        bool isCircular = e < SingularityTolerance;

        double raan;
        double argumentOfPeriapsis;
        double trueAnomaly;

        if (!isEquatorial && !isCircular)
        {
            // ---------- The general, well-behaved case -------------------------------
            // Omega: angle from the X axis (vernal equinox) round to the node vector.
            // Quadrant test: if the node vector has a negative Y component the node lies
            // in the western half of the equatorial plane, i.e. Omega > 180 deg.
            raan = Angles.SafeAcos(n.X / nMag);
            if (n.Y < 0.0)
            {
                raan = Constants.TwoPi - raan;
            }

            // omega: angle from the node round to periapsis, measured in the orbit plane.
            // Quadrant test: periapsis is below the equator (e_z < 0) => omega > 180 deg.
            argumentOfPeriapsis = Angles.SafeAcos(Vector3.Dot(n, eVec) / (nMag * e));
            if (eVec.Z < 0.0)
            {
                argumentOfPeriapsis = Constants.TwoPi - argumentOfPeriapsis;
            }

            // nu: angle from periapsis round to the satellite.
            // Quadrant test: r·v > 0 means the radius is growing, so the satellite is on
            // the outbound leg between periapsis and apoapsis => nu < 180 deg.
            trueAnomaly = Angles.SafeAcos(Vector3.Dot(eVec, r) / (e * rMag));
            if (rDotV < 0.0)
            {
                trueAnomaly = Constants.TwoPi - trueAnomaly;
            }
        }
        else if (isEquatorial && !isCircular)
        {
            // ---------- Elliptical equatorial: no node, so Omega is undefined ---------
            // Convention: set Omega = 0 and absorb everything into the "longitude of
            // periapsis" omega_true, measured from the X axis directly.
            raan = 0.0;

            argumentOfPeriapsis = Angles.SafeAcos(eVec.X / e);
            if (eVec.Y < 0.0)
            {
                argumentOfPeriapsis = Constants.TwoPi - argumentOfPeriapsis;
            }

            // For a retrograde equatorial orbit the angle runs the other way.
            if (h.Z < 0.0)
            {
                argumentOfPeriapsis = Constants.TwoPi - argumentOfPeriapsis;
            }

            trueAnomaly = Angles.SafeAcos(Vector3.Dot(eVec, r) / (e * rMag));
            if (rDotV < 0.0)
            {
                trueAnomaly = Constants.TwoPi - trueAnomaly;
            }
        }
        else if (!isEquatorial)
        {
            // ---------- Circular inclined: no periapsis, so omega is undefined --------
            // Convention: set omega = 0 and report the "argument of latitude" u — the angle
            // from the ascending node to the satellite — in the true-anomaly slot.
            raan = Angles.SafeAcos(n.X / nMag);
            if (n.Y < 0.0)
            {
                raan = Constants.TwoPi - raan;
            }

            argumentOfPeriapsis = 0.0;

            trueAnomaly = Angles.SafeAcos(Vector3.Dot(n, r) / (nMag * rMag));
            if (r.Z < 0.0)
            {
                trueAnomaly = Constants.TwoPi - trueAnomaly;
            }
        }
        else
        {
            // ---------- Circular equatorial: neither node nor periapsis --------------
            // Only one angle survives: the "true longitude" lambda, measured from X to r.
            raan = 0.0;
            argumentOfPeriapsis = 0.0;

            trueAnomaly = Angles.SafeAcos(r.X / rMag);
            if (r.Y < 0.0)
            {
                trueAnomaly = Constants.TwoPi - trueAnomaly;
            }

            if (h.Z < 0.0)
            {
                trueAnomaly = Constants.TwoPi - trueAnomaly;
            }
        }

        return new OrbitalElements(
            a,
            e,
            i,
            Angles.Normalize(raan),
            Angles.Normalize(argumentOfPeriapsis),
            Angles.Normalize(trueAnomaly));
    }

    /// <summary>
    /// <b>Classical orbital elements → state vector</b> (the "coe2rv" algorithm).
    /// </summary>
    /// <param name="elements">The six classical elements (angles in radians).</param>
    /// <param name="mu">Gravitational parameter of the central body [km^3/s^2].</param>
    /// <returns>Inertial position [km] and velocity [km/s].</returns>
    /// <remarks>
    /// <para>Two stages:</para>
    /// <list type="number">
    ///   <item><description>
    ///     Build r and v in the <b>perifocal (PQW) frame</b>, whose P axis points at
    ///     periapsis and whose W axis is along h. In that frame the algebra is trivial:
    ///     <code>
    ///       r_pqw = (p / (1 + e cos nu)) * [cos nu, sin nu, 0]
    ///       v_pqw = sqrt(mu / p)         * [-sin nu, e + cos nu, 0]
    ///     </code>
    ///   </description></item>
    ///   <item><description>
    ///     Rotate into the inertial frame with the classical 3-1-3 Euler sequence
    ///     <c>R3(−Omega) · R1(−i) · R3(−omega)</c>. The composite matrix is written out
    ///     explicitly below to avoid three separate matrix multiplications.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public static StateVector ToStateVector(OrbitalElements elements, double mu = Constants.MuEarth)
    {
        Guard.Positive(mu, nameof(mu));

        double e = elements.Eccentricity;
        Guard.NonNegative(e, nameof(elements.Eccentricity));

        // p = a(1 - e^2). Correct for ellipses AND hyperbolas (a < 0 flips both signs).
        double p = elements.SemiLatusRectumKm;
        if (double.IsInfinity(elements.SemiMajorAxisKm))
        {
            throw new ArgumentException(
                "For a parabolic orbit, specify the semi-latus rectum directly rather than a = infinity.",
                nameof(elements));
        }

        Guard.Positive(p, "semi-latus rectum p = a(1 - e^2)");

        double nu = elements.TrueAnomalyRad;
        double cosNu = Math.Cos(nu);
        double sinNu = Math.Sin(nu);

        // --- Stage 1: perifocal frame ------------------------------------------------
        double rMag = p / (1.0 + e * cosNu);
        Vector3 rPqw = new(rMag * cosNu, rMag * sinNu, 0.0);

        double sqrtMuOverP = Math.Sqrt(mu / p);
        Vector3 vPqw = new(-sqrtMuOverP * sinNu, sqrtMuOverP * (e + cosNu), 0.0);

        // --- Stage 2: rotate PQW -> IJK ----------------------------------------------
        double cosO = Math.Cos(elements.RaanRad);
        double sinO = Math.Sin(elements.RaanRad);
        double cosW = Math.Cos(elements.ArgumentOfPeriapsisRad);
        double sinW = Math.Sin(elements.ArgumentOfPeriapsisRad);
        double cosI = Math.Cos(elements.InclinationRad);
        double sinI = Math.Sin(elements.InclinationRad);

        // Rows of the composite rotation matrix R = R3(-Omega) R1(-i) R3(-omega).
        Vector3 row0 = new(
            cosO * cosW - sinO * sinW * cosI,
            -cosO * sinW - sinO * cosW * cosI,
            sinO * sinI);

        Vector3 row1 = new(
            sinO * cosW + cosO * sinW * cosI,
            -sinO * sinW + cosO * cosW * cosI,
            -cosO * sinI);

        Vector3 row2 = new(
            sinW * sinI,
            cosW * sinI,
            cosI);

        Vector3 r = new(Vector3.Dot(row0, rPqw), Vector3.Dot(row1, rPqw), Vector3.Dot(row2, rPqw));
        Vector3 v = new(Vector3.Dot(row0, vPqw), Vector3.Dot(row1, vPqw), Vector3.Dot(row2, vPqw));

        return new StateVector(r, v);
    }
}
