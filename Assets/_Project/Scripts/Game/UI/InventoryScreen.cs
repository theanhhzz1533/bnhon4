using System.Collections.Generic;
using Bnhon.Core.Combat;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Core.Vitals;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// Grid inventory screen: backpack and secure pouch on the left, and on
    /// the right whatever container was opened (base stash or a loot crate).
    /// Left-drag moves an item, R rotates it while dragging, right-click
    /// sends it to the other side. Tab or Esc closes.
    /// Immediate-mode GUI for the prototype; replace with UI Toolkit in M2.
    /// </summary>
    public sealed class InventoryScreen : MonoBehaviour
    {
        private sealed class Panel
        {
            public GridInventory Grid;
            public string Title;
            public Rect Area;
        }

        private const int BackpackColumnCells = 6;

        private readonly List<Panel> _panels = new List<Panel>();
        private readonly Rect[] _weaponSlots = new Rect[Loadout.SlotCount];
        private GridInventory _other;
        private string _otherTitle;
        private IItemConcealer _concealer;
        private float _cell;

        private PlacedItem _dragItem;
        private GridInventory _dragSource;
        private static readonly System.Collections.Generic.Dictionary<string, Texture> Icons = new System.Collections.Generic.Dictionary<string, Texture>();
        private bool _dragRotated;
        private PlacedItem _hoverItem;
        private GridInventory _hoverGrid;
        private int _grabX;
        private int _grabY;

        private GUIStyle _titleStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _unknownStyle;

        public static InventoryScreen Instance { get; private set; }

        public PlayerController Player { get; set; }
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Opens the screen. Pass a container to show it next to the backpack, or null for the backpack alone.</summary>
        public void Open(GridInventory other, string otherTitle)
        {
            Open(other, otherTitle, null);
        }

        /// <summary>Opens a container whose items may still be unidentified (see <see cref="IItemConcealer"/>).</summary>
        public void Open(GridInventory other, string otherTitle, IItemConcealer concealer)
        {
            CancelDrag();
            _other = other;
            _otherTitle = otherTitle;
            _concealer = concealer;
            IsOpen = true;
            if (Player != null) Player.ControlEnabled = false;
        }

        /// <summary>True while the screen is open with <paramref name="grid"/> as the container on the right.</summary>
        public bool IsShowing(GridInventory grid)
        {
            return IsOpen && grid != null && _other == grid;
        }

        public void SetOtherTitle(string title)
        {
            _otherTitle = title;
        }

        public void Close()
        {
            if (!IsOpen) return;
            CancelDrag();
            _hoverItem = null;
            _hoverGrid = null;
            _other = null;
            _concealer = null;
            IsOpen = false;
            if (Player != null) Player.ControlEnabled = true;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // A workbench or shop menu owns Tab while it is open, and on the frame it closes.
            if (ModalScreen.Current != null || ModalScreen.ClosedFrame == UnityEngine.Time.frameCount) return;

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open(null, null);
                return;
            }

            if (!IsOpen) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (_dragItem == null && _hoverItem != null && keyboard.fKey.wasPressedThisFrame)
            {
                if (_hoverGrid.Contains(_hoverItem))
                {
                    // F on a weapon equips it; on anything else it uses it.
                    if (_hoverItem.Item.Category == ItemCategory.Weapon)
                    {
                        if (Player != null && Player.Weapons != null) Player.Weapons.EquipFrom(_hoverGrid, _hoverItem);
                    }
                    else if (PlayerVitals.Instance != null)
                    {
                        PlayerVitals.Instance.UseItem(_hoverGrid, _hoverItem);
                    }
                }
                _hoverItem = null;
                _hoverGrid = null;
                return;
            }

            if (_dragItem != null && keyboard.rKey.wasPressedThisFrame && _dragItem.Item.Width != _dragItem.Item.Height)
            {
                _dragRotated = !_dragRotated;
                _grabX = 0;
                _grabY = 0;
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || GameRoot.Instance == null) return;

            EnsureStyles();
            BuildLayout();

            Event current = Event.current;
            Vector2 mouse = current.mousePosition;

            if (current.type == EventType.MouseDown)
            {
                HandleMouseDown(current, mouse);
            }
            else if (current.type == EventType.MouseUp && current.button == 0 && _dragItem != null)
            {
                Drop(mouse);
                current.Use();
            }

            if (current.type != EventType.Repaint) return;

            Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.65f));

            PlacedItem hovered = null;
            GridInventory hoveredGrid = null;
            foreach (Panel panel in _panels)
            {
                DrawPanel(panel);
                if (_dragItem == null && TryGetCell(panel, mouse, out int cx, out int cy))
                {
                    PlacedItem under = panel.Grid.GetAt(cx, cy);
                    if (under != null && !IsConcealed(panel.Grid, under))
                    {
                        hovered = under;
                        hoveredGrid = panel.Grid;
                    }
                }
            }

            _hoverItem = hovered;
            _hoverGrid = hoveredGrid;

            DrawWeaponSlots(mouse);
            if (_dragItem != null) DrawDragGhost(mouse);

            int hoverSlot = _dragItem == null ? WeaponSlotAt(mouse) : -1;
            string hint = hovered != null
                ? Describe(hovered)
                : hoverSlot >= 0
                ? "Ô vũ khí " + (hoverSlot + 2) + ": kéo một khẩu súng vào đây để trang bị · bấm vào súng đang trang bị để cất về balo"
                : "Kéo chuột trái: di chuyển · R: xoay khi đang kéo · Chuột phải: chuyển nhanh · F: dùng/trang bị · Tab/Esc: đóng";
            GUI.Label(new Rect(0f, Screen.height - _cell * 1.1f, Screen.width, _cell), hint, _hintStyle);
        }

        private void BuildLayout()
        {
            var session = GameRoot.Instance.Session;
            int otherColumns = _other != null ? _other.Width : 0;
            int otherRows = _other != null ? _other.Height : 0;

            // Rows needed: title + backpack + title + pouch on the left, title + container on the right.
            float leftRows = 0.8f + session.Backpack.Height + 0.9f + session.SecurePouch.Height;
            float rightRows = _other != null ? 0.8f + otherRows : 0f;
            float columns = BackpackColumnCells + (_other != null ? otherColumns + 1 : 0);
            float rows = Mathf.Max(leftRows, rightRows);

            _cell = Mathf.Floor(Mathf.Clamp(Mathf.Min(Screen.width / (columns + 2f), Screen.height / (rows + 2.6f)), 16f, 52f));
            _titleStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(_cell * 0.36f, 10f, 17f));
            _hintStyle.fontSize = _titleStyle.fontSize;
            _unknownStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(_cell * 0.5f, 10f, 24f));
            _itemStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(_cell * 0.27f, 8f, 13f));

            float x0 = Mathf.Floor((Screen.width - columns * _cell) * 0.5f);
            float y0 = Mathf.Floor(_cell * 0.6f);
            float titleHeight = _cell * 0.8f;

            _panels.Clear();

            float backpackY = y0 + titleHeight;
            AddPanel(session.Backpack, "Balo", x0, backpackY);

            float pouchY = backpackY + session.Backpack.Height * _cell + _cell * 0.9f;
            AddPanel(session.SecurePouch, "Túi an toàn", x0, pouchY);

            if (_other != null)
            {
                AddPanel(_other, _otherTitle, x0 + (BackpackColumnCells + 1) * _cell, backpackY);
            }

            // Two firearm slots, stacked to the right of the secure pouch.
            float slotX = x0 + (session.SecurePouch.Width + 0.5f) * _cell;
            float slotWidth = (BackpackColumnCells - session.SecurePouch.Width - 0.5f) * _cell;
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                _weaponSlots[i] = new Rect(slotX, pouchY + i * _cell, slotWidth, _cell - 2f);
            }
        }

        private int WeaponSlotAt(Vector2 mouse)
        {
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                if (_weaponSlots[i].Contains(mouse)) return i;
            }
            return -1;
        }

        private void DrawWeaponSlots(Vector2 mouse)
        {
            var session = GameRoot.Instance.Session;
            PlayerWeapons weapons = Player != null ? Player.Weapons : null;
            bool draggingWeapon = _dragItem != null && _dragItem.Item.Category == ItemCategory.Weapon;

            Rect first = _weaponSlots[0];
            GUI.Label(new Rect(first.x, first.y - _cell * 0.8f, first.width, _cell * 0.8f), "Vũ khí (mất khi chết)", _titleStyle);

            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                Rect rect = _weaponSlots[i];
                ItemDefinition weapon = session.Loadout.GetWeapon(i);
                bool hot = rect.Contains(mouse);

                Color frame = draggingWeapon && hot ? new Color(0.3f, 0.9f, 0.4f, 1f) : new Color(0.05f, 0.05f, 0.06f, 0.95f);
                Fill(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), frame);
                Fill(rect, weapon != null ? CategoryColor(ItemCategory.Weapon) : new Color(0.2f, 0.21f, 0.23f, 1f));

                string label = (i + 2) + ": ";
                if (weapon == null)
                {
                    label += "(trống)";
                }
                else
                {
                    WeaponDefinition definition = weapons != null ? weapons.DefinitionFor(weapon) : null;
                    label += weapon.DisplayName;
                    if (definition != null) label += "  " + session.Loadout.GetLoaded(i) + "/" + definition.MagazineSize;
                }
                GUI.Label(new Rect(rect.x + 6f, rect.y, rect.width - 8f, rect.height), label, _titleStyle);

                Texture icon = weapon != null ? IconFor(weapon) : null;
                if (icon != null) DrawIcon(new Rect(rect.xMax - rect.height * 2.2f, rect.y + 2f, rect.height * 2.1f, rect.height - 4f), icon, false);
            }
        }

        private void AddPanel(GridInventory grid, string title, float x, float y)
        {
            _panels.Add(new Panel
            {
                Grid = grid,
                Title = title,
                Area = new Rect(x, y, grid.Width * _cell, grid.Height * _cell)
            });
        }

        private void HandleMouseDown(Event current, Vector2 mouse)
        {
            // A drag that lost its mouse-up (released outside the window) ends on the next click.
            if (_dragItem != null)
            {
                Drop(mouse);
                current.Use();
                return;
            }

            int slot = WeaponSlotAt(mouse);
            if (slot >= 0)
            {
                // Clicking an equipped weapon puts it back in the backpack.
                if (current.button == 0 && Player != null && Player.Weapons != null) Player.Weapons.Unequip(slot);
                current.Use();
                return;
            }

            foreach (Panel panel in _panels)
            {
                if (!TryGetCell(panel, mouse, out int cx, out int cy)) continue;

                PlacedItem item = panel.Grid.GetAt(cx, cy);
                if (item == null || IsConcealed(panel.Grid, item)) return;

                if (current.button == 0)
                {
                    _dragItem = item;
                    _dragSource = panel.Grid;
                    _dragRotated = item.Rotated;
                    _grabX = cx - item.X;
                    _grabY = cy - item.Y;
                }
                else if (current.button == 1)
                {
                    QuickTransfer(panel.Grid, item);
                }

                current.Use();
                return;
            }
        }

        private void QuickTransfer(GridInventory source, PlacedItem item)
        {
            var session = GameRoot.Instance.Session;
            GridInventory target;
            if (source == session.Backpack) target = _other != null ? _other : session.SecurePouch;
            else target = session.Backpack;

            if (source.TransferTo(item, target) == 0) GameRoot.Instance.Notify("Không đủ chỗ.", 1.5f);
        }

        private void Drop(Vector2 mouse)
        {
            int slot = WeaponSlotAt(mouse);
            if (slot >= 0)
            {
                if (_dragItem.Item.Category == ItemCategory.Weapon && Player != null && Player.Weapons != null)
                {
                    Player.Weapons.EquipFrom(_dragSource, _dragItem, slot);
                }
                CancelDrag();
                return;
            }

            foreach (Panel panel in _panels)
            {
                if (!TryGetCell(panel, mouse, out int cx, out int cy)) continue;
                int x = cx - _grabX;
                int y = cy - _grabY;
                // Dropping onto an unidentified stack would merge into it and give away what it is.
                if (!IsConcealed(panel.Grid, panel.Grid.GetAt(x, y)))
                {
                    _dragSource.MoveTo(_dragItem, panel.Grid, x, y, _dragRotated);
                }
                break;
            }
            CancelDrag();
        }

        private void CancelDrag()
        {
            _dragItem = null;
            _dragSource = null;
        }

        private bool TryGetCell(Panel panel, Vector2 mouse, out int cx, out int cy)
        {
            cx = 0;
            cy = 0;
            if (!panel.Area.Contains(mouse)) return false;

            cx = Mathf.Min(panel.Grid.Width - 1, (int)((mouse.x - panel.Area.x) / _cell));
            cy = Mathf.Min(panel.Grid.Height - 1, (int)((mouse.y - panel.Area.y) / _cell));
            return true;
        }

        private void DrawPanel(Panel panel)
        {
            Rect area = panel.Area;
            GUI.Label(new Rect(area.x, area.y - _cell * 0.8f, Mathf.Max(area.width, _cell * 8f), _cell * 0.8f), panel.Title, _titleStyle);

            Fill(new Rect(area.x - 2f, area.y - 2f, area.width + 4f, area.height + 4f), new Color(0.05f, 0.05f, 0.06f, 0.95f));
            var cellColor = new Color(0.2f, 0.21f, 0.23f, 1f);
            for (int x = 0; x < panel.Grid.Width; x++)
            {
                for (int y = 0; y < panel.Grid.Height; y++)
                {
                    Fill(new Rect(area.x + x * _cell + 1f, area.y + y * _cell + 1f, _cell - 2f, _cell - 2f), cellColor);
                }
            }

            foreach (PlacedItem item in panel.Grid.Items)
            {
                var rect = new Rect(
                    area.x + item.X * _cell + 1f,
                    area.y + item.Y * _cell + 1f,
                    item.Width * _cell - 2f,
                    item.Height * _cell - 2f);
                if (IsConcealed(panel.Grid, item)) DrawUnknown(rect, _concealer.RevealProgress(item));
                else DrawItem(rect, item, item == _dragItem ? 0.3f : 1f);
            }
        }

        private bool IsConcealed(GridInventory grid, PlacedItem item)
        {
            return item != null && _concealer != null && grid == _other && _concealer.IsHidden(item);
        }

        /// <summary>An item that has not been identified yet: a dim block of the right size, with a bar while it is being searched.</summary>
        private void DrawUnknown(Rect rect, float progress)
        {
            Fill(rect, new Color(0.34f, 0.35f, 0.38f, 0.55f));
            Fill(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), new Color(0.13f, 0.14f, 0.16f, 0.9f));

            GUI.color = new Color(1f, 1f, 1f, progress > 0f ? 0.8f : 0.35f);
            GUI.Label(rect, "?", _unknownStyle);
            GUI.color = Color.white;

            if (progress <= 0f) return;
            float barHeight = Mathf.Max(3f, _cell * 0.1f);
            var bar = new Rect(rect.x + 3f, rect.y + rect.height - barHeight - 3f, rect.width - 6f, barHeight);
            Fill(bar, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(bar.x, bar.y, bar.width * progress, bar.height), new Color(0.95f, 0.85f, 0.4f, 1f));
        }

        private void DrawItem(Rect rect, PlacedItem item, float alpha)
        {
            Color rarity = RarityColor(item.Item.Rarity);
            rarity.a = alpha;
            Fill(rect, rarity);

            Color body = CategoryColor(item.Item.Category);
            body.a = alpha;
            Fill(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), body);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            Texture icon = IconFor(item.Item);
            if (icon == null)
            {
                string label = item.Count > 1 ? item.Item.DisplayName + "\nx" + item.Count : item.Item.DisplayName;
                GUI.Label(new Rect(rect.x + 3f, rect.y + 2f, rect.width - 6f, rect.height - 4f), label, _itemStyle);
            }
            else
            {
                // With a picture, the cell shows only the picture and the stack size; the name is in the hint line.
                DrawIcon(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), icon, item.Rotated);
                if (item.Count > 1)
                {
                    var corner = new Rect(rect.x + 3f, rect.y + rect.height - _cell * 0.42f, rect.width - 6f, _cell * 0.4f);
                    GUI.color = new Color(0f, 0f, 0f, alpha);
                    GUI.Label(new Rect(corner.x + 1f, corner.y + 1f, corner.width, corner.height), "x" + item.Count, _itemStyle);
                    GUI.color = new Color(1f, 1f, 1f, alpha);
                    GUI.Label(corner, "x" + item.Count, _itemStyle);
                }
            }
            GUI.color = Color.white;
        }

        /// <summary>The inventory picture for an item (Resources/Sprites/item_&lt;id&gt;.png), or null to fall back to its name.</summary>
        private static Texture IconFor(ItemDefinition item)
        {
            if (Icons.TryGetValue(item.Id, out Texture known)) return known;

            Sprite sprite = PlaceholderArt.FindSprite("item_" + item.Id);
            Texture icon = sprite != null ? sprite.texture : null;
            Icons[item.Id] = icon;
            return icon;
        }

        /// <summary>Draws a picture as large as fits in the rectangle, turned a quarter when the item lies rotated.</summary>
        private static void DrawIcon(Rect rect, Texture icon, bool rotated)
        {
            if (!rotated)
            {
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
                return;
            }

            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(90f, rect.center);
            var turned = new Rect(rect.center.x - rect.height * 0.5f, rect.center.y - rect.width * 0.5f, rect.height, rect.width);
            GUI.DrawTexture(turned, icon, ScaleMode.ScaleToFit);
            GUI.matrix = previous;
        }

        private void DrawDragGhost(Vector2 mouse)
        {
            int width = _dragRotated ? _dragItem.Item.Height : _dragItem.Item.Width;
            int height = _dragRotated ? _dragItem.Item.Width : _dragItem.Item.Height;

            bool overGrid = false;
            bool valid = false;
            var rect = new Rect(mouse.x - (_grabX + 0.5f) * _cell, mouse.y - (_grabY + 0.5f) * _cell, width * _cell - 2f, height * _cell - 2f);

            foreach (Panel panel in _panels)
            {
                if (!TryGetCell(panel, mouse, out int cx, out int cy)) continue;

                int x = cx - _grabX;
                int y = cy - _grabY;
                overGrid = true;
                valid = !IsConcealed(panel.Grid, panel.Grid.GetAt(x, y))
                    && _dragSource.CanMoveTo(_dragItem, panel.Grid, x, y, _dragRotated);
                // Snap the ghost to the cells it would occupy.
                rect.x = panel.Area.x + x * _cell + 1f;
                rect.y = panel.Area.y + y * _cell + 1f;
                break;
            }

            if (overGrid)
            {
                Color tint = valid ? new Color(0.3f, 0.9f, 0.4f, 0.35f) : new Color(0.95f, 0.25f, 0.25f, 0.4f);
                Fill(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), tint);
            }
            DrawItem(rect, _dragItem, 0.85f);
        }

        private static string Describe(PlacedItem item)
        {
            ItemDefinition definition = item.Item;
            return definition.DisplayName + " x" + item.Count
                + " · " + CategoryName(definition.Category)
                + " · " + definition.Width + "x" + definition.Height + " ô"
                + " · giá trị " + definition.BaseValue * item.Count
                + EffectText(definition);
        }

        private static string EffectText(ItemDefinition definition)
        {
            if (definition.Category == ItemCategory.Weapon) return " · F để trang bị (hoặc kéo vào ô vũ khí)";
            if (definition.Category == ItemCategory.Blueprint) return " · mang về đăng ký ở bàn chế tạo";

            ConsumableDefinition effect = PrototypeContent.FindConsumable(definition.Id);
            if (effect == null) return string.Empty;

            string text = " · F để dùng:";
            if (effect.Heal > 0f) text += " +" + effect.Heal + " máu";
            if (effect.Hunger > 0f) text += " +" + effect.Hunger + " no";
            if (effect.Thirst > 0f) text += " +" + effect.Thirst + " nước";
            return text;
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _titleStyle.normal.textColor = Color.white;

            _itemStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true, clipping = TextClipping.Clip };
            _itemStyle.normal.textColor = Color.white;
            _itemStyle.padding = new RectOffset(0, 0, 0, 0);

            _unknownStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _unknownStyle.normal.textColor = Color.white;

            _hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        }

        private static Color CategoryColor(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Material: return new Color(0.42f, 0.36f, 0.3f);
                case ItemCategory.Food: return new Color(0.62f, 0.42f, 0.2f);
                case ItemCategory.Medicine: return new Color(0.62f, 0.25f, 0.28f);
                case ItemCategory.Weapon: return new Color(0.3f, 0.32f, 0.38f);
                case ItemCategory.Ammo: return new Color(0.5f, 0.45f, 0.2f);
                case ItemCategory.Seed: return new Color(0.3f, 0.45f, 0.25f);
                case ItemCategory.Produce: return new Color(0.35f, 0.55f, 0.25f);
                case ItemCategory.Gift: return new Color(0.55f, 0.32f, 0.55f);
                case ItemCategory.Blueprint: return new Color(0.2f, 0.4f, 0.7f);
                default: return new Color(0.3f, 0.4f, 0.5f);
            }
        }

        private static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return new Color(0.35f, 0.8f, 0.4f);
                case ItemRarity.Rare: return new Color(0.3f, 0.55f, 0.95f);
                case ItemRarity.Epic: return new Color(0.7f, 0.4f, 0.95f);
                case ItemRarity.Legendary: return new Color(0.98f, 0.7f, 0.2f);
                default: return new Color(0.6f, 0.6f, 0.62f);
            }
        }

        private static string CategoryName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Material: return "Vật liệu";
                case ItemCategory.Food: return "Thức ăn";
                case ItemCategory.Medicine: return "Thuốc";
                case ItemCategory.Weapon: return "Vũ khí";
                case ItemCategory.Ammo: return "Đạn";
                case ItemCategory.Seed: return "Hạt giống";
                case ItemCategory.Produce: return "Nông sản";
                case ItemCategory.Gift: return "Quà tặng";
                case ItemCategory.Key: return "Chìa khóa";
                case ItemCategory.Blueprint: return "Bản vẽ";
                default: return "Vật phẩm nhiệm vụ";
            }
        }
    }
}
