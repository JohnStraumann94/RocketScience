"""
two_body.py — Chapter 1: Two-Body Orbital Mechanics.
=====================================================

Port of ``Astro.Core/TwoBody.cs``.

Everything here flows from the two-body equation of motion, obtained by
applying Newton's law of universal gravitation to two point masses and
neglecting all other forces::

    r'' + (μ / r³) · r = 0

Two conserved quantities fall straight out of that differential equation and
they are the backbone of the whole subject:

1. **Specific angular momentum** h = r × v  (a vector).
   Constant ⟹ the motion is confined to a fixed plane.

2. **Specific mechanical energy** ε = v²/2 − μ/r  (a scalar).
   Constant ⟹ speed is determined by radius alone, which is the vis-viva
   equation.

"Specific" simply means "per unit mass of the satellite" — which is why the
satellite's own mass never appears.  A feather and a space station in the same
orbit move identically.
"""

import math
from . import constants as _C, guard as _G


def specific_energy(
    radius_km: float,
    speed_km_per_sec: float,
    mu: float = _C.MU_EARTH,
) -> float:
    """
    Specific mechanical energy, ε = v²/2 − μ/r  [km²/s²].

    Args:
        radius_km:         Distance from the centre of the central body, r [km].
        speed_km_per_sec:  Inertial speed, v [km/s].
        mu:                Gravitational parameter of the central body [km³/s²].

    Returns:
        The specific orbital energy.  Its *sign* classifies the orbit outright:
        negative ⟹ closed (circle or ellipse), zero ⟹ parabolic escape,
        positive ⟹ hyperbolic.

    Note that the potential energy is −μ/r (inverse-first-power), not −μ/r²:
    the inverse-square law applies to *force*, the inverse-first-power to
    *energy*.
    """
    _G.positive(radius_km, "radius_km")
    _G.positive(mu, "mu")
    return (speed_km_per_sec ** 2) / 2.0 - mu / radius_km


def semi_major_axis_from_energy(
    specific_energy: float,
    mu: float = _C.MU_EARTH,
) -> float:
    """
    Semi-major axis derived from the specific energy: a = −μ / (2ε)  [km].

    Args:
        specific_energy: ε [km²/s²].
        mu:              Gravitational parameter [km³/s²].

    Returns:
        Semi-major axis *a* [km].  For a hyperbola the result is negative (the
        standard sign convention, making vis-viva work unchanged for all conic
        types).  Returns ``math.inf`` when ε = 0 (parabolic case).

    This is the inverse of :func:`specific_energy` and shows that energy and
    size are two views of the same fact.
    """
    if abs(specific_energy) < 1e-15:
        # ε = 0 is the parabolic case: a is infinite, so there is no meaningful value.
        return math.inf
    return -mu / (2.0 * specific_energy)


def vis_viva(
    radius_km: float,
    semi_major_axis_km: float,
    mu: float = _C.MU_EARTH,
) -> float:
    """
    The **vis-viva** ("living force") equation: v = √(μ(2/r − 1/a))  [km/s].

    Arguably the single most useful equation in astrodynamics.  Every
    impulsive-manoeuvre calculation in :mod:`maneuvers` is just vis-viva
    evaluated twice and subtracted.

    Args:
        radius_km:         Current radius r [km].
        semi_major_axis_km: Semi-major axis a [km].  Positive for ellipses,
                            negative for hyperbolas, and ``math.inf`` for a
                            parabola (which correctly degenerates to the
                            escape-speed formula because 1/a → 0).
        mu:                Gravitational parameter [km³/s²].

    Returns:
        The speed at that radius [km/s].

    Raises:
        ValueError: When the requested radius is unreachable on the given orbit
                    (e.g. r > apoapsis on a bound ellipse), which would make
                    v² < 0.
    """
    _G.positive(radius_km, "radius_km")
    _G.positive(mu, "mu")

    # When a = ∞ (parabola), 1/a → 0; vis-viva reduces to the escape formula.
    inverse_a = 0.0 if math.isinf(semi_major_axis_km) else 1.0 / semi_major_axis_km
    v_squared = mu * (2.0 / radius_km - inverse_a)

    if v_squared < 0.0:
        raise ValueError(
            f"Radius {radius_km:.3f} km is not reachable on an orbit with "
            f"a = {semi_major_axis_km:.3f} km."
        )
    return math.sqrt(v_squared)


def circular_speed(
    radius_km: float,
    mu: float = _C.MU_EARTH,
) -> float:
    """
    Speed of a circular orbit, v_c = √(μ/r)  [km/s].

    Args:
        radius_km: Orbital radius r [km].
        mu:        Gravitational parameter [km³/s²].

    Returns:
        The circular speed [km/s].

    This is a special case of vis-viva with r = a.
    """
    _G.positive(radius_km, "radius_km")
    return math.sqrt(mu / radius_km)


def escape_speed(
    radius_km: float,
    mu: float = _C.MU_EARTH,
) -> float:
    """
    Local escape (parabolic) speed, v_esc = √(2μ/r)  [km/s].

    Args:
        radius_km: Radius r [km].
        mu:        Gravitational parameter [km³/s²].

    Returns:
        The escape speed [km/s].

    Exactly √2 times the circular speed at the same radius — a ratio worth
    memorising, since it means only a 41.4% speed increase separates "in
    orbit" from "gone forever".
    """
    _G.positive(radius_km, "radius_km")
    return math.sqrt(2.0 * mu / radius_km)


def period(
    semi_major_axis_km: float,
    mu: float = _C.MU_EARTH,
) -> float:
    """
    Orbital period from Kepler's third law, T = 2π √(a³/μ)  [s].

    Args:
        semi_major_axis_km: Semi-major axis a [km].  Must be positive and finite
                            (i.e. the orbit is elliptical or circular).
        mu:                 Gravitational parameter [km³/s²].

    Returns:
        The period T [s].

    Raises:
        ValueError: For a non-positive or infinite semi-major axis: open
                    (parabolic/hyperbolic) trajectories never repeat and so
                    have no period.
    """
    if semi_major_axis_km <= 0.0 or math.isinf(semi_major_axis_km):
        raise ValueError(
            "Only closed (elliptical) orbits have a period; "
            "a must be finite and positive."
        )
    return _C.TWO_PI * math.sqrt(semi_major_axis_km ** 3 / mu)


def mean_motion(
    semi_major_axis_km: float,
    mu: float = _C.MU_EARTH,
) -> float:
    """
    Mean motion, n = √(μ/a³)  [rad/s] — the average angular rate around the orbit.

    Args:
        semi_major_axis_km: Semi-major axis a [km].
        mu:                 Gravitational parameter [km³/s²].

    Returns:
        Mean motion n [rad/s].
    """
    _G.positive(semi_major_axis_km, "semi_major_axis_km")
    return math.sqrt(mu / semi_major_axis_km ** 3)


def radius_at_true_anomaly(
    semi_latus_rectum_km: float,
    eccentricity: float,
    true_anomaly_rad: float,
) -> float:
    """
    The polar equation of a conic ("the orbit equation"):
    r = p / (1 + e cos ν)  [km].

    Args:
        semi_latus_rectum_km: Semi-latus rectum p = h²/μ = a(1 − e²) [km].
        eccentricity:         Eccentricity e [dimensionless].
        true_anomaly_rad:     True anomaly ν, measured from periapsis [rad].

    Returns:
        The radius r [km].

    Raises:
        ValueError: When ν lies beyond the asymptote of an open orbit.

    This one equation contains all four conic sections.  Kepler's first law
    is simply the statement that e < 1 for the planets.
    """
    _G.positive(semi_latus_rectum_km, "semi_latus_rectum_km")
    denominator = 1.0 + eccentricity * math.cos(true_anomaly_rad)
    if denominator <= 0.0:
        raise ValueError(
            "True anomaly lies beyond the asymptote of this hyperbola/parabola "
            "— no orbit point exists there."
        )
    return semi_latus_rectum_km / denominator


def periapsis_radius(
    semi_major_axis_km: float,
    eccentricity: float,
) -> float:
    """
    Periapsis radius, r_p = a(1 − e)  [km] — the closest approach.

    Args:
        semi_major_axis_km: Semi-major axis a [km].
        eccentricity:       Eccentricity e.

    Returns:
        Periapsis radius r_p [km].
    """
    return semi_major_axis_km * (1.0 - eccentricity)


def apoapsis_radius(
    semi_major_axis_km: float,
    eccentricity: float,
) -> float:
    """
    Apoapsis radius, r_a = a(1 + e)  [km] — the farthest point.

    Args:
        semi_major_axis_km: Semi-major axis a [km].
        eccentricity:       Eccentricity e.  Must be < 1.

    Returns:
        Apoapsis radius r_a [km].

    Raises:
        ValueError: For open trajectories (e ≥ 1), which have no apoapsis.
    """
    if eccentricity >= 1.0:
        raise ValueError(
            "Open trajectories (e >= 1) have no apoapsis."
        )
    return semi_major_axis_km * (1.0 + eccentricity)


def flight_path_angle(
    eccentricity: float,
    true_anomaly_rad: float,
) -> float:
    """
    Flight-path angle γ [rad] — the angle between the velocity vector and the
    local horizontal (the direction perpendicular to the radius).

    Args:
        eccentricity:     Eccentricity e.
        true_anomaly_rad: True anomaly ν [rad].

    Returns:
        Flight-path angle γ [rad].

    tan(γ) = e sin(ν) / (1 + e cos(ν)).  It is zero at periapsis and apoapsis
    (where motion is purely horizontal), positive while climbing and negative
    while descending.
    """
    return math.atan2(
        eccentricity * math.sin(true_anomaly_rad),
        1.0 + eccentricity * math.cos(true_anomaly_rad),
    )
