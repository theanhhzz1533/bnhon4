namespace Bnhon.Game.Combat
{
    public enum WeaponKind
    {
        Melee,
        Firearm
    }

    /// <summary>Stats of one weapon. Plain data for the prototype; becomes a ScriptableObject in M2.</summary>
    public sealed class WeaponDefinition
    {
        public string Name;

        /// <summary>Sound played on each use (see SoundId).</summary>
        public string Sound;
        public WeaponKind Kind;

        /// <summary>Damage per hit: per swing for melee, per pellet for firearms.</summary>
        public float Damage;
        public float Range;

        /// <summary>Seconds between uses.</summary>
        public float Interval;

        /// <summary>How far away enemies hear each use.</summary>
        public float NoiseRadius;

        // Firearm only
        public int MagazineSize;
        public float ReloadTime;
        public int Pellets = 1;
        public float SpreadDegrees;
        public string AmmoItemId;

        // Melee only
        public float ArcDegrees;
        public float StaminaCost;
    }
}
