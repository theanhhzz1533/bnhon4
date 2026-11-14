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
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(this);
            if (IsDead) Died?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }

        public void ResetFull()
        {
            Current = maxHealth;
        }
    }
}
