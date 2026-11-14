using System;
using System.Collections.Generic;
using Bnhon.Core.Pathfinding;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class PathGridTests
    {
        /// <summary>Builds a grid from rows of text: '#' is blocked. Row 0 is y = 0.</summary>
        private static PathGrid Grid(params string[] rows)
        {
            var grid = new PathGrid(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    grid.SetBlocked(x, y, rows[y][x] == '#');
                }
            }
            return grid;
        }

        private static void AssertValidPath(PathGrid grid, GridPoint start, GridPoint goal, List<GridPoint> path)
        {
            GridPoint previous = start;
            foreach (GridPoint step in path)
            {
                Assert.That(grid.IsWalkable(step.X, step.Y), Is.True);
                int dx = Math.Abs(step.X - previous.X);
                int dy = Math.Abs(step.Y - previous.Y);
                Assert.That(dx <= 1 && dy <= 1 && dx + dy > 0, Is.True);
                if (dx == 1 && dy == 1)
                {
                    Assert.That(grid.IsWalkable(step.X, previous.Y) && grid.IsWalkable(previous.X, step.Y), Is.True);
                }
                previous = step;
            }
            Assert.That(previous.X, Is.EqualTo(goal.X));
            Assert.That(previous.Y, Is.EqualTo(goal.Y));
        }

        [Test]
        public void OpenGrid_TakesTheShortestDiagonalRoute()
        {
            var grid = Grid(".....", ".....", ".....", ".....", ".....");
            var path = new List<GridPoint>();

            Assert.That(grid.FindPath(new GridPoint(0, 0), new GridPoint(4, 4), path), Is.True);

            Assert.That(path.Count, Is.EqualTo(4));
            AssertValidPath(grid, new GridPoint(0, 0), new GridPoint(4, 4), path);
        }

        [Test]
        public void Wall_IsWalkedAroundThroughTheGap()
        {
            var grid = Grid(
                ".....",
                "####.",
                ".....");
            var path = new List<GridPoint>();
            var start = new GridPoint(0, 0);
            var goal = new GridPoint(0, 2);

            Assert.That(grid.FindPath(start, goal, path), Is.True);

            AssertValidPath(grid, start, goal, path);
            bool usedGap = false;
            foreach (GridPoint step in path)
            {
                if (step.X == 4 && step.Y == 1) usedGap = true;
            }
            Assert.That(usedGap, Is.True);
            Assert.That(path.Count, Is.EqualTo(10));
        }

        [Test]
        public void Diagonal_DoesNotCutBetweenTwoBlockedCorners()
        {
            var grid = Grid(
                ".#",
                "#.");
            var path = new List<GridPoint>();

            Assert.That(grid.FindPath(new GridPoint(0, 0), new GridPoint(1, 1), path), Is.False);
        }

        [Test]
        public void SealedGoal_IsUnreachableAndLeavesPathEmpty()
        {
            var grid = Grid(
                ".....",
                ".###.",
                ".#.#.",
                ".###.",
                ".....");
            var path = new List<GridPoint>();
            path.Add(new GridPoint(9, 9));

            Assert.That(grid.FindPath(new GridPoint(0, 0), new GridPoint(2, 2), path), Is.False);
            Assert.That(path.Count, Is.EqualTo(0));
        }

        [Test]
        public void BlockedEnds_FailButNearestWalkableFixesThem()
        {
            var grid = Grid(
                "....",
                ".##.",
                "....");
            var path = new List<GridPoint>();

            Assert.That(grid.FindPath(new GridPoint(0, 0), new GridPoint(1, 1), path), Is.False);

            Assert.That(grid.TryFindNearestWalkable(1, 1, 2, out GridPoint near), Is.True);
            Assert.That(grid.IsWalkable(near.X, near.Y), Is.True);
            Assert.That(Math.Abs(near.X - 1) + Math.Abs(near.Y - 1), Is.EqualTo(1));
            Assert.That(grid.FindPath(new GridPoint(0, 0), near, path), Is.True);
        }

        [Test]
        public void SameCell_SucceedsWithEmptyPath()
        {
            var grid = Grid("...");
            var path = new List<GridPoint>();

            Assert.That(grid.FindPath(new GridPoint(1, 0), new GridPoint(1, 0), path), Is.True);
            Assert.That(path.Count, Is.EqualTo(0));
        }

        [Test]
        public void RepeatedSearches_ReuseBuffersCorrectly()
        {
            var grid = Grid(
                "........",
                ".######.",
                "........");
            var path = new List<GridPoint>();

            for (int i = 0; i < 20; i++)
            {
                var start = new GridPoint(i % 8, 0);
                var goal = new GridPoint(7 - i % 8, 2);
                Assert.That(grid.FindPath(start, goal, path), Is.True);
                AssertValidPath(grid, start, goal, path);
            }
        }
    }
}
