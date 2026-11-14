using System;
using UnityEngine;

namespace Bnhon.Game.Combat
{
    /// <summary>
    /// Loud sounds that enemies react to: gunshots and screamer calls.
    /// Anyone within <c>radius</c> of <c>source</c> hears it and goes to look
    /// at <c>pointOfInterest</c>.
    /// </summary>
    public static class Noise
    {
        public static event Action<Vector2, float, Vector2> Emitted;

        public static void Emit(Vector2 source, float radius, Vector2 pointOfInterest)
        {
            Emitted?.Invoke(source, radius, pointOfInterest);
        }

        public static void Emit(Vector2 source, float radius)
        {
            Emit(source, radius, source);
        }
    }
}
