using System;
using APSIM.Core;
using Models.Core;
using Models.Functions.SupplyFunctions;
using Models.PMF;
using Newtonsoft.Json;

namespace Models.DCAPST
{
    /// <summary>
    /// Opt-in daily whole-plant DM supply for SimpleLeaf. Inheriting the RUE model
    /// preserves its child paths, cultivar overrides and shared stress functions.
    /// Disabled mode evaluates the original RUE calculation unchanged.
    /// The water/carbon coupling must publish a current-day result before enabled use.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SimpleLeafDCaPSTSupply : RUEModel, IFunction
    {
        [Link]
        private IClock clock = null;

        [Link]
        private Plant plant = null;

        private DateTime supplyDate = DateTime.MinValue;
        private double dailySupply;

        /// <summary>Select DCaPST supply instead of the inherited RUE calculation.</summary>
        [Description("Use cached DCaPST whole-plant DM supply (requires water/carbon coupling)")]
        public bool UseDCaPST { get; set; }

        /// <summary>The current day's whole-plant supply, including roots.</summary>
        [JsonIgnore]
        [Units("g/m2/d")]
        public double DailySupply => supplyDate == clock?.Today ? dailySupply : 0;

        /// <summary>Cache water-limited whole-plant DM, without additional RUE stress multipliers.</summary>
        public void SetDailySupply(double wholePlantDryMatter)
        {
            if (!double.IsFinite(wholePlantDryMatter) || wholePlantDryMatter < 0)
                throw new ArgumentOutOfRangeException(nameof(wholePlantDryMatter), "DCaPST DM supply must be finite and nonnegative.");

            dailySupply = wholePlantDryMatter;
            supplyDate = clock.Today;
        }

        /// <summary>Return the original RUE supply or an already calculated daily DCaPST supply.</summary>
        public new double Value(int arrayIndex = -1)
        {
            if (!UseDCaPST)
                return base.Value(arrayIndex);

            if (supplyDate != clock.Today)
                throw new InvalidOperationException("DCaPST photosynthesis requires today's water-limited whole-plant DM supply before SetDMSupply. The soybean water/carbon coupling is not yet configured.");

            return dailySupply;
        }

        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs args)
        {
            ClearSupply();
        }

        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters args)
        {
            if (args.Plant == plant)
                ClearSupply();
        }

        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs args)
        {
            if (sender == plant)
                ClearSupply();
        }

        private void ClearSupply()
        {
            supplyDate = DateTime.MinValue;
            dailySupply = 0;
        }
    }
}
