using Bnhon.Core.Vitals;
using Bnhon.Game.Audio;
using Bnhon.Game.Combat;
using Bnhon.Game.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bnhon.Game.Player
{
    /// <summary>
    /// Top-down control: WASD to move, mouse to aim, left click to attack,
    /// 1-3 to pick a weapon, R to reload, Shift to sprint and Space to roll
    /// (both cost stamina), Q to use a healing item, E to interact.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float usingItemSpeedFactor = 0.45f;
        [SerializeField] private float sprintStaminaPerSecond = 16f;
        [SerializeField] private float interactRadius = 1.3f;
        [SerializeField] private float sprintNoiseRadius = 5.5f;
        [SerializeField] private float rollNoiseRadius = 4f;
        [SerializeField] private Transform aimPivot;

        [Header("Dodge roll")]
        [SerializeField] private float rollSpeed = 11f;
        [SerializeField] private float rollDuration = 0.28f;
        [SerializeField] private float rollCooldown = 0.25f;
        [SerializeField] private float rollStaminaCost = 25f;

        private Rigidbody2D _body;
        private Camera _camera;
        private Health _health;
        private PlayerWeapons _weapons;
        private PlayerVitals _vitals;
        private Vector2 _move;
        private bool _sprinting;
        private Vector2 _rollDirection;
        private float _rollUntil;
        private float _nextRollTime;
        private float _stepTimer;
        private bool _slowed;

        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public IInteractable Focused { get; private set; }
        public PlayerWeapons Weapons => _weapons;
        public bool IsRolling => UnityEngine.Time.time < _rollUntil;

        /// <summary>False while the player is being moved by the game or a screen is open.</summary>
        public bool ControlEnabled { get; set; } = true;

        public void SetAimPivot(Transform pivot)
        {
            aimPivot = pivot;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _weapons = GetComponent<PlayerWeapons>();
            _vitals = GetComponent<PlayerVitals>();
        }

        private void Update()
        {
            // A roll cannot be hurt; this also ends the protection on the frame the roll ends.
            if (_health != null) _health.Invulnerable = IsRolling;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            // The key that closed a menu this frame must not also roll or interact.
            bool menuJustClosed = Bnhon.Game.UI.ModalScreen.ClosedFrame == UnityEngine.Time.frameCount;
            if (keyboard == null || mouse == null || !ControlEnabled || menuJustClosed || GameRoot.Instance == null)
            {
                _move = Vector2.zero;
                _sprinting = false;
                _slowed = false;
                return;
            }

            var session = GameRoot.Instance.Session;
            Stamina stamina = session.Stamina;

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            _move = new Vector2(x, y);
            if (_move.sqrMagnitude > 1f) _move.Normalize();

            UpdateAim(mouse);

            if (IsRolling)
            {
                _sprinting = false;
                return;
            }

            bool moving = _move.sqrMagnitude > 0.01f;
            bool busy = _vitals != null && _vitals.IsUsing;
            // A click or a roll abandons the bandage or meal in progress, so the player can react.
            if (busy && (mouse.leftButton.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                _vitals.CancelUse("Hủy");
                busy = false;
            }
            _slowed = busy;

            _sprinting = moving
                && !busy
                && keyboard.leftShiftKey.isPressed
                && stamina.Drain(sprintStaminaPerSecond * UnityEngine.Time.deltaTime);

            if (!moving)
            {
                _stepTimer = 0.08f;
            }
            else
            {
                _stepTimer -= UnityEngine.Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    _stepTimer = _sprinting ? 0.26f : 0.38f;
                    Sfx.PlayUi(SoundId.Footstep, _sprinting ? 0.45f : 0.28f);
                    // Running is loud enough for nearby zombies to come looking. Walking is silent.
                    if (_sprinting) Noise.Emit(transform.position, sprintNoiseRadius);
                }
            }

            if (keyboard.spaceKey.wasPressedThisFrame && UnityEngine.Time.time >= _nextRollTime && stamina.TryUse(rollStaminaCost))
            {
                _rollDirection = moving ? _move.normalized : AimDirection;
                _rollUntil = UnityEngine.Time.time + rollDuration;
                _nextRollTime = _rollUntil + rollCooldown;
                if (_health != null) _health.Invulnerable = true;
                Sfx.PlayUi(SoundId.Roll, 0.7f);
                Noise.Emit(transform.position, rollNoiseRadius);
                return;
            }

            if (_weapons != null && !busy)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) _weapons.Select(0);
                if (keyboard.digit2Key.wasPressedThisFrame) _weapons.Select(1);
                if (keyboard.digit3Key.wasPressedThisFrame) _weapons.Select(2);

                if (mouse.leftButton.isPressed) _weapons.TryUse(transform.position, AimDirection, stamina);
                if (keyboard.rKey.wasPressedThisFrame) _weapons.Reload(session.Backpack);
            }

            if (_vitals != null && keyboard.qKey.wasPressedThisFrame) _vitals.QuickHeal();

            Focused = FindInteractable();
            if (Focused != null && keyboard.eKey.wasPressedThisFrame) Focused.Interact(this);
            if (keyboard.fKey.wasPressedThisFrame && Focused is FarmPlotView plot) plot.AlternateAction();
        }

        private void FixedUpdate()
        {
            if (IsRolling)
            {
                _body.linearVelocity = _rollDirection * rollSpeed;
                return;
            }
            float speed = _sprinting ? sprintSpeed : walkSpeed;
            if (_slowed) speed *= usingItemSpeedFactor;
            if (GameRoot.Instance != null) speed *= GameRoot.Instance.Session.Buffs.Multiplier(BuffStat.MoveSpeed);
            _body.linearVelocity = _move * speed;
        }

        public void Teleport(Vector2 position)
        {
            _rollUntil = 0f;
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
