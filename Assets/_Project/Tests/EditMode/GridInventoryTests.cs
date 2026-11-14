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

        [Test]
        public void MoveTo_CanShiftAnItemOverItsOwnCells()
        {
            var grid = new GridInventory(3, 2);
            var crate = grid.TryPlace(Crate, 1, 0, 0, false);

            Assert.That(grid.MoveTo(crate, grid, 1, 0, false), Is.True);

            Assert.That(crate.X, Is.EqualTo(1));
            Assert.That(grid.GetAt(0, 0) == null, Is.True);
            Assert.That(grid.GetAt(2, 1) == crate, Is.True);
            Assert.That(grid.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void MoveTo_OtherGridWithRotation()
        {
            var backpack = new GridInventory(3, 3);
            var stash = new GridInventory(3, 1);
            var plank = backpack.TryPlace(Plank, 1, 0, 0, false);

            Assert.That(backpack.MoveTo(plank, stash, 0, 0, false), Is.False);
            Assert.That(backpack.MoveTo(plank, stash, 0, 0, true), Is.True);

            Assert.That(backpack.Items.Count, Is.EqualTo(0));
            Assert.That(backpack.CanPlace(Crate, 0, 0, false), Is.True);
            Assert.That(stash.Items.Count, Is.EqualTo(1));
            Assert.That(stash.GetAt(2, 0) == plank, Is.True);
        }

        [Test]
        public void MoveTo_RejectsOccupiedCellsAndLeavesItemInPlace()
        {
            var grid = new GridInventory(4, 2);
            var a = grid.TryPlace(Crate, 1, 0, 0, false);
            grid.TryPlace(Crate, 1, 2, 0, false);

            Assert.That(grid.MoveTo(a, grid, 1, 0, false), Is.False);
            Assert.That(a.X, Is.EqualTo(0));
            Assert.That(grid.GetAt(0, 0) == a, Is.True);
        }

        [Test]
        public void MoveTo_MergesIntoMatchingStackAndKeepsRemainder()
        {
            var backpack = new GridInventory(2, 1);
            var stash = new GridInventory(2, 1);
            var small = backpack.TryPlace(Ammo, 20, 0, 0, false);
            var big = stash.TryPlace(Ammo, 25, 1, 0, false);

            Assert.That(backpack.MoveTo(small, stash, 1, 0, false), Is.True);

            Assert.That(big.Count, Is.EqualTo(30));
            Assert.That(small.Count, Is.EqualTo(15));
            Assert.That(backpack.CountOf("ammo"), Is.EqualTo(15));

            Assert.That(backpack.MoveTo(small, stash, 1, 0, false), Is.False);
        }

        [Test]
        public void TransferTo_MovesWhatFits()
        {
            var backpack = new GridInventory(2, 1);
            var pouch = new GridInventory(1, 1);
            var stack = backpack.TryPlace(Ammo, 30, 0, 0, false);
            pouch.TryPlace(Ammo, 20, 0, 0, false);

            Assert.That(backpack.TransferTo(stack, pouch), Is.EqualTo(10));
            Assert.That(stack.Count, Is.EqualTo(20));
            Assert.That(backpack.TransferTo(stack, pouch), Is.EqualTo(0));
            Assert.That(pouch.CountOf("ammo"), Is.EqualTo(30));
        }
    }
}
