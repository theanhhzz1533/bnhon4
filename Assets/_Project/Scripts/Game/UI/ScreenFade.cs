using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>Cuts to black and fades back in. Used when the player moves between the base and a raid.</summary>
    public sealed class ScreenFade : MonoBehaviour
    {
        private float _alpha;
        private float _seconds = 0.5f;

        public static ScreenFade Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Goes black now and fades back to the game over the given time.</summary>
        public static void Flash(float seconds = 0.5f)
        {
            if (Instance == null) return;
            Instance._alpha = 1f;
            Instance._seconds = Mathf.Max(0.05f, seconds);
        }

        private void Update()
        {
            if (_alpha > 0f) _alpha = Mathf.Max(0f, _alpha - UnityEngine.Time.unscaledDeltaTime / _seconds);
        }

        private void OnGUI()
        {
            if (_alpha <= 0f) return;

            // A lower depth is drawn later, so this covers the HUD and every menu.
            GUI.depth = -1000;
            GUI.color = new Color(0f, 0f, 0f, _alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
