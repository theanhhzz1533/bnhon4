using System.Collections.Generic;
using Bnhon.Core;
using Bnhon.Core.Npcs;
using Bnhon.Game.Audio;
using Bnhon.Game.Combat;
using Bnhon.Game.Interaction;
using Bnhon.Game.Player;
using Bnhon.Game.Vision;
using UnityEngine;

namespace Bnhon.Game.AI
{
    /// <summary>
    /// Someone waiting on a raid map to be rescued. Hand over what they need (if anything)
    /// and they follow the player, walking around walls. Zombies go for them too. Reaching
    /// an exit with them close by brings them to the base for good; if they fall, they can
    /// be found again on a later raid.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public sealed class SurvivorAI : MonoBehaviour, IInteractable
    {
        private enum State
        {
            Waiting,
            Following
        }

        private const float FollowDistance = 1.8f;
        private const float PleaDistance = 6f;
        private const float BodyRadius = 0.42f;

        private static readonly List<SurvivorAI> Alive = new List<SurvivorAI>();

        private readonly List<Vector2> _path = new List<Vector2>();
        private RescueDefinition _rescue;
        private string _name;
        private Transform _player;
        private Rigidbody2D _body;
        private Health _health;
        private SpriteRenderer[] _renderers;
        private State _state = State.Waiting;
        private Vector2 _velocity;
        private int _pathIndex;
        private float _nextRepathTime;
        private bool _pleaded;

        /// <summary>Everyone currently waiting or following in the raid.</summary>
        public static IReadOnlyList<SurvivorAI> All => Alive;

        public string NpcId => _rescue != null ? _rescue.NpcId : string.Empty;
        public string DisplayName => _name;
        public Health Health => _health;
        public bool IsFollowing => _state == State.Following;

        public string Prompt
        {
            get
            {
                if (_state == State.Following) return _name + " đang theo bạn. Tới một lối rút lui cùng nhau.";
                if (_rescue.Needs == null) return "E: Bảo " + _name + " đi theo";

                var needs = _rescue.Needs.Value;
                int have = GameRoot.Instance.Session.Backpack.CountOf(needs.Item.Id);
                return "E: Đưa " + needs.Item.DisplayName + " cho " + _name + " (balo có " + have + ")";
            }
        }

        public void Init(RescueDefinition rescue, string displayName, Transform player)
        {
            _rescue = rescue;
            _name = displayName;
            _player = player;
            _health.SetMax(rescue.Health);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
        }

        private void Start()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            Alive.Add(this);
            _health.Died += OnDied;
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            Alive.Remove(this);
            _health.Died -= OnDied;
            _health.Damaged -= OnDamaged;
        }

        public void Interact(PlayerController player)
        {
            if (_state == State.Following) return;
            GameSession session = GameRoot.Instance.Session;

            if (_rescue.Needs != null)
            {
                var needs = _rescue.Needs.Value;
                bool handed = session.Backpack.TryConsume(needs.Item.Id, needs.Count)
                    || session.SecurePouch.TryConsume(needs.Item.Id, needs.Count);
                if (!handed)
                {
                    GameRoot.Instance.Notify(_name + ": \"" + Fill(_rescue.Plea) + "\"", 6f);
                    return;
                }
            }

            _state = State.Following;
            Sfx.PlayUi(SoundId.UseItem, 0.6f);
            GameRoot.Instance.Notify(_name + ": \"" + Fill(_rescue.Thanks) + "\"", 6f);
        }

        private void Update()
        {
            if (_player == null) return;
            Vector2 position = transform.position;
            Vector2 toPlayer = (Vector2)_player.position - position;
            float distance = toPlayer.magnitude;

            // Seen only inside the player's view, like everything else out there.
            VisionSystem vision = VisionSystem.Instance;
            bool visible = vision == null || vision.IsVisible(position) || _state == State.Following;
            if (_renderers != null)
            {
                foreach (SpriteRenderer renderer in _renderers)
                {
                    if (renderer != null) renderer.enabled = visible;
                }
            }

            if (_state == State.Waiting)
            {
                _velocity = Vector2.zero;
                if (!_pleaded && distance < PleaDistance && visible)
                {
                    _pleaded = true;
                    GameRoot.Instance.Notify(_name + ": \"" + Fill(_rescue.Plea) + "\"", 7f);
                }
                return;
            }

            _velocity = distance > FollowDistance ? Steer(position, _player.position, _rescue.Speed) : Vector2.zero;
        }

        private void FixedUpdate()
        {
            _body.linearVelocity = _velocity;
        }

        /// <summary>Walks straight when it can; otherwise follows a path around the walls.</summary>
        private Vector2 Steer(Vector2 position, Vector2 destination, float speed)
        {
            Vector2 goal = destination;
            NavGrid nav = NavGrid.Instance;
            float now = UnityEngine.Time.time;

            if (nav != null && nav.IsReady && nav.IsLineBlocked(position, destination, BodyRadius))
            {
                if (now >= _nextRepathTime)
                {
                    _nextRepathTime = now + 0.4f;
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
            }

            Vector2 toGoal = goal - position;
            float length = toGoal.magnitude;
            return length > 0.01f ? toGoal / length * speed : Vector2.zero;
        }

        private void OnDamaged(Health health)
        {
            if (health.IsDead) return;
            Sfx.Play(SoundId.PlayerHurt, transform.position, 0.6f);
            if (health.Current < health.Max * 0.35f) GameRoot.Instance.Notify(_name + " sắp không trụ nổi!", 2f);
        }

        private void OnDied(Health health)
        {
            Sfx.Play(SoundId.PlayerDeath, transform.position, 0.8f);
            GameRoot.Instance.Notify(_name + " gục xuống rồi... Hôm khác quay lại đây, biết đâu vẫn còn kịp.", 7f);
            Destroy(gameObject);
        }

        /// <summary>
        /// Everyone following who is close to the player as they extract: they make it out.
        /// Returns their NPC ids.
        /// </summary>
        public static List<string> TakeRescued(Vector2 playerPosition, float radius)
        {
            var rescued = new List<string>();
            foreach (SurvivorAI survivor in Alive)
            {
                if (!survivor.IsFollowing || survivor._health.IsDead) continue;
                if (((Vector2)survivor.transform.position - playerPosition).sqrMagnitude > radius * radius) continue;
                rescued.Add(survivor.NpcId);
            }
            return rescued;
        }

        private string Fill(string text)
        {
            GameSession session = GameRoot.Instance.Session;
            NpcLife life = Bnhon.Core.Content.PrototypeContent.FindNpcLife(NpcId);
            return life != null ? DialogueBook.Format(text, life.Address(session.Relationships.PlayerGender)) : text;
        }
    }
}
