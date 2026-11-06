using NUnit.Framework;
using UnityEngine;

namespace FishingSim.Tests
{
    public class LineTensionModelTests
    {
        const float Dt = 0.05f;

        FightConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<FightConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Reeling_RaisesTension()
        {
            var model = new LineTensionModel(config, 20f);
            for (int i = 0; i < 20; i++) model.Step(Dt, true, 10f);
            Assert.Greater(model.Tension, 30f);
        }

        [Test]
        public void ReleasingButton_LowersTension()
        {
            var model = new LineTensionModel(config, 20f);
            for (int i = 0; i < 20; i++) model.Step(Dt, true, 60f);
            float high = model.Tension;
            for (int i = 0; i < 10; i++) model.Step(Dt, false, 0f);
            Assert.Less(model.Tension, high);
        }

        [Test]
        public void Reeling_WithWeakFish_ReducesDistance()
        {
            var model = new LineTensionModel(config, 20f);
            for (int i = 0; i < 20; i++) model.Step(Dt, true, 5f);
            Assert.Less(model.Distance, 20f);
        }

        [Test]
        public void StrongPull_PushesFishAway()
        {
            var model = new LineTensionModel(config, 20f);
            for (int i = 0; i < 20; i++) model.Step(Dt, false, 90f);
            Assert.Greater(model.Distance, 20f);
        }

        [Test]
        public void HoldingTensionAboveThreshold_BreaksLineAfterBreakTime()
        {
            var model = new LineTensionModel(config, 20f);
            float elapsedOver = 0f;
            FightResult result = FightResult.None;

            for (int i = 0; i < 200 && result == FightResult.None; i++)
            {
                result = model.Step(Dt, true, 100f);
                if (model.Tension > config.breakThreshold) elapsedOver += Dt;
            }

            Assert.AreEqual(FightResult.LineBroke, result);
            Assert.GreaterOrEqual(elapsedOver, config.breakTime - 1e-3f);
        }

        [Test]
        public void ReleasingQuicklyAfterCrossingThreshold_DoesNotBreakLine()
        {
            var model = new LineTensionModel(config, 20f);
            for (int i = 0; i < 200 && model.Tension <= config.breakThreshold; i++)
                model.Step(Dt, true, 100f);
            Assert.Greater(model.Tension, config.breakThreshold);

            for (int i = 0; i < 20; i++)
                Assert.AreEqual(FightResult.None, model.Step(Dt, false, 0f));
        }

        [Test]
        public void SlackLine_LetsFishEscapeAfterSlackTime()
        {
            var model = new LineTensionModel(config, 20f);
            FightResult result = FightResult.None;
            float elapsed = 0f;

            while (result == FightResult.None && elapsed < 20f)
            {
                result = model.Step(Dt, false, 0f);
                elapsed += Dt;
            }

            Assert.AreEqual(FightResult.FishEscaped, result);
            Assert.GreaterOrEqual(elapsed, config.slackTime);
        }

        [Test]
        public void ExhaustedFishNearShore_IsCaught()
        {
            var model = new LineTensionModel(config, 1f);
            FightResult result = FightResult.None;

            for (int i = 0; i < 1000 && result == FightResult.None; i++)
                result = model.Step(Dt, true, 5f);

            Assert.AreEqual(FightResult.Caught, result);
            Assert.AreEqual(0f, model.Stamina);
        }

        [Test]
        public void ExhaustedFishFarAway_IsNotCaught()
        {
            var model = new LineTensionModel(config, 30f);
            FightResult result = FightResult.None;

            for (int i = 0; i < 100 && result == FightResult.None; i++)
                result = model.Step(Dt, false, 40f);

            Assert.AreNotEqual(FightResult.Caught, result);
        }
    }
}
