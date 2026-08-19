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
            var parameters = new DCaPSTParameters
            {
                Windspeed = 4.0,
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
                Mock.Of<IAssimilationArea>());
            canopy.InitialiseDay(lai: 3.0, sln: 1.0);

            double topConductance = 0.01 * Math.Sqrt(parameters.Windspeed / parameters.Canopy.LeafWidth);
            double expected = topConductance * (1 - Math.Exp(-3.0));

            Assert.That(canopy.CalcBoundaryHeatConductance(), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void ZeroWindExtinctionUsesUnattenuatedLimit()
        {
            var parameters = new DCaPSTParameters
            {
                Windspeed = 4.0,
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
                Mock.Of<IAssimilationArea>());
            canopy.InitialiseDay(lai: 3.0, sln: 1.0);

            double expected = 0.01 * Math.Sqrt(parameters.Windspeed / parameters.Canopy.LeafWidth) * 3.0;

            Assert.That(canopy.CalcBoundaryHeatConductance(), Is.EqualTo(expected).Within(1e-12));
        }
    }
}
