using System;
using System.Collections.Generic;

namespace Bnhon.Core.Layout
{
    /// <summary>A kind of building that stands in the base, with its footprint in grid cells.</summary>
    public sealed class BuildingType
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Width { get; }
        public int Height { get; }

        public BuildingType(string id, string displayName, int width, int height)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Building id is required.", nameof(id));
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
            Id = id;
            DisplayName = displayName;
            Width = width;
            Height = height;
        }
    }

    public sealed class PlacedBuilding
    {
        public BuildingType Type { get; }

        /// <summary>Left column of the footprint.</summary>
        public int X { get; internal set; }

        /// <summary>Bottom row of the footprint.</summary>
        public int Y { get; internal set; }

        internal PlacedBuilding(BuildingType type, int x, int y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public bool Covers(int x, int y)
        {
            return x >= X && x < X + Type.Width && y >= Y && y < Y + Type.Height;
        }
    }

    /// <summary>
    /// Where the buildings of the base stand, on a grid of one-unit cells with (0,0) at the
    /// bottom left. Buildings can be moved anywhere they fit: inside the yard, off each other
    /// and off the reserved cells (the farm, the gate, where residents stand).
    /// </summary>
    public sealed class BaseLayout
    {
        private readonly bool[,] _reserved;
        private readonly List<PlacedBuilding> _buildings = new List<PlacedBuilding>();

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<PlacedBuilding> Buildings => _buildings;

        public BaseLayout(int width, int height)
        {
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            _reserved = new bool[width, height];
        }

        /// <summary>Marks cells nothing may be built on. Cells outside the grid are ignored.</summary>
        public void Reserve(int x, int y, int width = 1, int height = 1)
        {
            for (int cx = x; cx < x + width; cx++)
            {
                for (int cy = y; cy < y + height; cy++)
                {
                    if (InBounds(cx, cy)) _reserved[cx, cy] = true;
                }
            }
        }

        public bool IsReserved(int x, int y) => InBounds(x, y) && _reserved[x, y];

        /// <summary>Adds a building at its starting position. Throws if it does not fit there.</summary>
        public PlacedBuilding Add(BuildingType type, int x, int y)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (Get(type.Id) != null) throw new ArgumentException("Building '" + type.Id + "' is already placed.");
            if (!Fits(type, x, y, null)) throw new ArgumentException("Building '" + type.Id + "' does not fit at " + x + "," + y + ".");

            var placed = new PlacedBuilding(type, x, y);
            _buildings.Add(placed);
            return placed;
        }

        public PlacedBuilding Get(string buildingId)
        {
            foreach (PlacedBuilding building in _buildings)
            {
                if (building.Type.Id == buildingId) return building;
            }
            return null;
        }

        /// <summary>The building standing on a cell, or null.</summary>
        public PlacedBuilding At(int x, int y)
        {
            foreach (PlacedBuilding building in _buildings)
            {
                if (building.Covers(x, y)) return building;
            }
            return null;
        }

        /// <summary>True if the building could stand with its bottom-left corner at the cell. Its current place does not block it.</summary>
        public bool CanPlace(string buildingId, int x, int y)
        {
            PlacedBuilding building = Get(buildingId);
            return building != null && Fits(building.Type, x, y, building);
        }

        public bool Move(string buildingId, int x, int y)
        {
            if (!CanPlace(buildingId, x, y)) return false;
            PlacedBuilding building = Get(buildingId);
            building.X = x;
            building.Y = y;
            return true;
        }

        /// <summary>
        /// A free cell next to a building for someone to stand on: left, right, below, then
        /// above. Falls back to the cell on its left even if that is taken.
        /// </summary>
        public void StandCell(string buildingId, out int x, out int y)
        {
            PlacedBuilding building = Get(buildingId);
            if (building == null)
            {
                x = 0;
                y = 0;
                return;
            }

            int[] xs = { building.X - 1, building.X + building.Type.Width, building.X, building.X };
            int[] ys = { building.Y, building.Y, building.Y - 1, building.Y + building.Type.Height };
            for (int i = 0; i < xs.Length; i++)
            {
                if (!InBounds(xs[i], ys[i]) || At(xs[i], ys[i]) != null) continue;
                x = xs[i];
                y = ys[i];
                return;
            }

            x = Math.Max(0, building.X - 1);
            y = building.Y;
        }

        private bool Fits(BuildingType type, int x, int y, PlacedBuilding ignore)
        {
            for (int cx = x; cx < x + type.Width; cx++)
            {
                for (int cy = y; cy < y + type.Height; cy++)
                {
                    if (!InBounds(cx, cy) || _reserved[cx, cy]) return false;
                    PlacedBuilding other = At(cx, cy);
                    if (other != null && other != ignore) return false;
                }
            }
            return true;
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }
    }
}
