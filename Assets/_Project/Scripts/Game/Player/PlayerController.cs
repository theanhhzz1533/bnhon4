using Bnhon.Game.Combat;
using Bnhon.Game.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.Player
{
    /// <summary>
    /// Top-down twin-stick style control: WASD to move, mouse to aim,
    /// left click to fire, R to reload, E to interact, Shift to sprint.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float interactRadius = 1.3f;
        [SerializeField] private Transform aimPivot;

        private Rigidbody2D _body;
        private Camera _camera;
        private Gun _gun;
        private Vector2 _move;
        private bool _sprinting;

        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public IInteractable Focused { get; private set; }
        public Gun Gun => _gun;

        /// <summary>False while the player is being moved by the game (teleport, death).</summary>
        public bool ControlEnabled { get; set; } = true;

        public void SetAimPivot(Transform pivot)
        {
            aimPivot = pivot;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _gun = GetComponent<Gun>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null || !ControlEnabled)
            {
                _move = Vector2.zero;
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            _move = new Vector2(x, y);
            if (_move.sqrMagnitude > 1f) _move.Normalize();
            _sprinting = keyboard.leftShiftKey.isPressed;

            UpdateAim(mouse);

            if (_gun != null)
            {
                if (mouse.leftButton.isPressed) _gun.TryFire(transform.position, AimDirection);
                if (keyboard.rKey.wasPressedThisFrame) _gun.Reload(GameRoot.Instance.Session.Backpack);
            }

            Focused = FindInteractable();
            if (Focused != null && keyboard.eKey.wasPressedThisFrame) Focused.Interact(this);
        }

        private void FixedUpdate()
        {
            _body.linearVelocity = _move * (_sprinting ? sprintSpeed : walkSpeed);
        }

        public void Teleport(Vector2 position)
        {
            _body.linearVelocity = Vector2.zero;
            _body.position = position;
            transform.position = position;
        }

        private void UpdateAim(Mouse mouse)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            Vector2 direction = new Vector2(world.x - transform.position.x, world.y - transform.position.y);
            if (direction.sqrMagnitude < 0.0001f) return;

            AimDirection = direction.normalized;
            if (aimPivot != null)
            {
                float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
                aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private IInteractable FindInteractable()
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            IInteractable best = null;
            float bestDistance = float.MaxValue;

            foreach (Collider2D other in nearby)
            {
                var interactable = other.GetComponent<IInteractable>();
                if (interactable == null) continue;

                Vector2 closest = other.ClosestPoint(transform.position);
                float distance = (closest - (Vector2)transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = interactable;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
