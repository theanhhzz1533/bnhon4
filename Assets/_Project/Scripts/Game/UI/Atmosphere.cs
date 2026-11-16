using Bnhon.Core;
using Bnhon.Core.Npcs;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// A wash of colour over the view for the time of day: dusk at the base after a raid.
    /// A stand-in for real 2D lights, which need lit sprite materials set up in the editor.
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        private static readonly Color DuskWarm = new Color(1f, 0.5f, 0.15f, 0.10f);
        private static readonly Color DuskDark = new Color(0.02f, 0.03f, 0.12f, 0.22f);

        private float _dusk;

        private void Update()
        {
            if (GameRoot.Instance == null) return;
            GameSession session = GameRoot.Instance.Session;
            bool evening = !session.IsInRaid && session.Phase == DayPhase.Evening;
            _dusk = Mathf.MoveTowards(_dusk, evening ? 1f : 0f, UnityEngine.Time.unscaledDeltaTime * 1.5f);
        }

        private void OnGUI()
        {
            if (_dusk <= 0f) return;

            // A higher depth is drawn first, so the HUD and menus stay on top of the wash.
            GUI.depth = 100;
            var full = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(DuskDark.r, DuskDark.g, DuskDark.b, DuskDark.a * _dusk);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
            GUI.color = new Color(DuskWarm.r, DuskWarm.g, DuskWarm.b, DuskWarm.a * _dusk);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
