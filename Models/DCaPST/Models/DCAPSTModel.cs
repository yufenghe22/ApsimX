using Models.DCAPST.Canopy;
using Models.DCAPST.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.DCAPST
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class DCAPSTModel : IPhotosynthesisModel
    {
        /// <summary>
        /// The solar geometry
        /// </summary>
        private ISolarGeometry Solar { get; set; }

        /// <summary>
        /// The solar radiation
        /// </summary>
        private ISolarRadiation Radiation { get; set; }

        /// <summary>
        /// The environmental temperature
        /// </summary>
        private ITemperature Temperature { get; set; }

        /// <summary>
        /// The canopy undergoing photosynthesis
        /// </summary>
        private IReadOnlyList<ICanopyAttributes> Canopies { get; set; }

        /// <summary>
        /// The pathway parameters
        /// </summary>
        private readonly PathwayParameters pathway;

        /// <summary>
        /// The transpiration model
        /// </summary>
        private readonly Transpiration transpiration;

        /// <summary>
        /// A public option to toggle if the interval values are tracked or not
        /// </summary>
        public bool PrintIntervalValues { get; set; } = false;

        /// <summary>
        /// The biological transpiration limit of a plant
        /// </summary>
        public double Biolimit { get; set; } = 0;

        /// <summary>
        /// Excess water reduction fraction
        /// </summary>
        public double Reduction { get; set; } = 0;

        /// <summary>
        /// Used to track the interval values that are printed
        /// </summary>
        public string IntervalResults { get; private set; } = "";

        /// <summary>
        /// Biochemical Conversion and Maintenance Respiration
        /// </summary>
        public double B { get; set; } = 0.409;

        /// <summary>
        /// Total Potential daily biomass (Above ground + Roots)
        /// </summary>
        public double TotalPotentialBiomass { get; private set; }

        /// <summary>
        /// Total Actual daily biomass (Above ground + Roots)
        /// </summary>
        public double TotalActualBiomass { get; private set; }

        /// <summary>
        /// Potential total daily biomass (Above ground)
        /// </summary>
        public double PotentialBiomass { get; private set; }

        /// <summary>
        /// Actual total daily biomass (Above ground)
        /// </summary>
        public double ActualBiomass { get; private set; }

        /// <summary>
        /// The actual root biomass
        /// </summary>
        public double ActualRootBiomass { get; private set; }

        /// <summary>
        /// Daily water demand
        /// </summary>
        public double WaterDemanded { get; private set; }

        /// <summary>
        /// Daily water supplied
        /// </summary>
        public double WaterSupplied { get; private set; }

        /// <summary>
        /// The root shoot ratio used
        /// </summary>
        public double RootShootRatio { get; private set; }

        /// <summary>
        /// Daily intercepted radiation
        /// </summary>
        public double InterceptedRadiation { get; private set; }

        /// <summary>
        /// The water demands, taken from the sunlit and shaded areas.
        /// </summary>
        public List<double> WaterDemands { get; private set; } = new();

        /// <summary>
        /// This is the total potential biomass which will be limited when under water stress.
        /// </summary>
        public double CalculatedTotalPotentialBiomass { get; private set; }

        /// <summary>
        /// Contains the information for each interval throughout the day.
        /// </summary>
        public IntervalValues[] Intervals;

        private readonly double start = 6.0;
        private readonly double end = 18.0;
        private readonly double timestep = 1.0;

        // Seconds in an hour
        private const double SECONDS_IN_HOUR = 3600;

        // 1,000,000 mmol to mol
        private const double MMOL_TO_MOL = 1000000;

        // 44 mol wt CO2
        private const double MOL_WT_CO2 = 44;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public DCAPSTModel()
        {
            Solar = default;
            Radiation = default;
            Temperature = default;
            this.pathway = default;
            Canopies = Array.Empty<ICanopyAttributes>();
            transpiration = default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="solar"></param>
        /// <param name="radiation"></param>
        /// <param name="temperature"></param>
        /// <param name="pathway"></param>
        /// <param name="canopy"></param>
        /// <param name="trans"></param>
        public DCAPSTModel(
            ISolarGeometry solar,
            ISolarRadiation radiation,
            ITemperature temperature,
            PathwayParameters pathway,
            ICanopyAttributes canopy,
            Transpiration trans
        )
        {
            Solar = solar;
            Radiation = radiation;
            Temperature = temperature;
            this.pathway = pathway;
            Canopies = new[] { canopy };
            transpiration = trans;
        }

        /// <summary>Creates a photosynthesis model with one object per physical canopy layer.</summary>
        public DCAPSTModel(ISolarGeometry solar, ISolarRadiation radiation, ITemperature temperature,
                           PathwayParameters pathway, IReadOnlyList<ICanopyAttributes> canopies,
                           Transpiration trans)
        {
            Solar = solar;
            Radiation = radiation;
            Temperature = temperature;
            this.pathway = pathway;
            Canopies = canopies ?? throw new ArgumentNullException(nameof(canopies));
            if (Canopies.Count == 0)
                throw new ArgumentException("At least one canopy layer is required.", nameof(canopies));
            transpiration = trans;
        }

        /// <summary>
        /// Calculates the potential and actual biomass growth of a canopy across the span of a day,
        /// as well as the water requirements for both cases.
        /// </summary>
        public void DailyRun(
            double lai,
            double sln
        )
        {
            WaterDemands.Clear();

            var steps = (end - start) / timestep;
            if (steps % 1 == 0) steps++;

            Intervals = Enumerable.Range(0, (int)Math.Ceiling(steps))
                .Select(i => new IntervalValues() { Time = start + i * timestep })
                .ToArray();

            Solar.Initialise();
            foreach (ICanopyAttributes canopy in Canopies)
                canopy.InitialiseDay(lai, sln);

            // Unlimited potential calculations
            // Note: In the potential case, we assume unlimited water and therefore supply = demand
            transpiration.Limited = false;
            CalculatedTotalPotentialBiomass = CalculatePotential();
            WaterDemands = Intervals.Select(i => i.Sunlit.Water + i.Shaded.Water).ToList();

            // Check if the plant is biologically self-limiting
            if (Biolimit > 0)
            {
                // Bio-limited calculations
                transpiration.Limited = true;

                // Percentile reduction
                if (Reduction > 0)
                {
                    WaterDemands = WaterDemands.Select(w => ReductionFunction(w, Biolimit, Reduction)).ToList();
                }
                // Truncation
                else
                {
                    // Reduce to the flat biological limit
                    WaterDemands = WaterDemands.Select(w => Math.Min(w, Biolimit)).ToList();
                }

                CalculatedTotalPotentialBiomass = CalculateLimited(WaterDemands);
            }

            WaterDemanded = WaterDemands.Sum();
        }

        /// <summary>
        /// Calculates the biomass, using the supply that was calculated from the soil and
        /// the demand that was calculated by dcapst (CalculateDemand).
        /// </summary>
        /// <param name="soilWaterAvailable"></param>
        /// <param name="rootShootRatio"></param>
        public void CalculateBiomass(double soilWaterAvailable, double rootShootRatio)
        {
            RootShootRatio = rootShootRatio;

            // Initialise the water supply and actual biomass to their potential values.
            WaterSupplied = WaterDemanded;
            var calculatedTotalActualBiomass = CalculatedTotalPotentialBiomass;            
            
            // If we are limited by the soil water available.
            if (soilWaterAvailable < WaterDemanded)
            {
                transpiration.Limited = true;

                var limitedSupply = CalculateWaterSupplyLimits(soilWaterAvailable, WaterDemands);
                calculatedTotalActualBiomass = CalculateActual(limitedSupply.ToArray());
                WaterSupplied = limitedSupply.Sum();
            }

            // Now perform the biomass calculations.
            TotalActualBiomass = GetBiomassConversionFactor(calculatedTotalActualBiomass);
            var rootShootRatioDivisor = 1 + RootShootRatio;
            ActualBiomass = TotalActualBiomass / rootShootRatioDivisor;
            // The actual root biomass is the difference between the TotalActual,
            // which includes the above and below ground biomass, and the Actual,
            // which is the actual biomass minus the roots.
            ActualRootBiomass = TotalActualBiomass - ActualBiomass;
            TotalPotentialBiomass = GetBiomassConversionFactor(CalculatedTotalPotentialBiomass);
            PotentialBiomass = TotalPotentialBiomass / rootShootRatioDivisor;
        }

        private double GetBiomassConversionFactor(double biomass)
        {
            return biomass * SECONDS_IN_HOUR / MMOL_TO_MOL * MOL_WT_CO2 * B;
        }

        /// <summary>
        /// Reduces the value of any excess water past the limit by a given percentage
        /// </summary>
        /// <param name="water">The total water</param>
        /// <param name="limit">The water limit</param>
        /// <param name="percent">The precentage to reduce excess water by</param>
        /// <returns>Water with reduced excess</returns>
        private static double ReductionFunction(double water, double limit, double percent)
        {
            if (water < limit) return water;

            // Find amount of water past the limit
            var excess = water - limit;

            // Reduce the excess by the percentage
            var reduced = excess * percent;

            return limit + reduced;
        }

        /// <summary>
        /// Attempt to initialise models based on the current time, and test if they are sensible
        /// </summary>
        private bool TryInitiliase(IntervalValues intervalValues)
        {
            Temperature.UpdateAirTemperature(intervalValues.Time);
            Radiation.UpdateRadiationValues(intervalValues.Time);
            var sunAngle = Solar.SunAngle(intervalValues.Time);
            foreach (ICanopyAttributes canopy in Canopies)
                canopy.DoSolarAdjustment(sunAngle);

            if (IsSensible()) return true;

            intervalValues.Sunlit = new AreaValues();
            intervalValues.Shaded = new AreaValues();

            return false;
        }

        /// <summary>
        /// Tests if the basic conditions for photosynthesis to occur are met
        /// </summary>
        private bool IsSensible()
        {
            var temp = Temperature.AirTemperature;

            bool[] tempConditions = new bool[2]
            {
                temp > pathway.ElectronTransportRateParams.TMax,
                temp < pathway.ElectronTransportRateParams.TMin,
            };

            bool invalidTemp = tempConditions.Any(b => b == true);
            bool invalidRadn = Radiation.Total <= double.Epsilon;

            if (invalidTemp || invalidRadn) return false;
            
            return true;
        }

        /// <summary>
        /// Determine the total potential biomass for the day under ideal conditions
        /// </summary>
        private double CalculatePotential()
        {
            foreach (var interval in Intervals)
            {
                if (!TryInitiliase(interval)) continue;

                InterceptedRadiation += Radiation.Total * Canopies[Canopies.Count - 1].GetInterceptedRadiation() * SECONDS_IN_HOUR;

                DoTimestepUpdate(interval);
            }

            return Intervals.Select(i => i.Sunlit.A + i.Shaded.A).Sum();
        }

        /// <summary>
        /// Calculates the potential biomass in the case where a plant has a biological limit
        /// on transpiration rate
        /// </summary>
        private double CalculateLimited(IEnumerable<double> demands)
        {
            double[] limitedDemands = demands.ToArray();

            for (int i = 0; i < Intervals.Length; i++)
            {
                var interval = Intervals[i];

                if (!TryInitiliase(interval)) continue;

                DoTimestepUpdate(interval, limitedDemands[i]);
            }

            return Intervals.Select(i => i.Sunlit.A + i.Shaded.A).Sum();
        }

        /// <summary>
        /// Determine the total biomass that can be assimilated under the actual conditions 
        /// </summary>
        /// <summary>
        /// Calculate the total biomass assimilated under the given water supply conditions.
        /// </summary>
        /// <param name="waterSupply">An array of water supply values for each interval.</param>
        /// <returns>The total assimilated biomass.</returns>
        /// <exception cref="ArgumentNullException">Thrown if waterSupply or Intervals is null.</exception>
        /// <exception cref="ArgumentException">Thrown if waterSupply and Intervals lengths do not match.</exception>
        private double CalculateActual(double[] waterSupply)
        {
            if (waterSupply == null)
                throw new ArgumentNullException(nameof(waterSupply), "Water supply array cannot be null.");
            if (Intervals == null)
                throw new ArgumentNullException(nameof(Intervals), "Intervals array cannot be null.");
            if (waterSupply.Length != Intervals.Length)
                throw new ArgumentException("Water supply and Intervals must have the same length.");

            double total = 0;

            for (int i = 0; i < Intervals.Length; i++)
            {
                var interval = Intervals[i];
                if (interval == null)
                {
                    // Log a warning or skip if interval is unexpectedly null
                    continue;
                }

                // Access water supply and demand with bounds checking
                double intervalWaterSupply = i < waterSupply.Length ? waterSupply[i] : 0;
                double intervalWaterDemand = i < WaterDemands.Count ? WaterDemands[i] : 0;

                // Recalculate if the interval is limited
                if (Math.Abs(intervalWaterSupply - intervalWaterDemand) > double.Epsilon)
                {
                    if (!TryInitiliase(interval))
                        continue; // Skip if initialization fails

                    if (intervalWaterDemand > 0)
                        DoTimestepUpdate(interval, intervalWaterSupply);
                }

                // Accumulate total biomass
                total += interval.Sunlit.A + interval.Shaded.A;
            }

            return total;
        }


        /// <summary>
        /// Updates the model to a new timestep
        /// </summary>
        private void DoTimestepUpdate(IntervalValues interval, double? waterSupply = null)
        {
            CanopyLayerValues[] potentialLayers = interval.Layers;
            double potentialDemand = potentialLayers?.Sum(layer => layer.Sunlit.Water + layer.Shaded.Water) ?? 0;
            var layerValues = new CanopyLayerValues[Canopies.Count];
            interval.AirTemperature = Temperature.AirTemperature;

            for (int index = 0; index < Canopies.Count; index++)
            {
                ICanopyAttributes canopy = Canopies[index];
                canopy.DoTimestepAdjustment(Radiation);
                double totalHeat = canopy.CalcBoundaryHeatConductance();
                double sunlitHeat = canopy.CalcSunlitBoundaryHeatConductance();
                double shadedHeat = Math.Max(double.Epsilon, totalHeat - sunlitHeat);
                double sunFraction = waterSupply.HasValue && potentialDemand > 0
                    ? potentialLayers[index].Sunlit.Water / potentialDemand
                    : 0;
                double shadeFraction = waterSupply.HasValue && potentialDemand > 0
                    ? potentialLayers[index].Shaded.Water / potentialDemand
                    : 0;

                if (waterSupply.HasValue)
                    transpiration.MaxRate = waterSupply.Value;
                PerformPhotosynthesis(canopy.Sunlit, sunlitHeat, sunFraction);
                PerformPhotosynthesis(canopy.Shaded, shadedHeat, shadeFraction);
                layerValues[index] = new CanopyLayerValues
                {
                    SunlitLAI = canopy.Sunlit.LAI,
                    ShadedLAI = canopy.Shaded.LAI,
                    Sunlit = canopy.Sunlit.GetAreaValues(),
                    Shaded = canopy.Shaded.GetAreaValues()
                };
            }

            interval.Layers = layerValues;
            interval.SunlitLAI = layerValues.Sum(layer => layer.SunlitLAI);
            interval.ShadedLAI = layerValues.Sum(layer => layer.ShadedLAI);
            interval.Sunlit = AggregateArea(layerValues, true, interval.SunlitLAI);
            interval.Shaded = AggregateArea(layerValues, false, interval.ShadedLAI);
        }

        private static AreaValues AggregateArea(IEnumerable<CanopyLayerValues> layers, bool sunlit, double totalLai)
        {
            CanopyLayerValues[] values = layers.ToArray();
            AreaValues[] areas = values.Select(layer => sunlit ? layer.Sunlit : layer.Shaded).ToArray();
            double[] lais = values.Select(layer => sunlit ? layer.SunlitLAI : layer.ShadedLAI).ToArray();
            return new AreaValues
            {
                A = areas.Sum(area => area.A),
                Water = areas.Sum(area => area.Water),
                Temperature = WeightedMean(areas.Select(area => area.Temperature).ToArray(), lais, totalLai),
                VPD = WeightedMean(areas.Select(area => area.VPD).ToArray(), lais, totalLai),
                Ac1 = AggregatePath(areas.Select(area => area.Ac1).ToArray(), lais, totalLai),
                Ac2 = AggregatePath(areas.Select(area => area.Ac2).ToArray(), lais, totalLai),
                Aj = AggregatePath(areas.Select(area => area.Aj).ToArray(), lais, totalLai)
            };
        }

        private static PathValues AggregatePath(PathValues[] paths, double[] lais, double totalLai)
        {
            return new PathValues
            {
                Assimilation = paths.Sum(path => path.Assimilation),
                Water = paths.Sum(path => path.Water),
                Temperature = WeightedMean(paths.Select(path => path.Temperature).ToArray(), lais, totalLai),
                VPD = WeightedMean(paths.Select(path => path.VPD).ToArray(), lais, totalLai)
            };
        }

        private static double WeightedMean(double[] values, double[] weights, double totalWeight)
        {
            if (totalWeight <= 0)
                return 0;
            return values.Select((value, index) => value * weights[index]).Sum() / totalWeight;
        }

        /// <summary>
        /// Runs the photosynthesis simulation for an assimilating area
        /// </summary>
        /// <param name="area">The area to run photosynthesis for</param>
        /// <param name="gbh">The boundary heat conductance</param>
        /// <param name="fraction">Fraction of water allowance</param>
        private void PerformPhotosynthesis(IAssimilationArea area, double gbh, double fraction)
        {
            transpiration.BoundaryHeatConductance = gbh;
            transpiration.Fraction = fraction;
            area.DoPhotosynthesis(Temperature, transpiration);
        }

        /// <summary>
        /// In the case where there is greater water demand than supply allows, the water supply limit for each hour
        /// must be calculated. 
        /// 
        /// This is done by adjusting the maximum rate of water supply each hour, until the total water demand across
        /// the day is within some tolerance of the actual water available, as we want to make use of all the 
        /// accessible water.
        /// </summary>
        private static IEnumerable<double> CalculateWaterSupplyLimits(double soilWaterAvail, IEnumerable<double> demand)
        {
            const double tolerance = 0.000001;

            double initialDemand = demand.Sum();

            // If the total demand is less than available water, return the original demands.
            if (initialDemand < soilWaterAvail) return demand;

            // If water availability is negligible, return zeros for all demands.
            if (soilWaterAvail < tolerance) return demand.Select(_ => 0.0);

            // Start with the maximum possible rate.
            double maxDemandRate = demand.Max();
            // Minimum rate starts at zero.
            double minDemandRate = 0;
            double averageDemandRate = 0;
            double dailyDemand = initialDemand;

            // Perform binary search until the difference between demand and supply is within tolerance.
            while (Math.Abs(dailyDemand - soilWaterAvail) > tolerance)
            {
                averageDemandRate = (maxDemandRate + minDemandRate) / 2;

                // Calculate the total daily demand with the current average demand rate.
                dailyDemand = 0;
                foreach (var d in demand)
                {
                    dailyDemand += d > averageDemandRate ? averageDemandRate : d;
                }

                // Adjust demand bounds based on whether the demand exceeds the supply.
                if (dailyDemand < soilWaterAvail)
                {
                    minDemandRate = averageDemandRate;
                }
                else
                {
                    maxDemandRate = averageDemandRate;
                }
            }

            // Apply the final demand rate limit to all demands.
            return demand.Select(d => d > averageDemandRate ? averageDemandRate : d);
        }
    }
}
