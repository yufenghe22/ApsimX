using Models.DCAPST;
using Models.DCAPST.Canopy;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class DCaPSTRPackageParityTests
    {
        [TestCase(1, 2.2, 6.121926, 6.121926, 1.318704, 1.318704)]
        [TestCase(3, 2.2, 5.872761, 5.872761, 1.276771, 1.276771)]
        [TestCase(1, 0.8, 6.121926, 4.351701, 1.318704, 0.8000006)]
        [TestCase(3, 0.8, 5.872761, 4.317080, 1.276771, 0.8000001)]
        public void ReferenceScenarioMatchesRPackage(int layerCount, double availableWater,
            double expectedPotentialBiomass, double expectedActualBiomass,
            double expectedWaterDemand, double expectedWaterSupply)
        {
            DCAPSTModel model = CreateModel(layerCount);

            model.DailyRun(1.0, 0.914);
            model.CalculateBiomass(availableWater, 0.25);

            Assert.Multiple(() =>
            {
                Assert.That(model.PotentialBiomass,
                    Is.EqualTo(expectedPotentialBiomass).Within(expectedPotentialBiomass * 5e-4));
                Assert.That(model.ActualBiomass,
                    Is.EqualTo(expectedActualBiomass).Within(expectedActualBiomass * 5e-4));
                Assert.That(model.WaterDemanded,
                    Is.EqualTo(expectedWaterDemand).Within(expectedWaterDemand * 5e-4));
                Assert.That(model.WaterSupplied,
                    Is.EqualTo(expectedWaterSupply).Within(Math.Max(1e-5, expectedWaterSupply * 5e-4)));
            });
        }

        [TestCase(1, 6.259191085304, 1.385800969655, 13.1946572439331, 0.167877103310931)]
        [TestCase(3, 5.993056288031, 1.346770649759, 12.5385109206498, 0.162537137332654)]
        public void OneVsThreeLayerExampleMatchesRPackage(int layerCount,
            double expectedPotentialBiomass, double expectedWaterDemand,
            double expectedNoonAssimilation, double expectedNoonWater)
        {
            DCAPSTModel model = CreateModel(layerCount, windSpeed: 0.5);

            model.DailyRun(1.0, 0.914);
            model.CalculateBiomass(2.2, 0.25);
            IntervalValues noon = model.Intervals[6];

            Assert.Multiple(() =>
            {
                Assert.That(model.PotentialBiomass,
                    Is.EqualTo(expectedPotentialBiomass).Within(expectedPotentialBiomass * 5e-4));
                Assert.That(model.WaterDemanded,
                    Is.EqualTo(expectedWaterDemand).Within(expectedWaterDemand * 5e-4));
                Assert.That(noon.Sunlit.A + noon.Shaded.A,
                    Is.EqualTo(expectedNoonAssimilation).Within(expectedNoonAssimilation * 1e-3));
                Assert.That(noon.Sunlit.Water + noon.Shaded.Water,
                    Is.EqualTo(expectedNoonWater).Within(expectedNoonWater * 1e-3));
            });
        }

        private static DCAPSTModel CreateModel(int layerCount, double windSpeed = 3)
        {
            DCaPSTParameters parameters = SorghumCropParameterGenerator.Generate();
            var solar = new SolarGeometry
            {
                DayOfYear = 78,
                Latitude = -28.21 * Math.PI / 180.0
            };
            var radiation = new SolarRadiation(solar) { Daily = 23, RPAR = parameters.Rpar };
            var temperature = new Temperature(solar)
            {
                MinTemperature = 12.8,
                MaxTemperature = 30.1,
                AtmosphericPressure = 1.01325
            };
            IAssimilation assimilation = new AssimilationC4(
                parameters, parameters.Canopy, parameters.Pathway, 363);
            var canopies = new List<ICanopyAttributes>();
            for (int layer = 1; layer <= layerCount; layer++)
            {
                var sunlit = new AssimilationArea(true,
                    new AssimilationPathway(parameters.Canopy, parameters.Pathway, 363),
                    new AssimilationPathway(parameters.Canopy, parameters.Pathway, 363),
                    new AssimilationPathway(parameters.Canopy, parameters.Pathway, 363), assimilation);
                var shaded = new AssimilationArea(true,
                    new AssimilationPathway(parameters.Canopy, parameters.Pathway, 363),
                    new AssimilationPathway(parameters.Canopy, parameters.Pathway, 363),
                    new AssimilationPathway(parameters.Canopy, parameters.Pathway, 363), assimilation);
                canopies.Add(new CanopyAttributes(parameters, sunlit, shaded, windSpeed, layer, layerCount));
            }
            var transpiration = new Transpiration(
                parameters.Canopy, parameters.Pathway, new WaterInteraction(temperature),
                new TemperatureResponse(parameters.Canopy, parameters.Pathway), 363);
            return new DCAPSTModel(solar, radiation, temperature, parameters.Pathway, canopies, transpiration);
        }
    }
}
