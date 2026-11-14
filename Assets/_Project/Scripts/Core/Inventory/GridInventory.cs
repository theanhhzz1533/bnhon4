using System;
using System.Collections.Generic;
using Bnhon.Core.Items;

namespace Bnhon.Core.Inventory
{
    /// <summary>A stack of one item type placed at a position in a grid.</summary>
    public sealed class PlacedItem
    {
        public ItemDefinition Item { get; }
        public int Count { get; internal set; }
        public int X { get; internal set; }
        public int Y { get; internal set; }
        public bool Rotated { get; internal set; }

        public int Width => Rotated ? Item.Height : Item.Width;
        public int Height => Rotated ? Item.Width : Item.Height;

        internal PlacedItem(ItemDefinition item, int count, int x, int y, bool rotated)
        {
            Item = item;
            Count = count;
            X = x;
            Y = y;
            Rotated = rotated;
        }
    }

    /// <summary>
    /// Tarkov-style grid inventory: every item occupies a rectangle of cells
    /// and can be rotated 90 degrees. Used for the backpack, the secure pouch
    /// and the base stash.
    /// </summary>
    public sealed class GridInventory
    {
        private readonly PlacedItem[,] _cells;
        private readonly List<PlacedItem> _items = new List<PlacedItem>();

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<PlacedItem> Items => _items;

        public event Action Changed;

        public GridInventory(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            _cells = new PlacedItem[width, height];
        }

        public PlacedItem GetAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return _cells[x, y];
        }

        public bool CanPlace(ItemDefinition item, int x, int y, bool rotated)
        {
            return CanPlace(item, x, y, rotated, null);
        }

        /// <summary>Like <see cref="CanPlace(ItemDefinition,int,int,bool)"/>, but cells held by <paramref name="ignore"/> count as free.</summary>
        public bool CanPlace(ItemDefinition item, int x, int y, bool rotated, PlacedItem ignore)
        {
            if (item == null) return false;
            int w = rotated ? item.Height : item.Width;
            int h = rotated ? item.Width : item.Height;
            if (x < 0 || y < 0 || x + w > Width || y + h > Height) return false;

            for (int cx = x; cx < x + w; cx++)
            {
                for (int cy = y; cy < y + h; cy++)
                {
                    PlacedItem occupant = _cells[cx, cy];
                    if (occupant != null && occupant != ignore) return false;
                }
            }
            return true;
        }

        public bool Contains(PlacedItem placed)
        {
            return placed != null && _items.Contains(placed);
        }

        /// <summary>
        /// True if <paramref name="placed"/> (a stack in this grid) can be dropped at a cell of
        /// <paramref name="target"/>: either onto a stack of the same item that has room, or
        /// into free space. The target may be this grid.
        /// </summary>
        public bool CanMoveTo(PlacedItem placed, GridInventory target, int x, int y, bool rotated)
        {
            if (target == null || !Contains(placed)) return false;
            if (FindMergeTarget(placed, target, x, y) != null) return true;
            return target.CanPlace(placed.Item, x, y, rotated, target == this ? placed : null);
        }

        /// <summary>Drag-and-drop move. Merges into a matching stack under the drop cell, else relocates the stack.</summary>
        public bool MoveTo(PlacedItem placed, GridInventory target, int x, int y, bool rotated)
        {
            if (!CanMoveTo(placed, target, x, y, rotated)) return false;

            PlacedItem merge = FindMergeTarget(placed, target, x, y);
            if (merge != null)
            {
                int moved = Math.Min(merge.Item.MaxStack - merge.Count, placed.Count);
                merge.Count += moved;
                placed.Count -= moved;
                if (placed.Count == 0)
                {
                    Occupy(placed, null);
                    _items.Remove(placed);
                }
            }
            else
            {
                Occupy(placed, null);
                _items.Remove(placed);
                placed.X = x;
                placed.Y = y;
                placed.Rotated = rotated;
                target.Occupy(placed, placed);
                target._items.Add(placed);
            }

            Changed?.Invoke();
            if (target != this) target.Changed?.Invoke();
            return true;
        }

        /// <summary>Quick transfer: sends as much of the stack as fits anywhere in <paramref name="target"/>. Returns the number moved.</summary>
        public int TransferTo(PlacedItem placed, GridInventory target)
        {
            if (target == null || target == this || !Contains(placed)) return 0;

            int leftover = target.Add(placed.Item, placed.Count);
            int moved = placed.Count - leftover;
            if (moved == 0) return 0;

            if (leftover == 0)
            {
                Occupy(placed, null);
                _items.Remove(placed);
            }
            else
            {
                placed.Count = leftover;
            }

            Changed?.Invoke();
            return moved;
        }

        private static PlacedItem FindMergeTarget(PlacedItem placed, GridInventory target, int x, int y)
        {
            PlacedItem under = target.GetAt(x, y);
            if (under == null || under == placed) return null;
            if (under.Item != placed.Item || under.Count >= under.Item.MaxStack) return null;
            return under;
        }

        /// <summary>Places a stack at an exact position. Returns null if it does not fit.</summary>
        public PlacedItem TryPlace(ItemDefinition item, int count, int x, int y, bool rotated)
        {
            if (item == null || count < 1 || count > item.MaxStack) return null;
            if (!CanPlace(item, x, y, rotated)) return null;

            var placed = new PlacedItem(item, count, x, y, rotated);
            Occupy(placed, placed);
            _items.Add(placed);
            Changed?.Invoke();
            return placed;
        }

        /// <summary>
        /// Adds items wherever they fit: existing stacks first, then the first
        /// free spot scanning row by row. Returns how many did NOT fit.
        /// </summary>
        public int Add(ItemDefinition item, int count)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (count <= 0) return 0;

            int remaining = count;
            bool changed = false;

            if (item.MaxStack > 1)
            {
                foreach (var placed in _items)
                {
                    if (remaining == 0) break;
                    if (placed.Item != item || placed.Count >= item.MaxStack) continue;
                    int moved = Math.Min(item.MaxStack - placed.Count, remaining);
                    placed.Count += moved;
                    remaining -= moved;
                    changed = true;
                }
            }

            while (remaining > 0)
            {
                if (!TryFindSpace(item, out int x, out int y, out bool rotated)) break;
                int stack = Math.Min(item.MaxStack, remaining);
                var placed = new PlacedItem(item, stack, x, y, rotated);
                Occupy(placed, placed);
                _items.Add(placed);
                remaining -= stack;
                changed = true;
            }

            if (changed) Changed?.Invoke();
            return remaining;
        }

        public bool Remove(PlacedItem placed)
        {
            if (placed == null || !_items.Remove(placed)) return false;
            Occupy(placed, null);
            Changed?.Invoke();
            return true;
        }

        public int CountOf(string itemId)
        {
            int total = 0;
            foreach (var placed in _items)
            {
                if (placed.Item.Id == itemId) total += placed.Count;
            }
            return total;
        }

        /// <summary>Removes exactly <paramref name="count"/> items, or nothing if there are not enough.</summary>
        public bool TryConsume(string itemId, int count)
        {
            if (count <= 0) return true;
            if (CountOf(itemId) < count) return false;

            int remaining = count;
            for (int i = _items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var placed = _items[i];
                if (placed.Item.Id != itemId) continue;

                int taken = Math.Min(placed.Count, remaining);
                placed.Count -= taken;
                remaining -= taken;
                if (placed.Count == 0)
                {
                    Occupy(placed, null);
                    _items.RemoveAt(i);
                }
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Moves everything that fits into <paramref name="target"/>. Returns the number of items left behind.</summary>
        public int MoveAllTo(GridInventory target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target == this) return 0;

            int leftBehind = 0;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var placed = _items[i];
                int leftover = target.Add(placed.Item, placed.Count);
                if (leftover == 0)
                {
                    Occupy(placed, null);
                    _items.RemoveAt(i);
                }
                else
                {
                    placed.Count = leftover;
                    leftBehind += leftover;
                }
            }

            Changed?.Invoke();
            return leftBehind;
        }

        public void Clear()
        {
            if (_items.Count == 0) return;
            _items.Clear();
            Array.Clear(_cells, 0, _cells.Length);
            Changed?.Invoke();
        }

        public int TotalValue()
        {
            int total = 0;
            foreach (var placed in _items) total += placed.Item.BaseValue * placed.Count;
            return total;
        }

        private bool TryFindSpace(ItemDefinition item, out int x, out int y, out bool rotated)
        {
            for (int cy = 0; cy < Height; cy++)
            {
                for (int cx = 0; cx < Width; cx++)
                {
                    if (CanPlace(item, cx, cy, false))
                    {
                        x = cx; y = cy; rotated = false;
                        return true;
                    }
                    if (item.Width != item.Height && CanPlace(item, cx, cy, true))
                    {
                        x = cx; y = cy; rotated = true;
                        return true;
                    }
                }
            }
            x = y = 0;
            rotated = false;
            return false;
        }

        private void Occupy(PlacedItem area, PlacedItem value)
        {
            for (int cx = area.X; cx < area.X + area.Width; cx++)
            {
                for (int cy = area.Y; cy < area.Y + area.Height; cy++)
                {
                    _cells[cx, cy] = value;
                }
            }
        }
    }
}
