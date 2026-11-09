using Models.Core;

namespace Models.DCAPST
{
    /// <summary>Report-facing DCaPST values for one physical canopy layer.</summary>
    public class DCaPSTLayerOutput
    {
        /// <summary>One-based physical canopy layer number.</summary>
        public int Layer { get; internal set; }

        /// <summary>Sunlit leaf area index in this layer.</summary>
        [Units("m^2/m^2")]
        public double SunlitLAI { get; internal set; }

        /// <summary>Sunlit leaf temperature in this layer.</summary>
        [Units("°C")]
        public double SunlitTemperature { get; internal set; }

        /// <summary>Sunlit assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAssimilation { get; internal set; }

        /// <summary>Sunlit transpiration in this layer.</summary>
        [Units("mm")]
        public double SunlitWater { get; internal set; }

        /// <summary>Sunlit intercellular CO2 partial pressure in this layer.</summary>
        [Units("microbar")]
        public double SunlitIntercellularCO2 { get; internal set; }

        /// <summary>Sunlit mesophyll CO2 partial pressure in this layer.</summary>
        [Units("microbar")]
        public double SunlitMesophyllCO2 { get; internal set; }

        /// <summary>Sunlit mesophyll CO2 conductance in this layer.</summary>
        [Units("mol CO2/m^2/s/bar")]
        public double SunlitMesophyllCO2Conductance { get; internal set; }

        /// <summary>Sunlit stomatal CO2 conductance in this layer.</summary>
        [Units("mol CO2/m^2/s")]
        public double SunlitStomatalCO2Conductance { get; internal set; }

        /// <summary>Sunlit AC1 pathway assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAc1 { get; internal set; }

        /// <summary>Sunlit AC2 pathway assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAc2 { get; internal set; }

        /// <summary>Sunlit AJ pathway assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAj { get; internal set; }

        /// <summary>Shaded leaf area index in this layer.</summary>
        [Units("m^2/m^2")]
        public double ShadedLAI { get; internal set; }

        /// <summary>Shaded leaf temperature in this layer.</summary>
        [Units("°C")]
        public double ShadedTemperature { get; internal set; }

        /// <summary>Shaded assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAssimilation { get; internal set; }

        /// <summary>Shaded transpiration in this layer.</summary>
        [Units("mm")]
        public double ShadedWater { get; internal set; }

        /// <summary>Shaded intercellular CO2 partial pressure in this layer.</summary>
        [Units("microbar")]
        public double ShadedIntercellularCO2 { get; internal set; }

        /// <summary>Shaded mesophyll CO2 partial pressure in this layer.</summary>
        [Units("microbar")]
        public double ShadedMesophyllCO2 { get; internal set; }

        /// <summary>Shaded mesophyll CO2 conductance in this layer.</summary>
        [Units("mol CO2/m^2/s/bar")]
        public double ShadedMesophyllCO2Conductance { get; internal set; }

        /// <summary>Shaded stomatal CO2 conductance in this layer.</summary>
        [Units("mol CO2/m^2/s")]
        public double ShadedStomatalCO2Conductance { get; internal set; }

        /// <summary>Shaded AC1 pathway assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAc1 { get; internal set; }

        /// <summary>Shaded AC2 pathway assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAc2 { get; internal set; }

        /// <summary>Shaded AJ pathway assimilation in this layer.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAj { get; internal set; }
    }
}
