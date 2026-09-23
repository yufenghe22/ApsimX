namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// An instance of values present within an assimilation area
    /// </summary>
    public struct AreaValues
    {
        /// <summary>
        /// 
        /// </summary>
        public double A;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public double Water;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public double Temperature;

        /// <summary>
        /// Vapour pressure deficit for the limiting assimilation pathway.
        /// </summary>
        public double VPD;

        /// <summary>Intercellular CO2 partial pressure for the limiting pathway.</summary>
        public double IntercellularCO2;

        /// <summary>Mesophyll CO2 partial pressure for the limiting pathway.</summary>
        public double MesophyllCO2;

        /// <summary>Mesophyll CO2 conductance for the limiting pathway.</summary>
        public double MesophyllCO2Conductance;

        /// <summary>Stomatal CO2 conductance for the limiting pathway.</summary>
        public double StomatalCO2Conductance;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public PathValues Ac1;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public PathValues Ac2;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public PathValues Aj;
    }
}
