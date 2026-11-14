using UnityEngine;

namespace Bnhon.Game
{
    /// <summary>
    /// Flat colored squares so the prototype needs no art assets. Replace
    /// with real pixel art in M2.
    /// </summary>
    public static class PlaceholderArt
    {
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

                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                _unlit = new Material(shader);
                return _unlit;
            }
        }

        public static GameObject Box(string name, Vector2 position, Vector2 size, Color color, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Square;
            renderer.sharedMaterial = UnlitMaterial;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        public static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = color;
        }
    }
}
