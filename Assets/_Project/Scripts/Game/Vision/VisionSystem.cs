using Bnhon.Game.AI;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Vision
{
    /// <summary>
    /// Fog of war for raids. The player sees a cone in the aim direction plus
    /// a small circle around themselves, both cut off by walls. Everything
    /// else is covered by a dark mesh, and enemies there are hidden (they ask
    /// <see cref="IsVisible"/>). Off while the player is in the base.
    /// </summary>
    public sealed class VisionSystem : MonoBehaviour
    {
        [SerializeField] private float viewDistance = 11f;
        [SerializeField] private float coneAngle = 100f;
        [SerializeField] private float nearRadius = 2.2f;
        [SerializeField] private int rayCount = 360;
        [SerializeField] private float wallReveal = 0.45f;
        [SerializeField] private float fogAlpha = 0.78f;
        [SerializeField] private float farDistance = 80f;

        private PlayerController _player;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private Vector3[] _vertices;

        public static VisionSystem Instance { get; private set; }

        /// <summary>True while the fog is being drawn (the player is in a raid).</summary>
        public bool IsActive { get; private set; }

        public void Init(PlayerController player)
        {
            _player = player;
        }

        private void Awake()
        {
            Instance = this;

            var filter = gameObject.AddComponent<MeshFilter>();
            _renderer = gameObject.AddComponent<MeshRenderer>();

            var material = new Material(PlaceholderArt.UnlitMaterial);
            material.color = new Color(0f, 0f, 0f, fogAlpha);
            _renderer.sharedMaterial = material;
            _renderer.sortingOrder = 40;
            _renderer.enabled = false;

            _mesh = new Mesh();
            _mesh.name = "FogOfWar";
            _mesh.MarkDynamic();
            BuildTopology();
            filter.sharedMesh = _mesh;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// The fog is a ring: for each ray, one vertex at the edge of what the
        /// player sees and one far outside the screen. Only positions change
        /// per frame, so triangles and colors are set once.
        /// </summary>
        private void BuildTopology()
        {
            if (rayCount < 16) rayCount = 16;

            _vertices = new Vector3[rayCount * 2];
            var colors = new Color[rayCount * 2];
            var triangles = new int[rayCount * 6];

            for (int i = 0; i < rayCount; i++)
            {
                int inner = i * 2;
                int outer = inner + 1;
                int nextInner = (i + 1) % rayCount * 2;
                int nextOuter = nextInner + 1;

                colors[inner] = Color.white;
                colors[outer] = Color.white;

                int t = i * 6;
                triangles[t] = inner;
                triangles[t + 1] = outer;
                triangles[t + 2] = nextInner;
                triangles[t + 3] = nextInner;
                triangles[t + 4] = outer;
                triangles[t + 5] = nextOuter;
            }

            _mesh.vertices = _vertices;
            _mesh.colors = colors;
            _mesh.triangles = triangles;
        }

        private void LateUpdate()
        {
            bool active = _player != null && GameRoot.Instance != null && GameRoot.Instance.Session.IsInRaid;
            IsActive = active;
            _renderer.enabled = active;
            if (!active) return;

            Vector2 origin = _player.transform.position;
            Vector2 aim = _player.AimDirection;
            float aimDegrees = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            float halfCone = coneAngle * 0.5f;
            float stepDegrees = 360f / rayCount;

            for (int i = 0; i < rayCount; i++)
            {
                float degrees = i * stepDegrees;
                float radians = degrees * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                bool inCone = Mathf.Abs(Mathf.DeltaAngle(degrees, aimDegrees)) <= halfCone;
                float reach = inCone ? viewDistance : nearRadius;

                // Let the lit area run a little into the wall so its face is visible.
                float distance = SightBlocker.Distance(origin, direction, reach);
                if (distance < reach) distance = Mathf.Min(reach, distance + wallReveal);

                Vector2 edge = origin + direction * distance;
                Vector2 far = origin + direction * farDistance;
                _vertices[i * 2] = new Vector3(edge.x, edge.y, 0f);
                _vertices[i * 2 + 1] = new Vector3(far.x, far.y, 0f);
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        /// <summary>Can the player currently see this world position? Always true while the fog is off.</summary>
        public bool IsVisible(Vector2 point)
        {
            if (!IsActive || _player == null) return true;

            Vector2 origin = _player.transform.position;
            Vector2 toPoint = point - origin;
            float distance = toPoint.magnitude;
            if (distance > viewDistance) return false;

            if (distance > nearRadius)
            {
                Vector2 aim = _player.AimDirection;
                float aimDegrees = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                float pointDegrees = Mathf.Atan2(toPoint.y, toPoint.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(pointDegrees, aimDegrees)) > coneAngle * 0.5f) return false;
            }

            return !SightBlocker.IsBlocked(origin, point);
        }
    }
}
