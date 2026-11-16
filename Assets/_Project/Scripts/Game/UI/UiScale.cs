using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// Makes the immediate-mode UI the same apparent size in any window. Screens are laid
    /// out for a window 720 pixels tall and scaled to the real one; <see cref="Width"/>
    /// and <see cref="Height"/> are the window's size in those layout units.
    /// </summary>
    public static class UiScale
    {
        private const float ReferenceHeight = 720f;

        public static float Factor => Mathf.Clamp(Screen.height / ReferenceHeight, 0.7f, 3f);
        public static float Width => Screen.width / Factor;
        public static float Height => Screen.height / Factor;

        /// <summary>Scales everything drawn from now on. Returns the matrix to put back afterwards.</summary>
        public static Matrix4x4 Apply()
        {
            Matrix4x4 previous = GUI.matrix;
            float factor = Factor;
            GUI.matrix = Matrix4x4.Scale(new Vector3(factor, factor, 1f));
            return previous;
        }
    }
}
