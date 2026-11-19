using UnityEngine;

namespace Bnhon.Game.World
{
    /// <summary>A soft round dot, made once in code, for smoke, glows and dust.</summary>
    public static class SoftDot
    {
        private static Sprite _sprite;

        public static Sprite Sprite
        {
            get
            {
                if (_sprite != null) return _sprite;

                const int size = 32;
                var texture = new Texture2D(size, size);
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[size * size];
                float radius = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - radius) / radius;
                        float dy = (y + 0.5f - radius) / radius;
                        float fade = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        pixels[y * size + x] = new Color(1f, 1f, 1f, fade * fade);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                _sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                return _sprite;
            }
        }

        public static SpriteRenderer Make(string name, Transform parent, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite;
            renderer.sharedMaterial = PlaceholderArt.UnlitMaterial;
            renderer.sortingOrder = order;
            return renderer;
        }
    }

    /// <summary>
    /// A column of dark smoke rising from a wreck. Drawn above the fog of war, so a burning
    /// car is a landmark that can be seen from far away.
    /// </summary>
    public sealed class SmokePlume : MonoBehaviour
    {
        private const int Puffs = 9;
        private const float Life = 7f;

        private readonly SpriteRenderer[] _puffs = new SpriteRenderer[Puffs];
        private readonly float[] _age = new float[Puffs];
        private readonly Vector2[] _drift = new Vector2[Puffs];

        private void Start()
        {
            for (int i = 0; i < Puffs; i++)
            {
                _puffs[i] = SoftDot.Make("Puff", transform, YSort.AboveEverything + 2);
                _age[i] = Life * i / Puffs;
                _drift[i] = new Vector2(Random.Range(0.15f, 0.45f), Random.Range(0.5f, 0.8f));
            }
        }

        private void Update()
        {
            float delta = UnityEngine.Time.deltaTime;
            for (int i = 0; i < Puffs; i++)
            {
                _age[i] += delta;
                if (_age[i] >= Life)
                {
                    _age[i] -= Life;
                    _drift[i] = new Vector2(Random.Range(0.15f, 0.45f), Random.Range(0.5f, 0.8f));
                }

                float t = _age[i] / Life;
                Transform puff = _puffs[i].transform;
                puff.localPosition = new Vector3(_drift[i].x * _age[i], _drift[i].y * _age[i], 0f);
                float size = Mathf.Lerp(0.8f, 3.2f, t);
                puff.localScale = new Vector3(size, size, 1f);
                float alpha = Mathf.Sin(t * Mathf.PI) * 0.32f;
                _puffs[i].color = new Color(0.16f, 0.16f, 0.17f, alpha);
            }
        }
    }

    /// <summary>The flickering light of a fire in a barrel, with a thin trail of smoke.</summary>
    public sealed class FireGlow : MonoBehaviour
    {
        private SpriteRenderer _glow;
        private SpriteRenderer _core;
        private float _seed;

        private void Start()
        {
            _seed = Random.value * 100f;
            _glow = SoftDot.Make("Glow", transform, YSort.AboveEverything + 1);
            _glow.transform.localScale = new Vector3(4.5f, 4.5f, 1f);
            _core = SoftDot.Make("Flame", transform, YSort.AboveEverything + 1);
            _core.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            _core.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

        private void Update()
        {
            float flicker = Mathf.PerlinNoise(_seed, UnityEngine.Time.time * 3f);
            _glow.color = new Color(1f, 0.55f, 0.2f, 0.12f + flicker * 0.12f);
            _core.color = new Color(1f, 0.75f, 0.35f, 0.45f + flicker * 0.35f);
        }
    }
}
