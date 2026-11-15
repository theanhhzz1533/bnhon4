using System;
using Bnhon.Core.Content;
using Bnhon.Core.Loot;
using Bnhon.Core.Maps;
using Bnhon.Game.AI;
using Bnhon.Game.Combat;
using Bnhon.Game.Interaction;
using Bnhon.Game.Player;
using Bnhon.Game.Raid;
using Bnhon.Game.UI;
using Bnhon.Game.Vision;
using UnityEngine;

namespace Bnhon.Game
{
    /// <summary>
    /// Builds the whole test level at runtime: a base with a farm, residents,
    /// a stash and a bed, plus a raid zone read from a text map
    /// (<c>Resources/Maps/&lt;mapName&gt;.txt</c>, format in <see cref="RaidMap"/>).
    /// Add this to an empty GameObject in an empty scene and press Play.
    /// Objects are colored squares until art with the matching name is added
    /// to <c>Resources/Sprites</c> (see <see cref="PlaceholderArt"/>).
    /// </summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        /// <summary>Used when the map file is missing or invalid, so the game still starts.</summary>
        private const string FallbackMap =
            "##############\n" +
            "#P....#....E.#\n" +
            "#.....#..Z...#\n" +
            "#..C.....Z.W.#\n" +
            "##############\n";

        private static readonly Vector2 BaseCenter = Vector2.zero;
        private static readonly Vector2 BaseSize = new Vector2(20f, 14f);
        private static readonly Vector2 RaidCenter = new Vector2(80f, 0f);
        private static readonly Vector2 BaseSpawn = new Vector2(0f, -2f);

        private static readonly Color WallColor = new Color(0.18f, 0.18f, 0.2f);
        private static readonly Color RaidFloorColor = new Color(0.25f, 0.26f, 0.28f);

        [SerializeField] private string mapName = "outskirts";
        [SerializeField] private float cameraSize = 7f;

        private RaidMap _map;
        private Vector2 _raidSize;
        private Transform _raidRoot;
        private PlayerController _player;

        private void Start()
        {
            GameRoot.Ensure();
            LoadMap();

            BuildBase();
            _player = BuildPlayer();
            CameraFollow cameraFollow = SetUpCamera(_player);

            new GameObject("NavGrid").AddComponent<NavGrid>();

            var director = new GameObject("RaidDirector").AddComponent<RaidDirector>();
            director.Init(_player, cameraFollow, BaseSpawn, CellCenter(_map.Spawn.X, _map.Spawn.Y), BuildRaidZone);
            BuildRaidZone();

            var hud = new GameObject("DebugHud").AddComponent<DebugHud>();
            hud.Player = _player;

            var vision = new GameObject("VisionSystem").AddComponent<VisionSystem>();
            vision.Init(_player);

            var inventory = new GameObject("InventoryScreen").AddComponent<InventoryScreen>();
            inventory.Player = _player;
        }

        private void LoadMap()
        {
            var root = GameRoot.Instance;
            TextAsset asset = Resources.Load<TextAsset>("Maps/" + mapName);

            if (asset == null)
            {
                root.Notify("Không tìm thấy bản đồ Resources/Maps/" + mapName + ".txt, dùng bản đồ dự phòng.", 8f);
            }
            else
            {
                try
                {
                    _map = RaidMap.Parse(asset.text);
                }
                catch (FormatException error)
                {
                    root.Notify("Bản đồ " + mapName + " lỗi: " + error.Message + " Dùng bản đồ dự phòng.", 10f);
                }
            }

            if (_map == null) _map = RaidMap.Parse(FallbackMap);
            _raidSize = new Vector2(_map.Width, _map.Height);

            int unreachable = _map.FindUnreachableMarkers().Count;
            if (unreachable > 0)
            {
                root.Notify("Bản đồ " + mapName + ": có " + unreachable + " vị trí bị tường bít kín, không tới được.", 8f);
            }
        }

        /// <summary>World position of the centre of a map cell. Row 0 of the text is the top of the map.</summary>
        private Vector2 CellCenter(float x, float y)
        {
            return new Vector2(
                RaidCenter.x - _raidSize.x * 0.5f + x + 0.5f,
                RaidCenter.y + _raidSize.y * 0.5f - y - 0.5f);
        }

        private PlayerController BuildPlayer()
        {
            GameObject go = PlaceholderArt.Box("Player", BaseSpawn, new Vector2(0.8f, 0.8f), new Color(0.25f, 0.55f, 0.95f), null, 20, "player", SpriteFit.Character);
            go.AddComponent<BoxCollider2D>();

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            go.AddComponent<YSort>();
            go.AddComponent<Health>();
            go.AddComponent<PlayerWeapons>();
            go.AddComponent<PlayerVitals>();
            var player = go.AddComponent<PlayerController>();

            var pivot = new GameObject("AimPivot").transform;
            pivot.SetParent(go.transform, false);
            GameObject barrel = PlaceholderArt.Box("Barrel", Vector2.zero, new Vector2(0.7f, 0.2f), new Color(0.1f, 0.1f, 0.1f), pivot, 21);
            barrel.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            player.SetAimPivot(pivot);
            return player;
        }

        private CameraFollow SetUpCamera(PlayerController player)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObject = new GameObject("Main Camera");
                camObject.tag = "MainCamera";
                cam = camObject.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.08f);
            cam.transform.position = new Vector3(BaseSpawn.x, BaseSpawn.y, -10f);

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = player;
            return follow;
        }

        private void BuildBase()
        {
            Transform root = new GameObject("Base").transform;
            PlaceholderArt.Box("BaseFloor", BaseCenter, BaseSize, new Color(0.3f, 0.42f, 0.28f), root, 0);

            float halfX = BaseSize.x * 0.5f;
            float halfY = BaseSize.y * 0.5f;
            Wall(BaseCenter + new Vector2(0f, halfY + 0.5f), new Vector2(BaseSize.x + 2f, 1f), root, true);
            Wall(BaseCenter + new Vector2(0f, -halfY - 0.5f), new Vector2(BaseSize.x + 2f, 1f), root, true);
            Wall(BaseCenter + new Vector2(-halfX - 0.5f, 0f), new Vector2(1f, BaseSize.y), root, true);
            Wall(BaseCenter + new Vector2(halfX + 0.5f, 0f), new Vector2(1f, BaseSize.y), root, true);

            var session = GameRoot.Instance.Session;

            // 3x3 farm on the left side.
            int index = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (index >= session.Farm.Plots.Count) break;
                    var position = new Vector2(-7f + column * 1.2f, 1.5f - row * 1.2f);
                    GameObject plot = PlaceholderArt.Box("FarmPlot" + index, position, new Vector2(1.1f, 1.1f), Color.white, root, 1);
                    plot.AddComponent<BoxCollider2D>().isTrigger = true;
                    plot.AddComponent<FarmPlotView>().Init(session.Farm.Plots[index]);
                    index++;
                }
            }

            GameObject bed = PlaceholderArt.Box("Bed", new Vector2(7f, 4.5f), new Vector2(1.2f, 2f), new Color(0.7f, 0.3f, 0.3f), root, 2, "bed", SpriteFit.Centered(1.5f, 2.3f));
            bed.AddComponent<BoxCollider2D>();
            bed.AddComponent<YSort>();
            bed.AddComponent<Bed>();

            GameObject stash = PlaceholderArt.Box("Stash", new Vector2(4.5f, 5f), new Vector2(1.6f, 1f), new Color(0.6f, 0.45f, 0.2f), root, 2, "stash", SpriteFit.Standing(1.7f, 1.5f));
            stash.AddComponent<BoxCollider2D>();
            stash.AddComponent<YSort>();
            stash.AddComponent<StashCrate>();

            GameObject gate = PlaceholderArt.Box("RaidGate", new Vector2(BaseSize.x * 0.5f - 1f, -3f), new Vector2(1f, 2.4f), new Color(0.9f, 0.6f, 0.15f), root, 2, "gate", SpriteFit.Centered(2.4f, 2.4f));
            gate.AddComponent<BoxCollider2D>();
            gate.AddComponent<YSort>();
            gate.AddComponent<RaidGate>();

            BuildNpc(PrototypeContent.NpcLan, new Vector2(-2f, 4.5f), new Color(0.95f, 0.6f, 0.75f), root);
            BuildNpc(PrototypeContent.NpcMinh, new Vector2(0.5f, 4.5f), new Color(0.5f, 0.75f, 0.95f), root);
            BuildNpc(PrototypeContent.NpcBaTu, new Vector2(-5f, 4.5f), new Color(0.8f, 0.8f, 0.6f), root);
        }

        private static void BuildNpc(string npcId, Vector2 position, Color color, Transform parent)
        {
            GameObject npc = PlaceholderArt.Box("Npc_" + npcId, position, new Vector2(0.8f, 0.8f), color, parent, 10, "npc_" + npcId, SpriteFit.Character);
            npc.AddComponent<BoxCollider2D>();
            npc.AddComponent<YSort>();
            npc.AddComponent<NpcView>().Init(npcId);
        }

        /// <summary>Destroys and rebuilds everything in the raid zone from the map. Called on start and after each sleep.</summary>
        private void BuildRaidZone()
        {
            if (_raidRoot != null) Destroy(_raidRoot.gameObject);
            _raidRoot = new GameObject("RaidZone").transform;

            BuildFloor();
            BuildWalls();

            foreach (MapMarker marker in _map.Markers)
            {
                Vector2 position = CellCenter(marker.X, marker.Y);
                switch (marker.Symbol)
                {
                    case RaidMap.Extraction:
                        GameObject extraction = PlaceholderArt.Box("Extraction", position, new Vector2(3f, 3f), new Color(0.2f, 0.8f, 0.35f, 0.45f), _raidRoot, 1, "extraction", SpriteFit.Tile);
                        extraction.AddComponent<BoxCollider2D>().isTrigger = true;
                        extraction.AddComponent<ExtractionZone>();
                        break;

                    case 'C': BuildContainer(position, PrototypeContent.HouseCabinet, "crate_house", new Color(0.75f, 0.55f, 0.25f)); break;
                    case 'M': BuildContainer(position, PrototypeContent.MedicalCabinet, "crate_medical", new Color(0.8f, 0.8f, 0.82f)); break;
                    case 'S': BuildContainer(position, PrototypeContent.SeedStore, "crate_seed", new Color(0.55f, 0.7f, 0.3f)); break;
                    case 'W': BuildContainer(position, PrototypeContent.WeaponCrate, "crate_weapon", new Color(0.35f, 0.42f, 0.3f)); break;

                    case 'Z': BuildZombie(position, ZombieKind.Walker); break;
                    case 'R': BuildZombie(position, ZombieKind.Runner); break;
                    case 'G': BuildZombie(position, ZombieKind.Screamer); break;
                }
            }

            // Zombies walk straight until the walkability map is ready, one frame from now.
            if (NavGrid.Instance != null) NavGrid.Instance.RequestBuild(RaidCenter, _raidSize);
        }

        private void BuildFloor()
        {
            if (PlaceholderArt.FindSprite("floor") == null)
            {
                PlaceholderArt.Box("RaidFloor", RaidCenter, _raidSize, RaidFloorColor, _raidRoot, 0);
                return;
            }

            // With floor art, lay one tile per open cell.
            Transform floor = new GameObject("Floor").transform;
            floor.SetParent(_raidRoot, false);
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    if (_map.IsWall(x, y)) continue;
                    PlaceholderArt.Box("FloorTile", CellCenter(x, y), Vector2.one, RaidFloorColor, floor, 0, "floor", SpriteFit.Tile);
                }
            }
        }

        /// <summary>
        /// Merges wall cells into long boxes: horizontal runs first, then what is left in
        /// vertical runs. Long smooth colliders stop bodies snagging on the seams between
        /// cells. With wall art, each cell also gets its own tile on top.
        /// </summary>
        private void BuildWalls()
        {
            bool hasArt = PlaceholderArt.FindSprite("wall") != null;
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(_raidRoot, false);

            var merged = new bool[_map.Width, _map.Height];

            for (int y = 0; y < _map.Height; y++)
            {
                int x = 0;
                while (x < _map.Width)
                {
                    if (!_map.IsWall(x, y))
                    {
                        x++;
                        continue;
                    }

                    int start = x;
                    while (x < _map.Width && _map.IsWall(x, y)) x++;
                    int length = x - start;
                    if (length < 2) continue;

                    for (int cell = start; cell < x; cell++) merged[cell, y] = true;
                    Wall(CellCenter(start + (length - 1) * 0.5f, y), new Vector2(length, 1f), walls, !hasArt);
                }
            }

            for (int x = 0; x < _map.Width; x++)
            {
                int y = 0;
                while (y < _map.Height)
                {
                    if (!_map.IsWall(x, y) || merged[x, y])
                    {
                        y++;
                        continue;
                    }

                    int start = y;
                    while (y < _map.Height && _map.IsWall(x, y) && !merged[x, y]) y++;
                    int length = y - start;
                    Wall(CellCenter(x, start + (length - 1) * 0.5f), new Vector2(1f, length), walls, !hasArt);
                }
            }

            if (!hasArt) return;
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    if (_map.IsWall(x, y)) PlaceholderArt.Box("WallTile", CellCenter(x, y), Vector2.one, WallColor, walls, 5, "wall", SpriteFit.Tile);
                }
            }
        }

        private void BuildContainer(Vector2 position, LootTable table, string spriteKey, Color color)
        {
            GameObject container = PlaceholderArt.Box("Loot_" + table.Id, position, new Vector2(0.9f, 0.8f), color, _raidRoot, 2, spriteKey, SpriteFit.Crate);
            container.AddComponent<BoxCollider2D>();
            container.AddComponent<YSort>();
            container.AddComponent<LootContainer>().Init(table);
        }

        private void BuildZombie(Vector2 position, ZombieKind kind)
        {
            string spriteKey = "zombie_" + kind.ToString().ToLowerInvariant();
            GameObject go = PlaceholderArt.Box("Zombie_" + kind, position, new Vector2(0.8f, 0.8f), new Color(0.45f, 0.65f, 0.3f), _raidRoot, 15, spriteKey, SpriteFit.Character);
            // Round bodies slide past each other and around wall corners; boxes lock together.
            go.AddComponent<CircleCollider2D>().radius = 0.5f;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearDamping = 4f;

            go.AddComponent<YSort>();
            go.AddComponent<Health>();
            go.AddComponent<ZombieAI>().Init(_player.transform, kind);
        }

        /// <summary>A solid block that stops movement, bullets and sight.</summary>
        private static void Wall(Vector2 position, Vector2 size, Transform parent, bool visible)
        {
            GameObject wall = PlaceholderArt.Box("Wall", position, size, WallColor, parent, 5);
            if (!visible) wall.GetComponent<SpriteRenderer>().enabled = false;
            wall.AddComponent<BoxCollider2D>();
            wall.AddComponent<SightBlocker>();
        }
    }
}
