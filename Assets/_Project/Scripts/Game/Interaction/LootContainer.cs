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
    /// on first search. Items are then uncovered one at a time while the
    /// inventory screen is open on this container; rarer items take longer.
    /// Closing the screen pauses the search and keeps what was found.
    /// </summary>
    public sealed class LootContainer : MonoBehaviour, IInteractable
    {
        private static readonly Color EmptyColor = new Color(0.35f, 0.3f, 0.25f);

        [SerializeField] private int rolls = 3;
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 4;
        [SerializeField] private float baseRevealSeconds = 0.6f;
        [SerializeField] private float extraSecondsPerRarity = 0.4f;

        private LootTable _table;
        private GridInventory _contents;
        private List<LootDrop> _hidden;
        private float _revealTimer;
        private bool _markedEmpty;

        public string Prompt
        {
            get
            {
                if (_contents == null) return "E: Lục đồ";
                if (_hidden.Count > 0) return "E: Lục tiếp";
                return _contents.Items.Count == 0 ? "E: Mở thùng (trống)" : "E: Mở thùng";
            }
        }

        public void Init(LootTable table)
        {
            _table = table;
        }

        public void Interact(PlayerController player)
        {
            if (_contents == null)
            {
                _contents = new GridInventory(gridWidth, gridHeight);
                _hidden = _table != null
                    ? _table.Roll(GameRoot.Instance.Session.Rng, rolls)
                    : new List<LootDrop>();
                if (_hidden.Count > 0) _revealTimer = RevealSeconds(_hidden[0]);
            }

            if (InventoryScreen.Instance != null) InventoryScreen.Instance.Open(_contents, Title());
        }

        private void Update()
        {
            if (_contents == null) return;

            InventoryScreen screen = InventoryScreen.Instance;
            if (_hidden.Count > 0 && screen != null && screen.IsShowing(_contents))
            {
                _revealTimer -= UnityEngine.Time.deltaTime;
                if (_revealTimer <= 0f)
                {
                    LootDrop drop = _hidden[0];
                    _hidden.RemoveAt(0);
                    _contents.Add(drop.Item, drop.Count);
                    if (_hidden.Count > 0) _revealTimer = RevealSeconds(_hidden[0]);
                    screen.SetOtherTitle(Title());
                }
            }

            if (!_markedEmpty && _hidden.Count == 0 && _contents.Items.Count == 0)
            {
                _markedEmpty = true;
                PlaceholderArt.SetColor(gameObject, EmptyColor);
            }
        }

        private float RevealSeconds(LootDrop drop)
        {
            return baseRevealSeconds + extraSecondsPerRarity * (int)drop.Item.Rarity;
        }

        private string Title()
        {
            return _hidden.Count > 0 ? "Thùng đồ · đang lục... (còn " + _hidden.Count + " món)" : "Thùng đồ";
        }
    }
}
