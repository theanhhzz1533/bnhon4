using System;
using System.Collections;
using Bnhon.Core.Combat;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Core.Vitals;
using Bnhon.Game.AI;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.Combat
{
    /// <summary>
    /// The player's weapons and which one is in hand. Slot 1 is a silent
    /// melee weapon, slot 2 a pistol, slot 3 a loud shotgun. Every use emits
    /// a <see cref="Noise"/> sized to the weapon, which is how zombies hear
    /// the player.
    /// </summary>
    public sealed class PlayerWeapons : MonoBehaviour
    {
        [SerializeField] private float swapDelay = 0.3f;

        private WeaponDefinition[] _weapons;
        private int[] _ammo;
        private int _current = 1;
        private float _nextUseTime;
        private Health _ownHealth;
        private readonly SpreadBloom _bloom = new SpreadBloom();

        /// <summary>Raised with the aim direction and strength of each shot, for camera kick.</summary>
        public event Action<Vector2, float> Fired;

        /// <summary>Total cone of fire right now, in degrees: the weapon's own spread plus recoil. 0 for melee.</summary>
        public float CurrentSpread => Current.Kind == WeaponKind.Firearm ? Current.SpreadDegrees + _bloom.Degrees : 0f;

        public WeaponDefinition Current => _weapons[_current];
        public int CurrentSlot => _current + 1;
        public int SlotCount => _weapons.Length;
        public int AmmoInMagazine => _ammo[_current];
        public bool IsReloading { get; private set; }

        private void Awake()
        {
            _ownHealth = GetComponent<Health>();
            _weapons = new[]
            {
                new WeaponDefinition
                {
                    Name = "Dao", Sound = SoundId.KnifeSwing, Kind = WeaponKind.Melee, Damage = 40f, Range = 1.6f, Interval = 0.45f,
                    NoiseRadius = 1.5f, ArcDegrees = 110f, StaminaCost = 10f
                },
                new WeaponDefinition
                {
                    Name = "Súng ngắn", Sound = SoundId.PistolShot, Kind = WeaponKind.Firearm, Damage = 34f, Range = 12f, Interval = 0.25f,
                    NoiseRadius = 10f, MagazineSize = 8, ReloadTime = 1.2f, Pellets = 1, SpreadDegrees = 2f,
                    AmmoItemId = PrototypeContent.PistolAmmo.Id,
                    RecoilPerShot = 3.5f, MaxRecoil = 14f, RecoilRecovery = 20f, CameraKick = 0.12f
                },
                new WeaponDefinition
                {
                    Name = "Shotgun", Sound = SoundId.ShotgunShot, Kind = WeaponKind.Firearm, Damage = 18f, Range = 7f, Interval = 0.9f,
                    NoiseRadius = 17f, MagazineSize = 2, ReloadTime = 1.8f, Pellets = 7, SpreadDegrees = 24f,
                    AmmoItemId = PrototypeContent.ShotgunShell.Id,
                    RecoilPerShot = 7f, MaxRecoil = 12f, RecoilRecovery = 14f, CameraKick = 0.38f
                }
            };

            _ammo = new int[_weapons.Length];
            for (int i = 0; i < _weapons.Length; i++) _ammo[i] = _weapons[i].MagazineSize;
        }

        private void Update()
        {
            _bloom.Tick(UnityEngine.Time.deltaTime, Current.RecoilRecovery);
        }

        /// <summary>Switches to a weapon slot (0-based). Interrupts a reload in progress.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= _weapons.Length || index == _current) return;

            StopAllCoroutines();
            IsReloading = false;
            _current = index;
            _nextUseTime = UnityEngine.Time.time + swapDelay;
        }

        /// <summary>Attacks with the weapon in hand. Returns true if a shot or swing happened.</summary>
        public bool TryUse(Vector2 origin, Vector2 direction, Stamina stamina)
        {
            if (IsReloading || UnityEngine.Time.time < _nextUseTime) return false;
            if (direction.sqrMagnitude < 0.0001f) return false;
            direction.Normalize();

            WeaponDefinition weapon = Current;
            if (weapon.Kind == WeaponKind.Melee)
            {
                if (stamina != null && !stamina.TryUse(weapon.StaminaCost)) return false;
                Swing(weapon, origin, direction);
            }
            else
            {
                if (_ammo[_current] <= 0)
                {
                    // Dry fire: an audible click, and a short pause so holding the button does not buzz.
                    Sfx.PlayUi(SoundId.EmptyClick, 0.7f);
                    _nextUseTime = UnityEngine.Time.time + 0.35f;
                    return false;
                }
                _ammo[_current]--;
                float spread = weapon.SpreadDegrees + _bloom.Degrees;
                for (int i = 0; i < weapon.Pellets; i++)
                {
                    float offset = UnityEngine.Random.Range(-spread * 0.5f, spread * 0.5f);
                    FireRay(weapon, origin, Rotate(direction, offset));
                }
                _bloom.Kick(weapon.RecoilPerShot, weapon.MaxRecoil);
                Fired?.Invoke(direction, weapon.CameraKick);
            }

            _nextUseTime = UnityEngine.Time.time + weapon.Interval;
            Noise.Emit(origin, weapon.NoiseRadius);
            Sfx.PlayUi(weapon.Sound);
            return true;
        }

        /// <summary>Refills the magazine of the weapon in hand from matching ammo in <paramref name="ammoSource"/>.</summary>
        public void Reload(GridInventory ammoSource)
        {
            WeaponDefinition weapon = Current;
            if (weapon.Kind != WeaponKind.Firearm || IsReloading || ammoSource == null) return;
            if (_ammo[_current] >= weapon.MagazineSize) return;

            if (ammoSource.CountOf(weapon.AmmoItemId) <= 0)
            {
                if (GameRoot.Instance != null) GameRoot.Instance.Notify("Hết đạn cho " + weapon.Name + ".", 1.5f);
                return;
            }
            Sfx.PlayUi(SoundId.Reload, 0.8f);
            StartCoroutine(ReloadRoutine(weapon, _current, ammoSource));
        }

        private IEnumerator ReloadRoutine(WeaponDefinition weapon, int slot, GridInventory ammoSource)
        {
            IsReloading = true;
            yield return new WaitForSeconds(weapon.ReloadTime);

            int needed = weapon.MagazineSize - _ammo[slot];
            int loaded = Mathf.Min(needed, ammoSource.CountOf(weapon.AmmoItemId));
            if (loaded > 0 && ammoSource.TryConsume(weapon.AmmoItemId, loaded)) _ammo[slot] += loaded;
            IsReloading = false;
        }

        private void FireRay(WeaponDefinition weapon, Vector2 origin, Vector2 direction)
        {
            Vector2 end = origin + direction * weapon.Range;
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, weapon.Range);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit2D hit in hits)
            {
                Collider2D other = hit.collider;
                if (other == null) continue;

                // Characters are hit on their hurtbox (the visible body), not on the
                // small collider at their feet.
                var hurtbox = other.GetComponent<Hurtbox>();
                if (hurtbox != null)
                {
                    if (hurtbox.Owner == null || hurtbox.Owner == _ownHealth || hurtbox.Owner.IsDead) continue;
                    hurtbox.Owner.Damage(weapon.Damage);
                    end = hit.point;
                    break;
                }

                // Other triggers (zones, corpses) and the feet colliders of characters do not stop bullets.
                if (other.isTrigger || other.attachedRigidbody != null) continue;

                // A wall or a crate. Something without a hurtbox can still be damaged directly.
                var health = other.GetComponent<Health>();
                if (health != null) health.Damage(weapon.Damage);
                end = hit.point;
                break;
            }

            Tracer.Spawn(origin, end, 0.04f, new Color(1f, 0.9f, 0.5f));
        }

        private void Swing(WeaponDefinition weapon, Vector2 origin, Vector2 direction)
        {
            float halfArc = weapon.ArcDegrees * 0.5f;
            Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, weapon.Range);

            foreach (Collider2D other in nearby)
            {
                if (other == null) continue;

                var hurtbox = other.GetComponent<Hurtbox>();
                if (hurtbox == null || hurtbox.Owner == null || hurtbox.Owner == _ownHealth || hurtbox.Owner.IsDead) continue;

                Vector2 target = other.ClosestPoint(origin);
                Vector2 toTarget = target - origin;
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(direction, toTarget) > halfArc) continue;
                if (SightBlocker.IsBlocked(origin, target)) continue;

                hurtbox.Owner.Damage(weapon.Damage);
            }

            // Show the reach of the swing: its two edges and its middle.
            var color = new Color(0.9f, 0.95f, 1f);
            Tracer.Spawn(origin, origin + Rotate(direction, -halfArc) * weapon.Range, 0.05f, color);
            Tracer.Spawn(origin, origin + direction * weapon.Range, 0.08f, color);
            Tracer.Spawn(origin, origin + Rotate(direction, halfArc) * weapon.Range, 0.05f, color);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void OnDisable()
        {
            IsReloading = false;
        }
    }
}
