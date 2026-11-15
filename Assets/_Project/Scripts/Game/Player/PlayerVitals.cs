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
    /// the one place that uses a consumable item on the player. Using an
    /// item takes time and is interrupted by being hit.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerVitals : MonoBehaviour
    {
        [SerializeField] private float starvationDamagePerSecond = 2f;
        [SerializeField] private float weakenedRegenMultiplier = 0.4f;

        private Health _health;
        private float _nextHurtSound;
        private bool _starvationTick;
        private PlacedItem _useItem;
        private GridInventory _useGrid;
        private ConsumableDefinition _useEffect;
        private float _useElapsed;

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
            if (!_starvationTick) CancelUse("Bị đánh");

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

            UpdateUse(delta);

            bool weakened = session.Needs.IsStarving || session.Needs.IsDehydrated;
            session.Stamina.Tick(delta, weakened ? weakenedRegenMultiplier : 1f);

            // Hunger and thirst only run while outside.
            if (!session.IsInRaid) return;
            session.Needs.Tick(delta);
            if (weakened)
            {
                // Starving must not interrupt eating, or the player could never recover.
                _starvationTick = true;
                _health.Damage(starvationDamagePerSecond * delta);
                _starvationTick = false;
            }
        }

        /// <summary>True while an item is being used. The player cannot attack, sprint or roll meanwhile.</summary>
        public bool IsUsing => _useItem != null;

        /// <summary>0 to 1 through the current use.</summary>
        public float UseProgress => IsUsing && _useEffect.UseSeconds > 0f ? Mathf.Clamp(_useElapsed / _useEffect.UseSeconds, 0f, 1f) : 0f;

        public string UsingName => IsUsing ? _useItem.Item.DisplayName : string.Empty;

        /// <summary>
        /// Starts using one unit of an item from a grid. It takes the item's use time to finish;
        /// the item is only spent, and its effect only applied, at the end. Returns false (with
        /// a HUD message) if it cannot be used now.
        /// </summary>
        public bool UseItem(GridInventory grid, PlacedItem item)
        {
            var root = GameRoot.Instance;
            if (root == null || grid == null || item == null) return false;
            if (IsUsing)
            {
                root.Notify("Đang dùng " + UsingName + ".", 1.2f);
                return false;
            }

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

            _useItem = item;
            _useGrid = grid;
            _useEffect = effect;
            _useElapsed = 0f;
            return true;
        }

        /// <summary>Stops the use in progress. Nothing is spent.</summary>
        public void CancelUse(string reason)
        {
            if (!IsUsing) return;
            string name = UsingName;
            ClearUse();
            if (!string.IsNullOrEmpty(reason) && GameRoot.Instance != null)
            {
                GameRoot.Instance.Notify(reason + ": dừng dùng " + name + ".", 1.5f);
            }
        }

        private void UpdateUse(float delta)
        {
            if (!IsUsing) return;

            // The stack was moved away or used up some other way.
            if (!_useGrid.Contains(_useItem))
            {
                ClearUse();
                return;
            }

            _useElapsed += delta;
            if (_useElapsed < _useEffect.UseSeconds) return;

            PlacedItem item = _useItem;
            GridInventory grid = _useGrid;
            ConsumableDefinition effect = _useEffect;
            ClearUse();

            var root = GameRoot.Instance;
            if (!root.Session.Consume(grid, item, effect)) return;
            _health.Heal(effect.Heal);
            Sfx.PlayUi(SoundId.UseItem, 0.6f);
            root.Notify("Đã dùng " + item.Item.DisplayName + ".", 1.5f);
        }

        private void ClearUse()
        {
            _useItem = null;
            _useGrid = null;
            _useEffect = null;
            _useElapsed = 0f;
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
