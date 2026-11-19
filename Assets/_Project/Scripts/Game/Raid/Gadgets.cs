using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Game.AI;
using Bnhon.Game.Audio;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using Bnhon.Game.World;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>Using Vy's gadgets from the inventory: the decoy radio and the flare.</summary>
    public static class Gadgets
    {
        /// <summary>Uses a gadget if this item is one. Returns false for anything else, so the caller can handle it.</summary>
        public static bool TryUse(GridInventory grid, PlacedItem item, PlayerController player)
        {
            if (item == null || item.Item.Category != Bnhon.Core.Items.ItemCategory.Gadget) return false;

            GameSession session = GameRoot.Instance.Session;
            if (!session.IsInRaid)
            {
                GameRoot.Instance.Notify(item.Item.DisplayName + " chỉ dùng được khi ở ngoài.", 2f);
                return true;
            }

            if (item.Item == PrototypeContent.RadioDecoy)
            {
                if (!grid.TakeFromStack(item, 1)) return true;
                Vector2 from = player.transform.position;
                Vector2 direction = player.AimDirection;
                // Thrown up to 6 units ahead, stopping short of a wall.
                float reach = Mathf.Max(0.5f, SightBlocker.Distance(from, direction, 6f) - 0.5f);
                Spawn<RadioDecoy>("RadioDecoy", from + direction * reach);
                GameRoot.Instance.Notify("Radio mồi rè rè phát nhạc. Zombie quanh đó sẽ kéo tới chỗ nó.", 4f);
                return true;
            }

            if (item.Item == PrototypeContent.Flare)
            {
                if (FlareBeacon.Active != null)
                {
                    GameRoot.Instance.Notify("Pháo hiệu đã cháy rồi. Giữ chỗ chờ Vy.", 2f);
                    return true;
                }
                if (!grid.TakeFromStack(item, 1)) return true;
                Spawn<FlareBeacon>("Flare", player.transform.position);
                return true;
            }

            return false;
        }

        private static T Spawn<T>(string name, Vector2 position) where T : Component
        {
            var go = new GameObject(name);
            go.transform.position = position;
            return go.AddComponent<T>();
        }
    }

    /// <summary>A cheap radio blaring music. Every zombie that hears it comes to look, for half a minute.</summary>
    public sealed class RadioDecoy : MonoBehaviour
    {
        private const float Lifetime = 25f;
        private const float Interval = 1.5f;
        private const float Radius = 16f;

        private float _born;
        private float _next;
        private SpriteRenderer _blink;

        private void Start()
        {
            _born = UnityEngine.Time.time;
            PlaceholderArt.Box("Radio", transform.position, new Vector2(0.5f, 0.4f), new Color(0.3f, 0.3f, 0.32f), transform, 3, "item_radio_decoy", SpriteFit.Centered(0.7f, 0.7f));
            _blink = SoftDot.Make("Blink", transform, YSort.AboveEverything + 2);
            _blink.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

        private void Update()
        {
            float now = UnityEngine.Time.time;
            if (now - _born > Lifetime || GameRoot.Instance == null || !GameRoot.Instance.Session.IsInRaid)
            {
                Destroy(gameObject);
                return;
            }

            _blink.color = new Color(1f, 0.3f, 0.2f, Mathf.Repeat(now, 1f) < 0.5f ? 0.8f : 0.1f);
            if (now < _next) return;
            _next = now + Interval;

            Vector2 position = transform.position;
            Noise.Emit(position, Radius);
            Sfx.Play(SoundId.ItemReveal, position, 0.7f, 20f);

            // A radio heard right next to a zombie is not ignored even if it is chasing someone else.
            foreach (Collider2D other in Physics2D.OverlapCircleAll(position, 6f))
            {
                var zombie = other != null ? other.GetComponent<ZombieAI>() : null;
                if (zombie != null) zombie.Investigate(position, 6f);
            }
        }
    }

    /// <summary>
    /// A flare: red smoke and light that every zombie around can see and hear. Hold out for
    /// 40 seconds and Vy rides in: a way out opens right there for 30 seconds.
    /// </summary>
    public sealed class FlareBeacon : MonoBehaviour
    {
        private const float WaitSeconds = 40f;
        private const float PickupSeconds = 30f;
        private const float NoiseRadius = 20f;

        private float _lit;
        private float _nextNoise;
        private GameObject _exit;
        private SpriteRenderer _glow;

        public static FlareBeacon Active { get; private set; }

        /// <summary>Seconds until Vy arrives, or negative once she is here.</summary>
        public float Remaining => WaitSeconds - (UnityEngine.Time.time - _lit);

        private void Start()
        {
            Active = this;
            _lit = UnityEngine.Time.time;
            _glow = SoftDot.Make("FlareGlow", transform, YSort.AboveEverything + 1);
            _glow.transform.localScale = new Vector3(5f, 5f, 1f);
            var smoke = new GameObject("FlareSmoke");
            smoke.transform.SetParent(transform, false);
            smoke.AddComponent<SmokePlume>();
            Sfx.PlayUi(SoundId.ZombieScream, 0.4f);
            GameRoot.Instance.Notify("Pháo hiệu đỏ rực. Vy sẽ tới sau 40 giây, giữ chỗ này! Mọi zombie quanh đây đều thấy.", 6f);
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void Update()
        {
            if (GameRoot.Instance == null || !GameRoot.Instance.Session.IsInRaid)
            {
                Destroy(gameObject);
                return;
            }

            float now = UnityEngine.Time.time;
            _glow.color = new Color(1f, 0.15f, 0.1f, 0.25f + Mathf.PerlinNoise(now * 4f, 0f) * 0.2f);

            if (_exit == null && now >= _nextNoise && Remaining > 0f)
            {
                _nextNoise = now + 4f;
                Noise.Emit(transform.position, NoiseRadius);
            }

            if (_exit == null && Remaining <= 0f)
            {
                _exit = PlaceholderArt.Box("FlarePickup", transform.position, new Vector2(3f, 3f), new Color(0.2f, 0.8f, 0.35f, 0.45f), transform, 1, "extraction", SpriteFit.Tile);
                _exit.AddComponent<BoxCollider2D>().isTrigger = true;
                _exit.AddComponent<ExtractionZone>();
                Sfx.PlayUi(SoundId.Extract, 0.7f);
                GameRoot.Instance.Notify("Tiếng xe máy gầm tới! Vy đến rồi, vào vùng xanh để lên xe.", 5f);
            }

            if (_exit != null && Remaining < -PickupSeconds)
            {
                GameRoot.Instance.Notify("Vy không chờ được nữa, phải đi rồi.", 4f);
                Destroy(gameObject);
            }
        }
    }
}
