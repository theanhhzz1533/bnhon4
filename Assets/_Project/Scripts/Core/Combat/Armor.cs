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
}
