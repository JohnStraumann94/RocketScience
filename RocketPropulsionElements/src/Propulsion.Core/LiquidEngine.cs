namespace Propulsion.Core;

/// <summary>
/// Liquid rocket engine subsystems: injectors, feed system pressure budgets, turbopump power
/// and regenerative cooling. Sutton Chapters 6, 8, 9 and 10.
/// </summary>
public static class LiquidEngine
{
    /// <summary>
    /// Mass flow through an injector orifice.
    /// </summary>
    /// <param name="dischargeCoefficient">
    /// Discharge coefficient C_d. About 0.6 for a sharp-edged hole, 0.75-0.85 for a
    /// short tube with a rounded inlet, and up to 0.9 for a well-shaped nozzle.
    /// </param>
    /// <param name="orificeArea">Orifice cross-sectional area, m^2.</param>
    /// <param name="propellantDensity">Propellant density, kg/m^3.</param>
    /// <param name="pressureDrop">Pressure drop across the injector, Pa.</param>
    /// <returns>Mass flow rate in kg/s.</returns>
    /// <remarks>
    /// <para>
    /// mdot = C_d * A * sqrt(2 * rho * dp), which is just Bernoulli with a fudge factor for the
    /// vena contracta and viscous losses.
    /// </para>
    /// <para>
    /// The square root is what makes injector design a stability problem. Because flow responds
    /// only as sqrt(dp), a small injector pressure drop means chamber pressure fluctuations feed
    /// straight back into the propellant flow, and the coupling can drive combustion
    /// instability. The classic rule is to size the injector for a pressure drop of 15-20% of
    /// chamber pressure - enough stiffness to decouple feed from chamber. That is a real cost:
    /// on a 200 bar staged-combustion engine it means the pumps must deliver 30-40 bar of pure
    /// overhead just to keep the combustion quiet.
    /// </para>
    /// </remarks>
    public static double InjectorMassFlow(
        double dischargeCoefficient,
        double orificeArea,
        double propellantDensity,
        double pressureDrop)
    {
        Guard.InRange(dischargeCoefficient, 0.1, 1.0, nameof(dischargeCoefficient));
        Guard.Positive(orificeArea, nameof(orificeArea));
        Guard.Positive(propellantDensity, nameof(propellantDensity));
        Guard.Positive(pressureDrop, nameof(pressureDrop));

        return dischargeCoefficient * orificeArea * Math.Sqrt(2.0 * propellantDensity * pressureDrop);
    }

    /// <summary>
    /// Orifice area needed to pass a required mass flow.
    /// </summary>
    /// <param name="massFlowRate">Required mass flow rate, kg/s.</param>
    /// <param name="dischargeCoefficient">Discharge coefficient C_d.</param>
    /// <param name="propellantDensity">Propellant density, kg/m^3.</param>
    /// <param name="pressureDrop">Design pressure drop across the injector, Pa.</param>
    /// <returns>Required orifice area in m^2.</returns>
    public static double RequiredOrificeArea(
        double massFlowRate,
        double dischargeCoefficient,
        double propellantDensity,
        double pressureDrop)
    {
        Guard.Positive(massFlowRate, nameof(massFlowRate));
        Guard.InRange(dischargeCoefficient, 0.1, 1.0, nameof(dischargeCoefficient));
        Guard.Positive(propellantDensity, nameof(propellantDensity));
        Guard.Positive(pressureDrop, nameof(pressureDrop));

        return massFlowRate / (dischargeCoefficient * Math.Sqrt(2.0 * propellantDensity * pressureDrop));
    }

    /// <summary>
    /// Number of orifices of a given diameter needed to pass a required flow.
    /// </summary>
    /// <param name="totalArea">Total orifice area required, m^2.</param>
    /// <param name="orificeDiameter">Diameter of one orifice, m.</param>
    /// <returns>The number of orifices, rounded up.</returns>
    /// <remarks>
    /// Rounded up rather than to nearest, because under-sizing the injector starves the engine.
    /// Real injectors have anywhere from a handful of elements to the F-1's 2,600-odd, and the
    /// pattern in which they are arranged matters as much as their number - baffles and
    /// deliberate pattern variation near the wall are standard cures for combustion instability.
    /// </remarks>
    public static int OrificeCount(double totalArea, double orificeDiameter)
    {
        Guard.Positive(totalArea, nameof(totalArea));
        Guard.Positive(orificeDiameter, nameof(orificeDiameter));

        double singleArea = Math.PI * orificeDiameter * orificeDiameter / 4.0;

        return (int)Math.Ceiling(totalArea / singleArea);
    }

    /// <summary>
    /// Tank pressure required by a pressure-fed engine.
    /// </summary>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="injectorPressureDrop">Injector pressure drop, Pa.</param>
    /// <param name="lineAndValvePressureDrop">Losses in lines, valves and the cooling jacket, Pa.</param>
    /// <param name="dynamicPressureAllowance">Allowance for feed line dynamic head, Pa.</param>
    /// <returns>Required tank pressure in Pa.</returns>
    /// <remarks>
    /// <para>
    /// Simply the sum of everything downstream. The reason this matters so much is that in a
    /// pressure-fed system the propellant tanks <i>are</i> the pressure vessels, and tank wall
    /// mass scales directly with pressure times volume.
    /// </para>
    /// <para>
    /// That is why pressure-fed engines are limited to roughly 20-30 bar chamber pressure. Above
    /// that the tanks become heavier than the turbopumps you were trying to avoid. Pressure
    /// feeding survives because it is simple and utterly reliable - which is exactly what you
    /// want in a lunar module ascent engine that gets one attempt.
    /// </para>
    /// </remarks>
    public static double RequiredTankPressure(
        double chamberPressure,
        double injectorPressureDrop,
        double lineAndValvePressureDrop,
        double dynamicPressureAllowance = 0.0)
    {
        Guard.Positive(chamberPressure, nameof(chamberPressure));
        Guard.NonNegative(injectorPressureDrop, nameof(injectorPressureDrop));
        Guard.NonNegative(lineAndValvePressureDrop, nameof(lineAndValvePressureDrop));
        Guard.NonNegative(dynamicPressureAllowance, nameof(dynamicPressureAllowance));

        return chamberPressure
             + injectorPressureDrop
             + lineAndValvePressureDrop
             + dynamicPressureAllowance;
    }

    /// <summary>
    /// Shaft power required by a propellant pump.
    /// </summary>
    /// <param name="massFlowRate">Mass flow rate through the pump, kg/s.</param>
    /// <param name="pressureRise">Pressure rise across the pump, Pa.</param>
    /// <param name="density">Propellant density, kg/m^3.</param>
    /// <param name="pumpEfficiency">Pump efficiency, typically 0.6-0.8.</param>
    /// <returns>Required shaft power in watts.</returns>
    /// <remarks>
    /// <para>
    /// P = mdot * dp / (rho * eta). Note the density in the denominator: pumping a low-density
    /// propellant costs far more power for the same pressure rise and mass flow. This is why
    /// the hydrogen pump on an LH2 engine is a multi-stage monster while the oxygen pump beside
    /// it, moving five times the mass, is comparatively modest.
    /// </para>
    /// <para>
    /// The numbers get large fast. An F-1 turbopump absorbed around 41 MW - about 55,000
    /// horsepower - from a single shaft, which for context is comparable to the total output of
    /// a small power station, delivered by a machine that fits on a truck bed and runs for
    /// 165 seconds.
    /// </para>
    /// </remarks>
    public static double PumpPower(
        double massFlowRate,
        double pressureRise,
        double density,
        double pumpEfficiency)
    {
        Guard.Positive(massFlowRate, nameof(massFlowRate));
        Guard.Positive(pressureRise, nameof(pressureRise));
        Guard.Positive(density, nameof(density));
        Guard.InRange(pumpEfficiency, 0.1, 1.0, nameof(pumpEfficiency));

        return massFlowRate * pressureRise / (density * pumpEfficiency);
    }

    /// <summary>
    /// Pump head rise in metres of fluid.
    /// </summary>
    /// <param name="pressureRise">Pressure rise, Pa.</param>
    /// <param name="density">Fluid density, kg/m^3.</param>
    /// <returns>Head in metres.</returns>
    /// <remarks>
    /// Turbomachinery is designed in head, not pressure, because head is what determines the
    /// impeller tip speed a given stage can produce regardless of the fluid. The equivalent head
    /// for liquid hydrogen is enormous, which is precisely why hydrogen pumps need several
    /// stages.
    /// </remarks>
    public static double PumpHead(double pressureRise, double density)
    {
        Guard.Positive(pressureRise, nameof(pressureRise));
        Guard.Positive(density, nameof(density));

        return pressureRise / (density * Constants.StandardGravity);
    }

    /// <summary>
    /// Pump specific speed, the dimensionless similarity parameter for impeller selection.
    /// </summary>
    /// <param name="rotationalSpeedRadPerSec">Shaft speed, rad/s.</param>
    /// <param name="volumeFlowRate">Volume flow rate, m^3/s.</param>
    /// <param name="headMetres">Head rise, m.</param>
    /// <returns>The dimensionless specific speed.</returns>
    /// <remarks>
    /// N_s = omega * sqrt(Q) / (g*H)^0.75. It tells you what shape of impeller the duty calls
    /// for: low values indicate a radial centrifugal impeller (high head, low flow), high values
    /// an axial one (low head, high flow). Rocket pumps are usually centrifugal because the
    /// heads are extreme.
    /// </remarks>
    public static double PumpSpecificSpeed(
        double rotationalSpeedRadPerSec,
        double volumeFlowRate,
        double headMetres)
    {
        Guard.Positive(rotationalSpeedRadPerSec, nameof(rotationalSpeedRadPerSec));
        Guard.Positive(volumeFlowRate, nameof(volumeFlowRate));
        Guard.Positive(headMetres, nameof(headMetres));

        return rotationalSpeedRadPerSec * Math.Sqrt(volumeFlowRate)
             / Math.Pow(Constants.StandardGravity * headMetres, 0.75);
    }

    /// <summary>
    /// Convective heat transfer rate into a regeneratively cooled wall.
    /// </summary>
    /// <param name="heatTransferCoefficient">Gas-side film coefficient h, W/(m^2*K).</param>
    /// <param name="surfaceArea">Wetted surface area, m^2.</param>
    /// <param name="gasTemperature">Hot gas recovery temperature, K.</param>
    /// <param name="wallTemperature">Wall temperature, K.</param>
    /// <returns>Heat transfer rate in watts.</returns>
    /// <remarks>
    /// Straight Newton cooling, q = h * A * dT. The interesting part is where h is largest: the
    /// throat, where the gas is fastest and densest. Throat heat flux in a large engine reaches
    /// 20-160 MW/m^2, comparable to the surface of the sun, which is why the throat is where
    /// engines fail and where cooling channel design is at its most aggressive.
    /// </remarks>
    public static double ConvectiveHeatTransfer(
        double heatTransferCoefficient,
        double surfaceArea,
        double gasTemperature,
        double wallTemperature)
    {
        Guard.Positive(heatTransferCoefficient, nameof(heatTransferCoefficient));
        Guard.Positive(surfaceArea, nameof(surfaceArea));
        Guard.Positive(gasTemperature, nameof(gasTemperature));
        Guard.Positive(wallTemperature, nameof(wallTemperature));

        return heatTransferCoefficient * surfaceArea * (gasTemperature - wallTemperature);
    }

    /// <summary>
    /// Coolant temperature rise through a regenerative cooling jacket.
    /// </summary>
    /// <param name="heatRate">Heat absorbed by the coolant, W.</param>
    /// <param name="coolantMassFlow">Coolant mass flow rate, kg/s.</param>
    /// <param name="specificHeat">Coolant specific heat capacity, J/(kg*K).</param>
    /// <returns>Temperature rise in K.</returns>
    /// <remarks>
    /// dT = Q / (mdot * cp). Regenerative cooling is elegant precisely because this heat is not
    /// wasted: it goes into the propellant, which is about to be burned anyway, and in an
    /// expander cycle that same heat is what drives the turbines. The limit is coking for
    /// kerosene and film boiling for cryogens - exceed either and the wall temperature runs away.
    /// </remarks>
    public static double CoolantTemperatureRise(
        double heatRate,
        double coolantMassFlow,
        double specificHeat)
    {
        Guard.Positive(heatRate, nameof(heatRate));
        Guard.Positive(coolantMassFlow, nameof(coolantMassFlow));
        Guard.Positive(specificHeat, nameof(specificHeat));

        return heatRate / (coolantMassFlow * specificHeat);
    }
}
