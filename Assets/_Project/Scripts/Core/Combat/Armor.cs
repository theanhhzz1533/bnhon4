namespace Bnhon.Core.Combat
{
    /// <summary>
    /// Armour that only covers the front. A hit counts as frontal when it arrives from
    /// within about 70 degrees of the way the target is facing; hits from the side and
    /// from behind do full damage.
    /// </summary>
    public static class Armor
    {
        /// <summary>Cosine of the half-angle of the protected arc.</summary>
        public const float FrontArcCosine = 0.34f;

        /// <param name="facingX">Unit vector of the way the target faces.</param>
        /// <param name="fromX">Vector from the target to where the hit came from. Need not be unit length.</param>
        public static bool IsFrontal(float facingX, float facingY, float fromX, float fromY)
        {
            float length = (float)System.Math.Sqrt(fromX * fromX + fromY * fromY);
            if (length < 0.0001f) return true;
            float dot = (facingX * fromX + facingY * fromY) / length;
            return dot > FrontArcCosine;
        }

        /// <summary>Damage left after armour that stops <paramref name="reduction"/> (0..1) of frontal hits.</summary>
        public static float Apply(float damage, float reduction, float facingX, float facingY, float fromX, float fromY)
        {
            if (reduction <= 0f || !IsFrontal(facingX, facingY, fromX, fromY)) return damage;
            return damage * (1f - System.Math.Min(1f, reduction));
        }
    }

    /// <summary>
    /// A shield held in front. While it lasts it stops part of every frontal hit, and each
    /// frontal hit wears it down by the hit's full damage. Once its durability is gone it
    /// is broken for good and stops nothing.
    /// </summary>
    public sealed class Shield
    {
        public float Reduction { get; }
        public float MaxDurability { get; }
        public float Durability { get; private set; }

        public bool IsBroken => Durability <= 0f;

        /// <summary>How much of the shield is left, 0..1.</summary>
        public float Fraction => MaxDurability > 0f ? Durability / MaxDurability : 0f;

        public Shield(float reduction, float durability)
        {
            Reduction = System.Math.Max(0f, System.Math.Min(1f, reduction));
            MaxDurability = System.Math.Max(0f, durability);
            Durability = MaxDurability;
        }

        /// <summary>
        /// Takes a hit and returns the damage that gets through to the body. Hits from the
        /// side or behind miss the shield. The hit that breaks the shield is still reduced.
        /// </summary>
        /// <param name="wear">Multiplies how much the hit wears the shield down (a charm can raise it).</param>
        public float Absorb(float damage, float facingX, float facingY, float fromX, float fromY, float wear = 1f)
        {
            if (damage <= 0f || IsBroken || !Armor.IsFrontal(facingX, facingY, fromX, fromY)) return damage;

            Durability = System.Math.Max(0f, Durability - damage * System.Math.Max(0f, wear));
            return damage * (1f - Reduction);
        }
    }
}
