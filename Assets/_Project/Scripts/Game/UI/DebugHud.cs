using System.Text;
using Bnhon.Core.Quests;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using Bnhon.Game.Raid;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>Immediate-mode text HUD for the prototype. Replace with UI Toolkit in M2.</summary>
    public sealed class DebugHud : MonoBehaviour
    {
        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;
        private GUIStyle _centered;

        public PlayerController Player { get; set; }

        private void OnGUI()
        {
            var root = GameRoot.Instance;
            if (root == null || Player == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = false };
                _style.normal.textColor = Color.white;
                _centered = new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            }

            var session = root.Session;
            var health = Player.GetComponent<Health>();
            PlayerWeapons weapons = Player.Weapons;

            _text.Length = 0;
            _text.Append("Ngày ").Append(session.Clock.Day).Append(", ")
                .Append(RaidDirector.SeasonName(session.Clock.Season)).Append(", năm ").Append(session.Clock.Year)
                .Append(session.IsInRaid ? "  [ĐANG RAID]" : "  [CĂN CỨ]").Append('\n');

            _text.Append("Máu: ").Append(Mathf.CeilToInt(health.Current)).Append('/').Append(Mathf.CeilToInt(health.Max))
                .Append("   Thể lực: ").Append(Mathf.CeilToInt(session.Stamina.Current));
            if (session.Stamina.IsExhausted) _text.Append(" (kiệt sức)");
            _text.Append('\n');

            _text.Append("Đói: ").Append(Mathf.CeilToInt(session.Needs.Hunger))
                .Append("   Khát: ").Append(Mathf.CeilToInt(session.Needs.Thirst));
            if (session.Needs.IsStarving || session.Needs.IsDehydrated) _text.Append("  (đang mất máu!)");
            _text.Append('\n');

            if (weapons != null)
            {
                WeaponDefinition weapon = weapons.Current;
                _text.Append('[').Append(weapons.CurrentSlot).Append("] ").Append(weapon.Name);
                if (weapon.Kind == WeaponKind.Firearm)
                {
                    _text.Append("  ").Append(weapons.AmmoInMagazine).Append('/').Append(weapon.MagazineSize)
                        .Append("  (dự trữ ").Append(session.Backpack.CountOf(weapon.AmmoItemId)).Append(')');
                    if (weapons.IsReloading) _text.Append("  đang nạp");
                }
                _text.Append('\n');
            }
            _text.Append("Tem phiếu: ").Append(session.Wallet.Money).Append('\n');

            _text.Append("Balo: ").Append(session.Backpack.Items.Count).Append(" món, giá trị ")
                .Append(session.Backpack.TotalValue()).Append("  (Tab để mở)\n");

            _text.Append("\nNhiệm vụ:\n");
            foreach (DeliveryQuest quest in session.Quests.All)
            {
                if (quest.State != QuestState.Active) continue;
                _text.Append("  ").Append(quest.Title).Append(": ")
                    .Append(session.Backpack.CountOf(quest.ItemId)).Append('/').Append(quest.Count).Append('\n');
            }

            GUI.Label(new Rect(12f, 10f, 420f, Screen.height - 20f), _text.ToString(), _style);

            // The inventory screen covers the middle of the view and has its own hint line.
            if (InventoryScreen.Instance != null && InventoryScreen.Instance.IsOpen) return;

            float centerY = Screen.height - 120f;
            if (Player.Focused != null)
            {
                GUI.Label(new Rect(0f, centerY, Screen.width, 30f), Player.Focused.Prompt, _centered);
            }

            ExtractionZone zone = ExtractionZone.Active;
            if (zone != null && zone.Remaining >= 0f)
            {
                GUI.Label(new Rect(0f, centerY - 34f, Screen.width, 30f), "Đang rút lui... " + zone.Remaining.ToString("0.0") + "s", _centered);
            }

            if (root.HasMessage)
            {
                GUI.Label(new Rect(0f, centerY + 34f, Screen.width, 30f), root.Message, _centered);
            }

            GUI.Label(new Rect(Screen.width - 432f, 10f, 420f, 24f), "WASD · Chuột bắn · 1-3 vũ khí · R nạp · Space lăn · Q hồi máu · E · Tab", _style);
        }
    }
}
