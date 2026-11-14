using System;
using System.Collections.Generic;

namespace Bnhon.Core.Pathfinding
{
    public struct GridPoint
    {
        public int X;
        public int Y;

        public GridPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// A grid of walkable and blocked cells with A* search. Moves in eight
    /// directions and never cuts a corner between two blocked cells.
    /// Buffers are reused between searches, so one instance is not thread-safe.
    /// </summary>
    public sealed class PathGrid
    {
        private const int StraightCost = 10;
        private const int DiagonalCost = 14;

        private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] StepY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        private readonly bool[] _blocked;
        private readonly int[] _cost;
        private readonly int[] _parent;
        private readonly int[] _visitStamp;
        private readonly bool[] _closed;
        private readonly List<int> _heap = new List<int>();
        private readonly List<int> _heapPriority = new List<int>();
        private int _stamp;

        public int Width { get; }
        public int Height { get; }

        public PathGrid(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            int size = width * height;
            _blocked = new bool[size];
            _cost = new int[size];
            _parent = new int[size];
            _visitStamp = new int[size];
            _closed = new bool[size];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsWalkable(int x, int y) => InBounds(x, y) && !_blocked[y * Width + x];

        public void SetBlocked(int x, int y, bool blocked)
        {
            if (InBounds(x, y)) _blocked[y * Width + x] = blocked;
        }

        /// <summary>
        /// Finds the nearest walkable cell to (x, y) within <paramref name="maxRadius"/> cells,
        /// (x, y) itself included. Used when an agent or its goal sits inside an obstacle's margin.
        /// </summary>
        public bool TryFindNearestWalkable(int x, int y, int maxRadius, out GridPoint result)
        {
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                int best = int.MaxValue;
                GridPoint bestPoint = default(GridPoint);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                        if (!IsWalkable(x + dx, y + dy)) continue;

                        int distance = dx * dx + dy * dy;
                        if (distance < best)
                        {
                            best = distance;
                            bestPoint = new GridPoint(x + dx, y + dy);
                        }
                    }
                }

                if (best != int.MaxValue)
                {
                    result = bestPoint;
                    return true;
                }
            }

            result = default(GridPoint);
            return false;
        }

        /// <summary>
        /// A* from start to goal. On success fills <paramref name="path"/> with the cells to
        /// step through, start excluded and goal included (empty when start equals goal).
        /// Returns false if either end is blocked or the goal cannot be reached.
        /// </summary>
        public bool FindPath(GridPoint start, GridPoint goal, List<GridPoint> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            path.Clear();

            if (!IsWalkable(start.X, start.Y) || !IsWalkable(goal.X, goal.Y)) return false;

            int startIndex = start.Y * Width + start.X;
            int goalIndex = goal.Y * Width + goal.X;
            if (startIndex == goalIndex) return true;

            _stamp++;
            _heap.Clear();
            _heapPriority.Clear();

            Visit(startIndex, 0, -1);
            Push(startIndex, Heuristic(start.X, start.Y, goal));

            while (_heap.Count > 0)
            {
                int current = Pop();
                if (_closed[current]) continue;
                _closed[current] = true;

                if (current == goalIndex)
                {
                    for (int node = goalIndex; node != startIndex; node = _parent[node])
                    {
                        path.Add(new GridPoint(node % Width, node / Width));
                    }
                    path.Reverse();
                    return true;
                }

                int cx = current % Width;
                int cy = current / Width;

                for (int i = 0; i < 8; i++)
                {
                    int nx = cx + StepX[i];
                    int ny = cy + StepY[i];
                    if (!IsWalkable(nx, ny)) continue;

                    bool diagonal = i >= 4;
                    // No corner cutting: both cells beside a diagonal step must be free.
                    if (diagonal && (!IsWalkable(nx, cy) || !IsWalkable(cx, ny))) continue;

                    int next = ny * Width + nx;
                    int newCost = _cost[current] + (diagonal ? DiagonalCost : StraightCost);

                    if (_visitStamp[next] == _stamp)
                    {
                        if (_closed[next] || newCost >= _cost[next]) continue;
                        _cost[next] = newCost;
                        _parent[next] = current;
                    }
                    else
                    {
                        Visit(next, newCost, current);
                    }
                    Push(next, newCost + Heuristic(nx, ny, goal));
                }
            }

            return false;
        }

        private void Visit(int index, int cost, int parent)
        {
            _visitStamp[index] = _stamp;
            _closed[index] = false;
            _cost[index] = cost;
            _parent[index] = parent;
        }

        /// <summary>Octile distance: exact cost on an empty grid, so the search stays optimal.</summary>
        private static int Heuristic(int x, int y, GridPoint goal)
        {
            int dx = Math.Abs(x - goal.X);
            int dy = Math.Abs(y - goal.Y);
            return StraightCost * Math.Max(dx, dy) + (DiagonalCost - StraightCost) * Math.Min(dx, dy);
        }

        // Binary min-heap keyed by priority. Stale entries are skipped when popped.
        private void Push(int node, int priority)
        {
            _heap.Add(node);
            _heapPriority.Add(priority);
            int child = _heap.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (_heapPriority[parent] <= _heapPriority[child]) break;
                Swap(parent, child);
                child = parent;
            }
        }

        private int Pop()
        {
            int top = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heapPriority[0] = _heapPriority[last];
            _heap.RemoveAt(last);
            _heapPriority.RemoveAt(last);

            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;
                if (left < _heap.Count && _heapPriority[left] < _heapPriority[smallest]) smallest = left;
                if (right < _heap.Count && _heapPriority[right] < _heapPriority[smallest]) smallest = right;
                if (smallest == parent) break;
                Swap(parent, smallest);
                parent = smallest;
            }
            return top;
        }

        private void Swap(int a, int b)
        {
            int node = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = node;
            int priority = _heapPriority[a];
            _heapPriority[a] = _heapPriority[b];
            _heapPriority[b] = priority;
        }
    }
}
