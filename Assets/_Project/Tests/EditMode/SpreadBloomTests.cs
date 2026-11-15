using Bnhon.Core.Combat;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class SpreadBloomTests
    {
        [Test]
        public void Kick_AddsUpToTheCap()
        {
            var bloom = new SpreadBloom();

            bloom.Kick(4f, 10f);
            bloom.Kick(4f, 10f);
            Assert.That(bloom.Degrees, Is.EqualTo(8f));

            bloom.Kick(4f, 10f);
            Assert.That(bloom.Degrees, Is.EqualTo(10f));
        }

        [Test]
        public void Tick_RecoversOnlyAfterTheDelay()
        {
            var bloom = new SpreadBloom(recoveryDelay: 0.5f);
            bloom.Kick(10f, 10f);

            bloom.Tick(0.25f, 20f);
            Assert.That(bloom.Degrees, Is.EqualTo(10f));

            bloom.Tick(0.25f, 20f);
            Assert.That(bloom.Degrees, Is.EqualTo(5f));

            bloom.Tick(5f, 20f);
            Assert.That(bloom.Degrees, Is.EqualTo(0f));
        }

        [Test]
        public void Kick_RestartsTheDelay()
        {
            var bloom = new SpreadBloom(recoveryDelay: 0.5f);
            bloom.Kick(4f, 10f);
            bloom.Tick(0.4f, 20f);
            bloom.Kick(4f, 10f);

            bloom.Tick(0.4f, 20f);

            Assert.That(bloom.Degrees, Is.EqualTo(8f));
        }

        [Test]
        public void SwitchingToAWeaponWithALowerCap_DoesNotSnapSpreadDown()
        {
            var bloom = new SpreadBloom();
            bloom.Kick(12f, 12f);

            bloom.Kick(3f, 6f);

            Assert.That(bloom.Degrees, Is.EqualTo(12f));
        }
    }
}
