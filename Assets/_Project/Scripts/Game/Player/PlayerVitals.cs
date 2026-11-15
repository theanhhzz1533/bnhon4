using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Core.Vitals;
using Bnhon.Game.Audio;
using Bnhon.Game.Combat;
using UnityEngine;

namespace Bnhon.Game.Player
{
    /// <summary>
    /// Ticks stamina, hunger and thirst, applies starvation damage, and is
    /// the one place that uses a consumable item on the player.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerVitals : MonoBehaviour
    {
        [SerializeField] private float starvationDamagePerSecond = 2f;
        [SerializeField] private float weakenedRegenMultiplier = 0.4f;

        private Health _health;
        private float _nextHurtSound;

        public static PlayerVitals Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _health = GetComponent<Health>();
            _health.Damaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Damaged -= OnDamaged;
            if (Instance == this) Instance = null;
        }

        private void OnDamaged(Health health)
        {
            // Starvation damages every frame; without a gap this would be one continuous noise.
            if (UnityEngine.Time.time < _nextHurtSound) return;
            _nextHurtSound = UnityEngine.Time.time + 0.6f;
            Sfx.PlayUi(SoundId.PlayerHurt, 0.8f);
        }

        private void Update()
        {
            if (GameRoot.Instance == null) return;
            var session = GameRoot.Instance.Session;
            float delta = UnityEngine.Time.deltaTime;

            bool weakened = session.Needs.IsStarving || session.Needs.IsDehydrated;
            session.Stamina.Tick(delta, weakened ? weakenedRegenMultiplier : 1f);

            // Hunger and thirst only run while outside.
            if (!session.IsInRaid) return;
            session.Needs.Tick(delta);
            if (weakened) _health.Damage(starvationDamagePerSecond * delta);
        }

        /// <summary>Uses one unit of an item from a grid. Returns false (with a HUD message) if it cannot be used now.</summary>
        public bool UseItem(GridInventory grid, PlacedItem item)
        {
            var root = GameRoot.Instance;
            if (root == null || grid == null || item == null) return false;

            ItemDefinition definition = item.Item;
            ConsumableDefinition effect = PrototypeContent.FindConsumable(definition.Id);
            if (effect == null)
            {
                root.Notify(definition.DisplayName + " không dùng trực tiếp được.", 1.5f);
                return false;
            }

            var needs = root.Session.Needs;
            bool wouldHeal = effect.Heal > 0f && _health.Current < _health.Max;
            bool wouldFeed = (effect.Hunger > 0f && needs.Hunger < needs.Max) || (effect.Thirst > 0f && needs.Thirst < needs.Max);
            if (!wouldHeal && !wouldFeed)
            {
                root.Notify("Chưa cần dùng " + definition.DisplayName + ".", 1.5f);
                return false;
            }

            if (!root.Session.Consume(grid, item, effect)) return false;

            _health.Heal(effect.Heal);
            Sfx.PlayUi(SoundId.UseItem, 0.6f);
            root.Notify("Đã dùng " + definition.DisplayName + ".", 1.5f);
            return true;
        }

        /// <summary>Uses the first healing item in the backpack. Bound to a quick key so the player need not open the inventory mid-fight.</summary>
        public bool QuickHeal()
        {
            var root = GameRoot.Instance;
            if (root == null) return false;

            var backpack = root.Session.Backpack;
            foreach (PlacedItem item in backpack.Items)
            {
                ConsumableDefinition effect = PrototypeContent.FindConsumable(item.Item.Id);
                if (effect != null && effect.Heal > 0f) return UseItem(backpack, item);
            }

            root.Notify("Không có đồ hồi máu trong balo.", 1.5f);
            return false;
        }
    }
}
