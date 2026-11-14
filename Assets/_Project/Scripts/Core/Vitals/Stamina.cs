using System;

namespace Bnhon.Core.Vitals
{
    /// <summary>
    /// Stamina pool for sprinting, rolling and melee. Regenerates after a
    /// short delay since the last use. Running it down to zero leaves the
    /// player exhausted (no sprinting) until it recovers past a threshold.
    /// </summary>
    public sealed class Stamina
    {
        public float Max { get; }
        public float RegenPerSecond { get; }
        public float RegenDelay { get; }
        public float RecoverThreshold { get; }

        public float Current { get; private set; }
        public bool IsExhausted { get; private set; }

        private float _sinceUse;

        public Stamina(float max = 100f, float regenPerSecond = 22f, float regenDelay = 0.7f, float recoverThreshold = 25f)
        {
            Max = max;
            RegenPerSecond = regenPerSecond;
            RegenDelay = regenDelay;
            RecoverThreshold = recoverThreshold;
            Current = max;
            _sinceUse = regenDelay;
        }

        /// <summary>Spends a fixed cost (roll, melee swing). Fails without spending if there is not enough.</summary>
        public bool TryUse(float amount)
        {
            if (amount <= 0f) return true;
            if (Current < amount) return false;
            Spend(amount);
            return true;
        }

        /// <summary>Continuous drain (sprinting). Returns false when exhausted, so the caller stops sprinting.</summary>
        public bool Drain(float amount)
        {
            if (IsExhausted || Current <= 0f) return false;
            Spend(Math.Min(amount, Current));
            return true;
        }

        public void Tick(float deltaSeconds, float regenMultiplier = 1f)
        {
            _sinceUse += deltaSeconds;
            if (_sinceUse >= RegenDelay)
            {
                Current = Math.Min(Max, Current + RegenPerSecond * regenMultiplier * deltaSeconds);
            }
            if (IsExhausted && Current >= RecoverThreshold) IsExhausted = false;
        }

        public void Refill()
        {
            Current = Max;
            IsExhausted = false;
        }

        private void Spend(float amount)
        {
            Current = Math.Max(0f, Current - amount);
            _sinceUse = 0f;
            if (Current <= 0f) IsExhausted = true;
        }
    }
}
