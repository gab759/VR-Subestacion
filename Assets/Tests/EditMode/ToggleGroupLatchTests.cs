using NUnit.Framework;
using UnityEngine;
using VRSubestacion.Simulation;

namespace VRSubestacion.Simulation.Tests
{
    public sealed class ToggleGroupLatchTests
    {
        [Test]
        public void AllOn_FiresOnlyWhenEveryExpectedMemberIsOn()
        {
            var latch = new ToggleGroupLatch(3);
            int a = latch.Register();
            int b = latch.Register();
            int c = latch.Register();

            Assert.AreEqual(ToggleGroupLatch.Transition.None, latch.Set(a, true));
            Assert.AreEqual(ToggleGroupLatch.Transition.None, latch.Set(b, true));
            Assert.AreEqual(ToggleGroupLatch.Transition.AllOn, latch.Set(c, true));
        }

        [Test]
        public void AllOff_RequiresGroupToHaveBeenAllOnFirst()
        {
            var latch = new ToggleGroupLatch(2);
            int a = latch.Register();
            int b = latch.Register();

            latch.Set(a, true);
            Assert.AreEqual(ToggleGroupLatch.Transition.None, latch.Set(a, false));

            latch.Set(a, true);
            latch.Set(b, true);
            Assert.AreEqual(ToggleGroupLatch.Transition.None, latch.Set(a, false));
            Assert.AreEqual(ToggleGroupLatch.Transition.AllOff, latch.Set(b, false));
        }

        [Test]
        public void ReportingOnWhileComplete_RetriggersAllOn()
        {
            var latch = new ToggleGroupLatch(1);
            int a = latch.Register();

            Assert.AreEqual(ToggleGroupLatch.Transition.AllOn, latch.Set(a, true));
            Assert.AreEqual(ToggleGroupLatch.Transition.AllOn, latch.Set(a, true));
        }

        [Test]
        public void Register_BeyondCapacity_ReturnsInvalidIndexThatIsIgnored()
        {
            var latch = new ToggleGroupLatch(1);
            latch.Register();
            int overflow = latch.Register();

            Assert.AreEqual(-1, overflow);
            Assert.AreEqual(ToggleGroupLatch.Transition.None, latch.Set(overflow, true));
        }
    }

    public sealed class LeverAngleMathTests
    {
        [Test]
        public void SignedAngle_FollowsAxisDirection()
        {
            Quaternion rest = Quaternion.identity;
            Quaternion rotated = Quaternion.AngleAxis(70f, Vector3.right);

            Assert.AreEqual(70f, LeverAngleMath.SignedAngle(rest, rotated, Vector3.right), 0.01f);
            Assert.AreEqual(-70f, LeverAngleMath.SignedAngle(rest, rotated, Vector3.left), 0.01f);
            Assert.AreEqual(0f, LeverAngleMath.SignedAngle(rest, rest, Vector3.right), 0.01f);
        }

        [Test]
        public void EvaluateToggle_AppliesHysteresis()
        {
            Assert.IsFalse(LeverAngleMath.EvaluateToggle(false, 59f, 60f, 20f));
            Assert.IsTrue(LeverAngleMath.EvaluateToggle(false, 60f, 60f, 20f));
            Assert.IsTrue(LeverAngleMath.EvaluateToggle(true, 30f, 60f, 20f));
            Assert.IsFalse(LeverAngleMath.EvaluateToggle(true, 20f, 60f, 20f));
        }
    }
}
