using UnityEngine;

namespace Bnhon.Game
{
    /// <summary>
    /// Draws standing things in depth order: whatever is lower on the screen is in front.
    /// Sorts by the bottom of the footprint (the feet), so a character walking behind a crate
    /// is hidden by it and one walking in front covers it. Child renderers (the gun) keep
    /// their place relative to the main one.
    /// </summary>
    public sealed class YSort : MonoBehaviour
    {
        /// <summary>Sorting order for overlays that must cover every sorted object (fog, tracers).</summary>
        public const int AboveEverything = 20000;

        private const int Base = 10000;
        private const float StepsPerUnit = 20f;

        private SpriteRenderer[] _renderers;
        private int[] _offsets;

        private void Start()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            _offsets = new int[_renderers.Length];
            if (_renderers.Length == 0) return;

            int lowest = _renderers[0].sortingOrder;
            foreach (SpriteRenderer renderer in _renderers) lowest = Mathf.Min(lowest, renderer.sortingOrder);
            for (int i = 0; i < _renderers.Length; i++) _offsets[i] = _renderers[i].sortingOrder - lowest;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (_renderers == null) return;

            float feet = transform.position.y - transform.lossyScale.y * 0.5f;
            int order = Base - Mathf.RoundToInt(feet * StepsPerUnit);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].sortingOrder = order + _offsets[i];
            }
        }
    }
}
