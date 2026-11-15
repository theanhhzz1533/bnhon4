using System;

namespace Bnhon.Core.Combat
{
    /// <summary>
    /// Recoil as growing inaccuracy. Every shot adds degrees of spread up to a cap; after a
    /// short pause without firing, the spread shrinks back to zero. Firing in a steady rhythm
    /// stays accurate, holding the trigger does not.
    /// </summary>
    public sealed class SpreadBloom
    {
        private float _sinceKick;

        /// <summary>Extra spread right now, in degrees, on top of the weapon's base spread.</summary>
        public float Degrees { get; private set; }

        /// <summary>Seconds after a shot before recovery starts.</summary>
        public float RecoveryDelay { get; }

        public SpreadBloom(float recoveryDelay = 0.12f)
        {
            RecoveryDelay = recoveryDelay;
            _sinceKick = recoveryDelay;
        }

        public void Kick(float degrees, float maxDegrees)
        {
            if (degrees <= 0f) return;
            Degrees = Math.Min(Math.Max(maxDegrees, Degrees), Degrees + degrees);
            _sinceKick = 0f;
        }

        public void Tick(float deltaSeconds, float recoveryPerSecond)
        {
            _sinceKick += deltaSeconds;
            if (_sinceKick < RecoveryDelay) return;
            Degrees = Math.Max(0f, Degrees - recoveryPerSecond * deltaSeconds);
        }

        public void Reset()
        {
            Degrees = 0f;
            _sinceKick = RecoveryDelay;
        }
    }
}
