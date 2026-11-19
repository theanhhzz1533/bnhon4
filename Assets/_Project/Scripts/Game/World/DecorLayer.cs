using System.Collections.Generic;
using Bnhon.Core.Maps;
using Bnhon.Core.Pathfinding;
using UnityEngine;

namespace Bnhon.Game.World
{
    /// <summary>
    /// Dresses a raid map so it looks abandoned: stains, cracks and litter on the ground,
    /// weeds in the cracks, vines on walls, trees over the map's small rock-and-tree blobs,
    /// and wrecks, barricades and barrels standing about. Placement follows rules (what
    /// kind of ground, how near a wall or a zombie) with a fixed seed per map, so a map
    /// always looks the same.
    ///
    /// Flat details are only drawn when their picture exists, since a coloured square on
    /// the ground would look worse than nothing. Standing objects (cars, sandbags) are
    /// placed either way, as a coloured box without art, because they are cover and get
    /// in the way: the game plays the same with or without the pictures.
    /// </summary>
    public sealed class DecorLayer
    {
        private const string Paved = ".=";
        private const string Soft = ",:";
        private const string AnyGround = ".,=:~";

        private readonly RaidMap _map;
        private readonly System.Func<float, float, Vector2> _cellCenter;
        private readonly Transform _parent;
        private readonly System.Random _rng;
        private readonly bool[,] _taken;
        private readonly HashSet<int> _treeCells = new HashSet<int>();
        private readonly List<Vector2> _fires = new List<Vector2>();
        private readonly List<Vector2> _smokes = new List<Vector2>();

        /// <summary>Cells now covered by a standing object, where nothing else may be put.</summary>
        public IEnumerable<GridPoint> BlockedCells
        {
            get
            {
                for (int y = 0; y < _map.Height; y++)
                {
                    for (int x = 0; x < _map.Width; x++)
                    {
                        if (_taken[x, y] && !_map.IsWall(x, y)) yield return new GridPoint(x, y);
                    }
                }
            }
        }

        /// <summary>Wall cells drawn as trees or bushes, whose own wall picture should be hidden.</summary>
        public bool IsTreeCell(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _map.Width && y < _map.Height && _treeCells.Contains(y * _map.Width + x);
        }

        public IReadOnlyList<Vector2> Fires => _fires;
        public IReadOnlyList<Vector2> Smokes => _smokes;

        public DecorLayer(RaidMap map, System.Func<float, float, Vector2> cellCenter, Transform parent, int seed)
        {
            _map = map;
            _cellCenter = cellCenter;
            _parent = parent;
            _rng = new System.Random(seed);
            _taken = new bool[map.Width, map.Height];

            // Markers stay clear: nothing is put on or right next to loot, zombies, exits or the spawn.
            foreach (MapMarker marker in map.Markers) Reserve(marker.X, marker.Y, ExitRadius(marker.Symbol));
            Reserve(map.Spawn.X, map.Spawn.Y, 3);
        }

        private static int ExitRadius(char symbol)
        {
            return symbol == RaidMap.Extraction || symbol == RaidMap.PoweredExit || symbol == RaidMap.BossExit ? 3 : 1;
        }

        private void Reserve(int cx, int cy, int radius)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x >= 0 && y >= 0 && x < _map.Width && y < _map.Height) _taken[x, y] = true;
                }
            }
        }

        /// <summary>Works out which wall blobs are trees. Call before the walls are drawn.</summary>
        public void FindTrees()
        {
            var seen = new bool[_map.Width, _map.Height];
            var blob = new List<GridPoint>();
            var queue = new Queue<GridPoint>();

            for (int y = 1; y < _map.Height - 1; y++)
            {
                for (int x = 1; x < _map.Width - 1; x++)
                {
                    if (seen[x, y] || !_map.IsWall(x, y)) continue;

                    blob.Clear();
                    bool onNature = true;
                    queue.Enqueue(new GridPoint(x, y));
                    seen[x, y] = true;
                    while (queue.Count > 0)
                    {
                        GridPoint cell = queue.Dequeue();
                        blob.Add(cell);
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cell.X + (d == 0 ? 1 : d == 1 ? -1 : 0);
                            int ny = cell.Y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                            if (_map.IsWall(nx, ny))
                            {
                                if (nx > 0 && ny > 0 && nx < _map.Width - 1 && ny < _map.Height - 1 && !seen[nx, ny])
                                {
                                    seen[nx, ny] = true;
                                    queue.Enqueue(new GridPoint(nx, ny));
                                }
                            }
                            else if (Soft.IndexOf(_map.GroundAt(nx, ny)) < 0)
                            {
                                onNature = false;
                            }
                        }
                    }

                    // A small blob standing on grass or dirt is a tree or a bush, not a building or a car.
                    if (!onNature || blob.Count > 14) continue;
                    DrawTree(blob);
                }
            }
        }

        private void DrawTree(List<GridPoint> blob)
        {
            float sumX = 0f, sumY = 0f;
            foreach (GridPoint cell in blob)
            {
                sumX += cell.X;
                sumY += cell.Y;
            }
            Vector2 center = _cellCenter(sumX / blob.Count, sumY / blob.Count);

            string key = blob.Count <= 3 ? Pick("prop_bush_1", "prop_bush_2") : Pick("prop_tree_green", "prop_tree_green", "prop_tree_dead");
            Sprite art = PlaceholderArt.FindSprite(key);
            if (art == null) return;

            // Only blobs that are really drawn as a tree hide their wall picture.
            foreach (GridPoint cell in blob) _treeCells.Add(cell.Y * _map.Width + cell.X);

            float size = Mathf.Sqrt(blob.Count) * 1.55f + 0.6f;
            Picture(key, art, center, size, YSortOrder(center.y - 0.6f), false, Color.white);
        }

        /// <summary>Everything flat on the ground. Only drawn where the art exists.</summary>
        public void ScatterGround()
        {
            for (int y = 1; y < _map.Height - 1; y++)
            {
                for (int x = 1; x < _map.Width - 1; x++)
                {
                    if (_map.IsWall(x, y)) continue;
                    char ground = _map.GroundAt(x, y);
                    bool edge = TouchesSoft(x, y) && Paved.IndexOf(ground) >= 0;
                    bool nearWall = TouchesWall(x, y);

                    if (Paved.IndexOf(ground) >= 0)
                    {
                        if (Chance(edge ? 0.12 : 0.012)) Flat(x, y, Pick("decor_weeds_1", "decor_weeds_2"), 0.8f, 1.2f, false);
                        if (Chance(ground == '=' ? 0.02 : 0.01)) Flat(x, y, Pick("decor_crack_1", "decor_crack_2"), 1.6f, 2.6f, true);
                        if (Chance(0.004)) Flat(x, y, "decor_scorch", 1.8f, 3f, true);
                        if (Chance(0.006)) Flat(x, y, "decor_trash", 0.8f, 1.3f, true);
                        if (nearWall && Chance(0.01)) Flat(x, y, "decor_glass", 0.7f, 1.1f, true);
                    }
                    else if (ground == ':')
                    {
                        if (Chance(0.012)) Flat(x, y, Pick("decor_weeds_1", "decor_weeds_2"), 0.8f, 1.2f, false);
                        if (Chance(0.004)) Flat(x, y, "decor_trash", 0.8f, 1.2f, true);
                    }

                    if (ground != '~' && Chance(0.003)) Flat(x, y, "decor_puddle", 1.4f, 2.4f, true);
                    if (Soft.IndexOf(ground) >= 0 && TouchesTree(x, y) && Chance(0.18)) Flat(x, y, "decor_leaves", 1f, 1.6f, true);
                }
            }

            // Blood where the dead are: around zombie positions and a few drag marks on roads.
            foreach (MapMarker marker in _map.Markers)
            {
                if (RaidMap.ZombieSymbols.IndexOf(marker.Symbol) < 0) continue;
                if (Chance(0.35)) Flat(marker.X + _rng.Next(-2, 3), marker.Y + _rng.Next(-2, 3), "decor_blood_pool", 0.9f, 1.6f, true);
                if (Chance(0.12)) Flat(marker.X + _rng.Next(-3, 4), marker.Y + _rng.Next(-3, 4), "decor_blood_drag", 2f, 2.8f, true);
            }

            // Vines climbing walls that face grass.
            Sprite vines = PlaceholderArt.FindSprite("decor_vines");
            if (vines == null) return;
            for (int y = 1; y < _map.Height - 1; y++)
            {
                for (int x = 1; x < _map.Width - 1; x++)
                {
                    if (!_map.IsWall(x, y) || IsTreeCell(x, y) || !TouchesGround(x, y, ",")) continue;
                    if (Chance(0.18)) Picture("decor_vines", vines, _cellCenter(x, y), RandomRange(1.1f, 1.6f), 6, true, Color.white);
                }
            }
        }

        private struct PropKind
        {
            public string Key;
            public Vector2 Footprint;
            public SpriteFit Fit;
            public Color Color;
            public bool Blocks;
        }

        private static PropKind Prop(string key, float width, float height, float drawWidth, float drawHeight, Color color, bool blocks = true)
        {
            return new PropKind { Key = key, Footprint = new Vector2(width, height), Fit = SpriteFit.Standing(drawWidth, drawHeight), Color = color, Blocks = blocks };
        }

        private static readonly Color Rust = new Color(0.45f, 0.28f, 0.2f);
        private static readonly Color Steel = new Color(0.4f, 0.42f, 0.44f);
        private static readonly Color Burnt = new Color(0.16f, 0.14f, 0.13f);
        private static readonly Color Khaki = new Color(0.5f, 0.45f, 0.32f);

        private static readonly PropKind[] RoadProps =
        {
            Prop("prop_car_rust_red", 3.4f, 1.6f, 3.8f, 2.4f, Rust),
            Prop("prop_car_rust_blue", 3.4f, 1.6f, 3.8f, 2.4f, new Color(0.25f, 0.32f, 0.4f)),
            Prop("prop_car_burnt", 3.4f, 1.6f, 3.8f, 2.4f, Burnt),
            Prop("prop_motorbike", 1.6f, 0.7f, 1.9f, 1.4f, Steel),
            Prop("prop_concrete_barrier", 2.6f, 0.8f, 2.8f, 1.3f, new Color(0.55f, 0.55f, 0.53f)),
        };

        private static readonly PropKind[] StreetProps =
        {
            Prop("prop_trash_bin", 0.8f, 0.8f, 1f, 1.3f, new Color(0.25f, 0.35f, 0.28f)),
            Prop("prop_rubble", 1.6f, 1.1f, 2f, 1.5f, new Color(0.42f, 0.38f, 0.34f)),
            Prop("prop_barrel", 0.8f, 0.8f, 0.9f, 1.2f, Rust),
            Prop("prop_shopping_cart", 1f, 0.7f, 1.2f, 1.2f, Steel),
            Prop("prop_bicycle", 1.4f, 0.5f, 1.6f, 1.2f, Steel, false),
            Prop("prop_tire", 0.7f, 0.7f, 0.8f, 0.8f, Burnt, false),
            Prop("prop_suitcase", 0.8f, 0.5f, 1f, 0.9f, new Color(0.45f, 0.3f, 0.25f), false),
        };

        private static readonly PropKind[] RareProps =
        {
            Prop("prop_ambulance", 3.8f, 1.7f, 4.2f, 2.7f, new Color(0.75f, 0.75f, 0.72f)),
            Prop("prop_bus", 6.5f, 1.9f, 7f, 3f, new Color(0.55f, 0.45f, 0.2f)),
            Prop("prop_tent_collapsed", 2.6f, 2f, 3f, 2.4f, new Color(0.35f, 0.42f, 0.35f), false),
            Prop("prop_tarp_body", 1.8f, 0.8f, 2f, 1.1f, new Color(0.3f, 0.4f, 0.5f), false),
            Prop("prop_power_pole_fallen", 4.5f, 0.5f, 4.8f, 1.2f, new Color(0.35f, 0.3f, 0.25f), false),
            Prop("prop_sign_fallen", 1.6f, 0.6f, 1.8f, 1f, new Color(0.6f, 0.55f, 0.2f), false),
        };

        private static readonly PropKind[] BarricadeProps =
        {
            Prop("prop_sandbags", 2f, 0.8f, 2.2f, 1.3f, Khaki),
            Prop("prop_sandbags", 2f, 0.8f, 2.2f, 1.3f, Khaki),
            Prop("prop_barbed_wire", 2.2f, 0.6f, 2.4f, 1.1f, Steel),
            Prop("prop_barricade_furniture", 2.2f, 1f, 2.6f, 1.8f, new Color(0.4f, 0.3f, 0.22f)),
            Prop("prop_barrel", 0.8f, 0.8f, 0.9f, 1.2f, Rust),
        };

        /// <summary>
        /// Standing objects: wrecks on the roads, litter in the streets, barricades around the
        /// places worth guarding (switches, guarded exits, safes, bosses), and a fire or two.
        /// </summary>
        public void PlaceProps()
        {
            int roadCells = 0, streetCells = 0;
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    char ground = _map.GroundAt(x, y);
                    if (ground == '=') roadCells++;
                    else if (ground == '.' || ground == ':') streetCells++;
                }
            }

            Scatter(RoadProps, "=", roadCells / 260, 3);
            Scatter(StreetProps, ".:", streetCells / 320, 1);
            Scatter(RareProps, ".:=,", Mathf.Max(3, (roadCells + streetCells) / 4000), 2);

            // Barricades and burning barrels round the places someone would have defended.
            foreach (MapMarker marker in _map.Markers)
            {
                bool guarded = marker.Symbol == RaidMap.PowerSwitch || marker.Symbol == RaidMap.PoweredExit
                    || marker.Symbol == RaidMap.BossExit || marker.Symbol == 'V' || marker.Symbol == 'B';
                if (!guarded) continue;

                int placed = 0;
                for (int attempt = 0; attempt < 40 && placed < 4; attempt++)
                {
                    int x = marker.X + _rng.Next(-7, 8);
                    int y = marker.Y + _rng.Next(-7, 8);
                    if (TryPlace(BarricadeProps[_rng.Next(BarricadeProps.Length)], x, y, AnyGround, 1)) placed++;
                }
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int x = marker.X + _rng.Next(-6, 7);
                    int y = marker.Y + _rng.Next(-6, 7);
                    if (TryPlace(Prop("prop_barrel_fire", 0.8f, 0.8f, 0.9f, 1.3f, new Color(0.55f, 0.25f, 0.1f)), x, y, AnyGround, 1))
                    {
                        _fires.Add(_cellCenter(x, y));
                        break;
                    }
                }
            }
        }

        private void Scatter(PropKind[] kinds, string grounds, int count, int margin)
        {
            int placed = 0;
            for (int attempt = 0; attempt < count * 60 && placed < count; attempt++)
            {
                int x = _rng.Next(1, _map.Width - 1);
                int y = _rng.Next(1, _map.Height - 1);
                if (TryPlace(kinds[_rng.Next(kinds.Length)], x, y, grounds, margin)) placed++;
            }
        }

        /// <summary>
        /// Puts an object with its middle on a cell if the cells it covers, plus a margin, are
        /// all open ground of the right kind and unused. The margin keeps paths and doors clear.
        /// </summary>
        private bool TryPlace(PropKind kind, int cx, int cy, string grounds, int margin)
        {
            int halfW = Mathf.CeilToInt(kind.Footprint.x * 0.5f);
            int halfH = Mathf.CeilToInt(kind.Footprint.y * 0.5f);
            int reach = kind.Blocks ? margin : 0;

            for (int y = cy - halfH - reach; y <= cy + halfH + reach; y++)
            {
                for (int x = cx - halfW - reach; x <= cx + halfW + reach; x++)
                {
                    if (x < 1 || y < 1 || x >= _map.Width - 1 || y >= _map.Height - 1) return false;
                    if (_map.IsWall(x, y) || _taken[x, y]) return false;
                    bool covered = x >= cx - halfW && x <= cx + halfW && y >= cy - halfH && y <= cy + halfH;
                    if (covered && grounds.IndexOf(_map.GroundAt(x, y)) < 0) return false;
                }
            }

            for (int y = cy - halfH; y <= cy + halfH; y++)
            {
                for (int x = cx - halfW; x <= cx + halfW; x++) _taken[x, y] = true;
            }

            Vector2 center = _cellCenter(cx, cy);
            bool hasArt = PlaceholderArt.FindSprite(kind.Key) != null;
            GameObject go = PlaceholderArt.Box(kind.Key, center, kind.Footprint, kind.Color, _parent, kind.Blocks ? 4 : 2, kind.Key, kind.Fit);
            if (kind.Blocks)
            {
                go.AddComponent<BoxCollider2D>();
                go.AddComponent<YSort>();
            }
            else if (!hasArt)
            {
                // Without art, low clutter would just be odd squares in the way of nothing.
                Object.Destroy(go);
            }

            // Mirror some wrecks so the same picture does not look copied everywhere.
            if (hasArt && _rng.NextDouble() < 0.5)
            {
                var renderer = go.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null) renderer.flipX = true;
            }

            if (kind.Key == "prop_car_burnt" && _rng.NextDouble() < 0.5) _smokes.Add(center);
            return true;
        }

        private void Flat(int x, int y, string key, float minSize, float maxSize, bool rotate)
        {
            if (_map.IsWall(x, y)) return;
            Sprite art = PlaceholderArt.FindSprite(key);
            if (art == null) return;
            Vector2 center = _cellCenter(x + (float)_rng.NextDouble() - 0.5f, y + (float)_rng.NextDouble() - 0.5f);
            Picture(key, art, center, RandomRange(minSize, maxSize), 1, rotate, new Color(1f, 1f, 1f, 0.85f));
        }

        /// <summary>A picture scaled so its longest side is <paramref name="size"/> units.</summary>
        private void Picture(string name, Sprite art, Vector2 center, float size, int order, bool rotate, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_parent, false);
            go.transform.position = center;
            Vector3 bounds = art.bounds.size;
            float scale = size / Mathf.Max(0.0001f, Mathf.Max(bounds.x, bounds.y));
            go.transform.localScale = new Vector3(scale, scale, 1f);
            if (rotate) go.transform.rotation = Quaternion.Euler(0f, 0f, (float)_rng.NextDouble() * 360f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.sharedMaterial = PlaceholderArt.UnlitMaterial;
            renderer.sortingOrder = order;
            renderer.color = color;
            if (!rotate && _rng.NextDouble() < 0.5) renderer.flipX = true;
        }

        /// <summary>The order YSort gives something whose feet are at this height, for pictures that never move.</summary>
        private static int YSortOrder(float feetY)
        {
            return 10000 - Mathf.RoundToInt(feetY * 20f);
        }

        private bool TouchesSoft(int x, int y) => TouchesGround(x, y, Soft);

        private bool TouchesGround(int x, int y, string grounds)
        {
            return grounds.IndexOf(_map.GroundAt(x + 1, y)) >= 0 || grounds.IndexOf(_map.GroundAt(x - 1, y)) >= 0
                || grounds.IndexOf(_map.GroundAt(x, y + 1)) >= 0 || grounds.IndexOf(_map.GroundAt(x, y - 1)) >= 0;
        }

        private bool TouchesWall(int x, int y)
        {
            return _map.IsWall(x + 1, y) || _map.IsWall(x - 1, y) || _map.IsWall(x, y + 1) || _map.IsWall(x, y - 1);
        }

        /// <summary>True if a tree-sized wall blob is within two cells, drawn as a tree or not.</summary>
        private bool TouchesTree(int x, int y)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (IsTreeCell(nx, ny)) return true;
                    if (_map.IsWall(nx, ny) && Soft.IndexOf(_map.GroundAt(x, y)) >= 0 && nx > 0 && ny > 0 && nx < _map.Width - 1 && ny < _map.Height - 1
                        && !_map.IsWall(nx + 2, ny) && !_map.IsWall(nx - 2, ny)) return true;
                }
            }
            return false;
        }

        private bool Chance(double probability) => _rng.NextDouble() < probability;

        private float RandomRange(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

        private string Pick(params string[] keys) => keys[_rng.Next(keys.Length)];
    }
}
