using System.Collections.Generic;
using UnityEngine;

namespace Bnhon.Game
{
    /// <summary>
    /// Builds the visible objects of the prototype. Each object asks for a
    /// sprite by key; if <c>Assets/_Project/Resources/Sprites/&lt;key&gt;.png</c>
    /// exists it is used, otherwise the object is a flat colored square.
    /// So real art can replace the squares one file at a time.
    /// </summary>
    /// <summary>
    /// How a real sprite is sized on an object. Every kind of thing gets one display box in
    /// world units (1 unit = one map cell), and its picture is scaled to fit inside that box.
    /// So all characters come out the same height and all crates the same size, no matter
    /// how tightly each generated image happened to be cropped.
    /// </summary>
    public struct SpriteFit
    {
        public bool Stretch;
        public Vector2 Size;
        public bool AnchorBottom;

        /// <summary>Fill the footprint exactly (floor and wall tiles, zones).</summary>
        public static readonly SpriteFit Tile = new SpriteFit { Stretch = true };

        /// <summary>People and zombies: one and a half cells tall, standing on their footprint.</summary>
        public static readonly SpriteFit Character = Standing(1.05f, 1.5f);

        /// <summary>Loot containers.</summary>
        public static readonly SpriteFit Crate = Standing(1.15f, 1.25f);

        /// <summary>Fits inside width x height, resting on the bottom of the footprint.</summary>
        public static SpriteFit Standing(float width, float height)
        {
            return new SpriteFit { Size = new Vector2(width, height), AnchorBottom = true };
        }

        /// <summary>Fits inside width x height, centred on the footprint (things seen from above).</summary>
        public static SpriteFit Centered(float width, float height)
        {
            return new SpriteFit { Size = new Vector2(width, height) };
        }
    }

    public static class PlaceholderArt
    {
        private const string SpriteFolder = "Sprites/";

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Sprite _square;
        private static Material _unlit;

        public static Sprite Square
        {
            get
            {
                if (_square != null) return _square;

                // A 1x1 texture with the default Tight mesh type can produce a sprite
                // with no geometry, so use a small texture and a full-rect quad.
                const int size = 4;
                var texture = new Texture2D(size, size);
                texture.filterMode = FilterMode.Point;
                var pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                _square = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
                return _square;
            }
        }

        /// <summary>Unlit, so sprites are visible even when the scene has no 2D light.</summary>
        public static Material UnlitMaterial
        {
            get
            {
                if (_unlit != null) return _unlit;

                // Built-in unlit sprite shader: present in every project and drawn by
                // both the built-in pipeline and URP (2D and Universal renderers).
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                _unlit = new Material(shader);
                return _unlit;
            }
        }

        /// <summary>The art for a key, or null when no file with that name has been added yet.</summary>
        public static Sprite FindSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Sprite sprite = Resources.Load<Sprite>(SpriteFolder + key);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>A flat colored rectangle with no art key.</summary>
        public static GameObject Box(string name, Vector2 position, Vector2 size, Color color, Transform parent, int sortingOrder)
        {
            return Box(name, position, size, color, parent, sortingOrder, null, SpriteFit.Tile);
        }

        /// <summary>
        /// Creates an object whose footprint (the area colliders use) has the given world size.
        /// Without art for <paramref name="spriteKey"/> it is a colored rectangle of that size.
        /// With art, the sprite goes on a child and is sized by <paramref name="fit"/>, so the
        /// footprint stays the same whatever the picture's shape.
        /// </summary>
        public static GameObject Box(string name, Vector2 position, Vector2 size, Color color, Transform parent, int sortingOrder, string spriteKey, SpriteFit fit)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            Sprite art = FindSprite(spriteKey);
            if (art == null)
            {
                AddRenderer(go, Square, color, sortingOrder);
                return go;
            }

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            Vector3 artSize = art.bounds.size;
            float width = Mathf.Max(0.0001f, artSize.x);
            float height = Mathf.Max(0.0001f, artSize.y);

            if (fit.Stretch)
            {
                visual.transform.localScale = new Vector3(1f / width, 1f / height, 1f);
            }
            else
            {
                // Largest size that fits inside the display box with the picture's own proportions.
                float worldScale = Mathf.Min(fit.Size.x / width, fit.Size.y / height);
                visual.transform.localScale = new Vector3(worldScale / size.x, worldScale / size.y, 1f);

                if (fit.AnchorBottom)
                {
                    // Stand the picture on the bottom edge of the footprint: feet on the ground, head above.
                    float lift = (worldScale * height - size.y) * 0.5f;
                    visual.transform.localPosition = new Vector3(0f, lift / size.y, 0f);
                }
            }
            AddRenderer(visual, art, Color.white, sortingOrder);
            return go;
        }

        /// <summary>
        /// Multiplies real art by a colour, to darken or mute it. Placeholder squares keep
        /// the colour they were made with.
        /// </summary>
        public static GameObject Tint(GameObject go, Color tint)
        {
            var renderer = go.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && !IsPlaceholder(renderer)) renderer.color = tint;
            return go;
        }

        /// <summary>True when the object still shows a colored square rather than real art.</summary>
        public static bool IsPlaceholder(SpriteRenderer renderer)
        {
            return renderer != null && renderer.sprite == Square;
        }

        /// <summary>Recolors a placeholder square. Real art is left alone.</summary>
        public static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponentInChildren<SpriteRenderer>();
            if (IsPlaceholder(renderer)) renderer.color = color;
        }

        /// <summary>Marks an object as used up: a placeholder takes the given color, real art is darkened.</summary>
        public static void MarkSpent(GameObject go, Color placeholderColor)
        {
            var renderer = go.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) return;
            renderer.color = IsPlaceholder(renderer) ? placeholderColor : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private static void AddRenderer(GameObject go, Sprite sprite, Color color, int sortingOrder)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = UnlitMaterial;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
