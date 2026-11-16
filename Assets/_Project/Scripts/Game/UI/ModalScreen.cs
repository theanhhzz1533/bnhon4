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
        }

        private void OnGUI()
        {
            if (!IsOpen || GameRoot.Instance == null) return;
            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float width = Mathf.Min(920f, Screen.width - 40f);
            float height = Screen.height - 60f;
            var panel = new Rect((Screen.width - width) * 0.5f, 30f, width, height);

            GUI.color = new Color(0.1f, 0.11f, 0.13f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, panel.height - 24f));
            DrawContents();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Esc hoặc Tab để đóng", TextStyle);
            GUILayout.EndArea();

            // Lists are drawn from live collections; change them only after drawing is done.
            if (_pending != null)
            {
                System.Action action = _pending;
                _pending = null;
                action();
            }
        }

        protected abstract void DrawContents();

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

            TextStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            TextStyle.normal.textColor = Color.white;

            ButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }
    }
}
