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
            Gun gun = Player.Gun;

            _text.Length = 0;
            _text.Append("Ngày ").Append(session.Clock.Day).Append(", ")
                .Append(RaidDirector.SeasonName(session.Clock.Season)).Append(", năm ").Append(session.Clock.Year)
                .Append(session.IsInRaid ? "  [ĐANG RAID]" : "  [CĂN CỨ]").Append('\n');
            _text.Append("Máu: ").Append(Mathf.CeilToInt(health.Current)).Append('/').Append(Mathf.CeilToInt(health.Max));
            if (gun != null)
            {
                _text.Append("   Đạn: ").Append(gun.AmmoInMagazine).Append('/').Append(gun.MagazineSize);
                if (gun.IsReloading) _text.Append(" (đang nạp)");
            }
            _text.Append("   Tem phiếu: ").Append(session.Wallet.Money).Append('\n');

            _text.Append("\nBalo (").Append(session.Backpack.Width).Append('x').Append(session.Backpack.Height).Append("):\n");
            if (session.Backpack.Items.Count == 0) _text.Append("  (trống)\n");
            foreach (var placed in session.Backpack.Items)
            {
                _text.Append("  ").Append(placed.Item.DisplayName).Append(" x").Append(placed.Count).Append('\n');
            }

            _text.Append("\nNhiệm vụ:\n");
            foreach (DeliveryQuest quest in session.Quests.All)
            {
                if (quest.State != QuestState.Active) continue;
                _text.Append("  ").Append(quest.Title).Append(": ")
                    .Append(session.Backpack.CountOf(quest.ItemId)).Append('/').Append(quest.Count).Append('\n');
            }

            GUI.Label(new Rect(12f, 10f, 420f, Screen.height - 20f), _text.ToString(), _style);

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

            GUI.Label(new Rect(Screen.width - 432f, 10f, 420f, 24f), "WASD di chuyển · Chuột ngắm/bắn · R nạp đạn · E tương tác · Shift chạy", _style);
        }
    }
}
