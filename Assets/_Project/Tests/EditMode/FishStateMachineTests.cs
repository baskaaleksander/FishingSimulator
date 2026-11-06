using NUnit.Framework;
using UnityEngine;

namespace FishingSim.Tests
{
    public class FishStateMachineTests
    {
        FishConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<FishConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        FishStateMachine Create(int seed) => new FishStateMachine(config, new System.Random(seed));

        [Test]
        public void NewMachine_IsIdleWithNoPull()
        {
            var fish = Create(1);
            Assert.AreEqual(FishState.Idle, fish.State);
            Assert.AreEqual(0f, fish.Tick(0.1f, 100f));
        }

        [Test]
        public void Hooked_BecomesStrugglingAfterDelay()
        {
            var fish = Create(1);
            fish.Hook();
            Assert.AreEqual(FishState.Hooked, fish.State);

            for (int i = 0; i < 25; i++) fish.Tick(0.05f, 100f);
            Assert.AreEqual(FishState.Struggling, fish.State);
        }

        [Test]
        public void Struggling_AlternatesWithRecovering()
        {
            var fish = Create(7);
            fish.Hook();
            bool sawStruggling = false, sawRecovering = false;

            for (int i = 0; i < 600; i++)
            {
                fish.Tick(0.05f, 100f);
                if (fish.State == FishState.Struggling) sawStruggling = true;
                if (fish.State == FishState.Recovering) sawRecovering = true;
            }

            Assert.IsTrue(sawStruggling);
            Assert.IsTrue(sawRecovering);
        }

        [Test]
        public void LowStamina_MakesFishExhausted_WithMinimalPull()
        {
            var fish = Create(3);
            fish.Hook();
            for (int i = 0; i < 25; i++) fish.Tick(0.05f, 100f);

            float pull = fish.Tick(0.05f, 5f);

            Assert.AreEqual(FishState.Exhausted, fish.State);
            Assert.LessOrEqual(pull, config.exhaustedPull.y);
        }

        [Test]
        public void StrugglingPull_IsStrongerThanRecoveringPull()
        {
            Assert.Greater(config.strugglePull.x, config.recoverPull.y);
        }

        [Test]
        public void SameSeed_GivesSamePullSequence()
        {
            var a = Create(42);
            var b = Create(42);
            a.Hook();
            b.Hook();

            for (int i = 0; i < 200; i++)
                Assert.AreEqual(a.Tick(0.05f, 80f), b.Tick(0.05f, 80f));
        }

        [Test]
        public void CaughtAndEscaped_StopPulling()
        {
            var fish = Create(5);
            fish.Hook();
            fish.MarkCaught();
            Assert.AreEqual(FishState.Caught, fish.State);
            Assert.AreEqual(0f, fish.Tick(0.1f, 50f));

            fish.MarkEscaped();
            Assert.AreEqual(FishState.Escaped, fish.State);
            Assert.AreEqual(0f, fish.Tick(0.1f, 50f));
        }
    }
}
