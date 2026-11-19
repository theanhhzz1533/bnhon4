using System;
using System.Collections;
using System.Collections.Generic;
using Bnhon.Core.Combat;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Core.Vitals;
using Bnhon.Game.AI;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.Combat
{
    /// <summary>
    /// The weapon in the player's hand and how it fires. Slot 1 is the knife, which the
    /// player always has. Slots 2 and 3 are the two firearm slots of the session's
    /// <see cref="Loadout"/>: weapon items equipped from the inventory, lost on death.
    /// Every use emits a <see cref="Noise"/> sized to the weapon.
    /// </summary>
    public sealed class PlayerWeapons : MonoBehaviour
    {
        [SerializeField] private float swapDelay = 0.3f;

        private readonly Dictionary<string, WeaponDefinition> _firearms = new Dictionary<string, WeaponDefinition>();
        private readonly SpreadBloom _bloom = new SpreadBloom();
        private WeaponDefinition _knife;
        private int _current;
        private float _nextUseTime;
        private Health _ownHealth;

        /// <summary>Raised with the aim direction and strength of each shot, for camera kick.</summary>
        public event Action<Vector2, float> Fired;

        /// <summary>Total cone of fire right now, in degrees: the weapon's own spread plus recoil. 0 for melee.</summary>
        public float CurrentSpread => Current.Kind == WeaponKind.Firearm ? Current.SpreadDegrees + _bloom.Degrees : 0f;

        /// <summary>The weapon in hand. Falls back to the knife if the selected firearm slot is empty.</summary>
        public WeaponDefinition Current
        {
            get
            {
                if (_current == 0) return _knife;
                return DefinitionFor(Loadout.GetWeapon(_current - 1)) ?? _knife;
            }
        }

        /// <summary>1 for the knife, 2 and 3 for the firearm slots.</summary>
        public int CurrentSlot => _current + 1;

        public int AmmoInMagazine => _current == 0 ? 0 : Loadout.GetLoaded(_current - 1);
        public bool IsReloading { get; private set; }

        private static Loadout Loadout => GameRoot.Instance.Session.Loadout;

        /// <summary>Combat stats for a weapon item, or null if the item is not a known firearm.</summary>
        public WeaponDefinition DefinitionFor(ItemDefinition item)
        {
            if (item == null) return null;
            _firearms.TryGetValue(item.Id, out WeaponDefinition definition);
            return definition;
        }

        private void Awake()
        {
            _ownHealth = GetComponent<Health>();

            _knife = new WeaponDefinition
            {
                Name = "Dao", Sound = SoundId.KnifeSwing, Kind = WeaponKind.Melee, Damage = 40f, Range = 1.6f, Interval = 0.45f,
                NoiseRadius = 1.5f, ArcDegrees = 110f, StaminaCost = 10f
            };

            _firearms[PrototypeContent.Pistol.Id] = new WeaponDefinition
            {
                Name = "Súng ngắn", Sound = SoundId.PistolShot, Kind = WeaponKind.Firearm, Damage = 34f, Range = 12f, Interval = 0.25f,
                NoiseRadius = 10f, MagazineSize = PrototypeContent.PistolMagazine, ReloadTime = 1.2f, Pellets = 1, SpreadDegrees = 2f,
                AmmoItemId = PrototypeContent.PistolAmmo.Id,
                RecoilPerShot = 3.5f, MaxRecoil = 14f, RecoilRecovery = 20f, CameraKick = 0.12f
            };

            _firearms[PrototypeContent.Shotgun.Id] = new WeaponDefinition
            {
                Name = "Shotgun", Sound = SoundId.ShotgunShot, Kind = WeaponKind.Firearm, Damage = 18f, Range = 7f, Interval = 0.9f,
                NoiseRadius = 17f, MagazineSize = 2, ReloadTime = 1.8f, Pellets = 7, SpreadDegrees = 24f,
                AmmoItemId = PrototypeContent.ShotgunShell.Id,
                RecoilPerShot = 7f, MaxRecoil = 12f, RecoilRecovery = 14f, CameraKick = 0.38f
            };
        }

        private void Start()
        {
            // Begin with the first equipped firearm in hand, if there is one.
            for (int slot = 0; slot < Loadout.SlotCount; slot++)
            {
                if (Loadout.GetWeapon(slot) == null) continue;
                _current = slot + 1;
                break;
            }
        }

        private void Update()
        {
            // The weapon in hand can vanish: unequipped in the inventory, or lost on death.
            if (_current > 0 && Loadout.GetWeapon(_current - 1) == null)
            {
                CancelReload();
                _current = 0;
            }

            _bloom.Tick(UnityEngine.Time.deltaTime, Current.RecoilRecovery);
        }

        /// <summary>Switches to a slot: 0 is the knife, 1 and 2 the firearm slots. Interrupts a reload in progress.</summary>
        public void Select(int index)
        {
            if (index < 0 || index > Loadout.SlotCount || index == _current) return;
            if (index > 0 && Loadout.GetWeapon(index - 1) == null)
            {
                GameRoot.Instance.Notify("Ô vũ khí " + (index + 1) + " đang trống. Mở balo (Tab) để trang bị.", 2f);
                return;
            }

            CancelReload();
            _current = index;
            _nextUseTime = UnityEngine.Time.time + swapDelay;
        }

        /// <summary>
        /// Equips a weapon item from a grid. With no slot given it takes the first empty slot,
        /// else replaces the firearm in hand. A replaced weapon goes back into the same grid
        /// and its rounds return to the backpack as ammo.
        /// </summary>
        public bool EquipFrom(GridInventory grid, PlacedItem item, int slot = -1)
        {
            var root = GameRoot.Instance;
            if (grid == null || item == null || DefinitionFor(item.Item) == null) return false;

            if (slot < 0) slot = Loadout.FirstEmptySlot();
            if (slot < 0) slot = _current > 0 ? _current - 1 : 0;

            string name = item.Item.DisplayName;
            if (!Loadout.Equip(grid, item, slot, out ItemDefinition replaced, out int rounds))
            {
                root.Notify("Không đủ chỗ để cất vũ khí đang cầm.", 2f);
                return false;
            }

            CancelReload();
            ReturnRounds(replaced, rounds);
            _current = slot + 1;
            _nextUseTime = UnityEngine.Time.time + swapDelay;
            Sfx.PlayUi(SoundId.Reload, 0.6f);
            root.Notify("Đã trang bị " + name + " vào ô " + (slot + 2) + ". Nhấn R để nạp đạn.", 2.5f);
            return true;
        }

        /// <summary>Puts the firearm in a slot back into the backpack, with its rounds as loose ammo.</summary>
        public bool Unequip(int slot)
        {
            var root = GameRoot.Instance;
            if (!Loadout.Unequip(slot, root.Session.Backpack, out ItemDefinition weapon, out int rounds))
            {
                if (Loadout.GetWeapon(slot) != null) root.Notify("Balo không đủ chỗ cho vũ khí này.", 2f);
                return false;
            }

            if (_current == slot + 1) CancelReload();
            ReturnRounds(weapon, rounds);
            return true;
        }

        private void ReturnRounds(ItemDefinition weapon, int rounds)
        {
            WeaponDefinition definition = DefinitionFor(weapon);
            if (definition == null || rounds <= 0) return;

            ItemDefinition ammo = PrototypeContent.FindItem(definition.AmmoItemId);
            if (ammo == null) return;

            var session = GameRoot.Instance.Session;
            int leftover = session.Backpack.Add(ammo, rounds);
            if (leftover > 0) GameRoot.Instance.Notify("Balo đầy: rơi mất " + leftover + " viên " + ammo.DisplayName + ".", 2.5f);
        }

        private void CancelReload()
        {
            StopAllCoroutines();
            IsReloading = false;
        }

        /// <summary>Attacks with the weapon in hand. Returns true if a shot or swing happened.</summary>
        public bool TryUse(Vector2 origin, Vector2 direction, Stamina stamina)
        {
            if (IsReloading || UnityEngine.Time.time < _nextUseTime) return false;
            if (direction.sqrMagnitude < 0.0001f) return false;
            direction.Normalize();

            WeaponDefinition weapon = Current;
            // At the very start of the game the player has not found their knife yet.
            if (weapon == _knife && !GameRoot.Instance.Session.HasKnife) return false;
            if (weapon.Kind == WeaponKind.Melee)
            {
                if (stamina != null && !stamina.TryUse(weapon.StaminaCost)) return false;
                Swing(weapon, origin, direction);
            }
            else
            {
                int slot = _current - 1;
                int rounds = Loadout.GetLoaded(slot);
                if (rounds <= 0)
                {
                    // Dry fire: an audible click, and a short pause so holding the button does not buzz.
                    Sfx.PlayUi(SoundId.EmptyClick, 0.7f);
                    _nextUseTime = UnityEngine.Time.time + 0.35f;
                    return false;
                }
                Loadout.SetLoaded(slot, rounds - 1);

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

        /// <summary>Refills the magazine of the firearm in hand from matching ammo in <paramref name="ammoSource"/>.</summary>
        public void Reload(GridInventory ammoSource)
        {
            WeaponDefinition weapon = Current;
            if (weapon.Kind != WeaponKind.Firearm || IsReloading || ammoSource == null) return;

            int slot = _current - 1;
            if (Loadout.GetLoaded(slot) >= weapon.MagazineSize) return;

            if (ammoSource.CountOf(weapon.AmmoItemId) <= 0)
            {
                GameRoot.Instance.Notify("Hết đạn cho " + weapon.Name + ".", 1.5f);
                return;
            }
            Sfx.PlayUi(SoundId.Reload, 0.8f);
            StartCoroutine(ReloadRoutine(weapon, slot, Loadout.GetWeapon(slot), ammoSource));
        }

        private IEnumerator ReloadRoutine(WeaponDefinition weapon, int slot, ItemDefinition item, GridInventory ammoSource)
        {
            IsReloading = true;
            yield return new WaitForSeconds(weapon.ReloadTime);
            IsReloading = false;

            // The weapon may have been swapped out while the reload was running.
            if (Loadout.GetWeapon(slot) != item) yield break;

            int needed = weapon.MagazineSize - Loadout.GetLoaded(slot);
            int loaded = Mathf.Min(needed, ammoSource.CountOf(weapon.AmmoItemId));
            if (loaded > 0 && ammoSource.TryConsume(weapon.AmmoItemId, loaded))
            {
                Loadout.SetLoaded(slot, Loadout.GetLoaded(slot) + loaded);
            }
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
                    if (hurtbox.Owner == null || hurtbox.Owner == _ownHealth || hurtbox.Owner.IsDead || hurtbox.Friendly) continue;
                    hurtbox.Owner.Damage(weapon.Damage, origin);
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
                if (hurtbox == null || hurtbox.Owner == null || hurtbox.Owner == _ownHealth || hurtbox.Owner.IsDead || hurtbox.Friendly) continue;

                Vector2 target = other.ClosestPoint(origin);
                Vector2 toTarget = target - origin;
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(direction, toTarget) > halfArc) continue;
                if (SightBlocker.IsBlocked(origin, target)) continue;

                hurtbox.Owner.Damage(weapon.Damage, origin);
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
