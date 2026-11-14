using Bnhon.Game.Combat;
using UnityEngine;

namespace Bnhon.Game.AI
{
    /// <summary>
    /// Basic walker: wanders, investigates gunshots it can hear, chases the
    /// player on sight and hits at melee range.
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

        private Rigidbody2D _body;
        private Health _health;
        private Transform _target;
        private Health _targetHealth;
        private State _state = State.Wander;
        private Vector2 _velocity;
        private Vector2 _wanderDirection;
        private Vector2 _investigatePoint;
        private float _nextWanderChange;
        private float _nextAttackTime;
        private float _lastSeenTime;

        public void Init(Transform target)
        {
            _target = target;
            _targetHealth = target != null ? target.GetComponent<Health>() : null;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            Gun.NoiseEmitted += OnNoise;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            Gun.NoiseEmitted -= OnNoise;
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
            Vector2 toTarget = (Vector2)_target.position - position;
            float distance = toTarget.magnitude;
            float now = UnityEngine.Time.time;

            if (distance <= sightRange && HasLineOfSight(position, _target.position))
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
                    if (distance <= attackRange)
                    {
                        _velocity = Vector2.zero;
                        TryAttack(now);
                    }
                    else
                    {
                        _velocity = toTarget / distance * chaseSpeed;
                    }
                    break;

                case State.Investigate:
                    Vector2 toPoint = _investigatePoint - position;
                    if (toPoint.sqrMagnitude < 0.25f)
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

        private void TryAttack(float now)
        {
            if (now < _nextAttackTime || _targetHealth == null) return;
            _nextAttackTime = now + attackInterval;
            _targetHealth.Damage(attackDamage);
        }

        private static bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            RaycastHit2D[] hits = Physics2D.LinecastAll(from, to);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.GetComponent<SightBlocker>() != null) return false;
            }
            return true;
        }

        private void OnNoise(Vector2 position, float radius)
        {
            if (_state == State.Chase) return;
            if (((Vector2)transform.position - position).sqrMagnitude > radius * radius) return;

            _investigatePoint = position;
            _state = State.Investigate;
        }

        private void OnDied(Health health)
        {
            Destroy(gameObject);
        }
    }
}
