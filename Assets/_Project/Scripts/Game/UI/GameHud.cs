using Bnhon.Core;
using Bnhon.Core.Combat;
using Bnhon.Core.Npcs;
using Bnhon.Core.Quests;
using Bnhon.Core.Vitals;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using Bnhon.Game.Raid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// The in-game HUD: status and bars top left, the story objective top centre, quests
    /// top right, weapons bottom right, prompts and messages bottom centre. It steps aside
    /// while a menu is open. Immediate-mode GUI; to be replaced with UI Toolkit.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        private const float Margin = 12f;
        private const float Pad = 8f;

        private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.09f, 0.78f);
        private static readonly Color BarBack = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color HealthColor = new Color(0.85f, 0.25f, 0.25f);
        private static readonly Color StaminaColor = new Color(0.4f, 0.8f, 0.4f);
        private static readonly Color HungerColor = new Color(0.9f, 0.6f, 0.25f);
        private static readonly Color ThirstColor = new Color(0.35f, 0.65f, 0.95f);
        private static readonly Color Gold = new Color(0.95f, 0.85f, 0.4f);
        private static readonly Color Dim = new Color(0.72f, 0.72f, 0.75f);
        private static readonly Color Warning = new Color(1f, 0.45f, 0.4f);

        private GUIStyle _text;
        private GUIStyle _small;
        private GUIStyle _bold;
        private GUIStyle _big;
        private GUIStyle _right;
        private GUIStyle _centered;

        public PlayerController Player { get; set; }

        private void OnGUI()
        {
            var root = GameRoot.Instance;
            if (root == null || Player == null) return;

            // Menus cover the view and show messages themselves.
            if (ModalScreen.Current != null) return;

            EnsureStyles();
            GameSession session = root.Session;
            Health health = Player.GetComponent<Health>();
            PlayerWeapons weapons = Player.Weapons;
            bool inventoryOpen = InventoryScreen.Instance != null && InventoryScreen.Instance.IsOpen;

            // The crosshair follows the real mouse position, so it is drawn before scaling.
            if (!inventoryOpen) DrawCrosshair(weapons);

            Matrix4x4 previous = UiScale.Apply();
            float width = UiScale.Width;
            float height = UiScale.Height;

            if (inventoryOpen)
            {
                DrawCompactVitals(session, health, width);
            }
            else
            {
                DrawStatus(session, health);
                DrawObjective(width);
                DrawQuests(session, width);
                DrawWeapons(session, weapons, width, height);
                DrawFooter(session, height);
                DrawCentre(root, width, height);
            }

            GUI.matrix = previous;
        }

        private void DrawStatus(GameSession session, Health health)
        {
            int buffs = session.Buffs.Active.Count;
            bool weakened = session.Needs.IsStarving || session.Needs.IsDehydrated;
            float panelHeight = Pad * 2f + 40f + 4f * 20f + buffs * 16f + (weakened ? 18f : 0f);
            var panel = new Rect(Margin, Margin, 250f, panelHeight);
            Fill(panel, PanelColor);

            float x = panel.x + Pad;
            float y = panel.y + Pad;
            float inner = panel.width - Pad * 2f;

            GUI.Label(new Rect(x, y, inner, 20f),
                "Ngày " + session.Clock.Day + " · " + RaidDirector.SeasonName(session.Clock.Season) + " · năm " + session.Clock.Year, _bold);
            y += 20f;

            string place = session.IsInRaid
                ? (session.InIntro ? "Bệnh viện  " : "Đang raid  ") + RaidClock()
                : "Căn cứ · " + (session.Phase == DayPhase.Morning ? "buổi sáng" : "buổi tối");
            Label(new Rect(x, y, inner, 18f), place, _small, session.IsInRaid ? Gold : Dim);
            y += 20f;

            y = Bar(x, y, inner, "Máu", health.Current, health.Max, HealthColor);
            y = Bar(x, y, inner, session.Stamina.IsExhausted ? "Kiệt sức" : "Thể lực", session.Stamina.Current, session.Stamina.Max, StaminaColor);
            y = Bar(x, y, inner, "Đói", session.Needs.Hunger, session.Needs.Max, HungerColor);
            y = Bar(x, y, inner, "Khát", session.Needs.Thirst, session.Needs.Max, ThirstColor);

            if (weakened)
            {
                Label(new Rect(x, y, inner, 18f), "Đang mất máu vì đói khát!", _small, Warning);
                y += 18f;
            }

            foreach (ActiveBuff buff in session.Buffs.Active)
            {
                int left = Mathf.CeilToInt(buff.Remaining);
                Label(new Rect(x, y, inner, 16f),
                    "+ " + buff.Definition.DisplayName + "  " + (left / 60) + ":" + (left % 60).ToString("00"), _small, StaminaColor);
                y += 16f;
            }
        }

        /// <summary>One labelled bar. Returns the y of the next row.</summary>
        private float Bar(float x, float y, float width, string label, float value, float max, Color color)
        {
            const float labelWidth = 62f;
            const float valueWidth = 34f;
            GUI.Label(new Rect(x, y - 1f, labelWidth, 18f), label, _small);

            var back = new Rect(x + labelWidth, y + 3f, width - labelWidth - valueWidth - 4f, 11f);
            Fill(back, BarBack);
            float fraction = max > 0f ? Mathf.Clamp(value / max, 0f, 1f) : 0f;
            Fill(new Rect(back.x + 1f, back.y + 1f, (back.width - 2f) * fraction, back.height - 2f), color);

            GUI.Label(new Rect(x + width - valueWidth, y - 1f, valueWidth, 18f), Mathf.CeilToInt(value).ToString(), _right);
            return y + 20f;
        }

        private void DrawObjective(float width)
        {
            string objective = StoryDirector.Instance != null ? StoryDirector.Instance.Objective : null;
            if (objective == null) return;

            // Sits between the status panel on the left and the quest panel on the right.
            float panelWidth = Mathf.Clamp(width - 2f * 290f, 280f, 620f);
            string text = "Mục tiêu: " + objective;
            float textHeight = _centered.CalcHeight(new GUIContent(text), panelWidth - Pad * 2f);
            var panel = new Rect((width - panelWidth) * 0.5f, Margin, panelWidth, textHeight + Pad * 2f);

            Fill(panel, PanelColor);
            Fill(new Rect(panel.x, panel.y, 3f, panel.height), Gold);
            Label(new Rect(panel.x + Pad, panel.y + Pad, panel.width - Pad * 2f, textHeight), text, _centered, Gold);
        }

        private void DrawQuests(GameSession session, float width)
        {
            int active = 0;
            foreach (DeliveryQuest quest in session.Quests.All)
            {
                if (quest.State == QuestState.Active) active++;
            }
            if (active == 0) return;

            var panel = new Rect(width - Margin - 260f, Margin, 260f, Pad * 2f + 20f + active * 18f);
            Fill(panel, PanelColor);

            float x = panel.x + Pad;
            float y = panel.y + Pad;
            float inner = panel.width - Pad * 2f;
            GUI.Label(new Rect(x, y, inner, 20f), "Nhiệm vụ", _bold);
            y += 20f;

            foreach (DeliveryQuest quest in session.Quests.All)
            {
                if (quest.State != QuestState.Active) continue;
                int have = session.Backpack.CountOf(quest.ItemId);
                bool ready = have >= quest.Count;
                Label(new Rect(x, y, inner - 44f, 18f), quest.Title, _small, ready ? Gold : Color.white);
                Label(new Rect(x + inner - 44f, y, 44f, 18f), have + "/" + quest.Count, _right, ready ? Gold : Color.white);
                y += 18f;
            }
        }

        private void DrawWeapons(GameSession session, PlayerWeapons weapons, float width, float height)
        {
            if (weapons == null) return;

            var panel = new Rect(width - Margin - 270f, height - Margin - 84f, 270f, 84f);
            Fill(panel, PanelColor);
            float x = panel.x + Pad;
            float inner = panel.width - Pad * 2f;

            WeaponDefinition weapon = weapons.Current;
            bool bareHanded = weapon.Kind == WeaponKind.Melee && !session.HasKnife;
            GUI.Label(new Rect(x, panel.y + 6f, inner, 28f), bareHanded ? "Tay không" : weapon.Name, _big);

            if (weapon.Kind == WeaponKind.Firearm)
            {
                GUI.Label(new Rect(x, panel.y + 6f, inner, 28f), weapons.AmmoInMagazine + "/" + weapon.MagazineSize, BigRight);
                string reserve = weapons.IsReloading ? "Đang nạp đạn..." : "Dự trữ trong balo: " + session.Backpack.CountOf(weapon.AmmoItemId);
                Label(new Rect(x, panel.y + 36f, inner, 18f), reserve, _small, weapons.IsReloading ? Gold : Dim);
            }
            else
            {
                Label(new Rect(x, panel.y + 36f, inner, 18f), bareHanded ? "Chưa có gì để tự vệ" : "Cận chiến · tốn thể lực · không gây tiếng động", _small, Dim);
            }

            // The three slots, with the one in hand highlighted.
            float slotWidth = inner / 3f;
            for (int slot = 0; slot < 3; slot++)
            {
                string name;
                if (slot == 0)
                {
                    name = session.HasKnife ? "Dao" : "—";
                }
                else
                {
                    var equipped = session.Loadout.GetWeapon(slot - 1);
                    name = equipped != null ? equipped.DisplayName : "—";
                }

                bool current = weapons.CurrentSlot == slot + 1;
                var cell = new Rect(x + slot * slotWidth, panel.y + 58f, slotWidth - 4f, 20f);
                Fill(cell, current ? new Color(0.95f, 0.85f, 0.4f, 0.25f) : new Color(1f, 1f, 1f, 0.06f));
                Label(new Rect(cell.x + 4f, cell.y + 1f, cell.width - 6f, 18f), (slot + 1) + " " + name, _small, current ? Gold : Dim);
            }
        }

        private GUIStyle _bigRight;

        private GUIStyle BigRight
        {
            get
            {
                if (_bigRight == null) _bigRight = new GUIStyle(_big) { alignment = TextAnchor.UpperRight };
                return _bigRight;
            }
        }

        private void DrawFooter(GameSession session, float height)
        {
            var panel = new Rect(Margin, height - Margin - 46f, 420f, 46f);
            Fill(panel, PanelColor);
            float x = panel.x + Pad;
            float inner = panel.width - Pad * 2f;

            string line = "Tem phiếu " + session.Wallet.Money + "   ·   Balo " + session.Backpack.Items.Count + " món (Tab)";
            if (!session.IsInRaid) line += "   ·   Bình tưới " + session.WateringCan.Water + "/" + session.WateringCan.Capacity;
            GUI.Label(new Rect(x, panel.y + 5f, inner, 18f), line, _text);
            Label(new Rect(x, panel.y + 24f, inner, 16f), "WASD đi · Shift chạy · Space lăn · R nạp đạn · Q hồi máu · E tương tác", _small, Dim);
        }

        /// <summary>Bottom centre, from the bottom up: what E does, the latest message, the item-use bar, the extraction countdown.</summary>
        private void DrawCentre(GameRoot root, float width, float height)
        {
            float y = height - 150f;

            if (Player.Focused != null)
            {
                string prompt = Player.Focused.Prompt;
                if (!string.IsNullOrEmpty(prompt)) y = CentredPanel(prompt, width, y, Color.white, 620f);
            }

            if (root.HasMessage) y = CentredPanel(root.Message, width, y, Gold, 760f);

            PlayerVitals vitals = PlayerVitals.Instance;
            if (vitals != null && vitals.IsUsing)
            {
                var bar = new Rect(width * 0.5f - 130f, y - 14f, 260f, 10f);
                Fill(bar, BarBack);
                Fill(new Rect(bar.x + 1f, bar.y + 1f, (bar.width - 2f) * vitals.UseProgress, bar.height - 2f), Gold);
                y = bar.y - 6f;
                y = CentredPanel("Đang dùng " + vitals.UsingName + "... (bấm chuột để hủy)", width, y, Color.white, 520f);
            }

            ExtractionZone zone = ExtractionZone.Active;
            if (zone != null && zone.Remaining >= 0f)
            {
                CentredPanel("Đang rút lui... " + zone.Remaining.ToString("0.0") + " giây", width, y, StaminaColor, 360f);
            }
        }

        /// <summary>A dark strip with centred text whose bottom edge is at <paramref name="bottom"/>. Returns the y above it.</summary>
        private float CentredPanel(string text, float width, float bottom, Color color, float maxWidth)
        {
            float panelWidth = Mathf.Min(maxWidth, width - 2f * Margin);
            float textHeight = _centered.CalcHeight(new GUIContent(text), panelWidth - Pad * 2f);
            var panel = new Rect((width - panelWidth) * 0.5f, bottom - textHeight - Pad, panelWidth, textHeight + Pad);
            Fill(panel, PanelColor);
            Label(new Rect(panel.x + Pad, panel.y + Pad * 0.5f, panel.width - Pad * 2f, textHeight), text, _centered, color);
            return panel.y - 6f;
        }

        /// <summary>While the inventory is open only the numbers needed to decide what to eat or use are shown, out of its way.</summary>
        private void DrawCompactVitals(GameSession session, Health health, float width)
        {
            string text = "Máu " + Mathf.CeilToInt(health.Current) + "/" + Mathf.CeilToInt(health.Max)
                + "  ·  Đói " + Mathf.CeilToInt(session.Needs.Hunger)
                + "  ·  Khát " + Mathf.CeilToInt(session.Needs.Thirst);
            var panel = new Rect(width - Margin - 250f, Margin, 250f, 24f);
            Fill(panel, PanelColor);
            GUI.Label(new Rect(panel.x + Pad, panel.y + 3f, panel.width - Pad * 2f, 18f), text, _small);
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

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Label(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(rect, text, style);
            GUI.contentColor = previous;
        }

        private void EnsureStyles()
        {
            if (_text != null) return;

            _text = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = false };
            _text.normal.textColor = Color.white;
            _text.clipping = TextClipping.Clip;

            _small = new GUIStyle(_text) { fontSize = 12 };
            _bold = new GUIStyle(_text) { fontSize = 14, fontStyle = FontStyle.Bold };
            _big = new GUIStyle(_text) { fontSize = 20, fontStyle = FontStyle.Bold };
            _right = new GUIStyle(_small) { alignment = TextAnchor.UpperRight };
            _centered = new GUIStyle(_text) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _centered.clipping = TextClipping.Overflow;
        }
    }
}
