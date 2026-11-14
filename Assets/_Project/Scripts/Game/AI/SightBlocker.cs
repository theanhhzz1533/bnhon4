using System.Collections.Generic;
using UnityEngine;

namespace Bnhon.Game.AI
{
    /// <summary>
    /// Marks a collider that blocks line of sight (walls). Also the one place
    /// that answers "can A see B" for both the fog of war and enemy AI.
    /// </summary>
    public sealed class SightBlocker : MonoBehaviour
    {
        private static readonly HashSet<Collider2D> Blockers = new HashSet<Collider2D>();
        private static readonly RaycastHit2D[] Buffer = new RaycastHit2D[32];
        private static ContactFilter2D _filter = new ContactFilter2D().NoFilter();

        private Collider2D _collider;

        private void OnEnable()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null) Blockers.Add(_collider);
        }

        private void OnDisable()
        {
            if (_collider != null) Blockers.Remove(_collider);
        }

        /// <summary>Distance along the ray to the first sight blocker, or <paramref name="maxDistance"/> if none.</summary>
        public static float Distance(Vector2 origin, Vector2 direction, float maxDistance)
        {
            int count = Physics2D.Raycast(origin, direction, _filter, Buffer, maxDistance);
            float nearest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = Buffer[i];
                if (hit.distance < nearest && Blockers.Contains(hit.collider)) nearest = hit.distance;
            }
            return nearest;
        }

        public static bool IsBlocked(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.001f) return false;
            return Distance(from, delta / length, length) < length - 0.01f;
        }
    }
}
