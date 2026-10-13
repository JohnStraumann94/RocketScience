"""
orbital_elements.py — The six classical (Keplerian) orbital elements.
=====================================================================

Port of ``Astro.Core/OrbitalElements.cs``.

Referenced in BMW Chapter 2 and used throughout the library.

A state vector (r, v) is six numbers that change every instant.  The classical
elements are also six numbers, but five of them are *constant* under two-body
motion and only the sixth (the anomaly) advances with time.  That is why every
catalogue of satellites, from TLEs to planetary ephemerides, is published in
elements.

The six, and what each one does
---------------------------------
* **a** — semi-major axis: SIZE of the orbit (energy and period)
* **e** — eccentricity: SHAPE, how far from circular
* **i** — inclination: TILT of the orbit plane from the equator
* **Ω** — RAAN (right ascension of the ascending node): SWIVEL of that plane
* **ω** — argument of periapsis: ROTATION of the ellipse within its own plane
* **ν** — true anomaly: WHERE the satellite is right now along the ellipse

**All angles stored in radians.**  Degree-valued twins are exposed as ``_deg``
properties purely for display.
"""

from __future__ import annotations
import math
from dataclasses import dataclass
from enum import Enum, auto
from . import constants, angles as _angles


class OrbitType(Enum):
    """Classification of an orbit by the shape of its conic section."""

    CIRCULAR = auto()
    """e = 0.  A circle; energy is minimal for the given angular momentum."""

    ELLIPTICAL = auto()
    """0 < e < 1.  A closed, repeating orbit."""

    PARABOLIC = auto()
    """e = 1.  The exact escape trajectory; *a* is infinite."""

    HYPERBOLIC = auto()
    """e > 1.  An escape trajectory with energy to spare."""


@dataclass(frozen=True, slots=True)
class OrbitalElements:
    """
    The six classical orbital elements plus convenience properties.

    Attributes:
        semi_major_axis_km:         a — semi-major axis [km]; negative for
                                    hyperbolas, infinite for parabolas.
        eccentricity:               e — eccentricity [dimensionless, ≥ 0].
        inclination_rad:            i — inclination [rad], always in [0, π].
        raan_rad:                   Ω — right ascension of the ascending node
                                    [rad], in [0, 2π).
        argument_of_periapsis_rad:  ω — argument of periapsis [rad], in [0, 2π).
        true_anomaly_rad:           ν — true anomaly [rad], in [0, 2π).
    """

    semi_major_axis_km: float
    eccentricity: float
    inclination_rad: float
    raan_rad: float
    argument_of_periapsis_rad: float
    true_anomaly_rad: float

    # ------------------------------------------------------------------
    # Degree-valued display properties
    # ------------------------------------------------------------------

    @property
    def inclination_deg(self) -> float:
        """i — inclination in degrees, for display."""
        return self.inclination_rad * constants.RAD_TO_DEG

    @property
    def raan_deg(self) -> float:
        """Ω — RAAN in degrees, for display."""
        return self.raan_rad * constants.RAD_TO_DEG

    @property
    def argument_of_periapsis_deg(self) -> float:
        """ω — argument of periapsis in degrees, for display."""
        return self.argument_of_periapsis_rad * constants.RAD_TO_DEG

    @property
    def true_anomaly_deg(self) -> float:
        """ν — true anomaly in degrees, for display."""
        return self.true_anomaly_rad * constants.RAD_TO_DEG

    # ------------------------------------------------------------------
    # Conic classification
    # ------------------------------------------------------------------

    @property
    def orbit_type(self) -> OrbitType:
        """
        The conic type implied by the eccentricity.

        The tolerances are deliberately loose (1e-8 for circular, 1e-10 for
        parabolic) because a "perfectly" circular or parabolic orbit never
        survives floating-point arithmetic.
        """
        e = self.eccentricity
        if e < 1e-8:
            return OrbitType.CIRCULAR
        elif e < 1.0 - 1e-10:
            return OrbitType.ELLIPTICAL
        elif e <= 1.0 + 1e-10:
            return OrbitType.PARABOLIC
        else:
            return OrbitType.HYPERBOLIC

    # ------------------------------------------------------------------
    # Derived geometry
    # ------------------------------------------------------------------

    @property
    def semi_latus_rectum_km(self) -> float:
        """
        p — semi-latus rectum [km], the radius at ν = 90°.

        Computed as a(1 − e²), which stays valid (and positive) for hyperbolas
        because *a* is then negative (a × negative = positive).
        """
        return self.semi_major_axis_km * (1.0 - self.eccentricity ** 2)

    @property
    def periapsis_radius_km(self) -> float:
        """r_p — periapsis radius [km]."""
        return self.semi_major_axis_km * (1.0 - self.eccentricity)

    @property
    def apoapsis_radius_km(self) -> float:
        """
        r_a — apoapsis radius [km], or ``math.inf`` for an open orbit that
        never comes back.
        """
        if self.eccentricity < 1.0:
            return self.semi_major_axis_km * (1.0 + self.eccentricity)
        return math.inf

    # ------------------------------------------------------------------
    # Convenience constructors
    # ------------------------------------------------------------------

    def at_true_anomaly(self, true_anomaly_rad: float) -> "OrbitalElements":
        """
        Return a copy with the true anomaly replaced.

        Useful when sweeping a satellite around a fixed orbit for plotting.
        The new anomaly is normalised to [0, 2π) before storage.

        Args:
            true_anomaly_rad: The new true anomaly [rad].

        Returns:
            A new :class:`OrbitalElements` instance with only *ν* changed.
        """
        return OrbitalElements(
            self.semi_major_axis_km,
            self.eccentricity,
            self.inclination_rad,
            self.raan_rad,
            self.argument_of_periapsis_rad,
            _angles.normalize(true_anomaly_rad),
        )

    # ------------------------------------------------------------------
    # Display
    # ------------------------------------------------------------------

    def __str__(self) -> str:
        """A multi-line, human-readable dump of all six elements plus derived geometry."""
        lines = [
            f"a  (semi-major axis)      = {self.semi_major_axis_km:14.3f} km",
            f"e  (eccentricity)         = {self.eccentricity:14.7f}",
            f"i  (inclination)          = {self.inclination_deg:14.5f} deg",
            f"Om (RAAN)                 = {self.raan_deg:14.5f} deg",
            f"w  (arg. of periapsis)    = {self.argument_of_periapsis_deg:14.5f} deg",
            f"nu (true anomaly)         = {self.true_anomaly_deg:14.5f} deg",
            "-" * 50,
            f"p  (semi-latus rectum)    = {self.semi_latus_rectum_km:14.3f} km",
            f"rp (periapsis radius)     = {self.periapsis_radius_km:14.3f} km",
            f"ra (apoapsis radius)      = {self.apoapsis_radius_km:14.3f} km",
            f"type                      = {self.orbit_type.name.capitalize()}",
        ]
        return "\n".join(lines)

    def __repr__(self) -> str:
        return (
            f"OrbitalElements(a={self.semi_major_axis_km:.3f} km, "
            f"e={self.eccentricity:.7f}, "
            f"i={self.inclination_deg:.5f}°, "
            f"Ω={self.raan_deg:.5f}°, "
            f"ω={self.argument_of_periapsis_deg:.5f}°, "
            f"ν={self.true_anomaly_deg:.5f}°)"
        )
