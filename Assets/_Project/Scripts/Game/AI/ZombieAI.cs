using System.Collections.Generic;
using Bnhon.Core.Content;
using Bnhon.Core.Loot;
using Bnhon.Game.Audio;
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
        Screamer,

        /// <summary>Slow, hits hard, and shrugs off most damage from the front. Turns slowly: get behind it.</summary>
        Armored,

        /// <summary>One per area. Huge health, fast, partly armoured in front.</summary>
        Boss
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
        [SerializeField] private float attackDamage = 18f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float loseInterestTime = 4f;
        [SerializeField] private float investigateTimeout = 14f;

        [Tooltip("How much of a noise's reach is left when a wall stands between it and this zombie.")]
        [SerializeField] private float wallMuffle = 0.5f;

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
        private Transform _player;
        private Health _playerHealth;
        private float _nextTargetCheck;
        private float _stunnedUntil;
        private State _state = State.Wander;
        private Vector2 _velocity;
        private Vector2 _wanderDirection;
        private Vector2 _investigatePoint;
        private float _investigateUntil;
        private float _nextWanderChange;
        private float _nextAttackTime;
        private float _nextScreamTime;
        private float _nextGrowlTime;
        private float _nextStepTime;

        // Armour: which way the zombie faces, how fast it can turn, and how much of a frontal hit is stopped.
        private Vector2 _facing = Vector2.down;
        private float _turnDegreesPerSecond = 100000f;
        private Bnhon.Core.Combat.Shield _shield;
        private Transform _plate;
        private SpriteRenderer[] _extras;
        private float _nextClangTime;
        private static bool _armorHintShown;
        private float _lastSeenTime;
        private Vector2 _lastSeenPosition;
        private bool _seesTarget;

        private static readonly List<ZombieAI> Active = new List<ZombieAI>();

        private const float BodyRadius = 0.42f;

        /// <summary>Beyond this distance from the player a wandering zombie does nothing at all.</summary>
        private const float DormantDistance = 40f;

        /// <summary>
        /// Goes for whoever is nearest: the player, or a survivor they are escorting. Checked a
        /// couple of times a second, not every frame.
        /// </summary>
        private void PickTarget(Vector2 position)
        {
            _nextTargetCheck = UnityEngine.Time.time + 0.4f + UnityEngine.Random.value * 0.2f;
            Transform best = _player;
            Health bestHealth = _playerHealth;
            float bestDistance = ((Vector2)_player.position - position).sqrMagnitude;

            foreach (SurvivorAI survivor in SurvivorAI.All)
            {
                if (survivor == null || survivor.Health.IsDead) continue;
                float distance = ((Vector2)survivor.transform.position - position).sqrMagnitude;
                if (distance >= bestDistance || distance > sightRange * sightRange * 1.5f) continue;
                best = survivor.transform;
                bestHealth = survivor.Health;
                bestDistance = distance;
            }

            _target = best;
            _targetHealth = bestHealth;
        }

        private void SetDrawn(bool visible)
        {
            if (_renderer != null) _renderer.enabled = visible;
            if (_extras == null) return;
            foreach (SpriteRenderer extra in _extras)
            {
                if (extra != null) extra.enabled = visible;
            }
        }
        private readonly List<Vector2> _path = new List<Vector2>();
        private int _pathIndex;
        private float _nextRepathTime;

        public ZombieKind Kind { get; private set; } = ZombieKind.Walker;

        public static int AliveCount => Active.Count;

        /// <summary>Freezes the zombie in place for a moment (the Shock charm).</summary>
        public void Stun(float seconds)
        {
            if (seconds <= 0f || Kind == ZombieKind.Boss) return;
            _stunnedUntil = Mathf.Max(_stunnedUntil, UnityEngine.Time.time + seconds);
        }

        /// <summary>True if a living zombie is within a distance of a point.</summary>
        public static bool AnyAliveWithin(Vector2 point, float radius)
        {
            foreach (ZombieAI zombie in Active)
            {
                if (zombie != null && ((Vector2)zombie.transform.position - point).sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        /// <summary>Sends this zombie to look at a place for a while, unless it is already chasing.</summary>
        public void Investigate(Vector2 point, float seconds)
        {
            if (_state == State.Chase) return;
            _investigatePoint = point;
            _investigateUntil = UnityEngine.Time.time + seconds;
            _state = State.Investigate;
        }

        public void Init(Transform target, ZombieKind kind)
        {
            _target = target;
            _targetHealth = target != null ? target.GetComponent<Health>() : null;
            _player = _target;
            _playerHealth = _targetHealth;
            Configure(kind);
            _extras = GetComponentsInChildren<SpriteRenderer>();
            _nextGrowlTime = UnityEngine.Time.time + UnityEngine.Random.Range(0.5f, 5f);
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

                case ZombieKind.Armored:
                    _health.SetMax(220f);
                    chaseSpeed = 2.1f;
                    attackDamage = 26f;
                    attackInterval = 1.2f;
                    color = new Color(0.5f, 0.55f, 0.62f);
                    SetArmor(0.75f, 150f, 110f);
                    break;

                case ZombieKind.Boss:
                    _health.SetMax(900f);
                    chaseSpeed = 3.3f;
                    sightRange = 10f;
                    attackDamage = 32f;
                    attackInterval = 0.9f;
                    loseInterestTime = 10f;
                    color = new Color(0.6f, 0.15f, 0.15f);
                    SetArmor(0.5f, 320f, 200f);
                    break;

                default:
                    _health.SetMax(100f);
                    color = new Color(0.45f, 0.65f, 0.3f);
                    break;
            }

            // Real art keeps its own colors; only the placeholder square is tinted by type.
            if (PlaceholderArt.IsPlaceholder(_renderer)) _renderer.color = color;
        }

        /// <summary>
        /// Gives the zombie a shield, shown as a plate held out in the direction it faces.
        /// Shooting the plate wears it down; when it breaks the zombie is as soft as any other.
        /// </summary>
        private void SetArmor(float reduction, float durability, float turnDegreesPerSecond)
        {
            _shield = new Bnhon.Core.Combat.Shield(reduction, durability);
            _turnDegreesPerSecond = turnDegreesPerSecond;
            _health.Modifier = ShieldDamage;

            GameObject plate = PlaceholderArt.Box("Plate", transform.position, new Vector2(0.9f, 0.16f), PlateColor, transform, 16);
            _plate = plate.transform;
            _plate.name = "Plate";
        }

        private static readonly Color PlateColor = new Color(0.15f, 0.16f, 0.19f);
        private static readonly Color PlateWornColor = new Color(0.85f, 0.4f, 0.2f);

        private float ShieldDamage(float amount, Vector2 from)
        {
            if (_shield == null || _shield.IsBroken) return amount;

            Vector2 position = transform.position;
            float wear = CharmEffects.More(Bnhon.Core.Charms.CharmStat.ShieldBreak);
            float through = _shield.Absorb(amount, _facing.x, _facing.y, from.x - position.x, from.y - position.y, wear);
            if (through >= amount) return amount;

            if (_shield.IsBroken)
            {
                BreakShield(position);
                return through;
            }

            // The plate goes from dark steel toward glowing orange as it is about to give.
            if (_plate != null)
            {
                var plateRenderer = _plate.GetComponent<SpriteRenderer>();
                if (plateRenderer != null) plateRenderer.color = Color.Lerp(PlateWornColor, PlateColor, _shield.Fraction);
            }

            float now = UnityEngine.Time.time;
            if (now >= _nextClangTime)
            {
                _nextClangTime = now + 0.12f;
                Sfx.Play(SoundId.EmptyClick, position, 1f, 20f);
            }
            if (!_armorHintShown && GameRoot.Instance != null)
            {
                _armorHintShown = true;
                GameRoot.Instance.Notify("Đạn găm vào tấm chắn! Bắn tiếp cho vỡ, hoặc vòng ra hông và sau lưng.", 7f);
            }
            return through;
        }

        private void BreakShield(Vector2 position)
        {
            if (_plate != null) Destroy(_plate.gameObject);
            _plate = null;
            _turnDegreesPerSecond = 100000f;
            Sfx.Play(SoundId.ZombieHit, position, 1f, 22f);
            Sfx.Play(SoundId.Reload, position, 0.9f, 22f);
            if (!_shieldBreakHintShown && GameRoot.Instance != null)
            {
                _shieldBreakHintShown = true;
                GameRoot.Instance.Notify("Tấm chắn vỡ rồi! Giờ nó trúng đạn như zombie thường.", 4f);
            }
        }

        private static bool _shieldBreakHintShown;

        /// <summary>Turns toward where it is looking or going, no faster than its turn rate, and moves the plate with it.</summary>
        private void UpdateFacing(Vector2 toTarget, float distance)
        {
            Vector2 wanted = _facing;
            if (_state == State.Chase && _seesTarget && distance > 0.01f) wanted = toTarget / distance;
            else if (_velocity.sqrMagnitude > 0.04f) wanted = _velocity.normalized;

            float current = Mathf.Atan2(_facing.y, _facing.x) * Mathf.Rad2Deg;
            float target = Mathf.Atan2(wanted.y, wanted.x) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(current, target);
            float step = _turnDegreesPerSecond * UnityEngine.Time.deltaTime;
            float angle = current + Mathf.Clamp(delta, -step, step);
            float radians = angle * Mathf.Deg2Rad;
            _facing = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            if (_plate == null) return;
            _plate.localPosition = new Vector3(_facing.x * 0.62f, _facing.y * 0.62f, 0f);
            _plate.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
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
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            Active.Remove(this);
            Noise.Emitted -= OnNoise;
            _health.Died -= OnDied;
            _health.Damaged -= OnDamaged;
        }

        private void Update()
        {
            if (_player == null)
            {
                _velocity = Vector2.zero;
                return;
            }

            Vector2 position = transform.position;
            if (UnityEngine.Time.time < _stunnedUntil)
            {
                _velocity = Vector2.zero;
                return;
            }
            if (UnityEngine.Time.time >= _nextTargetCheck) PickTarget(position);

            // Far from the player and with nothing to do: stand still and skip the rest.
            // Noise still reaches it (OnNoise), which wakes it by changing its state.
            if (_state == State.Wander && ((Vector2)_target.position - position).sqrMagnitude > DormantDistance * DormantDistance)
            {
                _velocity = Vector2.zero;
                if (_renderer != null && _renderer.enabled) SetDrawn(false);
                return;
            }

            // Fog of war: only drawn while the player can actually see this spot.
            if (_renderer != null)
            {
                VisionSystem vision = VisionSystem.Instance;
                bool visible = vision == null || vision.IsVisible(position);
                _renderer.enabled = visible;
                // Armour plates and markers are hidden with the body, or they would give it away in the fog.
                if (_extras != null)
                {
                    foreach (SpriteRenderer extra in _extras)
                    {
                        if (extra != null) extra.enabled = visible;
                    }
                }
            }

            Vector2 toTarget = (Vector2)_target.position - position;
            float distance = toTarget.magnitude;
            float now = UnityEngine.Time.time;

            // Heard whether or not it is seen: the main warning of what is in the fog.
            if (now >= _nextGrowlTime)
            {
                _nextGrowlTime = now + UnityEngine.Random.Range(3f, 7f);
                Sfx.Play(SoundId.ZombieGrowl, position, _state == State.Chase ? 0.9f : 0.55f, 15f);
            }

            UpdateFacing(toTarget, distance);

            // Footsteps: slow scuffs from a walker, a quick patter from anything running at you.
            float pace = _body != null ? _body.linearVelocity.magnitude : 0f;
            if (pace > 0.4f && now >= _nextStepTime)
            {
                _nextStepTime = now + Mathf.Clamp(1.15f / pace, 0.19f, 0.62f);
                float loudness = Mathf.Clamp(0.22f + pace * 0.09f, 0.25f, 0.7f);
                Sfx.Play(SoundId.ZombieStep, position, loudness, 11f);
            }

            float sight = sightRange * CharmEffects.Less(Bnhon.Core.Charms.CharmStat.ZombieSight);
            _seesTarget = distance <= sight && !SightBlocker.IsBlocked(position, _target.position);
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
                Sfx.Play(SoundId.ZombieScream, position, 1f, 30f);
                if (GameRoot.Instance != null) GameRoot.Instance.Notify("Zombie gào đang gọi bầy!", 2.5f);
            }

            // Back away when the player closes in; otherwise stand and keep screaming.
            _velocity = distance < keepAwayDistance && distance > 0.01f
                ? toTarget / distance * -chaseSpeed
                : Vector2.zero;
        }

        /// <summary>Half of all corpses are empty; a few carry two items.</summary>
        private static int RollCorpseItems()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.5f) return 0;
            return roll < 0.87f ? 1 : 2;
        }

        private void OnNoise(Vector2 source, float radius, Vector2 pointOfInterest)
        {
            if (_state == State.Chase) return;

            // Walls muffle sound: with one in the way, a noise carries only part of the distance.
            // Out of reach even with nothing in the way: no need to check for walls.
            Vector2 position = transform.position;
            float distanceSq = (position - source).sqrMagnitude;
            if (distanceSq > radius * radius) return;
            if (SightBlocker.IsBlocked(source, position)) radius *= wallMuffle;
            if (distanceSq > radius * radius) return;

            Investigate(pointOfInterest, investigateTimeout);
        }

        private void OnDamaged(Health health)
        {
            if (!health.IsDead) Sfx.Play(SoundId.ZombieHit, transform.position, 0.8f);
        }

        private void OnDied(Health health)
        {
            Sfx.Play(SoundId.ZombieDeath, transform.position, 0.9f);
            if (Kind == ZombieKind.Boss) OnBossDied();
            SpawnCorpse();
            Destroy(gameObject);
        }

        /// <summary>The area's boss stays dead: the flag is set at once, so dying on the way home does not bring it back.</summary>
        private static void OnBossDied()
        {
            var director = Bnhon.Game.Raid.RaidDirector.Instance;
            var area = director != null ? director.CurrentArea : null;
            if (area == null || area.BossFlag == null || GameRoot.Instance == null) return;

            GameRoot.Instance.Session.Story.Set(area.BossFlag);
            GameRoot.Instance.Notify(area.BossName + " đã gục! Lục xác nó, rồi sống sót mà mang đồ về.", 7f);
            Sfx.PlayUi(SoundId.Extract, 0.8f);
        }

        /// <summary>Leaves a searchable body where the zombie fell. It carries zero to two items, by zombie type.</summary>
        private void SpawnCorpse()
        {
            LootTable table;
            switch (Kind)
            {
                case ZombieKind.Runner: table = PrototypeContent.RunnerCorpse; break;
                case ZombieKind.Screamer: table = PrototypeContent.ScreamerCorpse; break;
                case ZombieKind.Armored: table = PrototypeContent.ArmoredCorpse; break;
                default: table = PrototypeContent.WalkerCorpse; break;
            }

            Color color = PlaceholderArt.IsPlaceholder(_renderer) ? _renderer.color : Color.white;
            color = Color.Lerp(color, Color.black, 0.55f);

            // Same parent as the zombie, so corpses are cleared with the raid zone.
            GameObject corpse = PlaceholderArt.Box("Corpse_" + Kind, transform.position, new Vector2(0.9f, 0.55f), color, transform.parent, 3, "corpse", SpriteFit.Centered(1.5f, 1.2f));
            corpse.AddComponent<BoxCollider2D>().isTrigger = true;
            var loot = corpse.AddComponent<LootContainer>();
            if (GameRoot.Instance != null && !GameRoot.Instance.Session.InIntro)
            {
                int samples = PrototypeContent.SamplesFromBody(Kind.ToString().ToLowerInvariant(), GameRoot.Instance.Session.Rng);
                if (samples > 0) loot.AddExtra(PrototypeContent.Sample, samples);
            }
            var bossArea = Kind == ZombieKind.Boss && Bnhon.Game.Raid.RaidDirector.Instance != null ? Bnhon.Game.Raid.RaidDirector.Instance.CurrentArea : null;
            if (Kind == ZombieKind.Boss) loot.InitFixed(bossArea != null && bossArea.BossLoot != null ? bossArea.BossLoot : PrototypeContent.ButcherLoot, "Xác " + (bossArea != null ? bossArea.BossName : "trùm"));
            else loot.InitCorpse(table, Kind == ZombieKind.Armored ? Mathf.Max(1, RollCorpseItems()) : RollCorpseItems());
        }
    }
}
