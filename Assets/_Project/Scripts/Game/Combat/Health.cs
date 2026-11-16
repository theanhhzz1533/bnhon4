using System;
using UnityEngine;

namespace Bnhon.Game.Combat
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>While true, <see cref="Damage"/> does nothing (dodge roll).</summary>
        public bool Invulnerable { get; set; }

        /// <summary>
        /// Changes incoming damage that has a known source (amount, where it came from) before
        /// it is applied. Armour uses this. Null means damage is taken as is.
        /// </summary>
        public Func<float, Vector2, float> Modifier { get; set; }

        public event Action<Health> Damaged;
        public event Action<Health> Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void SetMax(float value)
        {
            maxHealth = value;
            Current = value;
        }

        public void Damage(float amount)
        {
            if (IsDead || Invulnerable || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(this);
            if (IsDead) Died?.Invoke(this);
        }

        /// <summary>Damage from a known place, such as a shot or a swing. The target's armour, if any, may reduce it.</summary>
        public void Damage(float amount, Vector2 from)
        {
            if (Modifier != null) amount = Modifier(amount, from);
            Damage(amount);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }

        public void ResetFull()
        {
            Invulnerable = false;
            Current = maxHealth;
        }
    }
}
