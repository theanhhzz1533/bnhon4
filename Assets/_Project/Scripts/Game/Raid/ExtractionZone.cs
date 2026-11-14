using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>Stand inside for a few seconds to leave the raid with everything in the backpack.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ExtractionZone : MonoBehaviour
    {
        [SerializeField] private float holdSeconds = 3f;

        private float _timer;
        private bool _playerInside;

        /// <summary>Seconds left, or a negative number when the player is not inside.</summary>
        public float Remaining => _playerInside ? Mathf.Max(0f, holdSeconds - _timer) : -1f;

        public static ExtractionZone Active { get; private set; }

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _playerInside = true;
            _timer = 0f;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _playerInside = false;
            _timer = 0f;
        }

        private void Update()
        {
            if (!_playerInside) return;

            _timer += UnityEngine.Time.deltaTime;
            if (_timer < holdSeconds) return;

            _playerInside = false;
            _timer = 0f;
            RaidDirector.Instance.Extract();
        }
    }
}
