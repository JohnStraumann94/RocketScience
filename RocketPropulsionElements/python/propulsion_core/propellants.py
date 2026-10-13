"""Representative propellant-combination data and mixture helpers.

Chapter reference: these definitions mirror the comparative propellant tables
and simple density / performance trade studies in Rocket Propulsion Elements.
"""

from dataclasses import dataclass

from .engine_analysis import analyse
from .guard import non_negative, positive


@dataclass(frozen=True)
class PropellantCombination:
    """Representative thermochemical and density data for a propellant pair."""

    name: str
    oxidizer: str
    fuel: str
    optimum_mixture_ratio: float
    chamber_temperature: float
    molecular_mass: float
    gamma: float
    oxidizer_density: float
    fuel_density: float
    notes: str

    @property
    def bulk_density(self) -> float:
        """Compute mixture bulk density at the optimum mixture ratio.

        Args:
            None.

        Returns:
            Effective bulk density in kilograms per cubic metre.

        Raises:
            None.

        Physics:
            A kilogram of fuel is paired with ``MR`` kilograms of oxidizer. The
            combined mass divided by the combined occupied volume gives bulk density.
        """
        oxidizer_mass = self.optimum_mixture_ratio
        total_volume = (oxidizer_mass / self.oxidizer_density) + (1.0 / self.fuel_density)
        return (oxidizer_mass + 1.0) / total_volume

    def ideal_vacuum_isp(self, chamber_pressure: float, area_ratio: float) -> float:
        """Estimate ideal vacuum specific impulse using the engine analyzer.

        Args:
            chamber_pressure: Chamber pressure in pascals.
            area_ratio: Exit area divided by throat area.

        Returns:
            Ideal vacuum specific impulse in seconds.

        Raises:
            ValueError: If the requested engine point is invalid.

        Physics:
            A small arbitrary throat area is sufficient because ideal specific
            impulse is independent of absolute engine scale at fixed conditions.
        """
        operating_point = analyse(
            gamma=self.gamma,
            chamber_temperature=self.chamber_temperature,
            molecular_mass=self.molecular_mass,
            chamber_pressure=chamber_pressure,
            area_ratio=area_ratio,
            throat_area=0.01,
            ambient_pressure=0.0,
        )
        return operating_point.specific_impulse

    def density_impulse(self, chamber_pressure: float, area_ratio: float) -> float:
        """Compute density impulse from vacuum specific impulse and bulk density.

        Args:
            chamber_pressure: Chamber pressure in pascals.
            area_ratio: Exit area divided by throat area.

        Returns:
            Density impulse in second-kilograms per cubic metre.

        Raises:
            ValueError: If the underlying specific-impulse estimate is invalid.

        Physics:
            Density impulse rewards combinations that offer both high specific
            impulse and high tankage density, a key launch-vehicle trade metric.
        """
        return self.ideal_vacuum_isp(chamber_pressure, area_ratio) * self.bulk_density


LOX_LH2 = PropellantCombination(
    "LOX / LH2",
    "Liquid oxygen",
    "Liquid hydrogen",
    5.5,
    3250.0,
    10.0,
    1.24,
    1141.0,
    71.0,
    "Very high specific impulse with very low bulk density.",
)
LOX_RP1 = PropellantCombination(
    "LOX / RP-1",
    "Liquid oxygen",
    "RP-1 kerosene",
    2.56,
    3670.0,
    21.9,
    1.24,
    1141.0,
    810.0,
    "Dense, operationally practical hydrocarbon launch propellant.",
)
LOX_METHANE = PropellantCombination(
    "LOX / CH4",
    "Liquid oxygen",
    "Liquid methane",
    3.4,
    3500.0,
    20.0,
    1.24,
    1141.0,
    423.0,
    "Compromise between hydrogen efficiency and kerosene density.",
)
N2O4_UDMH = PropellantCombination(
    "N2O4 / UDMH",
    "Nitrogen tetroxide",
    "Unsymmetrical dimethylhydrazine",
    2.6,
    3200.0,
    21.0,
    1.25,
    1450.0,
    793.0,
    "Storability and hypergolic ignition make this pair attractive in space systems.",
)
PEROXIDE_KEROSENE = PropellantCombination(
    "H2O2 / Kerosene",
    "High-test hydrogen peroxide (98%)",
    "Kerosene",
    7.5,
    2950.0,
    22.0,
    1.22,
    1440.0,
    810.0,
    "Operationally simple combination with moderate performance.",
)
APCP_SOLID = PropellantCombination(
    "APCP (solid)",
    "Ammonium perchlorate",
    "HTPB binder + aluminium",
    4.5,
    3400.0,
    26.0,
    1.18,
    1950.0,
    1500.0,
    "Representative composite solid propellant for first-order performance studies.",
)
ALL = [LOX_LH2, LOX_RP1, LOX_METHANE, N2O4_UDMH, PEROXIDE_KEROSENE, APCP_SOLID]


def split_flow(total_mass_flow_rate: float, mixture_ratio: float) -> tuple[float, float]:
    """Split total liquid-propellant flow into oxidizer and fuel streams.

    Args:
        total_mass_flow_rate: Total propellant mass flow rate in kilograms per second.
        mixture_ratio: Oxidizer-to-fuel mass ratio.

    Returns:
        A tuple ``(oxidizer_flow, fuel_flow)`` in kilograms per second.

    Raises:
        ValueError: If total mass flow is negative or mixture ratio is not positive.

    Physics:
        With ``MR = m_dot_ox / m_dot_fuel`` and ``m_dot_total = m_dot_ox + m_dot_fuel``,
        the individual stream rates follow directly from algebra.
    """
    non_negative(total_mass_flow_rate, "total_mass_flow_rate")
    positive(mixture_ratio, "mixture_ratio")
    fuel_flow = total_mass_flow_rate / (1.0 + mixture_ratio)
    oxidizer_flow = total_mass_flow_rate - fuel_flow
    return oxidizer_flow, fuel_flow


def tank_volumes(
    propellant_mass: float,
    combination: PropellantCombination,
    ullage_fraction: float = 0.05,
) -> tuple[float, float]:
    """Estimate oxidizer and fuel tank volumes from mass and densities.

    Args:
        propellant_mass: Total propellant mass in kilograms.
        combination: Propellant combination data.
        ullage_fraction: Extra free volume fraction for tank ullage.

    Returns:
        A tuple ``(oxidizer_volume, fuel_volume)`` in cubic metres.

    Raises:
        ValueError: If propellant mass or ullage fraction is negative.

    Physics:
        The total mass is split by mixture ratio, each mass is divided by its
        liquid density, and ullage is applied as a sizing margin.
    """
    non_negative(propellant_mass, "propellant_mass")
    non_negative(ullage_fraction, "ullage_fraction")
    oxidizer_mass, fuel_mass = split_flow(propellant_mass, combination.optimum_mixture_ratio)
    oxidizer_volume = (oxidizer_mass / combination.oxidizer_density) * (1.0 + ullage_fraction)
    fuel_volume = (fuel_mass / combination.fuel_density) * (1.0 + ullage_fraction)
    return oxidizer_volume, fuel_volume


__all__ = [
    "PropellantCombination",
    "LOX_LH2",
    "LOX_RP1",
    "LOX_METHANE",
    "N2O4_UDMH",
    "PEROXIDE_KEROSENE",
    "APCP_SOLID",
    "ALL",
    "split_flow",
    "tank_volumes",
]
