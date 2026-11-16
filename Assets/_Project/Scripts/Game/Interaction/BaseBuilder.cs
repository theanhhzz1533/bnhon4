using System.Collections.Generic;
using Bnhon.Core;
using Bnhon.Core.Layout;
using Bnhon.Game.Audio;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.Interaction
{
    /// <summary>
    /// Build mode for the base: B switches it on and off. Click a building to pick it up,
    /// move the mouse to carry it across the grid, click again to set it down where the
    /// outline is green. Right click or Esc puts it back.
    /// </summary>
    public sealed class BaseBuilder : MonoBehaviour
    {
        private static readonly Color Valid = new Color(0.3f, 0.9f, 0.4f, 0.4f);
        private static readonly Color Invalid = new Color(0.95f, 0.3f, 0.25f, 0.4f);
        private static readonly Color Hover = new Color(1f, 1f, 1f, 0.22f);

        private readonly Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();
        private PlayerController _player;
        private PlacedBuilding _held;
        private GameObject _outline;

        public static BaseBuilder Instance { get; private set; }

        /// <summary>True while build mode is on. Other screens stay closed meanwhile.</summary>
        public static bool Active { get; private set; }

        /// <summary>What to tell the player about build mode right now.</summary>
        public static string Hint { get; private set; } = string.Empty;

        /// <summary>World position of the bottom-left corner of a base cell. The yard is centred on the origin.</summary>
        public static Vector2 CellCorner(int x, int y)
        {
            return new Vector2(x - GameSession.BaseWidth * 0.5f, y - GameSession.BaseHeight * 0.5f);
        }

        public static Vector2 CellCenter(int x, int y)
        {
            return CellCorner(x, y) + new Vector2(0.5f, 0.5f);
        }

        /// <summary>World position of the middle of a building's footprint.</summary>
        public static Vector2 Center(BuildingType type, int x, int y)
        {
            return CellCorner(x, y) + new Vector2(type.Width * 0.5f, type.Height * 0.5f);
        }

        public void Init(PlayerController player)
        {
            _player = player;
        }

        /// <summary>Tells the builder which scene object shows a building.</summary>
        public void Register(string buildingId, GameObject building)
        {
            _objects[buildingId] = building;
        }

        private void Awake()
        {
            Instance = this;
            Active = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Active = false;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Camera cam = Camera.main;
            if (keyboard == null || mouse == null || cam == null || GameRoot.Instance == null || _player == null) return;

            GameSession session = GameRoot.Instance.Session;
            bool allowed = !session.IsInRaid && ModalScreen.Current == null
                && (InventoryScreen.Instance == null || !InventoryScreen.Instance.IsOpen);

            if (!Active)
            {
                if (allowed && keyboard.bKey.wasPressedThisFrame) Enter();
                return;
            }

            if (!allowed)
            {
                Leave();
                return;
            }

            if (keyboard.bKey.wasPressedThisFrame || (_held == null && keyboard.escapeKey.wasPressedThisFrame))
            {
                Leave();
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            int cellX = Mathf.FloorToInt(world.x + GameSession.BaseWidth * 0.5f);
            int cellY = Mathf.FloorToInt(world.y + GameSession.BaseHeight * 0.5f);

            if (_held == null) UpdateIdle(session.Base, mouse, cellX, cellY);
            else UpdateCarrying(session.Base, keyboard, mouse, cellX, cellY);
        }

        private void UpdateIdle(BaseLayout layout, Mouse mouse, int cellX, int cellY)
        {
            PlacedBuilding hovered = layout.At(cellX, cellY);
            if (hovered == null)
            {
                ShowOutline(false);
                Hint = "Chế độ xây dựng · bấm vào một công trình để nhấc lên · B hoặc Esc để thoát";
                return;
            }

            MoveOutline(hovered.Type, hovered.X, hovered.Y, Hover);
            Hint = hovered.Type.DisplayName + " · bấm chuột trái để nhấc lên";
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _held = hovered;
                Sfx.PlayUi(SoundId.LootOpen, 0.5f);
            }
        }

        private void UpdateCarrying(BaseLayout layout, Keyboard keyboard, Mouse mouse, int cellX, int cellY)
        {
            BuildingType type = _held.Type;
            // Carry it by its middle, so the outline sits under the pointer.
            int x = cellX - (type.Width - 1) / 2;
            int y = cellY - (type.Height - 1) / 2;

            bool valid = layout.CanPlace(type.Id, x, y) && !PlayerStandsIn(type, x, y);
            MoveOutline(type, x, y, valid ? Valid : Invalid);
            Hint = "Đang dời " + type.DisplayName + " · "
                + (valid ? "chuột trái để đặt" : "không đặt được ở đây") + " · chuột phải hoặc Esc để trả về chỗ cũ";

            if (mouse.rightButton.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                _held = null;
                return;
            }

            if (!valid || !mouse.leftButton.wasPressedThisFrame) return;

            layout.Move(type.Id, x, y);
            if (_objects.TryGetValue(type.Id, out GameObject building) && building != null)
            {
                Vector2 center = Center(type, x, y);
                building.transform.position = new Vector3(center.x, center.y, building.transform.position.z);
            }
            _held = null;
            GameRoot.Instance.Save();
            Sfx.PlayUi(SoundId.Reload, 0.5f);
        }

        /// <summary>A building may not be dropped on the player, or they would be stuck inside it.</summary>
        private bool PlayerStandsIn(BuildingType type, int x, int y)
        {
            Vector2 corner = CellCorner(x, y);
            Vector2 position = _player.transform.position;
            const float margin = 0.5f;
            return position.x > corner.x - margin && position.x < corner.x + type.Width + margin
                && position.y > corner.y - margin && position.y < corner.y + type.Height + margin;
        }

        private void Enter()
        {
            Active = true;
            _held = null;
            _player.ControlEnabled = false;
            Hint = string.Empty;
        }

        private void Leave()
        {
            Active = false;
            _held = null;
            _player.ControlEnabled = true;
            ShowOutline(false);
            Hint = string.Empty;
        }

        private void MoveOutline(BuildingType type, int x, int y, Color color)
        {
            if (_outline == null)
            {
                _outline = PlaceholderArt.Box("BuildOutline", Vector2.zero, Vector2.one, color, null, YSort.AboveEverything - 1);
            }

            Vector2 center = Center(type, x, y);
            _outline.transform.position = new Vector3(center.x, center.y, 0f);
            _outline.transform.localScale = new Vector3(type.Width, type.Height, 1f);
            _outline.GetComponent<SpriteRenderer>().color = color;
            ShowOutline(true);
        }

        private void ShowOutline(bool visible)
        {
            if (_outline != null && _outline.activeSelf != visible) _outline.SetActive(visible);
        }
    }
}
