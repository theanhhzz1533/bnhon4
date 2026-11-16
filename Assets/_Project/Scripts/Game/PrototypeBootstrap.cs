using System;
using System.Collections.Generic;
using Bnhon.Core;
using Bnhon.Core.Pathfinding;
using Bnhon.Core.Content;
using Bnhon.Core.Layout;
using Bnhon.Core.Loot;
using Bnhon.Core.Maps;
using Bnhon.Game.AI;
using Bnhon.Game.Audio;
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

        // Ground and wall art is drawn muted and, for the ground, at twice the size: a busy
        // texture repeated in every cell at full brightness is tiring to look at for long.
        private const int GroundTileCells = 2;
        private static readonly Color RaidFloorTint = new Color(0.5f, 0.51f, 0.55f);
        private static readonly Color WallTint = new Color(0.72f, 0.72f, 0.76f);
        private static readonly Color BaseFloorTint = new Color(0.78f, 0.8f, 0.76f);

        private const string HospitalMap = "hospital";

        [SerializeField] private string mapName = "outskirts";
        [SerializeField] private float cameraSize = 7f;

        [Tooltip("Thêm Pixel Perfect Camera của URP vào camera. Chỉ nên bật khi mọi sprite cùng mật độ điểm ảnh.")]
        [SerializeField] private bool pixelPerfect;
        [SerializeField] private int pixelsPerUnit = 16;

        private RaidMap _map;
        private readonly List<GridPoint> _openCells = new List<GridPoint>();
        private Vector2 _raidSize;
        private Transform _raidRoot;
        private Transform _raidGeometry;
        private PlayerController _player;
        private RaidDirector _director;
        private Transform _baseRoot;
        private GameObject _raidWorld;
        private BaseBuilder _builder;
        private readonly List<Vector2> _hordePoints = new List<Vector2>();

        private void Start()
        {
            GameRoot.Ensure();
            // A new game opens in the hospital; the normal raid map replaces it once the player is rescued.
            LoadMap(GameRoot.Instance.Session.InIntro ? HospitalMap : mapName);

            _builder = new GameObject("BaseBuilder").AddComponent<BaseBuilder>();
            BuildBase();
            // The raid map is only built, and only switched on, while the player is out there.
            _raidWorld = new GameObject("RaidWorld");
            _raidWorld.SetActive(false);

            _player = BuildPlayer();
            _builder.Init(_player);
            CameraFollow cameraFollow = SetUpCamera(_player);
            Sfx.Ensure().Listener = _player.transform;

            new GameObject("NavGrid").AddComponent<NavGrid>();

            var director = new GameObject("RaidDirector").AddComponent<RaidDirector>();
            _director = director;
            director.Init(_player, cameraFollow, BaseSpawn, CellCenter(_map.Spawn.X, _map.Spawn.Y), SetZone);

            new GameObject("RaidPressure").AddComponent<RaidPressure>().Init(SpawnReinforcements);

            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            hud.Player = _player;

            var vision = new GameObject("VisionSystem").AddComponent<VisionSystem>();
            vision.Init(_player);

            var inventory = new GameObject("InventoryScreen").AddComponent<InventoryScreen>();
            inventory.Player = _player;

            new GameObject("WorkbenchScreen").AddComponent<WorkbenchScreen>().Player = _player;
            new GameObject("ShopScreen").AddComponent<ShopScreen>().Player = _player;
            new GameObject("KitchenScreen").AddComponent<KitchenScreen>().Player = _player;
            new GameObject("NpcScreen").AddComponent<NpcScreen>().Player = _player;
            new GameObject("SceneScreen").AddComponent<SceneScreen>().Player = _player;

            new GameObject("ScreenFade").AddComponent<ScreenFade>();
            new GameObject("Atmosphere").AddComponent<Atmosphere>();
            if (pixelPerfect) AddPixelPerfectCamera(cameraFollow.gameObject);
            SplitIntoScenes();

            new GameObject("StoryDirector").AddComponent<StoryDirector>().Init(_player, ReleaseHorde, LeaveHospital, RestartHospital);
        }

        /// <summary>
        /// Shows the base or the raid map, never both. Entering the raid map builds it fresh:
        /// new loot and zombies every time. The cut is hidden by a fade from black.
        /// </summary>
        private void SetZone(bool raid)
        {
            _baseRoot.gameObject.SetActive(!raid);
            _raidWorld.SetActive(raid);
            if (raid) BuildRaidZone();
            ScreenFade.Flash();
        }

        /// <summary>
        /// Puts the base and the raid map in two scenes of their own, made at runtime, so
        /// the Hierarchy shows them apart and each can later become a scene file.
        /// </summary>
        private void SplitIntoScenes()
        {
            try
            {
                var baseScene = UnityEngine.SceneManagement.SceneManager.CreateScene("Base");
                var raidScene = UnityEngine.SceneManagement.SceneManager.CreateScene("Raid");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_baseRoot.gameObject, baseScene);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_raidWorld, raidScene);
            }
            catch (Exception error)
            {
                // The game plays the same in one scene; only the Hierarchy grouping is lost.
                Debug.LogWarning("Không tách được scene căn cứ và raid: " + error.Message);
            }
        }

        /// <summary>
        /// Adds URP's Pixel Perfect Camera without referencing the URP assembly, so the project
        /// still compiles if that package changes. Does nothing if the component is not found.
        /// </summary>
        private void AddPixelPerfectCamera(GameObject cameraObject)
        {
            Type type = Type.GetType("UnityEngine.Rendering.Universal.PixelPerfectCamera, Unity.RenderPipelines.Universal.2D.Runtime")
                ?? Type.GetType("UnityEngine.Rendering.Universal.PixelPerfectCamera, Unity.RenderPipelines.Universal.Runtime")
                ?? Type.GetType("UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera, Unity.RenderPipelines.Universal.Runtime");
            if (type == null)
            {
                Debug.LogWarning("Không tìm thấy Pixel Perfect Camera của URP; bỏ qua.");
                return;
            }

            Component component = cameraObject.GetComponent(type) ?? cameraObject.AddComponent(type);
            SetProperty(type, component, "assetsPPU", pixelsPerUnit);
            SetProperty(type, component, "refResolutionX", 480);
            SetProperty(type, component, "refResolutionY", 270);
        }

        private static void SetProperty(Type type, object target, string name, object value)
        {
            var property = type.GetProperty(name);
            if (property != null && property.CanWrite) property.SetValue(target, value);
        }

        /// <summary>Throws the current raid map away and builds another in its place.</summary>
        private void SwitchMap(string name)
        {
            LoadMap(name);
            if (_raidGeometry != null) Destroy(_raidGeometry.gameObject);
            _raidGeometry = null;
            _director.SetRaidSpawn(CellCenter(_map.Spawn.X, _map.Spawn.Y));
        }

        private void LeaveHospital()
        {
            SwitchMap(mapName);
            _director.PutPlayerAtBase();
        }

        private void RestartHospital()
        {
            // Entering the raid zone rebuilds it, so the zombies and the cabinet are back.
            _director.PutPlayerAtRaidSpawn();
        }

        /// <summary>Zombies burst out of every horde point and go for the player. Every fourth one is a runner.</summary>
        private int ReleaseHorde()
        {
            Vector2 playerPosition = _player.transform.position;
            for (int i = 0; i < _hordePoints.Count; i++)
            {
                ZombieKind kind = i % 4 == 3 ? ZombieKind.Runner : ZombieKind.Walker;
                BuildZombie(_hordePoints[i], kind).Investigate(playerPosition, 90f);
            }
            return _hordePoints.Count;
        }

        private void LoadMap(string mapName)
        {
            _map = null;
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

            // Floor cells with nothing placed on them: where reinforcements may appear.
            var occupied = new HashSet<int>();
            foreach (MapMarker marker in _map.Markers) occupied.Add(marker.Y * _map.Width + marker.X);
            _openCells.Clear();
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    if (!_map.IsWall(x, y) && !occupied.Contains(y * _map.Width + x)) _openCells.Add(new GridPoint(x, y));
                }
            }

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

            // Unity needs exactly one listener in the scene for any audio to be heard.
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = player;
            return follow;
        }

        private void BuildBase()
        {
            Transform root = new GameObject("Base").transform;
            _baseRoot = root;

            if (PlaceholderArt.FindSprite("base_floor") == null)
            {
                PlaceholderArt.Box("BaseFloor", BaseCenter, BaseSize, new Color(0.3f, 0.42f, 0.28f), root, 0);
            }
            else
            {
                Transform floor = new GameObject("BaseFloor").transform;
                floor.SetParent(root, false);
                for (int x = 0; x < GameSession.BaseWidth; x += GroundTileCells)
                {
                    for (int y = 0; y < GameSession.BaseHeight; y += GroundTileCells)
                    {
                        Vector2 corner = BaseBuilder.CellCorner(x, y);
                        var size = new Vector2(GroundTileCells, GroundTileCells);
                        PlaceholderArt.Tint(PlaceholderArt.Box("FloorTile", corner + size * 0.5f, size, Color.white, floor, 0, "base_floor", SpriteFit.Tile), BaseFloorTint);
                    }
                }
            }

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
                    GameObject plot = PlaceholderArt.Box("FarmPlot" + index, position, new Vector2(1.1f, 1.1f), Color.white, root, 1, "plot_dry", SpriteFit.Tile);
                    plot.AddComponent<BoxCollider2D>().isTrigger = true;
                    plot.AddComponent<FarmPlotView>().Init(session.Farm.Plots[index]);
                    index++;
                }
            }

            GameObject gate = PlaceholderArt.Box("RaidGate", new Vector2(BaseSize.x * 0.5f - 1f, -3f), new Vector2(1f, 2.4f), new Color(0.9f, 0.6f, 0.15f), root, 2, "gate", SpriteFit.Centered(2.4f, 2.4f));
            gate.AddComponent<BoxCollider2D>();
            gate.AddComponent<YSort>();
            gate.AddComponent<RaidGate>();

            // These stand where the saved layout says, and can be moved in build mode (B).
            Building(PrototypeContent.BuildingBed, new Color(0.7f, 0.3f, 0.3f), "bed", SpriteFit.Centered(1.5f, 2.3f), root).AddComponent<Bed>();
            Building(PrototypeContent.BuildingStash, new Color(0.6f, 0.45f, 0.2f), "stash", SpriteFit.Standing(1.7f, 1.5f), root).AddComponent<StashCrate>();
            Building(PrototypeContent.BuildingWorkbench, new Color(0.5f, 0.42f, 0.33f), "workbench", SpriteFit.Standing(1.7f, 1.5f), root).AddComponent<WorkbenchStation>();
            Building(PrototypeContent.BuildingWell, new Color(0.35f, 0.6f, 0.8f), "well", SpriteFit.Standing(1.4f, 1.5f), root).AddComponent<Well>();
            Building(PrototypeContent.BuildingKitchen, new Color(0.75f, 0.4f, 0.25f), "kitchen", SpriteFit.Standing(1.7f, 1.5f), root).AddComponent<KitchenStation>();
            Building(PrototypeContent.BuildingShop, new Color(0.3f, 0.5f, 0.62f), "shop", SpriteFit.Standing(1.8f, 1.7f), root).AddComponent<ShopStall>();

            BuildNpc(PrototypeContent.NpcLan, new Color(0.95f, 0.6f, 0.75f), root);
            BuildNpc(PrototypeContent.NpcMinh, new Color(0.5f, 0.75f, 0.95f), root);
            BuildNpc(PrototypeContent.NpcBaTu, new Color(0.8f, 0.8f, 0.6f), root);
        }

        /// <summary>One movable building of the base, placed by the layout. Slightly smaller than its cells, so there is room to walk between neighbours.</summary>
        private GameObject Building(BuildingType type, Color color, string spriteKey, SpriteFit fit, Transform root)
        {
            PlacedBuilding placed = GameRoot.Instance.Session.Base.Get(type.Id);
            var size = new Vector2(type.Width - 0.2f, type.Height - 0.15f);
            GameObject building = PlaceholderArt.Box(type.DisplayName, BaseBuilder.Center(type, placed.X, placed.Y), size, color, root, 2, spriteKey, fit);
            building.AddComponent<BoxCollider2D>();
            building.AddComponent<YSort>();
            _builder.Register(type.Id, building);
            return building;
        }

        /// <summary>The free cell beside a building, for whoever is scheduled to stand there.</summary>
        private static Vector2 Beside(BuildingType type)
        {
            GameRoot.Instance.Session.Base.StandCell(type.Id, out int x, out int y);
            return BaseBuilder.CellCenter(x, y);
        }

        /// <summary>Where each named spot of the base is. Residents stand at these by their schedules.</summary>
        private static Vector2 SpotPosition(string spot)
        {
            switch (spot)
            {
                case PrototypeContent.SpotClinic: return new Vector2(-2f, 4.5f);
                case PrototypeContent.SpotWorkshop: return new Vector2(0.5f, 4.5f);
                case PrototypeContent.SpotPorch: return new Vector2(-5f, 4.5f);
                case PrototypeContent.SpotKitchen: return Beside(PrototypeContent.BuildingKitchen);
                case PrototypeContent.SpotWell: return Beside(PrototypeContent.BuildingWell);
                case PrototypeContent.SpotGate: return new Vector2(7.4f, -5.2f);
                case PrototypeContent.SpotFarm: return new Vector2(-8.8f, 0.3f);
                default: return new Vector2(0f, 3f);
            }
        }

        private static void BuildNpc(string npcId, Color color, Transform parent)
        {
            GameObject npc = PlaceholderArt.Box("Npc_" + npcId, SpotPosition(PrototypeContent.SpotClinic), new Vector2(0.8f, 0.8f), color, parent, 10, "npc_" + npcId, SpriteFit.Character);
            npc.AddComponent<BoxCollider2D>();
            npc.AddComponent<YSort>();
            npc.AddComponent<NpcView>().Init(npcId, SpotPosition);
        }

        /// <summary>Destroys and rebuilds everything in the raid zone from the map. Called each time the player enters it.</summary>
        private void BuildRaidZone()
        {
            // Floor and walls never change, so they are built once. Only loot, zombies,
            // corpses and extraction points are thrown away and rebuilt each day.
            if (_raidGeometry == null)
            {
                _raidGeometry = new GameObject("RaidGeometry").transform;
                _raidGeometry.SetParent(_raidWorld.transform, false);
                BuildFloor();
                BuildWalls();
            }

            if (_raidRoot != null) Destroy(_raidRoot.gameObject);
            _raidRoot = new GameObject("RaidZone").transform;
            _raidRoot.SetParent(_raidWorld.transform, false);
            _hordePoints.Clear();

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

                    case 'K':
                        GameObject cabinet = PlaceholderArt.Box("Loot_story", position, new Vector2(0.9f, 0.8f), new Color(0.8f, 0.8f, 0.82f), _raidRoot, 2, "crate_medical", SpriteFit.Crate);
                        cabinet.AddComponent<BoxCollider2D>();
                        cabinet.AddComponent<YSort>();
                        cabinet.AddComponent<LootContainer>().InitFixed(PrototypeContent.IntroCabinet, "Tủ cạnh giường");
                        break;

                    case 'T':
                        GameObject trap = new GameObject("HordeTrigger");
                        trap.transform.SetParent(_raidRoot, false);
                        trap.transform.position = position;
                        var trapArea = trap.AddComponent<BoxCollider2D>();
                        trapArea.isTrigger = true;
                        trapArea.size = new Vector2(5f, 4f);
                        trap.AddComponent<StoryTrigger>().Init(() => { if (StoryDirector.Instance != null) StoryDirector.Instance.OnReachedCanteen(); });
                        break;

                    case 'H': _hordePoints.Add(position); break;

                    case RaidMap.Rescuer:
                        GameObject rescuer = PlaceholderArt.Box("Rescuer", position, new Vector2(0.8f, 0.8f), new Color(0.5f, 0.75f, 0.95f), _raidRoot, 10, "npc_minh", SpriteFit.Character);
                        rescuer.AddComponent<YSort>();
                        var reach = rescuer.AddComponent<BoxCollider2D>();
                        reach.isTrigger = true;
                        reach.size = new Vector2(4f, 4f);
                        rescuer.AddComponent<StoryTrigger>().Init(() => { if (StoryDirector.Instance != null) StoryDirector.Instance.OnRescued(); });
                        break;

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
                PlaceholderArt.Box("RaidFloor", RaidCenter, _raidSize, RaidFloorColor, _raidGeometry, 0);
                return;
            }

            // With floor art, lay large tiles under the whole map. Walls are drawn over them,
            // so there is no need to skip wall cells, and far fewer objects are needed.
            Transform floor = new GameObject("Floor").transform;
            floor.SetParent(_raidGeometry, false);
            for (int y = 0; y < _map.Height; y += GroundTileCells)
            {
                for (int x = 0; x < _map.Width; x += GroundTileCells)
                {
                    float half = (GroundTileCells - 1) * 0.5f;
                    var size = new Vector2(GroundTileCells, GroundTileCells);
                    PlaceholderArt.Tint(PlaceholderArt.Box("FloorTile", CellCenter(x + half, y + half), size, RaidFloorColor, floor, 0, "floor", SpriteFit.Tile), RaidFloorTint);
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
            walls.SetParent(_raidGeometry, false);

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
                    if (_map.IsWall(x, y)) PlaceholderArt.Tint(PlaceholderArt.Box("WallTile", CellCenter(x, y), Vector2.one, WallColor, walls, 5, "wall", SpriteFit.Tile), WallTint);
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

        /// <summary>
        /// Adds zombies on open floor far from the player and outside their view, and sends
        /// them toward where the player is now. Returns how many were placed.
        /// </summary>
        private int SpawnReinforcements(int count)
        {
            if (_raidRoot == null || _openCells.Count == 0) return 0;

            Vector2 playerPosition = _player.transform.position;
            VisionSystem vision = VisionSystem.Instance;
            int placed = 0;

            for (int attempt = 0; attempt < count * 25 && placed < count; attempt++)
            {
                GridPoint cell = _openCells[UnityEngine.Random.Range(0, _openCells.Count)];
                Vector2 position = CellCenter(cell.X, cell.Y);
                if ((position - playerPosition).sqrMagnitude < 15f * 15f) continue;
                if (vision != null && vision.IsActive && vision.IsVisible(position)) continue;

                float roll = UnityEngine.Random.value;
                ZombieKind kind = roll < 0.6f ? ZombieKind.Walker : (roll < 0.9f ? ZombieKind.Runner : ZombieKind.Screamer);
                BuildZombie(position, kind).Investigate(playerPosition, 45f);
                placed++;
            }
            return placed;
        }

        private ZombieAI BuildZombie(Vector2 position, ZombieKind kind)
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
            var health = go.AddComponent<Health>();
            Hurtbox.Attach(go, health);
            var zombie = go.AddComponent<ZombieAI>();
            zombie.Init(_player.transform, kind);
            return zombie;
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
