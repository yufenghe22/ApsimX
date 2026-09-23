namespace Models.DCAPST.Canopy
{
    /// <summary>Calculated values for one physical canopy layer.</summary>
    public class CanopyLayerValues
    {
        /// <summary>Sunlit values in this layer.</summary>
        public AreaValues Sunlit { get; set; }

        /// <summary>Shaded values in this layer.</summary>
        public AreaValues Shaded { get; set; }

        /// <summary>Sunlit leaf area index in this layer.</summary>
        public double SunlitLAI { get; set; }

        /// <summary>Shaded leaf area index in this layer.</summary>
        public double ShadedLAI { get; set; }
    }
}
