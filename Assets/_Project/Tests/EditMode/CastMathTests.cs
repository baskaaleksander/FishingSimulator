using NUnit.Framework;
using UnityEngine;

namespace FishingSim.Tests
{
    public class CastMathTests
    {
        [Test]
        public void Power_GrowsWithHoldTimeAndClampsAtOne()
        {
            Assert.AreEqual(0f, CastMath.Power01(0f, 2f));
            Assert.AreEqual(0.5f, CastMath.Power01(1f, 2f), 1e-5f);
            Assert.AreEqual(1f, CastMath.Power01(5f, 2f));
        }

        [Test]
        public void Force_InterpolatesBetweenMinAndMax()
        {
            Assert.AreEqual(5f, CastMath.Force(0f, 5f, 20f));
            Assert.AreEqual(20f, CastMath.Force(1f, 5f, 20f));
            Assert.Greater(CastMath.Force(0.8f, 5f, 20f), CastMath.Force(0.2f, 5f, 20f));
        }

        [Test]
        public void Direction_TiltsUpwardByElevation()
        {
            Vector3 dir = CastMath.Direction(Vector3.forward, Vector3.right, 20f);
            Assert.AreEqual(Mathf.Sin(20f * Mathf.Deg2Rad), dir.y, 1e-4f);
            Assert.Greater(dir.z, 0f);
            Assert.AreEqual(1f, dir.magnitude, 1e-4f);
        }

        [Test]
        public void Transitions_CannotCastWhileFighting()
        {
            Assert.IsFalse(GameManager.IsTransitionAllowed(GameState.Fighting, GameState.Charging));
            Assert.IsTrue(GameManager.IsTransitionAllowed(GameState.Ready, GameState.Charging));
            Assert.IsTrue(GameManager.IsTransitionAllowed(GameState.Casting, GameState.WaitingForBite));
        }
    }
}
