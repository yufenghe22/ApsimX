using Models.LeafWise;
using Models.PMF.Struct;
using NUnit.Framework;
using System.Linq;

namespace UnitTests.PMF
{
    [TestFixture]
    public class LeafWiseModelTests
    {
        [Test]
        public void CalculatesExpectedDimensionsAndArea()
        {
            var model = new LeafWiseModel();
            var culm = new Culm(0) { CulmNo = 0, FinalLeafNo = 17 };

            double length = model.CalculateLeafDimension(LeafWiseModel.LeafDimension.Length, 10, 17);
            double width = model.CalculateLeafDimension(LeafWiseModel.LeafDimension.Width, 10, 17);
            double area = model.CalculateIndividualLeafArea(10, culm);

            Assert.That(length, Is.EqualTo(516.544396333045).Within(1e-9));
            Assert.That(width, Is.EqualTo(66.1141434218796).Within(1e-9));
            Assert.That(area, Is.EqualTo(length * width * 0.71).Within(1e-9));
            Assert.That(model.LeafLengthsMain, Is.EqualTo(new[] { length }));
            Assert.That(model.LeafWidthsMain, Is.EqualTo(new[] { width }));
            Assert.That(model.EffectiveLeafWidth, Is.Zero);
        }

        [Test]
        public void EffectiveWidthTracksMaximumFullyExpandedLeafWidth()
        {
            var model = new LeafWiseModel();
            var culm = new Culm(0) { CulmNo = 0, FinalLeafNo = 17 };

            for (int leaf = 1; leaf <= 17; leaf++)
                model.CalculateIndividualLeafArea(leaf, culm);

            model.UpdateEffectiveLeafWidth(10.9);
            double expectedAtLeaf10 = model.LeafWidthsMain.Take(10).Max() / 1000.0;
            Assert.That(model.EffectiveLeafWidth, Is.EqualTo(expectedAtLeaf10).Within(1e-12));

            model.UpdateEffectiveLeafWidth(12.1);
            double maximumWidth = model.EffectiveLeafWidth;

            model.UpdateEffectiveLeafWidth(17.0);

            Assert.That(model.LeafWidthsMain[16], Is.LessThan(maximumWidth * 1000.0));
            Assert.That(model.EffectiveLeafWidth, Is.EqualTo(maximumWidth));
        }

        [Test]
        public void MaximumWidthRateChangesWidthButNotLength()
        {
            var narrow = new LeafWiseModel { MaximumWidthRate = 10 };
            var wide = new LeafWiseModel { MaximumWidthRate = 12.97882 };

            Assert.That(wide.CalculateLeafDimension(LeafWiseModel.LeafDimension.Width, 10, 17),
                        Is.GreaterThan(narrow.CalculateLeafDimension(LeafWiseModel.LeafDimension.Width, 10, 17)));
            Assert.That(wide.CalculateLeafDimension(LeafWiseModel.LeafDimension.Length, 10, 17),
                        Is.EqualTo(narrow.CalculateLeafDimension(LeafWiseModel.LeafDimension.Length, 10, 17)));
        }
    }
}
