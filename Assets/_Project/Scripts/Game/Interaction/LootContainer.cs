using Bnhon.Core.Inventory;
using Bnhon.Core.Loot;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>
    /// A searchable container in the raid zone. Rolls its loot table once,
    /// on first search, into its own small grid, then opens the inventory
    /// screen so the player picks what to take.
    /// </summary>
    public sealed class LootContainer : MonoBehaviour, IInteractable
    {
        private static readonly Color EmptyColor = new Color(0.35f, 0.3f, 0.25f);

        [SerializeField] private int rolls = 3;
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 4;

        private LootTable _table;
        private GridInventory _contents;
        private bool _markedEmpty;

        public string Prompt
        {
            get
            {
                if (_contents == null) return "E: Lục đồ";
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
                if (_table != null)
                {
                    foreach (LootDrop drop in _table.Roll(GameRoot.Instance.Session.Rng, rolls))
                    {
                        _contents.Add(drop.Item, drop.Count);
                    }
                }
            }

            if (InventoryScreen.Instance != null) InventoryScreen.Instance.Open(_contents, "Thùng đồ");
        }

        private void Update()
        {
            if (_markedEmpty || _contents == null || _contents.Items.Count > 0) return;
            _markedEmpty = true;
            PlaceholderArt.SetColor(gameObject, EmptyColor);
        }
    }
}
