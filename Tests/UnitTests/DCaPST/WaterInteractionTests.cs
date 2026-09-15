using System;
using NUnit.Framework;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;
using Moq;
using Models.DCAPST;

namespace UnitTests.DCaPST
{
    public class WaterInteractionTests
    {
        [Test]
        public void UnlimitedRtw_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Loose);
            temperature.Setup(t => t.AtmosphericPressure).Returns(1.01325).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();
            temperature.Setup(t => t.AirMolarDensity).Returns(40.63).Verifiable();

            var leafTemp = 27.0;
            var gbh = 0.127634;

            var A = 4.5;
            var Ca = 380.0;
            var Ci = 152.0;

            var expected = 1304.7005034119043;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, 0.0);
            water.LeafTemp = leafTemp;
            var actual = water.UnlimitedWaterResistance(A, Ca, Ci);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            temperature.Verify();
        }

        [Test]
        public void LimitedRtw_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Loose);
            temperature.Setup(t => t.AirTemperature).Returns(27.0).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var available = 0.15;
            var rn = 230;

            var expected = 309.2097657757547;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, rn);
            water.LeafTemp = leafTemp;
            var actual = water.LimitedWaterResistance(available);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            temperature.Verify();
        }

        [Test]
        public void HourlyWaterUse_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Loose);
            temperature.Setup(t => t.AirTemperature).Returns(27.0).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 700;
            var rn = 320;

            var expected = 0.080424818708166368;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, rn);
            water.LeafTemp = leafTemp;
            var actual = water.HourlyWaterUse(rtw);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Gt_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Loose);
            temperature.Setup(t => t.AirMolarDensity).Returns(40.63).Verifiable();
            temperature.Setup(t => t.AtmosphericPressure).Returns(1.01325).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 180;

            var expected = 0.1437732786549164;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, 0.0);
            water.LeafTemp = leafTemp;
            var actual = water.TotalCO2Conductance(rtw);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            temperature.Verify();
        }

        [Test]
        public void Temperature_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Loose);
            temperature.Setup(t => t.AirTemperature).Returns(27.0).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 700;
            var rn = 320;

            var expected = 28.732384941224293;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, rn);
            water.LeafTemp = leafTemp;
            var actual = water.LeafTemperature(rtw);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void LongwaveLossUsesFourthPowerTemperaturesAndAreaLai()
        {
            var temperature = new Mock<ITemperature>(MockBehavior.Loose);
            temperature.Setup(t => t.AirTemperature).Returns(25.0);
            temperature.Setup(t => t.MinTemperature).Returns(15.0);
            const double leafTemperature = 30.0;
            const double gbh = 0.1;
            const double absorbedRadiation = 350.0;
            const double areaLai = 0.7;
            const double resistance = 700.0;
            const double sigma = 0.0000000567;
            const double psychrometric = 0.066;
            const double heatCapacity = 1200.0;
            const double latentHeat = 2447000.0;

            double vapourPressureLeaf = 0.61365 * Math.Exp(17.502 * leafTemperature / (240.97 + leafTemperature));
            double vapourPressureAir = 0.61365 * Math.Exp(17.502 * 25.0 / (240.97 + 25.0));
            double vapourPressureAirPlusOne = 0.61365 * Math.Exp(17.502 * 26.0 / (240.97 + 26.0));
            double vapourPressureMinimum = 0.61365 * Math.Exp(17.502 * 15.0 / (240.97 + 15.0));
            double slope = vapourPressureAirPlusOne - vapourPressureAir;
            double vpd = vapourPressureLeaf - vapourPressureMinimum;
            double longwave = 2 * sigma * (Math.Pow(leafTemperature + 273.15, 4) -
                                            Math.Pow(25.0 + 273.15, 4)) * areaLai;
            double expectedLatent = (slope * (absorbedRadiation - longwave) + vpd * heatCapacity * gbh) /
                                    (slope + psychrometric * resistance * gbh);
            double expectedWater = expectedLatent / latentHeat * 3600.0;

            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, absorbedRadiation, areaLai);
            water.LeafTemp = leafTemperature;

            Assert.That(water.HourlyWaterUse(resistance), Is.EqualTo(expectedWater).Within(1e-12));
        }
    }
}
