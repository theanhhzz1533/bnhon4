using Bnhon.Core.Content;
using Bnhon.Core.Loot;
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
    /// Builds a complete graybox test level at runtime: a base with a farm,
    /// residents, a stash and a bed, plus a raid zone with loot, zombies and
    /// an extraction point. Add this to an empty GameObject in an empty
    /// scene and press Play. No prefabs or art assets are needed.
    /// </summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private static readonly Vector2 BaseCenter = Vector2.zero;
        private static readonly Vector2 BaseSize = new Vector2(20f, 14f);
        private static readonly Vector2 RaidCenter = new Vector2(80f, 0f);
        private static readonly Vector2 RaidSize = new Vector2(44f, 30f);
        private static readonly Vector2 BaseSpawn = new Vector2(0f, -2f);

        private static readonly Color WallColor = new Color(0.18f, 0.18f, 0.2f);

        [SerializeField] private int zombieCount = 12;
        [SerializeField] private int containerCount = 8;
        [SerializeField] private float cameraSize = 7f;

        private Transform _raidRoot;
        private PlayerController _player;
        private Vector2 _raidSpawn;

        private void Start()
        {
            GameRoot.Ensure();
            _raidSpawn = RaidCenter + new Vector2(-RaidSize.x * 0.5f + 3f, 0f);

            BuildBase();
            _player = BuildPlayer();
            CameraFollow cameraFollow = SetUpCamera(_player);

            new GameObject("NavGrid").AddComponent<NavGrid>();

            var director = new GameObject("RaidDirector").AddComponent<RaidDirector>();
            director.Init(_player, cameraFollow, BaseSpawn, _raidSpawn, BuildRaidZone);
            BuildRaidZone();

            var hud = new GameObject("DebugHud").AddComponent<DebugHud>();
            hud.Player = _player;

            var vision = new GameObject("VisionSystem").AddComponent<VisionSystem>();
            vision.Init(_player);

            var inventory = new GameObject("InventoryScreen").AddComponent<InventoryScreen>();
            inventory.Player = _player;
        }

        private PlayerController BuildPlayer()
        {
            GameObject go = PlaceholderArt.Box("Player", BaseSpawn, new Vector2(0.8f, 0.8f), new Color(0.25f, 0.55f, 0.95f), null, 20);
            go.AddComponent<BoxCollider2D>();

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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
            BuildPerimeter(BaseCenter, BaseSize, root);

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

            GameObject bed = PlaceholderArt.Box("Bed", new Vector2(7f, 4.5f), new Vector2(1.2f, 2f), new Color(0.7f, 0.3f, 0.3f), root, 2);
            bed.AddComponent<BoxCollider2D>();
            bed.AddComponent<Bed>();

            GameObject stash = PlaceholderArt.Box("Stash", new Vector2(4.5f, 5f), new Vector2(1.6f, 1f), new Color(0.6f, 0.45f, 0.2f), root, 2);
            stash.AddComponent<BoxCollider2D>();
            stash.AddComponent<StashCrate>();

            GameObject gate = PlaceholderArt.Box("RaidGate", new Vector2(BaseSize.x * 0.5f - 1f, -3f), new Vector2(1f, 2.4f), new Color(0.9f, 0.6f, 0.15f), root, 2);
            gate.AddComponent<BoxCollider2D>();
            gate.AddComponent<RaidGate>();

            BuildNpc(PrototypeContent.NpcLan, new Vector2(-2f, 4.5f), new Color(0.95f, 0.6f, 0.75f), root);
            BuildNpc(PrototypeContent.NpcMinh, new Vector2(0.5f, 4.5f), new Color(0.5f, 0.75f, 0.95f), root);
            BuildNpc(PrototypeContent.NpcBaTu, new Vector2(-5f, 4.5f), new Color(0.8f, 0.8f, 0.6f), root);
        }

        private static void BuildNpc(string npcId, Vector2 position, Color color, Transform parent)
        {
            GameObject npc = PlaceholderArt.Box("Npc_" + npcId, position, new Vector2(0.8f, 0.8f), color, parent, 10);
            npc.AddComponent<BoxCollider2D>();
            npc.AddComponent<NpcView>().Init(npcId);
        }

        /// <summary>Destroys and rebuilds everything in the raid zone. Called on start and after each sleep.</summary>
        private void BuildRaidZone()
        {
            if (_raidRoot != null) Destroy(_raidRoot.gameObject);
            _raidRoot = new GameObject("RaidZone").transform;

            PlaceholderArt.Box("RaidFloor", RaidCenter, RaidSize, new Color(0.25f, 0.26f, 0.28f), _raidRoot, 0);
            BuildPerimeter(RaidCenter, RaidSize, _raidRoot);

            // A few interior walls to break line of sight.
            Wall(RaidCenter + new Vector2(-8f, 6f), new Vector2(1f, 10f), _raidRoot);
            Wall(RaidCenter + new Vector2(-8f, -9f), new Vector2(1f, 8f), _raidRoot);
            Wall(RaidCenter + new Vector2(4f, 3f), new Vector2(12f, 1f), _raidRoot);
            Wall(RaidCenter + new Vector2(4f, -6f), new Vector2(1f, 9f), _raidRoot);
            Wall(RaidCenter + new Vector2(13f, -4f), new Vector2(8f, 1f), _raidRoot);

            var extractionPosition = RaidCenter + new Vector2(RaidSize.x * 0.5f - 3f, RaidSize.y * 0.5f - 3f);
            GameObject extraction = PlaceholderArt.Box("Extraction", extractionPosition, new Vector2(4f, 4f), new Color(0.2f, 0.8f, 0.35f, 0.45f), _raidRoot, 1);
            extraction.AddComponent<BoxCollider2D>().isTrigger = true;
            extraction.AddComponent<ExtractionZone>();

            var tables = PrototypeContent.RaidLootTables;
            for (int i = 0; i < containerCount; i++)
            {
                LootTable table = tables[i % tables.Count];
                Vector2 position = RandomRaidPoint(8f);
                GameObject container = PlaceholderArt.Box("Loot_" + table.Id, position, new Vector2(1f, 0.8f), new Color(0.75f, 0.55f, 0.25f), _raidRoot, 2);
                container.AddComponent<BoxCollider2D>();
                container.AddComponent<LootContainer>().Init(table);
            }

            for (int i = 0; i < zombieCount; i++)
            {
                // Mix per 6: one screamer, two runners, three walkers.
                ZombieKind kind = i % 6 == 0 ? ZombieKind.Screamer : (i % 3 == 1 ? ZombieKind.Runner : ZombieKind.Walker);
                BuildZombie(RandomRaidPoint(12f), kind);
            }

            // Zombies walk straight until the map is ready, one frame from now.
            if (NavGrid.Instance != null) NavGrid.Instance.RequestBuild(RaidCenter, RaidSize);
        }

        private void BuildZombie(Vector2 position, ZombieKind kind)
        {
            GameObject go = PlaceholderArt.Box("Zombie_" + kind, position, new Vector2(0.8f, 0.8f), new Color(0.45f, 0.65f, 0.3f), _raidRoot, 15);
            go.AddComponent<BoxCollider2D>();

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearDamping = 4f;

            go.AddComponent<Health>();
            go.AddComponent<ZombieAI>().Init(_player.transform, kind);
        }

        /// <summary>A random point inside the raid zone, at least <paramref name="minDistanceFromSpawn"/> away from where the player enters.</summary>
        private Vector2 RandomRaidPoint(float minDistanceFromSpawn)
        {
            Vector2 half = RaidSize * 0.5f - new Vector2(2f, 2f);
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var point = RaidCenter + new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
                if ((point - _raidSpawn).sqrMagnitude < minDistanceFromSpawn * minDistanceFromSpawn) continue;
                if (Physics2D.OverlapBox(point, new Vector2(1.4f, 1.4f), 0f) != null) continue;
                return point;
            }
            return RaidCenter;
        }

        private static void BuildPerimeter(Vector2 center, Vector2 size, Transform parent)
        {
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            Wall(center + new Vector2(0f, halfY + 0.5f), new Vector2(size.x + 2f, 1f), parent);
            Wall(center + new Vector2(0f, -halfY - 0.5f), new Vector2(size.x + 2f, 1f), parent);
            Wall(center + new Vector2(-halfX - 0.5f, 0f), new Vector2(1f, size.y), parent);
            Wall(center + new Vector2(halfX + 0.5f, 0f), new Vector2(1f, size.y), parent);
        }

        private static void Wall(Vector2 position, Vector2 size, Transform parent)
        {
            GameObject wall = PlaceholderArt.Box("Wall", position, size, WallColor, parent, 5);
            wall.AddComponent<BoxCollider2D>();
            wall.AddComponent<SightBlocker>();
        }
    }
}
