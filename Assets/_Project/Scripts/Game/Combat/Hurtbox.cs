using UnityEngine;

namespace Bnhon.Game.Combat
{
    /// <summary>
    /// The area where a character can be hit, as opposed to the small collider at its feet
    /// that it walks and bumps into things with. It covers the visible sprite, so a shot
    /// aimed at what the player sees on screen connects.
    /// </summary>
    public sealed class Hurtbox : MonoBehaviour
    {
        public Health Owner { get; private set; }

        /// <summary>
        /// Adds a hurtbox to a character built by <see cref="PlaceholderArt"/>. It matches the
        /// drawn sprite, slightly narrowed so near misses beside the body stay misses.
        /// </summary>
        public static Hurtbox Attach(GameObject character, Health owner)
        {
            Transform root = character.transform;
            Vector3 scale = root.lossyScale;
            Vector2 center = root.position;
            var size = new Vector2(scale.x, scale.y);

            var renderer = character.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && !PlaceholderArt.IsPlaceholder(renderer))
            {
                Bounds drawn = renderer.bounds;
                center = drawn.center;
                size = new Vector2(drawn.size.x * 0.8f, drawn.size.y * 0.95f);
            }

            var go = new GameObject("Hurtbox");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3((center.x - root.position.x) / scale.x, (center.y - root.position.y) / scale.y, 0f);
            go.transform.localScale = new Vector3(size.x / scale.x, size.y / scale.y, 1f);
            go.AddComponent<BoxCollider2D>().isTrigger = true;

            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Owner = owner;
            return hurtbox;
        }
    }
}
