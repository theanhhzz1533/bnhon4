using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class GridInventoryTests
    {
        private static readonly ItemDefinition Ammo = new ItemDefinition("ammo", "Ammo", ItemCategory.Ammo, maxStack: 30);
        private static readonly ItemDefinition Plank = new ItemDefinition("plank", "Plank", ItemCategory.Material, width: 1, height: 3);
        private static readonly ItemDefinition Crate = new ItemDefinition("crate", "Crate", ItemCategory.Material, width: 2, height: 2);

        [Test]
        public void Add_StacksUpToMaxStackThenOpensNewStack()
        {
            var grid = new GridInventory(4, 4);

            Assert.That(grid.Add(Ammo, 45), Is.EqualTo(0));

            Assert.That(grid.Items.Count, Is.EqualTo(2));
            Assert.That(grid.CountOf("ammo"), Is.EqualTo(45));
        }

        [Test]
        public void Add_FillsExistingStackFirst()
        {
            var grid = new GridInventory(4, 4);
            grid.Add(Ammo, 10);

            grid.Add(Ammo, 15);

            Assert.That(grid.Items.Count, Is.EqualTo(1));
            Assert.That(grid.Items[0].Count, Is.EqualTo(25));
        }

        [Test]
        public void Add_RotatesItemWhenItOnlyFitsSideways()
        {
            var grid = new GridInventory(3, 1);

            Assert.That(grid.Add(Plank, 1), Is.EqualTo(0));
            Assert.That(grid.Items[0].Rotated, Is.True);
        }

        [Test]
        public void Add_ReturnsLeftoverWhenFull()
        {
            var grid = new GridInventory(2, 2);

            Assert.That(grid.Add(Crate, 2), Is.EqualTo(1));
            Assert.That(grid.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryPlace_RejectsOverlapAndOutOfBounds()
        {
            var grid = new GridInventory(3, 3);

            Assert.That(grid.TryPlace(Crate, 1, 0, 0, false) != null, Is.True);
            Assert.That(grid.TryPlace(Crate, 1, 1, 1, false) == null, Is.True);
            Assert.That(grid.TryPlace(Crate, 1, 2, 2, false) == null, Is.True);
        }

        [Test]
        public void Remove_FreesCells()
        {
            var grid = new GridInventory(2, 2);
            var placed = grid.TryPlace(Crate, 1, 0, 0, false);

            Assert.That(grid.Remove(placed), Is.True);
            Assert.That(grid.CanPlace(Crate, 0, 0, false), Is.True);
        }

        [Test]
        public void TryConsume_IsAllOrNothing()
        {
            var grid = new GridInventory(4, 4);
            grid.Add(Ammo, 40);

            Assert.That(grid.TryConsume("ammo", 41), Is.False);
            Assert.That(grid.CountOf("ammo"), Is.EqualTo(40));

            Assert.That(grid.TryConsume("ammo", 35), Is.True);
            Assert.That(grid.CountOf("ammo"), Is.EqualTo(5));
            Assert.That(grid.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void MoveAllTo_LeavesBehindWhatDoesNotFit()
        {
            var backpack = new GridInventory(4, 2);
            var stash = new GridInventory(2, 2);
            backpack.Add(Crate, 2);

            int leftBehind = backpack.MoveAllTo(stash);

            Assert.That(leftBehind, Is.EqualTo(1));
            Assert.That(stash.CountOf("crate"), Is.EqualTo(1));
            Assert.That(backpack.CountOf("crate"), Is.EqualTo(1));
        }
    }
}
