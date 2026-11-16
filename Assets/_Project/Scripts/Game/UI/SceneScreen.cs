using System;
using System.Collections.Generic;
using Bnhon.Core.Npcs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// Plays a short story scene: narration and spoken lines shown one at a time, with what
    /// was already said left on screen. Closing it early skips to the end.
    /// </summary>
    public sealed class SceneScreen : ModalScreen
    {
        private static readonly Color Dim = new Color(0.72f, 0.72f, 0.75f);

        private IReadOnlyList<SceneLine> _lines;
        private string _title;
        private int _shown;
        private Action _onDone;

        public static SceneScreen Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        public void Play(string title, IReadOnlyList<SceneLine> lines, Action onDone = null)
        {
            if (lines == null || lines.Count == 0)
            {
                if (onDone != null) onDone();
                return;
            }

            _title = title;
            _lines = lines;
            _shown = 0;
            _onDone = onDone;
            Open();
        }

        protected override void OnClosed()
        {
            Action done = _onDone;
            _onDone = null;
            if (done != null) done();
        }

        /// <summary>A dialogue box across the bottom of the view, so the game stays visible behind it.</summary>
        protected override Rect PanelRect(float width, float height)
        {
            float panelWidth = Mathf.Min(860f, width - 40f);
            const float panelHeight = 210f;
            return new Rect((width - panelWidth) * 0.5f, height - panelHeight - 24f, panelWidth, panelHeight);
        }

        protected override float BackdropAlpha => 0.35f;

        protected override string FooterHint => "Space hoặc Enter: tiếp   ·   Esc: bỏ qua";

        protected override void OnKeys(Keyboard keyboard)
        {
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) Advance();
        }

        private void Advance()
        {
            if (_lines == null || _shown >= _lines.Count - 1) Close();
            else _shown++;
        }

        protected override void DrawContents()
        {
            if (_lines == null) return;
            SceneLine line = _lines[Mathf.Min(_shown, _lines.Count - 1)];

            GUILayout.BeginHorizontal();
            GUILayout.Label(line.Speaker ?? _title ?? string.Empty, HeaderStyle);
            GUILayout.FlexibleSpace();
            ColoredLabel((_shown + 1) + "/" + _lines.Count, Dim);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            if (line.Speaker == null) ColoredLabel(line.Text, Dim);
            else GUILayout.Label(line.Text, TextStyle);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool last = _shown >= _lines.Count - 1;
            if (Button(last ? "Xong" : "Tiếp", true, 110f)) Defer(Advance);
            GUILayout.EndHorizontal();
        }
    }
}
