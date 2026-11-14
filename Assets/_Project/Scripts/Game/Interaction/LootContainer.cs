using System.Collections.Generic;
using Bnhon.Core.Inventory;
using Bnhon.Core.Loot;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
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
        [SerializeField] private float baseRevealSeconds = 0.6f;
        [SerializeField] private float extraSecondsPerRarity = 0.4f;

        private LootTable _table;
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
                if (_table != null)
                {
                    foreach (LootDrop drop in _table.Roll(GameRoot.Instance.Session.Rng, rolls))
                    {
                        _contents.Add(drop.Item, drop.Count);
                    }
                }
                // Search order is the order the stacks sit in the grid.
                _hidden = new List<PlacedItem>(_contents.Items);
            }

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
            if (_contents == null) return;

            InventoryScreen screen = InventoryScreen.Instance;
            if (_hidden.Count > 0 && screen != null && screen.IsShowing(_contents))
            {
                _revealElapsed += UnityEngine.Time.deltaTime;
                if (_revealElapsed >= RevealSeconds(_hidden[0]))
                {
                    _hidden.RemoveAt(0);
                    _revealElapsed = 0f;
                }
            }

            if (!_markedEmpty && _contents.Items.Count == 0)
            {
                _markedEmpty = true;
                PlaceholderArt.SetColor(gameObject, EmptyColor);
            }
        }

        private float RevealSeconds(PlacedItem item)
        {
            return baseRevealSeconds + extraSecondsPerRarity * (int)item.Item.Rarity;
        }
    }
}
