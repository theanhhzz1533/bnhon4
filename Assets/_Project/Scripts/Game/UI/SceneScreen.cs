using System;
using System.Collections.Generic;
using Bnhon.Core.Npcs;
using UnityEngine;

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

        protected override void DrawContents()
        {
            if (_lines == null) return;

            GUILayout.Label(_title ?? string.Empty, TitleStyle);
            GUILayout.Space(10f);

            for (int i = 0; i <= _shown && i < _lines.Count; i++)
            {
                SceneLine line = _lines[i];
                if (line.Speaker == null) ColoredLabel(line.Text, Dim);
                else GUILayout.Label(line.Speaker + ": " + line.Text, TextStyle);
                GUILayout.Space(6f);
            }

            bool last = _shown >= _lines.Count - 1;
            if (Button(last ? "Xong" : "Tiếp", true, 110f))
            {
                if (last) Defer(Close);
                else Defer(() => _shown++);
            }
        }
    }
}
