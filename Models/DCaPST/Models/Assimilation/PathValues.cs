namespace Models.DCAPST
{
    /// <summary>
    /// 
    /// </summary>
    public struct PathValues
    {
        /// <summary>
        /// 
        /// </summary>
        public double Assimilation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Water { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double VPD { get; set; }

        /// <summary>Intercellular CO2 partial pressure (microbar).</summary>
        public double IntercellularCO2 { get; set; }

        /// <summary>Mesophyll CO2 partial pressure (microbar).</summary>
        public double MesophyllCO2 { get; set; }

        /// <summary>Mesophyll CO2 conductance.</summary>
        public double MesophyllCO2Conductance { get; set; }

        /// <summary>Stomatal CO2 conductance.</summary>
        public double StomatalCO2Conductance { get; set; }
    }
}
