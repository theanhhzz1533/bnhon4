using System;
using System.Collections;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using UnityEngine;

namespace Bnhon.Game.Combat
{
    /// <summary>
    /// Hitscan pistol. Every shot emits a <see cref="Noise"/>, which is how
    /// zombies hear the player.
    /// </summary>
    public sealed class Gun : MonoBehaviour
    {
        [SerializeField] private float damage = 34f;
        [SerializeField] private float range = 12f;
        [SerializeField] private float fireInterval = 0.25f;
        [SerializeField] private int magazineSize = 8;
        [SerializeField] private float reloadTime = 1.2f;
        [SerializeField] private float noiseRadius = 10f;

        private float _nextFireTime;

        public int AmmoInMagazine { get; private set; }
        public int MagazineSize => magazineSize;
        public bool IsReloading { get; private set; }

        private void Awake()
        {
            AmmoInMagazine = magazineSize;
        }

        public bool TryFire(Vector2 origin, Vector2 direction)
        {
            if (IsReloading || AmmoInMagazine <= 0 || UnityEngine.Time.time < _nextFireTime) return false;
            if (direction.sqrMagnitude < 0.0001f) return false;

            direction.Normalize();
            _nextFireTime = UnityEngine.Time.time + fireInterval;
            AmmoInMagazine--;

            Vector2 end = origin + direction * range;
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, range);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit2D hit in hits)
            {
                Collider2D other = hit.collider;
                if (other == null || other.isTrigger || other.gameObject == gameObject) continue;

                var health = other.GetComponent<Health>();
                if (health != null) health.Damage(damage);
                end = hit.point;
                break;
            }

            Tracer.Spawn(origin, end);
            Noise.Emit(origin, noiseRadius);
            return true;
        }

        /// <summary>Refills the magazine from pistol ammo in <paramref name="ammoSource"/>.</summary>
        public void Reload(GridInventory ammoSource)
        {
            if (IsReloading || AmmoInMagazine >= magazineSize || ammoSource == null) return;
            if (ammoSource.CountOf(PrototypeContent.PistolAmmo.Id) <= 0) return;
            StartCoroutine(ReloadRoutine(ammoSource));
        }

        private IEnumerator ReloadRoutine(GridInventory ammoSource)
        {
            IsReloading = true;
            yield return new WaitForSeconds(reloadTime);

            int needed = magazineSize - AmmoInMagazine;
            int available = ammoSource.CountOf(PrototypeContent.PistolAmmo.Id);
            int loaded = Mathf.Min(needed, available);
            if (loaded > 0 && ammoSource.TryConsume(PrototypeContent.PistolAmmo.Id, loaded))
            {
                AmmoInMagazine += loaded;
            }
            IsReloading = false;
        }

        private void OnDisable()
        {
            IsReloading = false;
        }
    }
}
