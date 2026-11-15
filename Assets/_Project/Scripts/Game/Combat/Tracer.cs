using UnityEngine;

namespace Bnhon.Game.Combat
{
    /// <summary>Draws a line (bullet path, melee swing) for a few frames.</summary>
    public static class Tracer
    {
        public static void Spawn(Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return;

            var go = new GameObject("Tracer");
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(length, width, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderArt.Square;
            renderer.sharedMaterial = PlaceholderArt.UnlitMaterial;
            renderer.color = color;
            renderer.sortingOrder = YSort.AboveEverything + 10;

            Object.Destroy(go, 0.06f);
        }
    }
}
