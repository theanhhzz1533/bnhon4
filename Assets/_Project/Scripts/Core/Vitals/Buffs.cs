using System;
using System.Collections.Generic;

namespace Bnhon.Core.Vitals
{
    public enum BuffStat
    {
        /// <summary>Amount is added to the stamina regeneration multiplier (0.3 = 30% faster).</summary>
        StaminaRegen,

        /// <summary>Amount is added to the movement speed multiplier (0.08 = 8% faster).</summary>
        MoveSpeed,

        /// <summary>Amount is health restored per second.</summary>
        HealthRegen,

        /// <summary>Amount is added to the hunger and thirst drain multiplier (-0.4 = 40% slower).</summary>
        NeedsDrain
    }

    /// <summary>A timed effect from a cooked meal.</summary>
    public sealed class BuffDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public BuffStat Stat { get; }
        public float Amount { get; }
        public float Seconds { get; }

        public BuffDefinition(string id, string displayName, BuffStat stat, float amount, float seconds)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Buff id is required.", nameof(id));
            if (seconds <= 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            Id = id;
            DisplayName = displayName;
            Stat = stat;
            Amount = amount;
            Seconds = seconds;
        }
    }

    public sealed class ActiveBuff
    {
        public BuffDefinition Definition { get; }
        public float Remaining { get; internal set; }

        internal ActiveBuff(BuffDefinition definition, float remaining)
        {
            Definition = definition;
            Remaining = remaining;
        }
    }

    /// <summary>
    /// The buffs on the player. Time only runs while <see cref="Tick"/> is called (in raids),
    /// so a meal eaten at the base lasts into the raid. They end when the player sleeps.
    /// </summary>
    public sealed class BuffSet
    {
        private readonly List<ActiveBuff> _active = new List<ActiveBuff>();

        public IReadOnlyList<ActiveBuff> Active => _active;

        /// <summary>Starts a buff. Eating the same meal again restarts its timer instead of stacking.</summary>
        public void Add(BuffDefinition definition)
        {
            Add(definition, definition != null ? definition.Seconds : 0f);
        }

        public void Add(BuffDefinition definition, float remaining)
        {
            if (definition == null || remaining <= 0f) return;
            remaining = Math.Min(remaining, definition.Seconds);

            foreach (ActiveBuff buff in _active)
            {
                if (buff.Definition.Id != definition.Id) continue;
                buff.Remaining = Math.Max(buff.Remaining, remaining);
                return;
            }
            _active.Add(new ActiveBuff(definition, remaining));
        }

        public bool Has(string buffId)
        {
            foreach (ActiveBuff buff in _active)
            {
                if (buff.Definition.Id == buffId) return true;
            }
            return false;
        }

        public void Tick(float deltaSeconds)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Remaining -= deltaSeconds;
                if (_active[i].Remaining <= 0f) _active.RemoveAt(i);
            }
        }

        /// <summary>Sum of the amounts of every active buff on a stat.</summary>
        public float Total(BuffStat stat)
        {
            float total = 0f;
            foreach (ActiveBuff buff in _active)
            {
                if (buff.Definition.Stat == stat) total += buff.Definition.Amount;
            }
            return total;
        }

        /// <summary>1 plus the total, never below 0.1. For the stats that scale something.</summary>
        public float Multiplier(BuffStat stat)
        {
            return Math.Max(0.1f, 1f + Total(stat));
        }

        public void Clear()
        {
            _active.Clear();
        }
    }
}
