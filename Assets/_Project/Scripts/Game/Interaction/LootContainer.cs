using System.Collections.Generic;
using System.Text;
using Bnhon.Core.Loot;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>A searchable container in the raid zone. Rolls its loot table once, on first search.</summary>
    public sealed class LootContainer : MonoBehaviour, IInteractable
    {
        [SerializeField] private int rolls = 3;

        private LootTable _table;
        private List<LootDrop> _contents;

        public string Prompt
        {
            get
            {
                if (_contents == null) return "E: Lục đồ";
                return _contents.Count == 0 ? "Trống" : "E: Lấy phần còn lại";
            }
        }

        public void Init(LootTable table)
        {
            _table = table;
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var session = root.Session;

            if (_contents == null)
            {
                _contents = _table != null ? _table.Roll(session.Rng, rolls) : new List<LootDrop>();
            }

            if (_contents.Count == 0)
            {
                root.Notify("Không còn gì ở đây.");
                return;
            }

            var taken = new StringBuilder();
            for (int i = _contents.Count - 1; i >= 0; i--)
            {
                LootDrop drop = _contents[i];
                int leftover = session.Backpack.Add(drop.Item, drop.Count);
                int moved = drop.Count - leftover;
                if (moved > 0)
                {
                    if (taken.Length > 0) taken.Append(", ");
                    taken.Append(drop.Item.DisplayName).Append(" x").Append(moved);
                }

                if (leftover == 0) _contents.RemoveAt(i);
                else _contents[i] = new LootDrop(drop.Item, leftover);
            }

            if (taken.Length == 0) root.Notify("Balo đã đầy.");
            else if (_contents.Count > 0) root.Notify("Nhặt được: " + taken + ". Balo đầy, còn đồ trong thùng.");
            else root.Notify("Nhặt được: " + taken);

            if (_contents.Count == 0) PlaceholderArt.SetColor(gameObject, new Color(0.35f, 0.3f, 0.25f));
        }
    }
}
