using System;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;

namespace Bnhon.Core.Combat
{
    /// <summary>
    /// The weapons the player has in hand, as opposed to in the backpack. Two slots, each
    /// holding one weapon item and the rounds loaded in it. Equipped weapons go on raids
    /// with the player and are lost on death like everything else they carry.
    /// </summary>
    public sealed class Loadout
    {
        public const int SlotCount = 2;

        private readonly ItemDefinition[] _weapons = new ItemDefinition[SlotCount];
        private readonly int[] _loaded = new int[SlotCount];

        public event Action Changed;

        public ItemDefinition GetWeapon(int slot)
        {
            return InRange(slot) ? _weapons[slot] : null;
        }

        public int GetLoaded(int slot)
        {
            return InRange(slot) ? _loaded[slot] : 0;
        }

        public void SetLoaded(int slot, int rounds)
        {
            if (!InRange(slot) || _weapons[slot] == null) return;
            _loaded[slot] = Math.Max(0, rounds);
        }

        public bool HasAny
        {
            get
            {
                foreach (ItemDefinition weapon in _weapons)
                {
                    if (weapon != null) return true;
                }
                return false;
            }
        }

        /// <summary>The first slot with no weapon, or -1 when both are taken.</summary>
        public int FirstEmptySlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_weapons[i] == null) return i;
            }
            return -1;
        }

        /// <summary>Puts a weapon straight into a slot (new game, rewards). Replaces what was there.</summary>
        public void Set(int slot, ItemDefinition weapon, int loaded)
        {
            if (!InRange(slot)) return;
            _weapons[slot] = weapon;
            _loaded[slot] = weapon != null ? Math.Max(0, loaded) : 0;
            Changed?.Invoke();
        }

        /// <summary>
        /// Moves a weapon item from a grid into a slot. A weapon already in the slot goes back
        /// into that same grid, unloaded; its rounds are reported in <paramref name="replacedRounds"/>
        /// for the caller to return as ammo. Fails, changing nothing, if the old weapon does not fit.
        /// </summary>
        public bool Equip(GridInventory grid, PlacedItem item, int slot, out ItemDefinition replaced, out int replacedRounds)
        {
            replaced = null;
            replacedRounds = 0;
            if (!InRange(slot) || grid == null || item == null || !grid.Contains(item)) return false;

            ItemDefinition weapon = item.Item;
            if (weapon.Category != ItemCategory.Weapon) return false;

            ItemDefinition old = _weapons[slot];
            int oldRounds = _loaded[slot];

            if (!grid.TakeFromStack(item, 1)) return false;
            if (old != null && grid.Add(old, 1) != 0)
            {
                // No room for the weapon coming out: put the new one back where it was.
                grid.Add(weapon, 1);
                return false;
            }

            _weapons[slot] = weapon;
            _loaded[slot] = 0;
            replaced = old;
            replacedRounds = oldRounds;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Moves the weapon in a slot into a grid. Its rounds are reported for the caller to return as ammo.</summary>
        public bool Unequip(int slot, GridInventory grid, out ItemDefinition weapon, out int rounds)
        {
            weapon = null;
            rounds = 0;
            if (!InRange(slot) || grid == null || _weapons[slot] == null) return false;
            if (grid.Add(_weapons[slot], 1) != 0) return false;

            weapon = _weapons[slot];
            rounds = _loaded[slot];
            _weapons[slot] = null;
            _loaded[slot] = 0;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Drops everything equipped (death in a raid).</summary>
        public void Clear()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _weapons[i] = null;
                _loaded[i] = 0;
            }
            Changed?.Invoke();
        }

        private static bool InRange(int slot)
        {
            return slot >= 0 && slot < SlotCount;
        }
    }
}
