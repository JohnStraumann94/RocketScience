"""propulsion_core package.

Python port of the C# propulsion utilities used while working through Rocket
Propulsion Elements. The modules are grouped by topic so chapter-level ideas
remain easy to find: performance relations, nozzle flow, vehicle staging,
trajectory approximations, and propulsion system component sizing.
"""

from . import constants as _constants
from . import electric_propulsion as _electric_propulsion
from . import engine_analysis as _engine_analysis
from . import flight_performance as _flight_performance
from . import gravity_turn as _gravity_turn
from . import liquid_engine as _liquid_engine
from . import nozzle_geometry as _nozzle_geometry
from . import nozzle_losses as _nozzle_losses
from . import nozzle_theory as _nozzle_theory
from . import performance as _performance
from . import propellants as _propellants
from . import rocket_equation as _rocket_equation
from . import solid_motor as _solid_motor
from . import staging as _staging
from . import thermochemistry as _thermochemistry
from .constants import *
from .electric_propulsion import *
from .engine_analysis import *
from .flight_performance import *
from .gravity_turn import *
from .liquid_engine import *
from .nozzle_geometry import *
from .nozzle_losses import *
from .nozzle_theory import *
from .performance import *
from .propellants import *
from .rocket_equation import *
from .solid_motor import *
from .staging import *
from .thermochemistry import *
from .nozzle_losses import ExpansionState

__all__ = (
    _constants.__all__
    + _electric_propulsion.__all__
    + _engine_analysis.__all__
    + _flight_performance.__all__
    + _gravity_turn.__all__
    + _liquid_engine.__all__
    + _nozzle_geometry.__all__
    + _nozzle_losses.__all__
    + _nozzle_theory.__all__
    + _performance.__all__
    + _propellants.__all__
    + _rocket_equation.__all__
    + _solid_motor.__all__
    + _staging.__all__
    + _thermochemistry.__all__
)
