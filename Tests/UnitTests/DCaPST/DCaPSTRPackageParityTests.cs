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

        private static DCAPSTModel CreateModel(int layerCount)
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
                canopies.Add(new CanopyAttributes(parameters, sunlit, shaded, 3, layer, layerCount));
            }
            var transpiration = new Transpiration(
                parameters.Canopy, parameters.Pathway, new WaterInteraction(temperature),
                new TemperatureResponse(parameters.Canopy, parameters.Pathway), 363);
            return new DCAPSTModel(solar, radiation, temperature, parameters.Pathway, canopies, transpiration);
        }
    }
}
