using System;

namespace Bnhon.Core.Vitals
{
    /// <summary>Hunger and thirst. Both run from Max (full) down to 0 and only drain while <see cref="Tick"/> is called (in raids).</summary>
    public sealed class Needs
    {
        public float Max { get; }
        public float HungerPerSecond { get; }
        public float ThirstPerSecond { get; }

        public float Hunger { get; private set; }
        public float Thirst { get; private set; }

        public bool IsStarving => Hunger <= 0f;
        public bool IsDehydrated => Thirst <= 0f;

        public Needs(float max = 100f, float hungerPerSecond = 0.2f, float thirstPerSecond = 0.3f)
        {
            Max = max;
            HungerPerSecond = hungerPerSecond;
            ThirstPerSecond = thirstPerSecond;
            Hunger = max;
            Thirst = max;
        }

        public void Tick(float deltaSeconds)
        {
            Hunger = Math.Max(0f, Hunger - HungerPerSecond * deltaSeconds);
            Thirst = Math.Max(0f, Thirst - ThirstPerSecond * deltaSeconds);
        }

        public void Eat(float amount)
        {
            if (amount > 0f) Hunger = Math.Min(Max, Hunger + amount);
        }

        public void Drink(float amount)
        {
            if (amount > 0f) Thirst = Math.Min(Max, Thirst + amount);
        }
    }

    /// <summary>What using one unit of an item does to the player.</summary>
    public sealed class ConsumableDefinition
    {
        public string ItemId { get; }
        public float Heal { get; }
        public float Hunger { get; }
        public float Thirst { get; }

        /// <summary>How long the player is busy using it. They cannot attack meanwhile, and being hit interrupts it.</summary>
        public float UseSeconds { get; }

        public ConsumableDefinition(string itemId, float heal = 0f, float hunger = 0f, float thirst = 0f, float useSeconds = 2f)
        {
            UseSeconds = useSeconds;
            ItemId = itemId;
            Heal = heal;
            Hunger = hunger;
            Thirst = thirst;
        }
    }
}
