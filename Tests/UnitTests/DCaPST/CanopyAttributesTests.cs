using Models.DCAPST;
using Models.DCAPST.Canopy;
using Models.DCAPST.Interfaces;
using Moq;
using NUnit.Framework;
using System;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class CanopyAttributesTests
    {
        [Test]
        public void BoundaryConductanceUsesWindExtinctionCoefficient()
        {
            const double windspeed = 4.0;
            var parameters = new DCaPSTParameters
            {
                Canopy = new CanopyParameters
                {
                    LeafWidth = 0.1,
                    WindSpeedExtinction = 2.0,
                    SLNRatioTop = 1.3,
                    MinimumN = 1.0
                }
            };
            var canopy = new CanopyAttributes(
                parameters,
                Mock.Of<IAssimilationArea>(),
                Mock.Of<IAssimilationArea>(),
                windspeed);
            canopy.InitialiseDay(lai: 3.0, sln: 1.0);

            double topConductance = 0.01 * Math.Sqrt(windspeed / parameters.Canopy.LeafWidth);
            double expected = topConductance * (1 - Math.Exp(-3.0));

            Assert.That(canopy.CalcBoundaryHeatConductance(), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void ZeroWindExtinctionUsesUnattenuatedLimit()
        {
            const double windspeed = 4.0;
            var parameters = new DCaPSTParameters
            {
                Canopy = new CanopyParameters
                {
                    LeafWidth = 0.1,
                    WindSpeedExtinction = 0.0,
                    SLNRatioTop = 1.3,
                    MinimumN = 1.0
                }
            };
            var canopy = new CanopyAttributes(
                parameters,
                Mock.Of<IAssimilationArea>(),
                Mock.Of<IAssimilationArea>(),
                windspeed);
            canopy.InitialiseDay(lai: 3.0, sln: 1.0);

            double expected = 0.01 * Math.Sqrt(windspeed / parameters.Canopy.LeafWidth) * 3.0;

            Assert.That(canopy.CalcBoundaryHeatConductance(), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void ZeroWindUsesMinimumCanopyConductance()
        {
            var sunlit = new Mock<IAssimilationArea>();
            sunlit.SetupProperty(area => area.LAI);
            var shaded = new Mock<IAssimilationArea>();
            shaded.SetupProperty(area => area.LAI);
            var parameters = new DCaPSTParameters
            {
                Canopy = new CanopyParameters
                {
                    LeafWidth = 0.1,
                    WindSpeedExtinction = 1.5,
                    SLNRatioTop = 1.3,
                    MinimumN = 1.0
                }
            };
            var canopy = new CanopyAttributes(parameters, sunlit.Object, shaded.Object, windspeed: 0.0);
            canopy.InitialiseDay(lai: 3.0, sln: 1.0);

            // Supply a sunlit/shaded partition for the zero-wind fallback.
            sunlit.Object.LAI = 1.0;
            shaded.Object.LAI = 2.0;

            double total = canopy.CalcBoundaryHeatConductance();
            double sun = canopy.CalcSunlitBoundaryHeatConductance();

            Assert.That(total, Is.EqualTo(0.005));
            Assert.That(sun, Is.EqualTo(0.005 / 3.0).Within(1e-12));
            Assert.That(total - sun, Is.EqualTo(0.010 / 3.0).Within(1e-12));
        }

        [Test]
        public void LayerBoundaryConductancesSumToWholeCanopyConductance()
        {
            const double windspeed = 4.0;
            var parameters = new DCaPSTParameters
            {
                Canopy = new CanopyParameters
                {
                    LeafWidth = 0.1,
                    WindSpeedExtinction = 2.0,
                    SLNRatioTop = 1.3,
                    MinimumN = 1.0
                }
            };
            double layered = 0;
            for (int layer = 1; layer <= 3; layer++)
            {
                var canopy = new CanopyAttributes(parameters, Mock.Of<IAssimilationArea>(),
                                                  Mock.Of<IAssimilationArea>(), windspeed, layer, 3);
                canopy.InitialiseDay(3.0, 1.0);
                layered += canopy.CalcBoundaryHeatConductance();
            }
            var whole = new CanopyAttributes(parameters, Mock.Of<IAssimilationArea>(),
                                             Mock.Of<IAssimilationArea>(), windspeed);
            whole.InitialiseDay(3.0, 1.0);

            Assert.That(layered, Is.EqualTo(whole.CalcBoundaryHeatConductance()).Within(1e-12));
        }
    }
}
