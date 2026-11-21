using Bnhon.Core;
using Bnhon.Core.Npcs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bnhon.Game.World
{
    /// <summary>
    /// The colour of the picture, done with URP post-processing: warm daylight at the base,
    /// a duller and harder look outside, dusk in the evening, plus bloom and a soft vignette.
    /// Built in code, so nothing needs setting up in the editor.
    /// </summary>
    public sealed class Look : MonoBehaviour
    {
        private struct Preset
        {
            public float Temperature, Saturation, Contrast, Exposure, Vignette;
            public Color Filter;

            public static Preset Lerp(Preset a, Preset b, float t)
            {
                return new Preset
                {
                    Temperature = Mathf.Lerp(a.Temperature, b.Temperature, t),
                    Saturation = Mathf.Lerp(a.Saturation, b.Saturation, t),
                    Contrast = Mathf.Lerp(a.Contrast, b.Contrast, t),
                    Exposure = Mathf.Lerp(a.Exposure, b.Exposure, t),
                    Vignette = Mathf.Lerp(a.Vignette, b.Vignette, t),
                    Filter = Color.Lerp(a.Filter, b.Filter, t)
                };
            }
        }

        private static readonly Preset Day = new Preset { Temperature = 14f, Saturation = 12f, Contrast = 10f, Exposure = 0.15f, Vignette = 0.22f, Filter = Color.white };
        private static readonly Preset Raid = new Preset { Temperature = 8f, Saturation = -8f, Contrast = 16f, Exposure = 0f, Vignette = 0.32f, Filter = new Color(1f, 0.97f, 0.92f) };
        private static readonly Preset Dusk = new Preset { Temperature = 40f, Saturation = 0f, Contrast = 12f, Exposure = -0.45f, Vignette = 0.4f, Filter = new Color(1f, 0.82f, 0.72f) };

        private ColorAdjustments _colour;
        private WhiteBalance _white;
        private Vignette _vignette;
        private float _raid;
        private float _dusk;

        public static void SetUp(Camera camera)
        {
            // Post-processing is off on a camera made in code until asked for.
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            new GameObject("Look").AddComponent<Look>();
        }

        private void Awake()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.85f;
            bloom.intensity.value = 0.6f;
            bloom.scatter.value = 0.7f;

            _colour = profile.Add<ColorAdjustments>(true);
            _white = profile.Add<WhiteBalance>(true);
            _vignette = profile.Add<Vignette>(true);
            _vignette.smoothness.value = 0.45f;
            _vignette.color.value = new Color(0.05f, 0.03f, 0.02f);

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
            Apply(Day);
        }

        private void Update()
        {
            if (GameRoot.Instance == null) return;
            GameSession session = GameRoot.Instance.Session;
            bool evening = !session.IsInRaid && session.Phase == DayPhase.Evening;
            float step = UnityEngine.Time.unscaledDeltaTime * 1.5f;
            _raid = Mathf.MoveTowards(_raid, session.IsInRaid ? 1f : 0f, step);
            _dusk = Mathf.MoveTowards(_dusk, evening ? 1f : 0f, step);
            Apply(Preset.Lerp(Preset.Lerp(Day, Raid, _raid), Dusk, _dusk));
        }

        private void Apply(Preset preset)
        {
            _white.temperature.value = preset.Temperature;
            _colour.saturation.value = preset.Saturation;
            _colour.contrast.value = preset.Contrast;
            _colour.postExposure.value = preset.Exposure;
            _colour.colorFilter.value = preset.Filter;
            _vignette.intensity.value = preset.Vignette;
        }
    }

    /// <summary>Soft shadows on the ground: under people, and along the foot of walls.</summary>
    public static class Shadows
    {
        private static Sprite _face;
        private static Sprite _strip;

        /// <summary>A soft dark oval at the feet of a character whose footprint is centred on it.</summary>
        public static void UnderFeet(GameObject go, float footprintHalfHeight, float width)
        {
            SpriteRenderer shadow = SoftDot.Make("Shadow", go.transform, -1);
            shadow.color = new Color(0f, 0f, 0f, 0.38f);
            Vector3 parentScale = go.transform.lossyScale;
            shadow.transform.localPosition = new Vector3(0f, (-footprintHalfHeight + 0.05f) / parentScale.y, 0f);
            shadow.transform.localScale = new Vector3(width / parentScale.x, width * 0.36f / parentScale.y, 1f);
        }

        /// <summary>
        /// The visible south side of a run of wall, drawn over the bottom of the wall cells,
        /// and its shadow falling on the ground in front. Gives walls height in a 3/4 view.
        /// </summary>
        public static void WallFront(Vector2 runCentre, float length, Transform parent, Color wallColour)
        {
            const float faceHeight = 0.42f;
            const float shadowHeight = 0.45f;

            var face = new GameObject("WallFace");
            face.transform.SetParent(parent, false);
            face.transform.position = new Vector3(runCentre.x, runCentre.y - 0.5f + faceHeight * 0.5f, 0f);
            Sprite art = PlaceholderArt.FindSprite("wall_face");
            var renderer = face.AddComponent<SpriteRenderer>();
            Sprite sprite = art != null ? art : Face;
            renderer.sprite = sprite;
            renderer.sharedMaterial = PlaceholderArt.UnlitMaterial;
            renderer.sortingOrder = 6;
            // The code-made face is tinted to the wall; real art (a horizontal strip) keeps its colours.
            if (art == null) renderer.color = Color.Lerp(wallColour, Color.black, 0.35f);
            face.transform.localScale = new Vector3(length / sprite.bounds.size.x, faceHeight / sprite.bounds.size.y, 1f);

            var shade = new GameObject("WallShadow");
            shade.transform.SetParent(parent, false);
            shade.transform.position = new Vector3(runCentre.x, runCentre.y - 0.5f - shadowHeight * 0.5f, 0f);
            var shadeRenderer = shade.AddComponent<SpriteRenderer>();
            shadeRenderer.sprite = Strip;
            shadeRenderer.sharedMaterial = PlaceholderArt.UnlitMaterial;
            shadeRenderer.sortingOrder = 2;
            shadeRenderer.color = new Color(0f, 0f, 0f, 0.42f);
            shade.transform.localScale = new Vector3(length / Strip.bounds.size.x, shadowHeight / Strip.bounds.size.y, 1f);
        }

        /// <summary>Plain wall front: lighter at the top edge, darker toward the ground, with mortar lines.</summary>
        private static Sprite Face
        {
            get
            {
                if (_face != null) return _face;
                const int height = 12;
                var texture = new Texture2D(1, height);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < height; y++)
                {
                    // y = 0 is the bottom row, at the ground.
                    float shade = 0.65f + 0.35f * y / (height - 1f);
                    if (y == height - 1) shade = 1.15f;
                    if (y == 4 || y == 8) shade *= 0.8f;
                    texture.SetPixel(0, y, new Color(shade, shade, shade, 1f));
                }
                texture.Apply();
                _face = Sprite.Create(texture, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f), height);
                return _face;
            }
        }

        /// <summary>Opaque at the top (against the wall), fading out downward.</summary>
        private static Sprite Strip
        {
            get
            {
                if (_strip != null) return _strip;
                const int height = 16;
                var texture = new Texture2D(1, height);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < height; y++)
                {
                    float t = y / (height - 1f);
                    texture.SetPixel(0, y, new Color(1f, 1f, 1f, t * t));
                }
                texture.Apply();
                _strip = Sprite.Create(texture, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f), height);
                return _strip;
            }
        }
    }
}
