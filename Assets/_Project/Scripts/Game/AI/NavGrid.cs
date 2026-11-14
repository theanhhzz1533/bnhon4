using System.Collections.Generic;
using Bnhon.Core.Pathfinding;
using UnityEngine;

namespace Bnhon.Game.AI
{
    /// <summary>
    /// Walkability map of the raid zone for enemy pathfinding. A cell is
    /// blocked when an agent standing on its centre would touch a static
    /// obstacle: any non-trigger collider without a Rigidbody2D (walls,
    /// crates). Rebuilt one frame after <see cref="RequestBuild"/>, so that
    /// objects destroyed and created in the same frame are settled.
    /// </summary>
    public sealed class NavGrid : MonoBehaviour
    {
        [SerializeField] private float cellSize = 0.5f;
        [SerializeField] private float agentSize = 0.86f;

        private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];
        private static readonly RaycastHit2D[] CastBuffer = new RaycastHit2D[24];
        private static ContactFilter2D _filter = new ContactFilter2D().NoFilter();

        private readonly List<GridPoint> _cells = new List<GridPoint>();
        private PathGrid _grid;
        private Vector2 _origin;
        private Vector2 _pendingCenter;
        private Vector2 _pendingSize;
        private int _buildAtFrame = -1;

        public static NavGrid Instance { get; private set; }

        public bool IsReady => _grid != null && _buildAtFrame < 0;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Schedules a rebuild of the map over a world rectangle.</summary>
        public void RequestBuild(Vector2 center, Vector2 size)
        {
            _pendingCenter = center;
            _pendingSize = size;
            _buildAtFrame = UnityEngine.Time.frameCount + 1;
        }

        private void Update()
        {
            if (_buildAtFrame < 0 || UnityEngine.Time.frameCount < _buildAtFrame) return;
            _buildAtFrame = -1;
            Build(_pendingCenter, _pendingSize);
        }

        private void Build(Vector2 center, Vector2 size)
        {
            int width = Mathf.Max(1, Mathf.CeilToInt(size.x / cellSize));
            int height = Mathf.Max(1, Mathf.CeilToInt(size.y / cellSize));
            _origin = center - size * 0.5f;
            _grid = new PathGrid(width, height);

            var box = new Vector2(agentSize, agentSize);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int count = Physics2D.OverlapBox(CellCenter(x, y), box, 0f, _filter, OverlapBuffer);
                    bool blocked = false;
                    for (int i = 0; i < count && !blocked; i++) blocked = IsStaticObstacle(OverlapBuffer[i]);
                    _grid.SetBlocked(x, y, blocked);
                }
            }
        }

        /// <summary>True if an agent of the given radius cannot walk in a straight line between two points.</summary>
        public bool IsLineBlocked(Vector2 from, Vector2 to, float radius)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return false;

            int count = Physics2D.CircleCast(from, radius, delta / length, _filter, CastBuffer, length);
            for (int i = 0; i < count; i++)
            {
                if (IsStaticObstacle(CastBuffer[i].collider)) return true;
            }
            return false;
        }

        /// <summary>
        /// Fills <paramref name="waypoints"/> with world points leading from one position to another
        /// around obstacles. The last waypoint is the destination itself when its cell is walkable.
        /// </summary>
        public bool TryFindPath(Vector2 from, Vector2 to, List<Vector2> waypoints)
        {
            waypoints.Clear();
            if (!IsReady) return false;

            GridPoint start = ToCell(from);
            GridPoint goal = ToCell(to);
            if (!_grid.TryFindNearestWalkable(start.X, start.Y, 4, out start)) return false;
            if (!_grid.TryFindNearestWalkable(goal.X, goal.Y, 4, out goal)) return false;
            if (!_grid.FindPath(start, goal, _cells)) return false;

            foreach (GridPoint cell in _cells) waypoints.Add(CellCenter(cell.X, cell.Y));
            return true;
        }

        private static bool IsStaticObstacle(Collider2D collider)
        {
            return collider != null && !collider.isTrigger && collider.attachedRigidbody == null;
        }

        private GridPoint ToCell(Vector2 world)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((world.x - _origin.x) / cellSize), 0, _grid.Width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((world.y - _origin.y) / cellSize), 0, _grid.Height - 1);
            return new GridPoint(x, y);
        }

        private Vector2 CellCenter(int x, int y)
        {
            return new Vector2(_origin.x + (x + 0.5f) * cellSize, _origin.y + (y + 0.5f) * cellSize);
        }
    }
}
