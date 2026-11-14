using Bnhon.Core.Content;
using Bnhon.Core.Relationships;
using Bnhon.Core.Vitals;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class VitalsTests
    {
        [Test]
        public void Stamina_TryUseFailsWithoutSpendingWhenTooLow()
        {
            var stamina = new Stamina(max: 100f);

            Assert.That(stamina.TryUse(80f), Is.True);
            Assert.That(stamina.TryUse(25f), Is.False);
            Assert.That(stamina.Current, Is.EqualTo(20f));
        }

        [Test]
        public void Stamina_RegeneratesOnlyAfterDelay()
        {
            var stamina = new Stamina(max: 100f, regenPerSecond: 10f, regenDelay: 1f);
            stamina.TryUse(50f);

            stamina.Tick(0.5f);
            Assert.That(stamina.Current, Is.EqualTo(50f));

            stamina.Tick(0.5f);
            stamina.Tick(1f);
            Assert.That(stamina.Current, Is.EqualTo(65f));

            stamina.Tick(100f);
            Assert.That(stamina.Current, Is.EqualTo(100f));
        }

        [Test]
        public void Stamina_ExhaustionBlocksDrainUntilRecovered()
        {
            var stamina = new Stamina(max: 100f, regenPerSecond: 10f, regenDelay: 0f, recoverThreshold: 25f);

            Assert.That(stamina.Drain(150f), Is.True);
            Assert.That(stamina.IsExhausted, Is.True);
            Assert.That(stamina.Drain(1f), Is.False);

            stamina.Tick(2f);
            Assert.That(stamina.IsExhausted, Is.True);
            Assert.That(stamina.Drain(1f), Is.False);

            stamina.Tick(1f);
            Assert.That(stamina.IsExhausted, Is.False);
            Assert.That(stamina.Drain(1f), Is.True);
        }

        [Test]
        public void Needs_DrainToZeroAndRefillToMax()
        {
            var needs = new Needs(max: 100f, hungerPerSecond: 1f, thirstPerSecond: 2f);

            needs.Tick(10f);
            Assert.That(needs.Hunger, Is.EqualTo(90f));
            Assert.That(needs.Thirst, Is.EqualTo(80f));

            needs.Tick(1000f);
            Assert.That(needs.IsStarving, Is.True);
            Assert.That(needs.IsDehydrated, Is.True);

            needs.Eat(30f);
            needs.Drink(500f);
            Assert.That(needs.Hunger, Is.EqualTo(30f));
            Assert.That(needs.Thirst, Is.EqualTo(100f));
        }

        [Test]
        public void Session_ConsumeTakesOneItemAndAppliesEffect()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.Backpack.Clear();
            session.Backpack.Add(PrototypeContent.SweetPotato, 2);
            var stack = session.Backpack.Items[0];
            var effect = PrototypeContent.FindConsumable(PrototypeContent.SweetPotato.Id);

            session.Needs.Tick(200f);
            float hungerBefore = session.Needs.Hunger;

            Assert.That(session.Consume(session.Backpack, stack, effect), Is.True);
            Assert.That(session.Backpack.CountOf(PrototypeContent.SweetPotato.Id), Is.EqualTo(1));
            Assert.That(session.Needs.Hunger, Is.EqualTo(hungerBefore + 25f));

            Assert.That(session.Consume(session.Backpack, stack, effect), Is.True);
            Assert.That(session.Backpack.Items.Count, Is.EqualTo(0));
            Assert.That(session.Consume(session.Backpack, stack, effect), Is.False);
        }

        [Test]
        public void Session_ConsumeRejectsMismatchedEffect()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.Backpack.Clear();
            session.Backpack.Add(PrototypeContent.Wood, 1);
            var bandage = PrototypeContent.FindConsumable(PrototypeContent.Bandage.Id);

            Assert.That(PrototypeContent.FindConsumable(PrototypeContent.Wood.Id) == null, Is.True);
            Assert.That(session.Consume(session.Backpack, session.Backpack.Items[0], bandage), Is.False);
            Assert.That(session.Backpack.CountOf(PrototypeContent.Wood.Id), Is.EqualTo(1));
        }

        [Test]
        public void Session_SleepRestoresThirstAndSomeHunger()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.Needs.Tick(10000f);
            session.Stamina.Drain(1000f);

            session.Sleep();

            Assert.That(session.Needs.Thirst, Is.EqualTo(100f));
            Assert.That(session.Needs.Hunger, Is.EqualTo(30f));
            Assert.That(session.Stamina.Current, Is.EqualTo(100f));
        }
    }
}
