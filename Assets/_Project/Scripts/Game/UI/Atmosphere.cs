using Bnhon.Core;
using Bnhon.Core.Npcs;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// The mood of the picture, laid over the game view under the HUD: a cold grey-blue cast
    /// and darkened edges outside, dust drifting in the air, and dusk at the base after a
    /// raid. A stand-in for colour grading and 2D lights, which need setting up in the editor.
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        private static readonly Color RaidCold = new Color(0.12f, 0.17f, 0.22f, 0.22f);
        private static readonly Color DuskWarm = new Color(1f, 0.5f, 0.15f, 0.10f);
        private static readonly Color DuskDark = new Color(0.02f, 0.03f, 0.12f, 0.22f);

        private const int DustCount = 70;

        private Texture2D _vignette;
        private readonly Vector2[] _dust = new Vector2[DustCount];
        private readonly Vector2[] _dustVelocity = new Vector2[DustCount];
        private readonly float[] _dustSize = new float[DustCount];
        private readonly float[] _dustPhase = new float[DustCount];

        private float _raid;
        private float _dusk;

        private void Start()
        {
            _vignette = MakeVignette(128);
            for (int i = 0; i < DustCount; i++) ResetDust(i, true);
        }

        private void OnDestroy()
        {
            if (_vignette != null) Destroy(_vignette);
        }

        private void Update()
        {
            if (GameRoot.Instance == null) return;
            GameSession session = GameRoot.Instance.Session;
            bool evening = !session.IsInRaid && session.Phase == DayPhase.Evening;
            float step = UnityEngine.Time.unscaledDeltaTime * 1.5f;
            _raid = Mathf.MoveTowards(_raid, session.IsInRaid ? 1f : 0f, step);
            _dusk = Mathf.MoveTowards(_dusk, evening ? 1f : 0f, step);

            float delta = UnityEngine.Time.deltaTime;
            for (int i = 0; i < DustCount; i++)
            {
                _dust[i] += _dustVelocity[i] * delta;
                if (_dust[i].x < -0.05f || _dust[i].x > 1.05f || _dust[i].y < -0.05f || _dust[i].y > 1.05f) ResetDust(i, false);
            }
        }

        private void ResetDust(int i, bool anywhere)
        {
            _dust[i] = anywhere ? new Vector2(Random.value, Random.value) : new Vector2(-0.04f, Random.value);
            _dustVelocity[i] = new Vector2(Random.Range(0.006f, 0.02f), Random.Range(-0.006f, 0.004f));
            _dustSize[i] = Random.Range(1.5f, 3.5f);
            _dustPhase[i] = Random.value * 10f;
        }

        private void OnGUI()
        {
            if (GameRoot.Instance == null) return;

            // A higher depth is drawn first, so the HUD and menus stay on top of all this.
            GUI.depth = 100;
            var full = new Rect(0f, 0f, Screen.width, Screen.height);

            if (_raid > 0f)
            {
                Draw(full, Texture2D.whiteTexture, new Color(RaidCold.r, RaidCold.g, RaidCold.b, RaidCold.a * _raid));
                float scale = Screen.height / 720f;
                float now = UnityEngine.Time.time;
                for (int i = 0; i < DustCount; i++)
                {
                    float alpha = (0.1f + 0.1f * Mathf.Sin(now * 0.8f + _dustPhase[i])) * _raid;
                    float size = _dustSize[i] * scale;
                    var rect = new Rect(_dust[i].x * Screen.width, _dust[i].y * Screen.height, size, size);
                    Draw(rect, Texture2D.whiteTexture, new Color(0.85f, 0.82f, 0.75f, alpha));
                }
            }

            if (_dusk > 0f)
            {
                Draw(full, Texture2D.whiteTexture, new Color(DuskDark.r, DuskDark.g, DuskDark.b, DuskDark.a * _dusk));
                Draw(full, Texture2D.whiteTexture, new Color(DuskWarm.r, DuskWarm.g, DuskWarm.b, DuskWarm.a * _dusk));
            }

            // Darker edges everywhere, darker still outside the base.
            if (_vignette != null) Draw(full, _vignette, new Color(0f, 0f, 0f, 0.45f + 0.3f * _raid));
        }

        private static void Draw(Rect rect, Texture texture, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, texture);
            GUI.color = Color.white;
        }

        /// <summary>Clear in the middle, opaque at the corners. Its colour comes from GUI.color.</summary>
        private static Texture2D MakeVignette(int size)
        {
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / 1.41421f;
                    float alpha = Mathf.Clamp01((distance - 0.45f) / 0.55f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
