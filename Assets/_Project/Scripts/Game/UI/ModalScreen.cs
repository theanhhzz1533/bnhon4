using Bnhon.Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// Base for full-screen menus opened from an object in the base (workbench, shop).
    /// Only one is open at a time, the player cannot move or act while it is, and Esc or
    /// Tab closes it. Subclasses draw their contents with GUILayout inside the panel.
    /// Immediate-mode GUI for the prototype; replace with UI Toolkit later in M2.
    /// </summary>
    public abstract class ModalScreen : MonoBehaviour
    {
        private int _openedFrame;
        private System.Action _pending;

        protected GUIStyle TitleStyle;
        protected GUIStyle HeaderStyle;
        protected GUIStyle TextStyle;
        protected GUIStyle ButtonStyle;

        public static ModalScreen Current { get; private set; }

        /// <summary>The frame on which a menu last closed, so the same key press does not open something else.</summary>
        public static int ClosedFrame { get; private set; } = -1;

        public PlayerController Player { get; set; }
        public bool IsOpen => Current == this;

        public void Open()
        {
            if (Current != null && Current != this) Current.Close();
            if (InventoryScreen.Instance != null) InventoryScreen.Instance.Close();

            Current = this;
            _openedFrame = UnityEngine.Time.frameCount;
            if (Player != null) Player.ControlEnabled = false;
        }

        public void Close()
        {
            if (Current != this) return;
            Current = null;
            ClosedFrame = UnityEngine.Time.frameCount;
            if (Player != null) Player.ControlEnabled = true;
            OnClosed();
        }

        /// <summary>Called after the menu has closed, however it was closed.</summary>
        protected virtual void OnClosed()
        {
        }

        protected virtual void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (!IsOpen || UnityEngine.Time.frameCount == _openedFrame) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame) Close();
            else OnKeys(keyboard);
        }

        /// <summary>Called every frame the menu is open, for menus with their own keys.</summary>
        protected virtual void OnKeys(Keyboard keyboard)
        {
        }

        /// <summary>Where the panel sits, in layout units (see <see cref="UiScale"/>). The default nearly fills the window.</summary>
        protected virtual Rect PanelRect(float width, float height)
        {
            float panelWidth = Mathf.Min(920f, width - 40f);
            return new Rect((width - panelWidth) * 0.5f, 30f, panelWidth, height - 60f);
        }

        /// <summary>How dark the game behind the panel gets.</summary>
        protected virtual float BackdropAlpha => 0.7f;

        private void OnGUI()
        {
            if (!IsOpen || GameRoot.Instance == null) return;
            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, BackdropAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            Matrix4x4 previous = UiScale.Apply();
            Rect panel = PanelRect(UiScale.Width, UiScale.Height);

            GUI.color = new Color(0.1f, 0.11f, 0.13f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.85f, 0.4f, 0.9f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 36f, panel.height - 26f));
            DrawContents();
            GUILayout.FlexibleSpace();
            // The HUD is hidden behind a menu, so the latest message is shown here instead.
            if (GameRoot.Instance.HasMessage) ColoredLabel(GameRoot.Instance.Message, new Color(0.95f, 0.85f, 0.4f));
            ColoredLabel(FooterHint, new Color(0.6f, 0.6f, 0.63f));
            GUILayout.EndArea();
            GUI.matrix = previous;

            // Lists are drawn from live collections; change them only after drawing is done.
            if (_pending != null)
            {
                System.Action action = _pending;
                _pending = null;
                action();
            }
        }

        protected abstract void DrawContents();

        /// <summary>The line at the bottom of the panel saying how to leave.</summary>
        protected virtual string FooterHint => "Esc hoặc Tab để đóng";

        /// <summary>Runs after this frame's drawing. Use it for anything a button does to the lists being shown.</summary>
        protected void Defer(System.Action action)
        {
            _pending = action;
        }

        /// <summary>A button that can be greyed out.</summary>
        protected bool Button(string label, bool enabled, float width)
        {
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            bool clicked = GUILayout.Button(label, ButtonStyle, GUILayout.Width(width));
            GUI.enabled = previous;
            return clicked;
        }

        protected void ColoredLabel(string text, Color color, params GUILayoutOption[] options)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(text, TextStyle, options);
            GUI.contentColor = previous;
        }

        private void EnsureStyles()
        {
            if (TitleStyle != null) return;

            TitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            TitleStyle.normal.textColor = Color.white;

            HeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            HeaderStyle.normal.textColor = new Color(0.95f, 0.85f, 0.4f);

            TextStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            TextStyle.normal.textColor = Color.white;

            ButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }
    }
}
