using System;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>
    /// Stand inside for a few seconds to leave the raid with everything in the backpack.
    /// Some exits are locked until a condition is met; while locked they are drawn in red
    /// and standing in them only says why.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ExtractionZone : MonoBehaviour
    {
        private static readonly Color OpenColor = new Color(0.2f, 0.8f, 0.35f, 0.45f);
        private static readonly Color LockedColor = new Color(0.9f, 0.3f, 0.2f, 0.4f);

        [SerializeField] private float holdSeconds = 3f;

        private float _timer;
        private bool _playerInside;
        private Func<string> _lockReason;
        private SpriteRenderer _renderer;
        private bool _wasLocked;

        /// <summary>Seconds left, or a negative number when the player is not inside or the exit is locked.</summary>
        public float Remaining => _playerInside && LockReason == null ? Mathf.Max(0f, holdSeconds - _timer) : -1f;

        /// <summary>Why this exit cannot be used yet, or null if it is open.</summary>
        public string LockReason => _lockReason != null ? _lockReason() : null;

        public bool PlayerInside => _playerInside;

        /// <summary>The exit the player is standing in, if any.</summary>
        public static ExtractionZone Active { get; private set; }

        /// <param name="lockReason">Returns why the exit is closed, or null once it is open. Null for an exit that is always open.</param>
        public void Init(Func<string> lockReason)
        {
            _lockReason = lockReason;
            Recolor(true);
        }

        private void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            _playerInside = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _playerInside = true;
            _timer = 0f;
            Active = this;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _playerInside = false;
            _timer = 0f;
            if (Active == this) Active = null;
        }

        private void Update()
        {
            Recolor(false);
            if (!_playerInside) return;

            if (LockReason != null)
            {
                _timer = 0f;
                return;
            }

            _timer += UnityEngine.Time.deltaTime;
            if (_timer < holdSeconds) return;

            _playerInside = false;
            _timer = 0f;
            if (Active == this) Active = null;
            RaidDirector.Instance.Extract();
        }

        private void Recolor(bool force)
        {
            if (_renderer == null || !PlaceholderArt.IsPlaceholder(_renderer)) return;
            bool locked = LockReason != null;
            if (!force && locked == _wasLocked) return;
            _wasLocked = locked;
            _renderer.color = locked ? LockedColor : OpenColor;
        }
    }
}
