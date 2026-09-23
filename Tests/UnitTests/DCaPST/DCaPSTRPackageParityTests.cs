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
        [TestCase(1, 0.8, 6.121926, 4.838808, 1.318704, 0.8000006)]
        [TestCase(3, 0.8, 5.872761, 4.779490, 1.276771, 0.8000001)]
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

        [Test]
        public void OneLayerHourlyFluxesMatchRPackageReference()
        {
            double[][] expectedSunlit =
            {
                new[] { 0.287810196853046, 0.000682576733205088, 161.058216952301, 133.521620400703, 0.0104516048626763, 0.0014620866579085 },
                new[] { 6.00519196975581, 0.0149188466095181, 160.404226327353, 131.237165694592, 0.20588397269894, 0.0306309814700176 },
                new[] { 13.1045720453127, 0.0645132204064549, 158.908538679005, 127.074116720162, 0.411636075246873, 0.0674722873399952 },
                new[] { 15.5339320266805, 0.0870527368098706, 74.5543943666962, 49.5793999964539, 0.621957495464665, 0.0557914631208371 },
                new[] { 12.5138082855843, 0.0879983201296248, 36.2894036966802, 21.1905477075992, 0.828739827284293, 0.0392002003922475 },
                new[] { 10.0272111414455, 0.0883433068142155, 23.400431664436, 13.4222700833107, 1.00483377986988, 0.0300437091591889 },
                new[] { 8.3306578668026, 0.0880515489042756, 17.6359171118421, 10.281074816367, 1.13255073878277, 0.0244677050688627 },
                new[] { 7.51372720557437, 0.0879799725881595, 15.1811825477054, 9.01936970015295, 1.21923249086247, 0.0218813582345934 },
                new[] { 7.0482394140023, 0.0871975240475723, 14.5675870244948, 8.76245142074221, 1.2139706660114, 0.0204821746786294 },
                new[] { 6.88371827624494, 0.0855118211629546, 15.9094742345536, 9.58684626282017, 1.08859373345143, 0.020098813976092 },
                new[] { 7.17863710699908, 0.0831827553598391, 21.3879974968706, 12.8147207090757, 0.837233703608929, 0.0213676360510085 },
                new[] { 8.19899776293741, 0.0809342657246529, 48.3152576103734, 30.4517000884619, 0.458956692537661, 0.0268588216105139 },
                new[] { 0.460044595178324, 0.00495599604535537, 159.529245226991, 135.97490983427, 0.0195304992919252, 0.00235944276419128 }
            };
            double[][] expectedShaded =
            {
                new[] { 0.0993181525690504, 7.81142985441451e-05, 163.330500893076, 163.179671550613, 0.654302557189656, 0.000497520709255658 },
                new[] { 2.34485419120952, 0.00154162072449084, 162.618462649576, 157.390997527519, 0.44849765717058, 0.0117976583610266 },
                new[] { 3.46489774151211, 0.0105638687739343, 161.79702841637, 153.43528935658, 0.414327896363344, 0.0175214363434973 },
                new[] { 3.61359438879694, 0.0131724313188679, 44.4701549814677, 35.2905463450372, 0.393614196930796, 0.0115177233275085 },
                new[] { 2.42515266098626, 0.0122268479991137, 13.7531839965321, 7.49947475509349, 0.38774417111013, 0.00702397117633248 },
                new[] { 1.77461864146439, 0.0118818613145229, 9.83637231701698, 5.49583483950244, 0.40876306082717, 0.0050715511362555 },
                new[] { 1.48514240817708, 0.0121736192244628, 7.67671903516691, 4.43911678251314, 0.458581940323951, 0.00421219226130288 },
                new[] { 1.31979199381313, 0.012245195540579, 6.56099518320144, 3.90583211605283, 0.496905859084761, 0.00372828545177364 },
                new[] { 1.32378217050258, 0.0130276440811661, 5.8899128281567, 3.56841522732171, 0.570029079800682, 0.00373016649491976 },
                new[] { 1.50498648025168, 0.0147133469657839, 5.53236113414465, 3.35962018172595, 0.692416094523689, 0.00423459423552893 },
                new[] { 1.89069433814073, 0.0170424127688994, 5.49021414357424, 3.28291940526565, 0.85625907870521, 0.00531780061756544 },
                new[] { 2.27421549349205, 0.0192909024040856, 43.1803735813772, 41.0454853100373, 1.06478492840509, 0.00714808052760301 },
                new[] { 0.0680273689647048, 0.00071901674087233, 163.336069982306, 163.28532939101, 1.3179957715425, 0.000340762187015379 }
            };

            DCAPSTModel model = CreateModel(1);
            model.DailyRun(3.0, 0.914);
            model.CalculateBiomass(1.0, 0.25);

            Assert.That(model.Intervals, Has.Length.EqualTo(13));
            for (int index = 0; index < model.Intervals.Length; index++)
            {
                AssertAreaMatchesReference(model.Intervals[index].Sunlit, expectedSunlit[index], index + 6, "sunlit");
                AssertAreaMatchesReference(model.Intervals[index].Shaded, expectedShaded[index], index + 6, "shaded");
            }
        }

        [Test]
        public void CO2DiffusionTermsUseBoundaryLayerCO2AndNoTernaryCorrection()
        {
            const double ratio = 0.45;
            const double ambientCO2 = 400.0;
            const double assimilation = 12.0;
            const double boundaryCO2Conductance = 0.3;
            const double stomatalCO2Conductance = 0.2;
            const double mesophyllCO2Conductance = 0.5;
            double totalCO2Conductance = 1.0 /
                (1.0 / boundaryCO2Conductance + 1.0 / stomatalCO2Conductance);

            Assert.Multiple(() =>
            {
                Assert.That(Transpiration.UnlimitedCO2Intercept(ratio, ambientCO2),
                    Is.EqualTo(ratio * ambientCO2));
                Assert.That(Transpiration.UnlimitedCO2Resistance(
                        ratio, boundaryCO2Conductance, mesophyllCO2Conductance),
                    Is.EqualTo(ratio / boundaryCO2Conductance + 1.0 / mesophyllCO2Conductance));
                Assert.That(Transpiration.UnlimitedIntercellularCO2(
                        ratio, ambientCO2, assimilation, boundaryCO2Conductance),
                    Is.EqualTo(ratio * (ambientCO2 - assimilation / boundaryCO2Conductance)));
                Assert.That(Transpiration.LimitedCO2Resistance(
                        totalCO2Conductance, mesophyllCO2Conductance),
                    Is.EqualTo(1.0 / totalCO2Conductance + 1.0 / mesophyllCO2Conductance));
                Assert.That(Transpiration.LimitedIntercellularCO2(
                        ambientCO2, assimilation, totalCO2Conductance),
                    Is.EqualTo(ambientCO2 - assimilation / totalCO2Conductance));
            });
        }

        private static void AssertAreaMatchesReference(AreaValues actual, double[] expected,
                                                       int hour, string fraction)
        {
            double[] values =
            {
                actual.A,
                actual.Water,
                actual.IntercellularCO2,
                actual.MesophyllCO2,
                actual.MesophyllCO2Conductance,
                actual.StomatalCO2Conductance
            };
            string[] metrics = { "A", "T", "Ci", "Cm", "gm", "gs" };
            Assert.Multiple(() =>
            {
                for (int index = 0; index < values.Length; index++)
                {
                    double tolerance = Math.Max(1e-8, Math.Abs(expected[index]) * 1e-2);
                    Assert.That(values[index], Is.EqualTo(expected[index]).Within(tolerance),
                        $"hour {hour} {fraction} {metrics[index]}");
                }
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
