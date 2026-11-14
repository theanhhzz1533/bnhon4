using Bnhon.Core.Content;
using Bnhon.Core.Loot;
using Bnhon.Game.Combat;
using Bnhon.Game.Interaction;
using Bnhon.Game.Vision;
using UnityEngine;

namespace Bnhon.Game.AI
{
    public enum ZombieKind
    {
        /// <summary>Slow and tough. Pressure in numbers.</summary>
        Walker,

        /// <summary>Fast and fragile. Outruns a walking player, not a sprinting one.</summary>
        Runner,

        /// <summary>Never attacks. Screams on sight to call every zombie nearby, then backs away.</summary>
        Screamer
    }

    /// <summary>
    /// Zombie brain: wanders, investigates noises it can hear, reacts to the
    /// player on sight. What "reacts" means depends on <see cref="ZombieKind"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public sealed class ZombieAI : MonoBehaviour
    {
        private enum State
        {
            Wander,
            Investigate,
            Chase
        }

        [SerializeField] private float wanderSpeed = 1f;
        [SerializeField] private float chaseSpeed = 2.7f;
        [SerializeField] private float sightRange = 6f;
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float loseInterestTime = 4f;
        [SerializeField] private float investigateTimeout = 8f;

        [Header("Screamer")]
        [SerializeField] private float screamRadius = 16f;
        [SerializeField] private float screamInterval = 8f;
        [SerializeField] private float keepAwayDistance = 5f;

        private Rigidbody2D _body;
        private Health _health;
        private SpriteRenderer _renderer;
        private Transform _target;
        private Health _targetHealth;
        private State _state = State.Wander;
        private Vector2 _velocity;
        private Vector2 _wanderDirection;
        private Vector2 _investigatePoint;
        private float _investigateUntil;
        private float _nextWanderChange;
        private float _nextAttackTime;
        private float _nextScreamTime;
        private float _lastSeenTime;

        public ZombieKind Kind { get; private set; } = ZombieKind.Walker;

        public void Init(Transform target, ZombieKind kind)
        {
            _target = target;
            _targetHealth = target != null ? target.GetComponent<Health>() : null;
            Configure(kind);
        }

        /// <summary>Applies the stats and placeholder color of a zombie type.</summary>
        private void Configure(ZombieKind kind)
        {
            Kind = kind;
            Color color;

            switch (kind)
            {
                case ZombieKind.Runner:
                    _health.SetMax(55f);
                    chaseSpeed = 4.8f;
                    sightRange = 7.5f;
                    attackDamage = 10f;
                    attackInterval = 0.7f;
                    color = new Color(0.9f, 0.5f, 0.2f);
                    break;

                case ZombieKind.Screamer:
                    _health.SetMax(45f);
                    chaseSpeed = 2.2f;
                    sightRange = 9f;
                    color = new Color(0.7f, 0.35f, 0.85f);
                    break;

                default:
                    _health.SetMax(100f);
                    color = new Color(0.45f, 0.65f, 0.3f);
                    break;
            }

            if (_renderer != null) _renderer.color = color;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            Noise.Emitted += OnNoise;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            Noise.Emitted -= OnNoise;
            _health.Died -= OnDied;
        }

        private void Update()
        {
            if (_target == null)
            {
                _velocity = Vector2.zero;
                return;
            }

            Vector2 position = transform.position;

            // Fog of war: only drawn while the player can actually see this spot.
            if (_renderer != null)
            {
                VisionSystem vision = VisionSystem.Instance;
                _renderer.enabled = vision == null || vision.IsVisible(position);
            }

            Vector2 toTarget = (Vector2)_target.position - position;
            float distance = toTarget.magnitude;
            float now = UnityEngine.Time.time;

            if (distance <= sightRange && !SightBlocker.IsBlocked(position, _target.position))
            {
                _state = State.Chase;
                _lastSeenTime = now;
            }
            else if (_state == State.Chase && now - _lastSeenTime > loseInterestTime)
            {
                _state = State.Wander;
            }

            switch (_state)
            {
                case State.Chase:
                    if (Kind == ZombieKind.Screamer) UpdateScreamer(position, toTarget, distance, now);
                    else UpdateAttacker(toTarget, distance, now);
                    break;

                case State.Investigate:
                    Vector2 toPoint = _investigatePoint - position;
                    if (toPoint.sqrMagnitude < 0.25f || now > _investigateUntil)
                    {
                        _state = State.Wander;
                        _velocity = Vector2.zero;
                    }
                    else
                    {
                        _velocity = toPoint.normalized * chaseSpeed * 0.8f;
                    }
                    break;

                default:
                    if (now >= _nextWanderChange)
                    {
                        _nextWanderChange = now + UnityEngine.Random.Range(2f, 4f);
                        _wanderDirection = UnityEngine.Random.value < 0.3f
                            ? Vector2.zero
                            : UnityEngine.Random.insideUnitCircle.normalized;
                    }
                    _velocity = _wanderDirection * wanderSpeed;
                    break;
            }
        }

        private void FixedUpdate()
        {
            _body.linearVelocity = _velocity;
        }

        private void UpdateAttacker(Vector2 toTarget, float distance, float now)
        {
            if (distance > attackRange)
            {
                _velocity = toTarget / distance * chaseSpeed;
                return;
            }

            _velocity = Vector2.zero;
            if (now < _nextAttackTime || _targetHealth == null) return;
            _nextAttackTime = now + attackInterval;
            _targetHealth.Damage(attackDamage);
        }

        private void UpdateScreamer(Vector2 position, Vector2 toTarget, float distance, float now)
        {
            if (now >= _nextScreamTime)
            {
                _nextScreamTime = now + screamInterval;
                // Heard around the screamer, but it sends the pack to where the player is.
                Noise.Emit(position, screamRadius, _target.position);
                if (GameRoot.Instance != null) GameRoot.Instance.Notify("Zombie gào đang gọi bầy!", 2.5f);
            }

            // Back away when the player closes in; otherwise stand and keep screaming.
            _velocity = distance < keepAwayDistance && distance > 0.01f
                ? toTarget / distance * -chaseSpeed
                : Vector2.zero;
        }

        private void OnNoise(Vector2 source, float radius, Vector2 pointOfInterest)
        {
            if (_state == State.Chase) return;
            if (((Vector2)transform.position - source).sqrMagnitude > radius * radius) return;

            _investigatePoint = pointOfInterest;
            _investigateUntil = UnityEngine.Time.time + investigateTimeout;
            _state = State.Investigate;
        }

        private void OnDied(Health health)
        {
            SpawnCorpse();
            Destroy(gameObject);
        }

        /// <summary>Leaves a searchable body where the zombie fell. It carries zero to two items, by zombie type.</summary>
        private void SpawnCorpse()
        {
            LootTable table;
            switch (Kind)
            {
                case ZombieKind.Runner: table = PrototypeContent.RunnerCorpse; break;
                case ZombieKind.Screamer: table = PrototypeContent.ScreamerCorpse; break;
                default: table = PrototypeContent.WalkerCorpse; break;
            }

            Color color = _renderer != null ? _renderer.color : Color.white;
            color = Color.Lerp(color, Color.black, 0.55f);

            // Same parent as the zombie, so corpses are cleared with the raid zone.
            GameObject corpse = PlaceholderArt.Box("Corpse_" + Kind, transform.position, new Vector2(0.9f, 0.55f), color, transform.parent, 3);
            corpse.AddComponent<BoxCollider2D>().isTrigger = true;
            corpse.AddComponent<LootContainer>().InitCorpse(table, UnityEngine.Random.Range(0, 3));
        }
    }
}
