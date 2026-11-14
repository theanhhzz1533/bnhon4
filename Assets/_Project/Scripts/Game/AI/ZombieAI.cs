using System.Collections.Generic;
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
    /// Walks around walls with <see cref="NavGrid"/>. A zombie that loses sight
    /// of the player goes to where it last saw them, not to where they are.
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

        [Header("Crowding")]
        [SerializeField] private float separationRadius = 1.25f;
        [SerializeField] private float separationStrength = 2.4f;

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
        private Vector2 _lastSeenPosition;
        private bool _seesTarget;

        private static readonly List<ZombieAI> Active = new List<ZombieAI>();

        private const float BodyRadius = 0.42f;
        private readonly List<Vector2> _path = new List<Vector2>();
        private int _pathIndex;
        private float _nextRepathTime;

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

            // Real art keeps its own colors; only the placeholder square is tinted by type.
            if (PlaceholderArt.IsPlaceholder(_renderer)) _renderer.color = color;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            Active.Add(this);
            Noise.Emitted += OnNoise;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            Active.Remove(this);
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

            _seesTarget = distance <= sightRange && !SightBlocker.IsBlocked(position, _target.position);
            if (_seesTarget)
            {
                _state = State.Chase;
                _lastSeenTime = now;
                _lastSeenPosition = _target.position;
            }
            else if (_state == State.Chase && now - _lastSeenTime > loseInterestTime)
            {
                _state = State.Wander;
            }

            switch (_state)
            {
                case State.Chase:
                    if (!_seesTarget) _velocity = Steer(position, _lastSeenPosition, chaseSpeed, now);
                    else if (Kind == ZombieKind.Screamer) UpdateScreamer(position, toTarget, distance, now);
                    else UpdateAttacker(position, distance, now);
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
                        _velocity = Steer(position, _investigatePoint, chaseSpeed * 0.8f, now);
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

            _velocity = SpreadOut(position, _velocity);
        }

        /// <summary>
        /// Pushes this zombie away from others standing too close, so a pack fans out around
        /// its target and files through gaps instead of piling onto one spot.
        /// </summary>
        private Vector2 SpreadOut(Vector2 position, Vector2 velocity)
        {
            Vector2 push = Vector2.zero;
            foreach (ZombieAI other in Active)
            {
                if (other == this) continue;

                Vector2 away = position - (Vector2)other.transform.position;
                float distance = away.magnitude;
                if (distance >= separationRadius) continue;

                if (distance < 0.01f)
                {
                    // Exactly on top of each other: pick any direction to break the tie.
                    away = UnityEngine.Random.insideUnitCircle;
                    distance = Mathf.Max(0.01f, away.magnitude);
                }
                push += away / distance * (1f - distance / separationRadius);
            }

            if (push.sqrMagnitude < 0.0001f) return velocity;

            Vector2 result = velocity + push * separationStrength;
            float limit = Mathf.Max(chaseSpeed, wanderSpeed) * 1.15f;
            float speed = result.magnitude;
            return speed > limit ? result / speed * limit : result;
        }

        private void FixedUpdate()
        {
            _body.linearVelocity = _velocity;
        }

        private void UpdateAttacker(Vector2 position, float distance, float now)
        {
            if (distance > attackRange)
            {
                _velocity = Steer(position, _target.position, chaseSpeed, now);
                return;
            }

            _velocity = Vector2.zero;
            if (now < _nextAttackTime || _targetHealth == null) return;
            _nextAttackTime = now + attackInterval;
            _targetHealth.Damage(attackDamage);
        }

        /// <summary>
        /// Velocity toward a destination. Walks straight when nothing is in the way; otherwise
        /// follows an A* path, re-planned a couple of times a second, skipping ahead to the
        /// farthest waypoint it can reach in a straight line.
        /// </summary>
        private Vector2 Steer(Vector2 position, Vector2 destination, float speed, float now)
        {
            Vector2 goal = destination;
            NavGrid nav = NavGrid.Instance;

            if (nav != null && nav.IsReady && nav.IsLineBlocked(position, destination, BodyRadius))
            {
                if (now >= _nextRepathTime)
                {
                    // Spread re-planning out so a pack does not all search on the same frame.
                    _nextRepathTime = now + 0.45f + UnityEngine.Random.value * 0.25f;
                    nav.TryFindPath(position, destination, _path);
                    _pathIndex = 0;
                }

                while (_pathIndex < _path.Count && (_path[_pathIndex] - position).sqrMagnitude < 0.09f) _pathIndex++;
                while (_pathIndex + 1 < _path.Count && !nav.IsLineBlocked(position, _path[_pathIndex + 1], BodyRadius)) _pathIndex++;

                if (_pathIndex < _path.Count) goal = _path[_pathIndex];
            }
            else
            {
                _path.Clear();
                _pathIndex = 0;
                _nextRepathTime = 0f;
            }

            Vector2 toGoal = goal - position;
            float distance = toGoal.magnitude;
            return distance > 0.01f ? toGoal / distance * speed : Vector2.zero;
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

            Color color = PlaceholderArt.IsPlaceholder(_renderer) ? _renderer.color : Color.white;
            color = Color.Lerp(color, Color.black, 0.55f);

            // Same parent as the zombie, so corpses are cleared with the raid zone.
            GameObject corpse = PlaceholderArt.Box("Corpse_" + Kind, transform.position, new Vector2(0.9f, 0.55f), color, transform.parent, 3, "corpse", false);
            corpse.AddComponent<BoxCollider2D>().isTrigger = true;
            corpse.AddComponent<LootContainer>().InitCorpse(table, UnityEngine.Random.Range(0, 3));
        }
    }
}
