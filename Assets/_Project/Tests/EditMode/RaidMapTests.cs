using System;
using Bnhon.Core.Maps;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class RaidMapTests
    {
        private static bool Throws(string text)
        {
            try
            {
                RaidMap.Parse(text);
                return false;
            }
            catch (FormatException)
            {
                return true;
            }
        }

        [Test]
        public void Parse_ReadsWallsSpawnAndMarkers()
        {
            var map = RaidMap.Parse(
                "; a comment line\n" +
                "######\r\n" +
                "#P.ZC#\n" +
                "#..E.#\n" +
                "######\n");

            Assert.That(map.Width, Is.EqualTo(6));
            Assert.That(map.Height, Is.EqualTo(4));
            Assert.That(map.Spawn.X, Is.EqualTo(1));
            Assert.That(map.Spawn.Y, Is.EqualTo(1));
            Assert.That(map.IsWall(0, 0), Is.True);
            Assert.That(map.IsWall(2, 1), Is.False);
            Assert.That(map.IsWall(3, 1), Is.False);
            Assert.That(map.IsWall(-1, 2), Is.True);
            Assert.That(map.Markers.Count, Is.EqualTo(3));
            Assert.That(map.Markers[0].Symbol, Is.EqualTo('Z'));
            Assert.That(map.Markers[2].Symbol, Is.EqualTo('E'));
            Assert.That(map.Markers[2].X, Is.EqualTo(3));
            Assert.That(map.Markers[2].Y, Is.EqualTo(2));
        }

        [Test]
        public void Parse_PadsShortRowsWithWall()
        {
            var map = RaidMap.Parse("#####\n#PE\n#####");

            Assert.That(map.Width, Is.EqualTo(5));
            Assert.That(map.IsWall(3, 1), Is.True);
            Assert.That(map.IsWall(4, 1), Is.True);
        }

        [Test]
        public void Parse_RejectsBadMaps()
        {
            Assert.That(Throws(""), Is.True);
            Assert.That(Throws("#.E#"), Is.True);
            Assert.That(Throws("#P.#"), Is.True);
            Assert.That(Throws("#PPE#"), Is.True);
            Assert.That(Throws("#P?E#"), Is.True);
            Assert.That(Throws("#P.E#"), Is.False);
        }

        [Test]
        public void FindUnreachableMarkers_ReportsSealedOffCells()
        {
            var map = RaidMap.Parse(
                "#######\n" +
                "#P.#C.#\n" +
                "#.E#..#\n" +
                "#######");

            var unreachable = map.FindUnreachableMarkers();

            Assert.That(unreachable.Count, Is.EqualTo(1));
            Assert.That(unreachable[0].Symbol, Is.EqualTo('C'));
        }
    }
}
