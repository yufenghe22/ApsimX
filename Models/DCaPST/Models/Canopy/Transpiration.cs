using Models.DCAPST.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// Models transpiration in the canopy
    /// </summary>
    public class Transpiration
    {
        /// <summary>
        /// The canopy parameters
        /// </summary>
        private readonly CanopyParameters canopy;

        /// <summary>
        /// The pathway parameters
        /// </summary>
        private readonly PathwayParameters pathway;

        /// <summary>
        /// Models the leaf water interaction
        /// </summary>
        private readonly IWaterInteraction water;

        /// <summary>
        /// Models how the leaf responds to different temperatures
        /// </summary>
        private readonly TemperatureResponse leaf;

        /// <summary>
        /// Provides access to the leaf GmT value.
        /// </summary>
        public double LeafGmT => leaf.GmT;

        /// <summary>
        /// If the transpiration rate is limited
        /// </summary>
        public bool Limited { get; set; }

        /// <summary>
        /// The boundary heat conductance
        /// </summary>
        public double BoundaryHeatConductance { get; set; }

        /// <summary>
        /// Maximum transpiration rate
        /// </summary>
        public double MaxRate { get; set; }

        /// <summary>
        /// Fraction of water allocated
        /// </summary>
        public double Fraction { get; set; }

        /// <summary>
        /// Resistance to water
        /// </summary>
        public double Resistance { get; private set; }

        /// <summary>Maximum number of shared-state iterations.</summary>
        public int MaximumIterations { get; set; } = 2500;

        /// <summary>Leaf-temperature convergence tolerance.</summary>
        public double TemperatureTolerance { get; set; } = 1e-3;

        /// <summary>Assimilation convergence tolerance.</summary>
        public double AssimilationTolerance { get; set; } = 1e-4;

        /// <summary>Mesophyll CO2 convergence tolerance.</summary>
        public double MesophyllTolerance { get; set; } = 1e-3;

        /// <summary>
        /// The amount of CO2 in the air.
        /// </summary>
        private readonly double ambientCO2;

        /// <summary>
        /// 
        /// Initializes a new instance of the <see cref="Transpiration"/> class.
        /// </summary>
        /// <param name="canopy">Canopy parameters</param>
        /// <param name="pathway">Pathway parameters</param>
        /// <param name="water">Water interaction model</param>
        /// <param name="leaf">Leaf temperature response</param>
        /// <param name="ambientCO2"></param>
        public Transpiration(
            CanopyParameters canopy,
            PathwayParameters pathway,
            IWaterInteraction water,
            TemperatureResponse leaf,
            double ambientCO2
        )
        {
            this.canopy = canopy;
            this.pathway = pathway;
            this.water = water;
            this.leaf = leaf;
            this.ambientCO2 = ambientCO2;
        }

        /// <summary>
        /// Sets the current conditions for transpiration
        /// </summary>
        /// <param name="at25C">Parameter rates at 25°C</param>
        /// <param name="photons">Photon flux density</param>
        /// <param name="radiation">Radiation</param>
        /// <param name="leafAreaIndex">Leaf area represented by this calculation</param>
        public void SetConditions(ParameterRates at25C, double photons, double radiation, double leafAreaIndex = 1.0)
        {
            leaf.SetConditions(at25C, photons);
            water.SetConditions(BoundaryHeatConductance, radiation, leafAreaIndex);
        }

        /// <summary>
        /// Solves all candidate pathways at one shared temperature, intercellular CO2,
        /// and mesophyll CO2 state.
        /// </summary>
        public bool SolveShared(IReadOnlyList<AssimilationPathway> pathways, IAssimilation assimilation,
                                ParameterRates at25C, double photons, double radiation,
                                double areaLai, double airTemperature)
        {
            if (areaLai <= 1e-3 || photons <= 0 || at25C.Gm <= 0)
            {
                SetSharedResults(pathways, new double[pathways.Count], 0, airTemperature,
                                 double.NaN, double.NaN, double.NaN, double.NaN);
                return true;
            }

            foreach (AssimilationPathway candidate in pathways)
                candidate.SetConditions(airTemperature, areaLai);

            leaf.SetConditions(at25C, photons);
            water.SetConditions(BoundaryHeatConductance, radiation, areaLai);

            double ratio = pathway.IntercellularToBoundaryLayerCO2Ratio;
            double temperatureValue = airTemperature;
            double mesophyllCO2 = ratio * ambientCO2;
            double assimilationRate = 0;
            double previousAssimilation = double.NaN;
            double relaxation = 0.25;
            const double minimumRelaxation = 1e-5;
            double previousResidual = double.PositiveInfinity;
            double previousSignedResidual = double.NaN;
            double waterUse = 0;
            double intercellularCO2 = ratio * ambientCO2;
            double[] rates = new double[pathways.Count];
            bool converged = false;
            double totalCO2Conductance = double.NaN;

            for (int iteration = 0; iteration < MaximumIterations; iteration++)
            {
                leaf.LeafTemperature = temperatureValue;
                water.LeafTemp = temperatureValue;

                double resistance;
                if (Limited)
                {
                    waterUse = MaxRate * Fraction;
                    resistance = water.LimitedWaterResistance(waterUse);
                    totalCO2Conductance = water.TotalCO2Conductance(resistance);
                }
                else
                {
                    resistance = double.NaN;
                }

                for (int index = 0; index < pathways.Count; index++)
                {
                    AssimilationPathway candidate = pathways[index];
                    candidate.Temperature = temperatureValue;
                    candidate.MesophyllCO2 = mesophyllCO2;
                    candidate.IntercellularCO2 = intercellularCO2;
                    AssimilationFunction function = assimilation.GetFunction(candidate, leaf);
                    if (Limited)
                    {
                        function.CmIntercept = ambientCO2;
                        function.CmSlope = LimitedCO2Resistance(totalCO2Conductance, leaf.GmT);
                    }
                    else
                    {
                        function.CmIntercept = UnlimitedCO2Intercept(ratio, ambientCO2);
                        function.CmSlope = UnlimitedCO2Resistance(
                            ratio, water.BoundaryCO2Conductance, leaf.GmT);
                    }
                    rates[index] = function.Value();
                }

                assimilationRate = rates.Min();
                if (Limited)
                {
                    intercellularCO2 = LimitedIntercellularCO2(
                        ambientCO2, assimilationRate, totalCO2Conductance);
                }
                else
                {
                    intercellularCO2 = UnlimitedIntercellularCO2(
                        ratio, ambientCO2, assimilationRate, water.BoundaryCO2Conductance);
                    resistance = water.UnlimitedWaterResistance(assimilationRate, ambientCO2, intercellularCO2);
                    waterUse = water.HourlyWaterUse(resistance);
                }

                double targetMesophyllCO2 = intercellularCO2 - assimilationRate / leaf.GmT;
                double targetTemperature = water.LeafTemperature(resistance);
                if (!double.IsFinite(assimilationRate) || !double.IsFinite(waterUse) ||
                    !double.IsFinite(resistance) || !double.IsFinite(targetTemperature))
                {
                    if (relaxation > minimumRelaxation)
                    {
                        relaxation = Math.Max(minimumRelaxation, relaxation / 2.0);
                        continue;
                    }
                    SetSharedResults(pathways, new double[pathways.Count], 0, airTemperature,
                                     double.NaN, double.NaN, double.NaN, double.NaN);
                    return false;
                }

                double signedResidual = targetTemperature - temperatureValue;
                double temperatureChange = Math.Abs(signedResidual);
                double assimilationChange = double.IsFinite(previousAssimilation)
                    ? Math.Abs(assimilationRate - previousAssimilation)
                    : double.PositiveInfinity;
                double mesophyllChange = Math.Abs(targetMesophyllCO2 - mesophyllCO2);

                if ((temperatureChange > 1.25 * previousResidual ||
                     double.IsFinite(previousSignedResidual) && signedResidual * previousSignedResidual < 0) &&
                    relaxation > minimumRelaxation)
                    relaxation = Math.Max(minimumRelaxation, relaxation / 2.0);

                if (temperatureChange <= TemperatureTolerance &&
                    assimilationChange <= AssimilationTolerance &&
                    mesophyllChange <= MesophyllTolerance)
                {
                    converged = true;
                    break;
                }

                temperatureValue += relaxation * signedResidual;
                mesophyllCO2 += 0.25 * (targetMesophyllCO2 - mesophyllCO2);
                previousResidual = temperatureChange;
                previousSignedResidual = signedResidual;
                previousAssimilation = assimilationRate;
            }

            double boundaryLayerCO2 = ambientCO2 - assimilationRate / water.BoundaryCO2Conductance;
            double stomatalCO2Conductance = Limited
                ? 1.0 / (1.0 / totalCO2Conductance - 1.0 / water.BoundaryCO2Conductance)
                : assimilationRate / (boundaryLayerCO2 - intercellularCO2);
            SetSharedResults(pathways, rates, waterUse, temperatureValue,
                             intercellularCO2, mesophyllCO2, leaf.GmT, stomatalCO2Conductance);
            return converged;
        }

        private void SetSharedResults(IReadOnlyList<AssimilationPathway> pathways, double[] rates,
                                      double waterUse, double temperatureValue,
                                      double intercellularCO2, double mesophyllCO2,
                                      double mesophyllCO2Conductance, double stomatalCO2Conductance)
        {
            for (int index = 0; index < pathways.Count; index++)
            {
                pathways[index].CO2Rate = rates[index];
                pathways[index].WaterUse = waterUse;
                pathways[index].Temperature = temperatureValue;
                pathways[index].VPD = water.VPD;
                pathways[index].IntercellularCO2 = intercellularCO2;
                pathways[index].MesophyllCO2 = mesophyllCO2;
                pathways[index].MesophyllCO2Conductance = mesophyllCO2Conductance;
                pathways[index].StomatalCO2Conductance = stomatalCO2Conductance;
            }
        }

        internal static double UnlimitedCO2Intercept(double ratio, double ambientCO2) =>
            ratio * ambientCO2;

        internal static double UnlimitedCO2Resistance(double ratio,
                                                       double boundaryCO2Conductance,
                                                       double mesophyllCO2Conductance) =>
            ratio / boundaryCO2Conductance + 1.0 / mesophyllCO2Conductance;

        internal static double LimitedCO2Resistance(double totalCO2Conductance,
                                                     double mesophyllCO2Conductance) =>
            1.0 / totalCO2Conductance + 1.0 / mesophyllCO2Conductance;

        internal static double UnlimitedIntercellularCO2(double ratio, double ambientCO2,
                                                          double assimilation,
                                                          double boundaryCO2Conductance) =>
            ratio * (ambientCO2 - assimilation / boundaryCO2Conductance);

        internal static double LimitedIntercellularCO2(double ambientCO2, double assimilation,
                                                        double totalCO2Conductance) =>
            ambientCO2 - assimilation / totalCO2Conductance;

        /// <summary>
        /// Sets the temperature which is needed by the leaf and water interaction.
        /// </summary>
        /// <param name="leafTemperature">Leaf temperature</param>
        public void SetLeafTemperature(double leafTemperature)
        {
            leaf.LeafTemperature = leafTemperature;
            water.LeafTemp = leafTemperature;
        }

        /// <summary>
        /// Signals that the temperature has been updated so that we can recalculate parameters.
        /// </summary>
        public void TemperatureUpdated()
        {
            water.RecalculateParams();
        }

        /// <summary>
        /// Updates the assimilation function for the given pathway.
        /// </summary>
        /// <param name="assimilation">Assimilation model</param>
        /// <param name="pathway">Assimilation pathway</param>
        /// <returns>The updated assimilation function</returns>
        public AssimilationFunction UpdateA(IAssimilation assimilation, AssimilationPathway pathway)
        {
            var func = assimilation.GetFunction(pathway, leaf);

            if (Limited)
            {
                pathway.WaterUse = MaxRate * Fraction;

                // Calculate resistance and conductance
                Resistance = water.LimitedWaterResistance(pathway.WaterUse);
                double Gt = water.TotalCO2Conductance(Resistance);

                // Update function parameters
                func.CmIntercept = ambientCO2;
                func.CmSlope = LimitedCO2Resistance(Gt, leaf.GmT);

                // Update pathway
                pathway.CO2Rate = func.Value();
                assimilation.UpdateIntercellularCO2(pathway, Gt);
                pathway.MesophyllCO2Conductance = leaf.GmT;
                pathway.StomatalCO2Conductance =
                    1.0 / (1.0 / Gt - 1.0 / water.BoundaryCO2Conductance);
            }
            else
            {
                double ratio_Ci_Cbl = this.pathway.IntercellularToBoundaryLayerCO2Ratio;

                // Update function parameters
                func.CmIntercept = UnlimitedCO2Intercept(ratio_Ci_Cbl, ambientCO2);
                func.CmSlope = UnlimitedCO2Resistance(
                    ratio_Ci_Cbl, water.BoundaryCO2Conductance, leaf.GmT);

                // Update pathway
                pathway.CO2Rate = func.Value();
                pathway.IntercellularCO2 = UnlimitedIntercellularCO2(
                    ratio_Ci_Cbl, ambientCO2, pathway.CO2Rate, water.BoundaryCO2Conductance);
                pathway.MesophyllCO2Conductance = leaf.GmT;
                double boundaryLayerCO2 = ambientCO2 -
                    pathway.CO2Rate / water.BoundaryCO2Conductance;
                pathway.StomatalCO2Conductance = pathway.CO2Rate /
                    (boundaryLayerCO2 - pathway.IntercellularCO2);

                Resistance = water.UnlimitedWaterResistance(pathway.CO2Rate, ambientCO2, pathway.IntercellularCO2);
                pathway.WaterUse = water.HourlyWaterUse(Resistance);
            }

            // Update vapor pressure deficit
            pathway.VPD = water.VPD;

            return func;
        }

        /// <summary>
        /// Updates the temperature of a pathway
        /// </summary>
        /// <param name="pathway">Assimilation pathway</param>
        public void UpdateTemperature(AssimilationPathway pathway)
        {
            double leafTemp = water.LeafTemperature(Resistance);
            pathway.Temperature = (leafTemp + pathway.Temperature) / 2.0;
        }
    }
}
