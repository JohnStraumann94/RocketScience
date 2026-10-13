"""
astro_core — Pure-Python port of Astro.Core (C#)
=================================================

This package implements the mathematics of Bate, Mueller & White,
*Fundamentals of Astrodynamics* (Dover, 1971), using only the Python
standard library (math, dataclasses, typing, enum).

Module layout
-------------
constants          Physical and astronomical constants
guard              Argument-validation helpers
angles             Angle normalisation and conversion utilities
vector3            Immutable 3-D Cartesian vector (double precision)
orbital_elements   The six classical (Keplerian) orbital elements
two_body           Chapter 1 — two-body orbital mechanics
orbit_conversions  Chapter 2 — state-vector ↔ element conversions
kepler_solver      Chapter 4 — Kepler's equation and anomaly conversions
universal_variables Chapter 4 — universal-variable propagation (Stumpff)
lambert            Chapter 5 — Lambert's problem
maneuvers          Chapter 3 — impulsive orbital manoeuvres
ballistic_missile  Chapter 6 — ballistic missile free-flight trajectories
lunar_trajectory   Chapter 7 — patched-conic lunar transfers
interplanetary     Chapter 8 — interplanetary Hohmann transfers & gravity assists
perturbations      Chapter 9 — secular J2 perturbation effects
"""

from .constants import *          # noqa: F401, F403
from .guard import *              # noqa: F401, F403
from .angles import *             # noqa: F401, F403
from .vector3 import Vector3      # noqa: F401
from .orbital_elements import OrbitalElements, OrbitType  # noqa: F401
from .two_body import *           # noqa: F401, F403
from .orbit_conversions import StateVector, OrbitConversions  # noqa: F401
from .kepler_solver import KeplerSolver, KeplerSolution  # noqa: F401
from .universal_variables import UniversalVariables  # noqa: F401
from .lambert import Lambert, LambertSolution, TransferDirection  # noqa: F401
from .maneuvers import Maneuvers, TransferResult  # noqa: F401
from .ballistic_missile import BallisticMissile, BallisticTrajectory  # noqa: F401
from .lunar_trajectory import LunarTrajectory  # noqa: F401
from .interplanetary import Interplanetary, InterplanetaryTransfer  # noqa: F401
from .perturbations import Perturbations  # noqa: F401
