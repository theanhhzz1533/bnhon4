using System.Text;
using Bnhon.Core.Combat;
using Bnhon.Core.Quests;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using Bnhon.Game.Raid;
using UnityEngine;
using UnityEngine.InputSystem;

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
                .Append(session.IsInRaid ? "  [ĐANG RAID " + RaidClock() + "]" : "  [CĂN CỨ]").Append('\n');

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
                bool bareHanded = weapon.Kind == WeaponKind.Melee && !session.HasKnife;
                _text.Append('[').Append(weapons.CurrentSlot).Append("] ").Append(bareHanded ? "Tay không" : weapon.Name);
                if (weapon.Kind == WeaponKind.Firearm)
                {
                    _text.Append("  ").Append(weapons.AmmoInMagazine).Append('/').Append(weapon.MagazineSize)
                        .Append("  (dự trữ ").Append(session.Backpack.CountOf(weapon.AmmoItemId)).Append(')');
                    if (weapons.IsReloading) _text.Append("  đang nạp");
                }
                _text.Append('\n');

                _text.Append(session.HasKnife ? "    1 Dao" : "    1 (chưa có dao)");
                for (int slot = 0; slot < Loadout.SlotCount; slot++)
                {
                    var equipped = session.Loadout.GetWeapon(slot);
                    _text.Append(" · ").Append(slot + 2).Append(' ').Append(equipped != null ? equipped.DisplayName : "(trống)");
                }
                _text.Append('\n');
            }
            foreach (Bnhon.Core.Vitals.ActiveBuff buff in session.Buffs.Active)
            {
                int left = Mathf.CeilToInt(buff.Remaining);
                _text.Append("  + ").Append(buff.Definition.DisplayName).Append("  ").Append(left / 60).Append(':').Append((left % 60).ToString("00")).Append('\n');
            }
            if (!session.IsInRaid)
            {
                _text.Append("Bình tưới: ").Append(session.WateringCan.Water).Append('/').Append(session.WateringCan.Capacity).Append('\n');
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
            if (ModalScreen.Current != null) return;

            DrawCrosshair(weapons);

            string objective = StoryDirector.Instance != null ? StoryDirector.Instance.Objective : null;
            if (objective != null)
            {
                GUI.color = new Color(0.95f, 0.85f, 0.4f, 1f);
                GUI.Label(new Rect(0f, 40f, Screen.width, 30f), "Mục tiêu: " + objective, _centered);
                GUI.color = Color.white;
            }

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

            PlayerVitals vitals = PlayerVitals.Instance;
            if (vitals != null && vitals.IsUsing)
            {
                var bar = new Rect(Screen.width * 0.5f - 110f, centerY - 68f, 220f, 10f);
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = new Color(0.95f, 0.85f, 0.4f, 1f);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * vitals.UseProgress, bar.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(0f, bar.y - 30f, Screen.width, 28f), "Đang dùng " + vitals.UsingName + "... (bấm chuột để hủy)", _centered);
            }

            if (root.HasMessage)
            {
                GUI.Label(new Rect(0f, centerY + 34f, Screen.width, 30f), root.Message, _centered);
            }

            GUI.Label(new Rect(Screen.width - 432f, 10f, 420f, 24f), "WASD · Chuột bắn · 1-3 vũ khí · R nạp · Space lăn · Q hồi máu · E · Tab", _style);
        }

        private static string RaidClock()
        {
            int seconds = RaidPressure.Instance != null ? (int)RaidPressure.Instance.RaidSeconds : 0;
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }

        /// <summary>
        /// Four ticks around the mouse pointer. The gap between them is how wide the shots can
        /// land at that distance, so it opens with recoil and closes as the aim settles.
        /// </summary>
        private void DrawCrosshair(PlayerWeapons weapons)
        {
            Mouse mouse = Mouse.current;
            Camera cam = Camera.main;
            if (weapons == null || mouse == null || cam == null || weapons.Current.Kind != WeaponKind.Firearm) return;

            Vector2 pointer = mouse.position.ReadValue();
            Vector3 origin = cam.WorldToScreenPoint(Player.transform.position);
            float distance = new Vector2(pointer.x - origin.x, pointer.y - origin.y).magnitude;
            float radius = Mathf.Max(5f, distance * Mathf.Tan(weapons.CurrentSpread * 0.5f * Mathf.Deg2Rad));

            // GUI space has its origin at the top left; screen space at the bottom left.
            float x = pointer.x;
            float y = Screen.height - pointer.y;
            const float length = 7f;
            const float thickness = 2f;

            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(x - radius - length, y - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + radius, y - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - thickness * 0.5f, y - radius - length, thickness, length), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - thickness * 0.5f, y + radius, thickness, length), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
