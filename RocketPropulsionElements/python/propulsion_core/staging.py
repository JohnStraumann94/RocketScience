"""Ideal multistage vehicle bookkeeping utilities.

Chapter reference: these functions support the staging trade studies in Rocket
Propulsion Elements by combining stage masses, structural fractions, and ideal
delta-v contributions into a complete vehicle picture.
"""

import math
from dataclasses import dataclass

from .constants import STANDARD_GRAVITY
from .guard import non_negative, positive
from .rocket_equation import payload_fraction as single_stage_payload_fraction


@dataclass(frozen=True)
class Stage:
    """Definition of a single ideal rocket stage."""

    name: str
    propellant_mass: float
    structural_mass: float
    specific_impulse: float

    @property
    def total_mass(self) -> float:
        """Return stage propellant plus structure mass.

        Args:
            None.

        Returns:
            Total stage mass in kilograms.

        Raises:
            None.

        Physics:
            The stage total mass is the mass carried with that stage before it
            burns, excluding anything stacked above it.
        """
        return self.propellant_mass + self.structural_mass

    @property
    def structural_coefficient(self) -> float:
        """Return stage structural coefficient.

        Args:
            None.

        Returns:
            Structural mass divided by total stage mass.

        Raises:
            None.

        Physics:
            This coefficient measures how much of the stage remains inert after
            propellant is consumed and strongly limits achievable delta-v.
        """
        return self.structural_mass / self.total_mass

    @property
    def effective_exhaust_velocity(self) -> float:
        """Return stage effective exhaust velocity from specific impulse.

        Args:
            None.

        Returns:
            Effective exhaust velocity in metres per second.

        Raises:
            None.

        Physics:
            Specific impulse and effective exhaust velocity differ only by the
            standard-gravity normalization constant.
        """
        return self.specific_impulse * STANDARD_GRAVITY


@dataclass(frozen=True)
class StageResult:
    """Ideal performance result for one stage within a vehicle stack."""

    stage: Stage
    ignition_mass: float
    burnout_mass: float
    mass_ratio: float
    delta_v: float


@dataclass(frozen=True)
class VehicleResult:
    """Aggregated ideal result for a multistage vehicle."""

    stages: list[StageResult]
    payload_mass: float
    gross_liftoff_mass: float
    total_delta_v: float
    payload_fraction: float

    def __str__(self) -> str:
        """Render a readable staging summary.

        Args:
            None.

        Returns:
            A multi-line report string.

        Raises:
            None.

        Physics:
            Vehicle staging studies are typically reviewed as a table of stage
            ignition masses, burnout masses, and per-stage delta-v contributions.
        """
        lines = [
            "Vehicle Result",
            f"  Payload mass: {self.payload_mass:.3f} kg",
            f"  Gross liftoff mass: {self.gross_liftoff_mass:.3f} kg",
            f"  Total delta-v: {self.total_delta_v:.3f} m/s",
            f"  Payload fraction: {self.payload_fraction:.6f}",
            "  Stages:",
        ]
        for index, result in enumerate(self.stages, start=1):
            lines.append(
                f"    {index}. {result.stage.name}: ignition={result.ignition_mass:.3f} kg, "
                f"burnout={result.burnout_mass:.3f} kg, mass_ratio={result.mass_ratio:.6f}, "
                f"delta_v={result.delta_v:.3f} m/s"
            )
        return "\n".join(lines)


def analyse_vehicle(stages: list[Stage], payload_mass: float) -> VehicleResult:
    """Compute ignition masses and ideal delta-v for a multistage stack.

    Args:
        stages: Ordered list of stages from first stage to upper stage.
        payload_mass: Payload carried above the top stage in kilograms.

    Returns:
        A ``VehicleResult`` containing per-stage and whole-vehicle data.

    Raises:
        ValueError: If no stages are supplied or if any mass or specific impulse is invalid.

    Physics:
        Working backward gives the ignition mass each stage must lift; working
        forward then gives each stage's burnout mass ratio and delta-v.
    """
    if not stages:
        raise ValueError("stages must contain at least one stage.")
    non_negative(payload_mass, "payload_mass")
    for stage in stages:
        positive(stage.propellant_mass, f"{stage.name}.propellant_mass")
        positive(stage.structural_mass, f"{stage.name}.structural_mass")
        positive(stage.specific_impulse, f"{stage.name}.specific_impulse")
    ignition_masses = [0.0] * len(stages)
    mass_above = payload_mass
    for index in range(len(stages) - 1, -1, -1):
        ignition_masses[index] = mass_above + stages[index].total_mass
        mass_above = ignition_masses[index]
    results: list[StageResult] = []
    total_delta_v = 0.0
    for stage, ignition_mass in zip(stages, ignition_masses):
        burnout_mass = ignition_mass - stage.propellant_mass
        mass_ratio = ignition_mass / burnout_mass
        delta_v = stage.effective_exhaust_velocity * math.log(mass_ratio)
        total_delta_v += delta_v
        results.append(
            StageResult(
                stage=stage,
                ignition_mass=ignition_mass,
                burnout_mass=burnout_mass,
                mass_ratio=mass_ratio,
                delta_v=delta_v,
            )
        )
    gross_liftoff_mass = ignition_masses[0]
    return VehicleResult(
        stages=results,
        payload_mass=payload_mass,
        gross_liftoff_mass=gross_liftoff_mass,
        total_delta_v=total_delta_v,
        payload_fraction=payload_mass / gross_liftoff_mass,
    )


def ideal_payload_fraction(
    total_delta_v: float,
    effective_exhaust_velocity: float,
    structural_coefficient: float,
    stage_count: int,
) -> float:
    """Estimate ideal payload fraction for equal stages sharing total delta-v.

    Args:
        total_delta_v: Required total ideal delta-v in metres per second.
        effective_exhaust_velocity: Effective exhaust velocity used by each stage.
        structural_coefficient: Structural coefficient assumed for each stage.
        stage_count: Number of equal stages.

    Returns:
        Ideal overall payload fraction.

    Raises:
        ValueError: If any input is outside its valid range.

    Physics:
        Splitting total delta-v equally across identical stages lets the overall
        payload fraction be expressed as the product of the repeated stage value.
    """
    non_negative(total_delta_v, "total_delta_v")
    positive(effective_exhaust_velocity, "effective_exhaust_velocity")
    if stage_count <= 0:
        raise ValueError("stage_count must be a positive integer.")
    per_stage_fraction = single_stage_payload_fraction(
        total_delta_v / stage_count,
        effective_exhaust_velocity,
        structural_coefficient,
    )
    return per_stage_fraction ** stage_count


def optimum_stage_count(
    total_delta_v: float,
    effective_exhaust_velocity: float,
    structural_coefficient: float,
    maximum_stages: int = 6,
) -> tuple[int, float]:
    """Brute-force the stage count that maximizes ideal payload fraction.

    Args:
        total_delta_v: Required mission delta-v in metres per second.
        effective_exhaust_velocity: Effective exhaust velocity for each stage.
        structural_coefficient: Structural coefficient for each stage.
        maximum_stages: Highest stage count to evaluate.

    Returns:
        A tuple ``(best_stage_count, best_payload_fraction)``.

    Raises:
        ValueError: If the stage-count limit is not positive or other inputs are invalid.

    Physics:
        More stages reduce per-stage mass ratio demands, but each stage also has
        structural overhead. Sweeping stage count exposes the best compromise.
    """
    if maximum_stages <= 0:
        raise ValueError("maximum_stages must be a positive integer.")
    best_count = 1
    best_fraction = ideal_payload_fraction(
        total_delta_v,
        effective_exhaust_velocity,
        structural_coefficient,
        1,
    )
    for stage_count in range(2, maximum_stages + 1):
        candidate_fraction = ideal_payload_fraction(
            total_delta_v,
            effective_exhaust_velocity,
            structural_coefficient,
            stage_count,
        )
        if candidate_fraction > best_fraction:
            best_count = stage_count
            best_fraction = candidate_fraction
    return best_count, best_fraction


__all__ = [
    "Stage",
    "StageResult",
    "VehicleResult",
    "analyse_vehicle",
    "ideal_payload_fraction",
    "optimum_stage_count",
]
