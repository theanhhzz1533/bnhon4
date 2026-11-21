using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.Player
{
    /// <summary>Follows the player and leans toward where they aim.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothing = 8f;
        [SerializeField] private float aimLead = 1.5f;

        [SerializeField] private float kickRecovery = 14f;
        [SerializeField] private float minZoom = 3.5f;
        [SerializeField] private float maxZoom = 7.5f;

        private Camera _camera;
        private float _zoom = -1f;

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
            Zoom(delta);
        }

        /// <summary>The mouse wheel zooms in and out, except while a menu (which may scroll) is open.</summary>
        private void Zoom(float delta)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null || !_camera.orthographic) return;
            if (_zoom < 0f) _zoom = _camera.orthographicSize;

            Mouse mouse = Mouse.current;
            if (mouse != null && Bnhon.Game.UI.ModalScreen.Current == null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel != 0f) _zoom = Mathf.Clamp(_zoom - Mathf.Sign(wheel) * 0.5f, minZoom, maxZoom);
            }
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _zoom, 1f - Mathf.Exp(-10f * delta));
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
