using Bnhon.Core.Combat;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Core.Relationships;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class LoadoutTests
    {
        private static readonly ItemDefinition Pistol = PrototypeContent.Pistol;
        private static readonly ItemDefinition Shotgun = PrototypeContent.Shotgun;

        [Test]
        public void Equip_MovesWeaponFromGridIntoSlot()
        {
            var loadout = new Loadout();
            var grid = new GridInventory(4, 2);
            grid.Add(Pistol, 1);

            Assert.That(loadout.Equip(grid, grid.Items[0], 0, out var replaced, out int rounds), Is.True);

            Assert.That(loadout.GetWeapon(0) == Pistol, Is.True);
            Assert.That(loadout.GetLoaded(0), Is.EqualTo(0));
            Assert.That(replaced == null, Is.True);
            Assert.That(rounds, Is.EqualTo(0));
            Assert.That(grid.Items.Count, Is.EqualTo(0));
            Assert.That(loadout.FirstEmptySlot(), Is.EqualTo(1));
        }

        [Test]
        public void Equip_RejectsNonWeapons()
        {
            var loadout = new Loadout();
            var grid = new GridInventory(4, 2);
            grid.Add(PrototypeContent.Bandage, 1);

            Assert.That(loadout.Equip(grid, grid.Items[0], 0, out _, out _), Is.False);
            Assert.That(grid.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void Equip_SwapsAndReportsRoundsOfTheOldWeapon()
        {
            var loadout = new Loadout();
            loadout.Set(0, Pistol, 6);
            var grid = new GridInventory(3, 1);
            grid.Add(Shotgun, 1);

            Assert.That(loadout.Equip(grid, grid.Items[0], 0, out var replaced, out int rounds), Is.True);

            Assert.That(loadout.GetWeapon(0) == Shotgun, Is.True);
            Assert.That(replaced == Pistol, Is.True);
            Assert.That(rounds, Is.EqualTo(6));
            Assert.That(grid.CountOf(Pistol.Id), Is.EqualTo(1));
            Assert.That(grid.CountOf(Shotgun.Id), Is.EqualTo(0));
        }

        [Test]
        public void Equip_FailsCleanlyWhenTheOldWeaponDoesNotFit()
        {
            var loadout = new Loadout();
            loadout.Set(0, Shotgun, 2);
            // Room for the 2x1 pistol only; the 3x1 shotgun coming out cannot fit.
            var grid = new GridInventory(2, 1);
            grid.Add(Pistol, 1);

            Assert.That(loadout.Equip(grid, grid.Items[0], 0, out _, out _), Is.False);

            Assert.That(loadout.GetWeapon(0) == Shotgun, Is.True);
            Assert.That(loadout.GetLoaded(0), Is.EqualTo(2));
            Assert.That(grid.CountOf(Pistol.Id), Is.EqualTo(1));
        }

        [Test]
        public void Unequip_ReturnsWeaponAndRounds_OrFailsWhenFull()
        {
            var loadout = new Loadout();
            loadout.Set(1, Pistol, 5);
            var full = new GridInventory(1, 1);
            var grid = new GridInventory(2, 1);

            Assert.That(loadout.Unequip(1, full, out _, out _), Is.False);
            Assert.That(loadout.GetWeapon(1) == Pistol, Is.True);

            Assert.That(loadout.Unequip(1, grid, out var weapon, out int rounds), Is.True);
            Assert.That(weapon == Pistol, Is.True);
            Assert.That(rounds, Is.EqualTo(5));
            Assert.That(loadout.HasAny, Is.False);
            Assert.That(grid.CountOf(Pistol.Id), Is.EqualTo(1));
            Assert.That(loadout.Unequip(1, grid, out _, out _), Is.False);
        }

        [Test]
        public void NewGame_StartsWithALoadedPistol_AndDeathTakesIt()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            Assert.That(session.Loadout.GetWeapon(0) == Pistol, Is.True);
            Assert.That(session.Loadout.GetLoaded(0), Is.EqualTo(8));

            var raid = session.TryStartRaid();
            raid.Die();

            Assert.That(session.Loadout.HasAny, Is.False);
        }

        [Test]
        public void Extracting_KeepsEquippedWeapons()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.TryStartRaid().Extract();

            Assert.That(session.Loadout.GetWeapon(0) == Pistol, Is.True);
        }

        [Test]
        public void FallbackWeapon_IsGrantedOnlyWhenThePlayerOwnsNoFirearm()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            Assert.That(PrototypeContent.GrantFallbackWeapon(session), Is.False);

            session.TryStartRaid().Die();
            Assert.That(PrototypeContent.GrantFallbackWeapon(session), Is.True);
            Assert.That(session.Stash.CountOf(Pistol.Id), Is.EqualTo(1));
            Assert.That(session.Stash.CountOf(PrototypeContent.PistolAmmo.Id) >= 8, Is.True);

            Assert.That(PrototypeContent.GrantFallbackWeapon(session), Is.False);
        }
    }
}
