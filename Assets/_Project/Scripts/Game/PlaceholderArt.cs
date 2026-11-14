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
            return Box(name, position, size, color, parent, sortingOrder, null, false);
        }

        /// <summary>
        /// Creates an object of the given world size. With art for <paramref name="spriteKey"/>
        /// the sprite goes on a child so the root keeps a clean 1x1 footprint for colliders:
        /// it is stretched over the footprint when <paramref name="stretch"/> is set (tiles),
        /// otherwise scaled to the footprint's width with its own proportions (characters, props).
        /// </summary>
        public static GameObject Box(string name, Vector2 position, Vector2 size, Color color, Transform parent, int sortingOrder, string spriteKey, bool stretch)
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
            if (stretch)
            {
                visual.transform.localScale = new Vector3(1f / width, 1f / height, 1f);
            }
            else
            {
                float worldScale = size.x / width;
                visual.transform.localScale = new Vector3(worldScale / size.x, worldScale / size.y, 1f);
            }
            AddRenderer(visual, art, Color.white, sortingOrder);
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
