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
        // Far from the base, so even the largest map never overlaps it.
        private static readonly Vector2 RaidCenter = new Vector2(400f, 0f);
        private static readonly Vector2 BaseSpawn = new Vector2(0f, -2f);

        // Weathered concrete rather than black: walls read as buildings, not holes.
        private static readonly Color WallColor = new Color(0.3f, 0.28f, 0.26f);

        // Base ground and wall art is drawn muted and, for the ground, at twice the size: a busy
        // texture repeated in every cell at full brightness is tiring to look at for long.
        // Raid maps draw their ground from the map itself (see BuildFloor).
        private const int GroundTileCells = 2;
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
        private string _loadedMap;
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
            director.AreaChosen = SelectArea;

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
            new GameObject("AreaScreen").AddComponent<AreaScreen>().Player = _player;

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

        /// <summary>Puts the chosen area's map in place. The outskirts use the map named in the Inspector.</summary>
        private void SelectArea(Bnhon.Core.Raid.RaidArea area)
        {
            string wanted = area.Id == PrototypeContent.AreaOutskirtsId ? mapName : area.MapName;
            if (wanted != _loadedMap) SwitchMap(wanted);
        }

        /// <summary>A walker placed by the map may turn up as something worse, the more days have passed. Not during the opening.</summary>
        private static ZombieKind Escalate(ZombieKind kind)
        {
            var session = GameRoot.Instance.Session;
            if (kind != ZombieKind.Walker || session.InIntro) return kind;

            int level = Bnhon.Core.Raid.Threat.Level(session.Clock.TotalDays);
            switch (Bnhon.Core.Raid.Threat.Roll(level, session.Rng))
            {
                case Bnhon.Core.Raid.ThreatUpgrade.Armored: return ZombieKind.Armored;
                case Bnhon.Core.Raid.ThreatUpgrade.Runner: return ZombieKind.Runner;
                default: return kind;
            }
        }

        /// <summary>Throws the current raid map away and builds another in its place.</summary>
        private void SwitchMap(string name)
        {
            if (_groundTexture != null) Destroy(_groundTexture);
            _groundTexture = null;
            _decor = null;
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
            _loadedMap = mapName;
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
                _decor = new World.DecorLayer(_map, CellCenter, _raidGeometry, StableHash(_loadedMap));
                _decor.FindTrees();
                BuildWalls();
                _decor.ScatterGround();
                _decor.PlaceProps();
                foreach (Vector2 smoke in _decor.Smokes) Effect<World.SmokePlume>("Smoke", smoke);
                foreach (Vector2 fire in _decor.Fires)
                {
                    Effect<World.FireGlow>("Fire", fire);
                    Effect<World.SmokePlume>("Smoke", fire).transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                }

                // Reinforcements must not appear inside a car or a pile of sandbags.
                var blocked = new HashSet<GridPoint>(_decor.BlockedCells);
                _openCells.RemoveAll(cell => blocked.Contains(cell));
            }

            if (_raidRoot != null) Destroy(_raidRoot.gameObject);
            GeneratorSwitch.ResetPower();
            _raidRoot = new GameObject("RaidZone").transform;
            _raidRoot.SetParent(_raidWorld.transform, false);
            _hordePoints.Clear();

            foreach (MapMarker marker in _map.Markers)
            {
                Vector2 position = CellCenter(marker.X, marker.Y);
                switch (marker.Symbol)
                {
                    case RaidMap.Extraction: BuildExit(position, null); break;

                    case RaidMap.PoweredExit:
                        BuildExit(position, () => GeneratorSwitch.PowerOn ? null : "Cửa cuốn đóng chặt. Cần điện: tìm cầu dao trong cửa hàng trạm xăng.");
                        break;

                    case RaidMap.BossExit:
                        BuildExit(position, () =>
                        {
                            var area = _director != null ? _director.CurrentArea : null;
                            if (area == null || area.BossDefeated(GameRoot.Instance.Session)) return null;
                            return "Lối mòn sau đồi bị chặn. " + area.BossName + " còn sống thì không ai qua được.";
                        });
                        break;

                    case RaidMap.PowerSwitch:
                        GameObject power = PlaceholderArt.Box("PowerSwitch", position, new Vector2(0.7f, 0.9f), new Color(0.9f, 0.8f, 0.15f), _raidRoot, 2, "power_switch", SpriteFit.Crate);
                        power.AddComponent<BoxCollider2D>();
                        power.AddComponent<YSort>();
                        power.AddComponent<GeneratorSwitch>();
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

                    case 'V': BuildContainer(position, PrototypeContent.Safe, "crate_safe", new Color(0.28f, 0.33f, 0.45f)); break;

                    case 'Z': BuildZombie(position, Escalate(ZombieKind.Walker)); break;
                    case 'A': BuildZombie(position, ZombieKind.Armored); break;
                    case 'B':
                        // A boss that has been killed does not come back; one of its guards stands there instead.
                        var area = _director != null ? _director.CurrentArea : null;
                        bool dead = area != null && area.BossDefeated(GameRoot.Instance.Session);
                        ZombieAI boss = BuildZombie(position, dead ? ZombieKind.Armored : ZombieKind.Boss);
                        if (!dead && area != null && area.BossHealth > 0f) boss.GetComponent<Health>().SetMax(area.BossHealth);
                        break;
                    case 'R': BuildZombie(position, ZombieKind.Runner); break;
                    case 'G': BuildZombie(position, ZombieKind.Screamer); break;
                }
            }

            // Zombies walk straight until the walkability map is ready, one frame from now.
            if (NavGrid.Instance != null) NavGrid.Instance.RequestBuild(RaidCenter, _raidSize);
        }

        private Texture2D _groundTexture;
        private World.DecorLayer _decor;

        private T Effect<T>(string name, Vector2 position) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(_raidGeometry, false);
            go.transform.position = position;
            return go.AddComponent<T>();
        }

        /// <summary>A hash of a string that is the same on every run, unlike string.GetHashCode.</summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text ?? string.Empty) hash = hash * 31 + c;
                return hash;
            }
        }

        /// <summary>
        /// Draws the ground of the whole map as one picture, a few pixels per cell, coloured by
        /// the kind of ground (concrete, grass, road, dirt, paddy) with a little grain. One object
        /// for the whole map, however big, and muted colours that are easy on the eyes.
        /// </summary>
        private void BuildFloor()
        {
            const int pixelsPerCell = 4;
            int width = _map.Width * pixelsPerCell;
            int height = _map.Height * pixelsPerCell;

            if (_groundTexture != null) Destroy(_groundTexture);
            _groundTexture = new Texture2D(width, height);
            _groundTexture.filterMode = FilterMode.Point;

            var pixels = new Color[width * height];
            for (int py = 0; py < height; py++)
            {
                // Texture rows start at the bottom; map rows start at the top.
                int cellY = _map.Height - 1 - py / pixelsPerCell;
                for (int px = 0; px < width; px++)
                {
                    int cellX = px / pixelsPerCell;
                    pixels[py * width + px] = GroundPixel(cellX, cellY, px % pixelsPerCell, pixelsPerCell - 1 - py % pixelsPerCell, px, py);
                }
            }
            _groundTexture.SetPixels(pixels);
            _groundTexture.Apply();

            var ground = new GameObject("Ground");
            ground.transform.SetParent(_raidGeometry, false);
            ground.transform.position = RaidCenter;
            var renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(_groundTexture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerCell);
            renderer.sharedMaterial = PlaceholderArt.UnlitMaterial;
            renderer.sortingOrder = 0;
        }

        /// <summary>
        /// One pixel of the ground. Besides the colour of its kind it gets broad patches of
        /// lighter and darker (so a big map is not one flat colour), and grass creeping over
        /// the edges of roads and paving.
        /// </summary>
        private Color GroundPixel(int cellX, int cellY, int localX, int localY, int px, int py)
        {
            char ground = _map.GroundAt(cellX, cellY);
            float grain = Grain(px, py);

            // Ragged edges: a paved cell next to grass loses some of its border pixels to the grass.
            if ((ground == '=' || ground == '.') && grain > 0.45f)
            {
                const int last = 3;
                bool creep = (localX == 0 && _map.GroundAt(cellX - 1, cellY) == ',')
                    || (localX == last && _map.GroundAt(cellX + 1, cellY) == ',')
                    || (localY == 0 && _map.GroundAt(cellX, cellY - 1) == ',')
                    || (localY == last && _map.GroundAt(cellX, cellY + 1) == ',');
                if (creep) ground = ',';
            }

            Color patchy = GroundColor(ground, grain, py);
            float patch = (ValueNoise(px / 28f, py / 28f) - 0.5f) * 0.08f + (ValueNoise(px / 7f + 31f, py / 7f) - 0.5f) * 0.03f;
            return new Color(patchy.r + patch, patchy.g + patch * 1.1f, patchy.b + patch * 0.8f, 1f);
        }

        /// <summary>Smooth noise in 0..1 from the grain hash at whole-number points.</summary>
        private static float ValueNoise(float x, float y)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Grain(x0 * 7919, y0 * 104729), b = Grain((x0 + 1) * 7919, y0 * 104729);
            float c = Grain(x0 * 7919, (y0 + 1) * 104729), d = Grain((x0 + 1) * 7919, (y0 + 1) * 104729);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static Color GroundColor(char ground, float grain, int py)
        {
            Color color;
            switch (ground)
            {
                case ',':
                    color = new Color(0.24f, 0.31f, 0.19f);
                    if (grain > 0.82f) color = new Color(0.29f, 0.37f, 0.22f);
                    break;
                case '=':
                    color = new Color(0.19f, 0.19f, 0.2f);
                    break;
                case ':':
                    color = new Color(0.35f, 0.28f, 0.2f);
                    break;
                case '~':
                    // Rows of rice standing in water.
                    color = py % 3 == 0 ? new Color(0.25f, 0.34f, 0.23f) : new Color(0.21f, 0.29f, 0.28f);
                    break;
                case '#':
                    color = new Color(0.15f, 0.14f, 0.13f);
                    break;
                default:
                    color = new Color(0.31f, 0.3f, 0.28f);
                    break;
            }

            float shade = (grain - 0.5f) * 0.05f;
            return new Color(color.r + shade, color.g + shade, color.b + shade, 1f);
        }

        /// <summary>A fixed pseudo-random value in 0..1 for a pixel, so the grain is the same every raid.</summary>
        private static float Grain(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
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
                    Wall(CellCenter(start + (length - 1) * 0.5f, y), new Vector2(length, 1f), walls, !hasArt && !IsTree(start, y));
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
                    Wall(CellCenter(x, start + (length - 1) * 0.5f), new Vector2(1f, length), walls, !hasArt && !IsTree(x, start));
                }
            }

            if (!hasArt) return;
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    if (_map.IsWall(x, y) && !IsTree(x, y)) PlaceholderArt.Tint(PlaceholderArt.Box("WallTile", CellCenter(x, y), Vector2.one, WallColor, walls, 5, "wall", SpriteFit.Tile), WallTint);
                }
            }
        }

        /// <summary>True if this wall cell is drawn as a tree, so its own wall picture is hidden.</summary>
        private bool IsTree(int x, int y)
        {
            return _decor != null && _decor.IsTreeCell(x, y);
        }

        private void BuildExit(Vector2 position, Func<string> lockReason)
        {
            GameObject extraction = PlaceholderArt.Box("Extraction", position, new Vector2(3f, 3f), new Color(0.2f, 0.8f, 0.35f, 0.45f), _raidRoot, 1, "extraction", SpriteFit.Tile);
            extraction.AddComponent<BoxCollider2D>().isTrigger = true;
            extraction.AddComponent<ExtractionZone>().Init(lockReason);
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
                ZombieKind kind = Escalate(roll < 0.6f ? ZombieKind.Walker : (roll < 0.9f ? ZombieKind.Runner : ZombieKind.Screamer));
                BuildZombie(position, kind).Investigate(playerPosition, 45f);
                placed++;
            }
            return placed;
        }

        private ZombieAI BuildZombie(Vector2 position, ZombieKind kind)
        {
            string spriteKey = "zombie_" + kind.ToString().ToLowerInvariant();
            // The boss keeps a normal footprint, so it fits through doors, but is drawn much bigger.
            SpriteFit fit = kind == ZombieKind.Boss ? SpriteFit.Standing(1.8f, 2.5f) : SpriteFit.Character;
            GameObject go = PlaceholderArt.Box("Zombie_" + kind, position, new Vector2(0.8f, 0.8f), new Color(0.45f, 0.65f, 0.3f), _raidRoot, 15, spriteKey, fit);
            if (kind == ZombieKind.Boss && PlaceholderArt.FindSprite(spriteKey) == null)
            {
                // Without art the boss is a square like the rest; a dark halo marks it out.
                PlaceholderArt.Box("Halo", position, new Vector2(1.7f, 1.7f), new Color(0.35f, 0.05f, 0.05f, 0.55f), _raidRoot, 14).transform.SetParent(go.transform, true);
            }
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
