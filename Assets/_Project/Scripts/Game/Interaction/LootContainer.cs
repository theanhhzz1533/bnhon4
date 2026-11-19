using System.Collections.Generic;
using Bnhon.Core.Inventory;
using Bnhon.Core.Loot;
using Bnhon.Game.Audio;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using Bnhon.Game.Combat;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>
    /// A searchable container in the raid zone. Rolls its loot table once,
    /// on first search. Every item is laid out in the grid straight away but
    /// starts concealed: the screen shows it as a dim unknown block of the
    /// right size. Items are identified one at a time while the inventory
    /// screen is open on this container; rarer items take longer. Closing
    /// the screen pauses the search.
    /// </summary>
    public sealed class LootContainer : MonoBehaviour, IInteractable, IItemConcealer
    {
        private static readonly Color EmptyColor = new Color(0.35f, 0.3f, 0.25f);

        [SerializeField] private int rolls = 3;
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 4;
        [SerializeField] private float baseRevealSeconds = 1.1f;
        [SerializeField] private float extraSecondsPerRarity = 0.6f;

        private LootTable _table;
        private IReadOnlyList<Bnhon.Core.Crafting.ItemAmount> _fixed;
        private List<Bnhon.Core.Crafting.ItemAmount> _extra;
        private SpriteRenderer _senseGlow;
        private string _title = "Thùng đồ";
        private string _searchPrompt = "E: Lục đồ";
        private string _openPrompt = "E: Mở thùng";
        private GridInventory _contents;
        private List<PlacedItem> _hidden;
        private float _revealElapsed;
        private bool _markedEmpty;

        public string Prompt
        {
            get
            {
                if (_contents == null) return _searchPrompt;
                if (_hidden.Count > 0) return "E: Lục tiếp";
                return _contents.Items.Count == 0 ? _openPrompt + " (trống)" : _openPrompt;
            }
        }

        public void Init(LootTable table)
        {
            _table = table;
        }

        /// <summary>A container that always holds the same things, for story moments.</summary>
        public void InitFixed(IReadOnlyList<Bnhon.Core.Crafting.ItemAmount> items, string title)
        {
            _fixed = items;
            _title = title;
        }

        /// <summary>Something added on top of whatever the container rolls, such as samples on a body.</summary>
        public void AddExtra(Bnhon.Core.Items.ItemDefinition item, int count)
        {
            if (_extra == null) _extra = new List<Bnhon.Core.Crafting.ItemAmount>();
            _extra.Add(new Bnhon.Core.Crafting.ItemAmount(item, count));
        }

        /// <summary>Sets this up as a zombie corpse: its own wording, loot count and a small grid.</summary>
        public void InitCorpse(LootTable table, int rollCount)
        {
            _table = table;
            rolls = rollCount;
            gridWidth = 3;
            gridHeight = 2;
            _title = "Xác zombie";
            _searchPrompt = "E: Lục xác";
            _openPrompt = "E: Xem xác";
        }

        public void Interact(PlayerController player)
        {
            if (_contents == null)
            {
                _contents = new GridInventory(gridWidth, gridHeight);
                if (_fixed != null)
                {
                    foreach (Bnhon.Core.Crafting.ItemAmount item in _fixed) _contents.Add(item.Item, item.Count);
                }
                if (_table != null)
                {
                    foreach (LootDrop drop in _table.Roll(GameRoot.Instance.Session.Rng, rolls))
                    {
                        _contents.Add(drop.Item, drop.Count);
                    }
                }
                if (_extra != null)
                {
                    foreach (Bnhon.Core.Crafting.ItemAmount item in _extra) _contents.Add(item.Item, item.Count);
                }
                // Search order is the order the stacks sit in the grid.
                _hidden = new List<PlacedItem>(_contents.Items);
            }

            Sfx.PlayUi(SoundId.LootOpen, 0.6f);
            if (InventoryScreen.Instance != null) InventoryScreen.Instance.Open(_contents, _title, this);
        }

        public bool IsHidden(PlacedItem item)
        {
            return _hidden != null && _hidden.Contains(item);
        }

        public float RevealProgress(PlacedItem item)
        {
            if (_hidden == null || _hidden.Count == 0 || _hidden[0] != item) return 0f;
            return Mathf.Clamp(_revealElapsed / RevealSeconds(item), 0f, 1f);
        }

        private void Update()
        {
            UpdateSense();
            if (_contents == null) return;

            InventoryScreen screen = InventoryScreen.Instance;
            if (_hidden.Count > 0 && screen != null && screen.IsShowing(_contents))
            {
                _revealElapsed += UnityEngine.Time.deltaTime;
                if (_revealElapsed >= RevealSeconds(_hidden[0]))
                {
                    _hidden.RemoveAt(0);
                    _revealElapsed = 0f;
                    Sfx.PlayUi(SoundId.ItemReveal, 0.5f);
                }
            }

            if (!_markedEmpty && _contents.Items.Count == 0)
            {
                _markedEmpty = true;
                PlaceholderArt.MarkSpent(gameObject, EmptyColor);
            }
        }

        private float RevealSeconds(PlacedItem item)
        {
            float seconds = baseRevealSeconds + extraSecondsPerRarity * (int)item.Item.Rarity;
            return seconds / CharmEffects.More(Bnhon.Core.Charms.CharmStat.SearchSpeed);
        }

        /// <summary>
        /// With the Scent charm, a container nobody has searched glows faintly when the player
        /// is near, even through walls and fog.
        /// </summary>
        private void UpdateSense()
        {
            float range = CharmEffects.Get(Bnhon.Core.Charms.CharmStat.LootSense);
            bool show = false;
            if (range > 0f && _contents == null && PlayerVitals.Instance != null)
            {
                Vector2 offset = PlayerVitals.Instance.transform.position - transform.position;
                show = offset.sqrMagnitude <= range * range;
            }

            if (show && _senseGlow == null)
            {
                _senseGlow = Bnhon.Game.World.SoftDot.Make("Scent", transform, YSort.AboveEverything + 3);
                _senseGlow.color = new Color(0.95f, 0.85f, 0.4f, 0.55f);
                Vector3 scale = transform.lossyScale;
                _senseGlow.transform.localScale = new Vector3(1.4f / Mathf.Max(0.01f, scale.x), 1.4f / Mathf.Max(0.01f, scale.y), 1f);
            }
            if (_senseGlow != null && _senseGlow.enabled != show) _senseGlow.enabled = show;
        }
    }
}
