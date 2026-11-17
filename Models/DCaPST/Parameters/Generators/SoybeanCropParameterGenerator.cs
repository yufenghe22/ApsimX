namespace Models.DCAPST
{
    /// <summary>
    /// Provisional C3 parameters for soybean integration development only.
    /// All values are borrowed from the wheat generator, including leaflet width,
    /// and require independent soybean calibration before scientific use.
    /// </summary>
    public static class SoybeanCropParameterGenerator
    {
        /// <summary>The registered crop name.</summary>
        public const string CROP_NAME = "soybean";

        /// <summary>
        /// Creates an independent parameter set using the existing C3 diagnostic defaults.
        /// LeafWidth is a leaflet dimension in metres, not SimpleLeaf canopy width.
        /// MinimumN is in mmol N/m2 leaf and must not be subtracted from incoming SLN.
        /// </summary>
        public static DCaPSTParameters Generate()
        {
            return WheatCropParameterGenerator.Generate();
        }
    }
}
