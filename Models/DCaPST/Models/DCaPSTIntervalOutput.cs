using Models.Core;
using System;
using System.Linq;

namespace Models.DCAPST
{
    /// <summary>
    /// Values calculated by DCaPST for one sub-daily interval.
    /// </summary>
    public class DCaPSTIntervalOutput : EventArgs
    {
        /// <summary>
        /// Creates an empty interval output. This supports discovery of nested
        /// properties by the report variable locator.
        /// </summary>
        public DCaPSTIntervalOutput()
        {
            Layers = Array.Empty<DCaPSTLayerOutput>();
        }

        /// <summary>Creates an output from a calculated DCaPST interval.</summary>
        /// <param name="date">Date on which the interval was calculated.</param>
        /// <param name="interval">Calculated interval values.</param>
        internal DCaPSTIntervalOutput(DateTime date, IntervalValues interval)
        {
            Hour = interval.Time;
            IntervalDateTime = date.Date.AddHours(Hour);
            AirTemperature = interval.AirTemperature;
            SunlitLAI = interval.SunlitLAI;
            ShadedLAI = interval.ShadedLAI;

            SunlitAssimilation = interval.Sunlit.A;
            SunlitWater = interval.Sunlit.Water;
            SunlitTemperature = interval.Sunlit.Temperature;
            SunlitVPD = interval.Sunlit.VPD;
            SunlitIntercellularCO2 = interval.Sunlit.IntercellularCO2;
            SunlitMesophyllCO2 = interval.Sunlit.MesophyllCO2;
            SunlitMesophyllCO2Conductance = interval.Sunlit.MesophyllCO2Conductance;
            SunlitStomatalCO2Conductance = interval.Sunlit.StomatalCO2Conductance;
            SunlitAc1 = interval.Sunlit.Ac1.Assimilation;
            SunlitAc2 = interval.Sunlit.Ac2.Assimilation;
            SunlitAj = interval.Sunlit.Aj.Assimilation;

            ShadedAssimilation = interval.Shaded.A;
            ShadedWater = interval.Shaded.Water;
            ShadedTemperature = interval.Shaded.Temperature;
            ShadedVPD = interval.Shaded.VPD;
            ShadedIntercellularCO2 = interval.Shaded.IntercellularCO2;
            ShadedMesophyllCO2 = interval.Shaded.MesophyllCO2;
            ShadedMesophyllCO2Conductance = interval.Shaded.MesophyllCO2Conductance;
            ShadedStomatalCO2Conductance = interval.Shaded.StomatalCO2Conductance;
            ShadedAc1 = interval.Shaded.Ac1.Assimilation;
            ShadedAc2 = interval.Shaded.Ac2.Assimilation;
            ShadedAj = interval.Shaded.Aj.Assimilation;

            Layers = interval.Layers?
                .Select((values, index) => new DCaPSTLayerOutput
                {
                    Layer = index + 1,
                    SunlitLAI = values.SunlitLAI,
                    SunlitTemperature = values.Sunlit.Temperature,
                    SunlitAssimilation = values.Sunlit.A,
                    SunlitWater = values.Sunlit.Water,
                    SunlitIntercellularCO2 = values.Sunlit.IntercellularCO2,
                    SunlitMesophyllCO2 = values.Sunlit.MesophyllCO2,
                    SunlitMesophyllCO2Conductance = values.Sunlit.MesophyllCO2Conductance,
                    SunlitStomatalCO2Conductance = values.Sunlit.StomatalCO2Conductance,
                    SunlitAc1 = values.Sunlit.Ac1.Assimilation,
                    SunlitAc2 = values.Sunlit.Ac2.Assimilation,
                    SunlitAj = values.Sunlit.Aj.Assimilation,
                    ShadedLAI = values.ShadedLAI,
                    ShadedTemperature = values.Shaded.Temperature,
                    ShadedAssimilation = values.Shaded.A,
                    ShadedWater = values.Shaded.Water,
                    ShadedIntercellularCO2 = values.Shaded.IntercellularCO2,
                    ShadedMesophyllCO2 = values.Shaded.MesophyllCO2,
                    ShadedMesophyllCO2Conductance = values.Shaded.MesophyllCO2Conductance,
                    ShadedStomatalCO2Conductance = values.Shaded.StomatalCO2Conductance,
                    ShadedAc1 = values.Shaded.Ac1.Assimilation,
                    ShadedAc2 = values.Shaded.Ac2.Assimilation,
                    ShadedAj = values.Shaded.Aj.Assimilation
                })
                .ToArray() ?? Array.Empty<DCaPSTLayerOutput>();

            double totalLAI = SunlitLAI + ShadedLAI;
            CanopyTemperature = LAIWeightedMean(SunlitTemperature, SunlitLAI, ShadedTemperature, ShadedLAI, totalLAI);
            CanopyVPD = LAIWeightedMean(SunlitVPD, SunlitLAI, ShadedVPD, ShadedLAI, totalLAI);
        }

        /// <summary>Date and time of the interval.</summary>
        public DateTime IntervalDateTime { get; private set; }

        /// <summary>Hour of the interval.</summary>
        [Units("hours")]
        public double Hour { get; private set; }

        /// <summary>Air temperature during the interval.</summary>
        [Units("°C")]
        public double AirTemperature { get; private set; }

        /// <summary>Values for each physical canopy layer, ordered from top to bottom.</summary>
        public DCaPSTLayerOutput[] Layers { get; private set; }

        /// <summary>Leaf area index of the sunlit canopy.</summary>
        [Units("m^2/m^2")]
        public double SunlitLAI { get; private set; }

        /// <summary>Leaf area index of the shaded canopy.</summary>
        [Units("m^2/m^2")]
        public double ShadedLAI { get; private set; }

        /// <summary>LAI-weighted canopy temperature.</summary>
        [Units("°C")]
        public double CanopyTemperature { get; private set; }

        /// <summary>LAI-weighted canopy vapour pressure deficit.</summary>
        [Units("kPa")]
        public double CanopyVPD { get; private set; }

        /// <summary>Sunlit canopy assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAssimilation { get; private set; }

        /// <summary>Sunlit canopy water use.</summary>
        [Units("mm")]
        public double SunlitWater { get; private set; }

        /// <summary>Sunlit canopy temperature.</summary>
        [Units("°C")]
        public double SunlitTemperature { get; private set; }

        /// <summary>Sunlit canopy vapour pressure deficit.</summary>
        [Units("kPa")]
        public double SunlitVPD { get; private set; }

        /// <summary>Sunlit intercellular CO2 partial pressure.</summary>
        [Units("microbar")]
        public double SunlitIntercellularCO2 { get; private set; }

        /// <summary>Sunlit mesophyll CO2 partial pressure.</summary>
        [Units("microbar")]
        public double SunlitMesophyllCO2 { get; private set; }

        /// <summary>Sunlit mesophyll CO2 conductance.</summary>
        [Units("mol CO2/m^2/s/bar")]
        public double SunlitMesophyllCO2Conductance { get; private set; }

        /// <summary>Sunlit stomatal CO2 conductance.</summary>
        [Units("mol CO2/m^2/s")]
        public double SunlitStomatalCO2Conductance { get; private set; }

        /// <summary>Sunlit AC1 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAc1 { get; private set; }

        /// <summary>Sunlit AC2 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAc2 { get; private set; }

        /// <summary>Sunlit AJ pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAj { get; private set; }

        /// <summary>Shaded canopy assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAssimilation { get; private set; }

        /// <summary>Shaded canopy water use.</summary>
        [Units("mm")]
        public double ShadedWater { get; private set; }

        /// <summary>Shaded canopy temperature.</summary>
        [Units("°C")]
        public double ShadedTemperature { get; private set; }

        /// <summary>Shaded canopy vapour pressure deficit.</summary>
        [Units("kPa")]
        public double ShadedVPD { get; private set; }

        /// <summary>Shaded intercellular CO2 partial pressure.</summary>
        [Units("microbar")]
        public double ShadedIntercellularCO2 { get; private set; }

        /// <summary>Shaded mesophyll CO2 partial pressure.</summary>
        [Units("microbar")]
        public double ShadedMesophyllCO2 { get; private set; }

        /// <summary>Shaded mesophyll CO2 conductance.</summary>
        [Units("mol CO2/m^2/s/bar")]
        public double ShadedMesophyllCO2Conductance { get; private set; }

        /// <summary>Shaded stomatal CO2 conductance.</summary>
        [Units("mol CO2/m^2/s")]
        public double ShadedStomatalCO2Conductance { get; private set; }

        /// <summary>Shaded AC1 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAc1 { get; private set; }

        /// <summary>Shaded AC2 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAc2 { get; private set; }

        /// <summary>Shaded AJ pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAj { get; private set; }

        /// <summary>Calculates a canopy mean weighted by sunlit and shaded leaf area.</summary>
        private static double LAIWeightedMean(double sunlitValue, double sunlitLAI, double shadedValue, double shadedLAI, double totalLAI)
        {
            if (totalLAI <= 0)
                return 0;

            return (sunlitValue * sunlitLAI + shadedValue * shadedLAI) / totalLAI;
        }
    }
}
