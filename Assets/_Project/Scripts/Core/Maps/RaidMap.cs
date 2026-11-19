using System;
using System.Collections.Generic;
using Bnhon.Core.Pathfinding;

namespace Bnhon.Core.Maps
{
    public struct MapMarker
    {
        public char Symbol;
        public int X;
        public int Y;

        public MapMarker(char symbol, int x, int y)
        {
            Symbol = symbol;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// A raid map written as text, one character per cell:
    /// <code>
    ///   #  wall                 .  floor
    ///   P  player entry (exactly one)
    ///   E  extraction point (at least one)
    ///   C  house cabinet   M  medical cabinet   S  seed store   W  weapon crate
    ///   V  safe (rare loot)
    ///   Z  walker          R  runner            G  screamer
    ///   A  armoured        B  the area's boss
    /// </code>
    /// Story maps (the hospital opening) also use:
    /// <code>
    ///   K  cabinet with fixed story loot      T  trigger that releases the horde
    ///   H  where a horde zombie appears       N  the rescuer; reaching them ends the map
    /// </code>
    /// A map needs a way out: at least one E, or an N.
    /// Lines starting with ';' are comments. Short rows are padded with wall.
    /// The first text row is the top of the map; X grows right, Y grows down.
    /// </summary>
    public sealed class RaidMap
    {
        public const char Wall = '#';
        public const char Floor = '.';
        public const char PlayerSpawn = 'P';
        public const char Extraction = 'E';
        public const string LootSymbols = "CMSWV";
        public const string ZombieSymbols = "ZRGAB";
        public const string StorySymbols = "KTHN";
        public const char Rescuer = 'N';

        /// <summary>Walkable ground kinds, drawn differently: concrete or indoor floor, grass, road, dirt, paddy.</summary>
        public const string GroundSymbols = ".,=:~";

        /// <summary>A power switch (X), an exit that needs the power on (J), an exit that needs the area's boss dead (U).</summary>
        public const string ConditionalSymbols = "XJU";
        public const char PowerSwitch = 'X';
        public const char PoweredExit = 'J';
        public const char BossExit = 'U';

        private readonly bool[,] _walls;
        private readonly char[,] _ground;
        private readonly List<MapMarker> _markers;

        public int Width { get; }
        public int Height { get; }
        public GridPoint Spawn { get; }

        /// <summary>Everything placed on the map except the player spawn.</summary>
        public IReadOnlyList<MapMarker> Markers => _markers;

        private RaidMap(bool[,] walls, char[,] ground, GridPoint spawn, List<MapMarker> markers)
        {
            _walls = walls;
            _ground = ground;
            Width = walls.GetLength(0);
            Height = walls.GetLength(1);
            Spawn = spawn;
            _markers = markers;
        }

        /// <summary>Cells outside the map count as wall.</summary>
        /// <summary>The kind of ground on a cell: one of <see cref="GroundSymbols"/>, or '#' for a wall. Cells with a marker are plain floor.</summary>
        public char GroundAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return Wall;
            return _ground[x, y];
        }

        public bool IsWall(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return true;
            return _walls[x, y];
        }

        /// <summary>Parses map text. Throws <see cref="FormatException"/> with a readable reason if the map is invalid.</summary>
        public static RaidMap Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var rows = new List<string>();
            foreach (string raw in text.Replace("\r", string.Empty).Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.Length == 0 || line[0] == ';') continue;
                rows.Add(line);
            }
            if (rows.Count == 0) throw new FormatException("The map has no rows.");

            int width = 0;
            foreach (string row in rows) width = Math.Max(width, row.Length);

            var walls = new bool[width, rows.Count];
            var ground = new char[width, rows.Count];
            var markers = new List<MapMarker>();
            GridPoint spawn = default(GridPoint);
            int spawnCount = 0;
            int extractionCount = 0;

            for (int y = 0; y < rows.Count; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    char symbol = x < row.Length ? row[x] : Wall;
                    ground[x, y] = Floor;
                    if (symbol == Wall)
                    {
                        walls[x, y] = true;
                        ground[x, y] = Wall;
                    }
                    else if (symbol == ' ')
                    {
                        // open ground
                    }
                    else if (GroundSymbols.IndexOf(symbol) >= 0)
                    {
                        ground[x, y] = symbol;
                    }
                    else if (symbol == PlayerSpawn)
                    {
                        spawn = new GridPoint(x, y);
                        spawnCount++;
                    }
                    else if (symbol == Extraction || LootSymbols.IndexOf(symbol) >= 0 || ZombieSymbols.IndexOf(symbol) >= 0 || StorySymbols.IndexOf(symbol) >= 0
                        || ConditionalSymbols.IndexOf(symbol) >= 0)
                    {
                        if (symbol == Extraction || symbol == Rescuer) extractionCount++;
                        markers.Add(new MapMarker(symbol, x, y));
                    }
                    else
                    {
                        throw new FormatException("Unknown map symbol '" + symbol + "' at row " + (y + 1) + ", column " + (x + 1) + ".");
                    }
                }
            }

            if (spawnCount != 1) throw new FormatException("The map needs exactly one player spawn 'P', found " + spawnCount + ".");
            if (extractionCount < 1) throw new FormatException("The map needs at least one extraction point 'E' (or a rescuer 'N' on a story map).");

            return new RaidMap(walls, ground, spawn, markers);
        }

        /// <summary>Markers the player cannot walk to from the spawn. A well-formed map returns none.</summary>
        public List<MapMarker> FindUnreachableMarkers()
        {
            var grid = new PathGrid(Width, Height);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    grid.SetBlocked(x, y, _walls[x, y]);
                }
            }

            var unreachable = new List<MapMarker>();
            var path = new List<GridPoint>();
            foreach (MapMarker marker in _markers)
            {
                if (!grid.FindPath(Spawn, new GridPoint(marker.X, marker.Y), path)) unreachable.Add(marker);
            }
            return unreachable;
        }
    }
}
