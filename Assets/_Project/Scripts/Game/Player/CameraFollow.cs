using UnityEngine;

namespace Bnhon.Game.Player
{
    /// <summary>Follows the player and leans toward where they aim.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothing = 8f;
        [SerializeField] private float aimLead = 1.5f;

        [SerializeField] private float kickRecovery = 14f;

        private PlayerController _target;
        private Vector3 _smooth;
        private Vector3 _kick;
        private bool _initialized;

        public PlayerController Target
        {
            get => _target;
            set
            {
                if (_target != null && _target.Weapons != null) _target.Weapons.Fired -= OnFired;
                _target = value;
                if (_target != null && _target.Weapons != null) _target.Weapons.Fired += OnFired;
            }
        }

        private void OnDestroy()
        {
            if (_target != null && _target.Weapons != null) _target.Weapons.Fired -= OnFired;
        }

        /// <summary>A shot shoves the view away from where it was aimed; it settles back within a moment.</summary>
        private void OnFired(Vector2 direction, float strength)
        {
            _kick -= new Vector3(direction.x, direction.y, 0f) * strength;
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            if (!_initialized)
            {
                _smooth = transform.position;
                _initialized = true;
            }

            Vector2 focus = (Vector2)Target.transform.position + Target.AimDirection * aimLead;
            Vector3 goal = new Vector3(focus.x, focus.y, _smooth.z);
            float delta = UnityEngine.Time.deltaTime;
            _smooth = Vector3.Lerp(_smooth, goal, 1f - Mathf.Exp(-smoothing * delta));
            _kick = Vector3.Lerp(_kick, Vector3.zero, 1f - Mathf.Exp(-kickRecovery * delta));
            transform.position = _smooth + _kick;
        }

        public void SnapToTarget()
        {
            if (Target == null) return;
            Vector3 p = Target.transform.position;
            _smooth = new Vector3(p.x, p.y, transform.position.z);
            _kick = Vector3.zero;
            _initialized = true;
            transform.position = _smooth;
        }
    }
}
