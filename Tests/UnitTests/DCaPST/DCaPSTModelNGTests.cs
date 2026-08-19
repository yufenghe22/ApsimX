using APSIM.Core;
using GLib;
using Models.DCAPST;
using Models.LeafWise;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Struct;
using Moq;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class DCaPSTModelNGTests
    {
        #region Tests

        [Test]
        public void SetCropName_NewValue_HandleCropChangeCalledOnce()
        {
            // Arrange
            var cropName = "testCrop";
            var mock = new Mock<ICropParameterGenerator>(MockBehavior.Strict);
            mock.Setup(cropGen => cropGen.Generate(It.IsAny<string>())).Returns(new DCaPSTParameters()).Verifiable();

            var model = new DCaPSTModelNG();
            Node.Create(model);
            DCaPSTModelNG.ParameterGenerator = mock.Object;

            // Act
            model.CropName = cropName;

            // Assert
            mock.Verify(cropGen => cropGen.Generate(cropName), Times.Once());
            Assert.That(model.CropName, Is.EqualTo(cropName));
        }

        [Test]
        public void SetCropName_SameValue_HandleCropChangeCalledOnce()
        {
            // Arrange
            var cropName = "testCrop";
            var mock = new Mock<ICropParameterGenerator>(MockBehavior.Strict);
            mock.Setup(cropGen => cropGen.Generate(It.IsAny<string>())).Returns(new DCaPSTParameters()).Verifiable();

            var model = new DCaPSTModelNG();
            Node.Create(model);
            DCaPSTModelNG.ParameterGenerator = mock.Object;

            // Act
            model.CropName = cropName;

            // Assert
            mock.Verify(cropGen => cropGen.Generate(cropName), Times.Once());
            Assert.That(model.CropName, Is.EqualTo(cropName));
        }

        [Test]
        public void SetCropName_DifferentValue_HandleCropChangeCalledTwice()
        {
            // Arrange
            var cropName = "testCrop";
            var differentCropName = $"Different-{cropName}";
            var mock = new Mock<ICropParameterGenerator>(MockBehavior.Strict);
            mock.Setup(cropGen => cropGen.Generate(It.IsAny<string>())).Returns(new DCaPSTParameters()).Verifiable();

            var model = new DCaPSTModelNG();
            Node.Create(model);
            DCaPSTModelNG.ParameterGenerator = mock.Object;

            // Act
            model.CropName = cropName;
            model.CropName = differentCropName;

            // Assert
            mock.Verify(cropGen => cropGen.Generate(cropName), Times.Once());
            mock.Verify(cropGen => cropGen.Generate(differentCropName), Times.Once());
            Assert.That(model.CropName, Is.EqualTo(differentCropName));
        }

        [Test]
        public void EffectiveLeafWidthUsesLeafWiseForMatchingCrop()
        {
            var plant = new Plant { Name = "Sorghum" };
            var leafWise = new LeafWiseModel { CropName = plant.Name };
            var culm = new Culm(0) { CulmNo = 0, FinalLeafNo = 17 };
            leafWise.CalculateIndividualLeafArea(1, culm);
            leafWise.CalculateIndividualLeafArea(2, culm);

            double result = DCaPSTModelNG.GetEffectiveLeafWidth(0.09, leafWise, plant);

            Assert.That(result, Is.EqualTo(leafWise.AverageLeafWidth));
        }

        [Test]
        public void EffectiveLeafWidthKeepsConfiguredWidthWithoutMatchingLeafWiseOutput()
        {
            var plant = new Plant { Name = "Sorghum" };
            var leafWise = new LeafWiseModel { CropName = "Maize" };

            double result = DCaPSTModelNG.GetEffectiveLeafWidth(0.09, leafWise, plant);

            Assert.That(result, Is.EqualTo(0.09));
        }

        #endregion
    }
}
