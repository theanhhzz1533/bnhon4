using Bnhon.Core.Content;
using Bnhon.Core.Crafting;
using Bnhon.Core.Layout;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class BaseLayoutTests
    {
        private static readonly BuildingType Hut = new BuildingType("hut", "Hut", 2, 1);
        private static readonly BuildingType Shed = new BuildingType("shed", "Shed", 1, 2);

        [Test]
        public void Building_CannotLeaveTheYard_OverlapAnother_OrStandOnReservedGround()
        {
            var layout = new BaseLayout(6, 4);
            layout.Reserve(0, 3);
            layout.Add(Hut, 0, 0);
            layout.Add(Shed, 4, 0);

            Assert.That(layout.CanPlace("hut", 5, 0), Is.False);
            Assert.That(layout.CanPlace("hut", -1, 0), Is.False);
            Assert.That(layout.CanPlace("hut", 3, 1), Is.False);
            Assert.That(layout.CanPlace("hut", 0, 3), Is.False);
            Assert.That(layout.CanPlace("hut", 2, 2), Is.True);
        }

        [Test]
        public void Building_CanShiftOntoCellsItAlreadyCovers()
        {
            var layout = new BaseLayout(6, 4);
            layout.Add(Hut, 0, 0);

            Assert.That(layout.Move("hut", 1, 0), Is.True);
            Assert.That(layout.Get("hut").X, Is.EqualTo(1));
            Assert.That(layout.At(2, 0) == layout.Get("hut"), Is.True);
            Assert.That(layout.At(0, 0) == null, Is.True);
        }

        [Test]
        public void FailedMove_LeavesTheBuildingWhereItWas()
        {
            var layout = new BaseLayout(6, 4);
            layout.Add(Hut, 0, 0);
            layout.Add(Shed, 4, 0);

            Assert.That(layout.Move("hut", 3, 0), Is.False);
            Assert.That(layout.Get("hut").X, Is.EqualTo(0));
        }

        [Test]
        public void StandCell_IsAFreeCellBesideTheBuilding()
        {
            var layout = new BaseLayout(6, 4);
            layout.Add(Hut, 0, 0);

            int x, y;
            layout.StandCell("hut", out x, out y);
            Assert.That(x, Is.EqualTo(2));
            Assert.That(y, Is.EqualTo(0));

            layout.Move("hut", 3, 2);
            layout.StandCell("hut", out x, out y);
            Assert.That(x, Is.EqualTo(2));
            Assert.That(y, Is.EqualTo(2));
        }

        [Test]
        public void NewGame_HasAllSixBuildings_NoneOnReservedGround()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            BaseLayout layout = session.Base;

            Assert.That(layout.Buildings.Count, Is.EqualTo(6));
            foreach (PlacedBuilding building in layout.Buildings)
            {
                Assert.That(layout.CanPlace(building.Type.Id, building.X, building.Y), Is.True);
            }
        }

        [Test]
        public void Save_KeepsWhereBuildingsWereMoved()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            Assert.That(session.Base.Move("well", 12, 6), Is.True);

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out report);

            Assert.That(loaded.Base.Get("well").X, Is.EqualTo(12));
            Assert.That(loaded.Base.Get("well").Y, Is.EqualTo(6));
            Assert.That(report.Dropped.Count, Is.EqualTo(0));
        }

        [Test]
        public void Save_WithABuildingOnGroundThatIsNowReserved_KeepsTheDefaultPlace()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            SaveData data = SaveMapper.Capture(session, false);
            foreach (BuildingSave saved in data.buildings)
            {
                if (saved.id != "well") continue;
                saved.x = 3;
                saved.y = 6;
            }

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(data, 2, out report);

            Assert.That(loaded.Base.Get("well").X, Is.EqualTo(4));
            Assert.That(report.Dropped.Count, Is.EqualTo(1));
        }

        [Test]
        public void WateringCan_UpgradeAddsCharges_NeedsMaterials_AndStopsAtTheTop()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            Assert.That(session.WateringCan.Capacity, Is.EqualTo(6));
            Assert.That(PrototypeContent.UpgradeWateringCan(session), Is.EqualTo(CraftBlock.Materials));

            session.Stash.Add(PrototypeContent.ScrapMetal, 4);
            session.Stash.Add(PrototypeContent.Cloth, 2);
            Assert.That(PrototypeContent.UpgradeWateringCan(session), Is.EqualTo(CraftBlock.None));
            Assert.That(session.WateringCan.Capacity, Is.EqualTo(9));
            Assert.That(session.WateringCan.IsFull, Is.True);
            Assert.That(session.Stash.CountOf(PrototypeContent.ScrapMetal.Id), Is.EqualTo(0));

            // The last level needs a better workbench.
            session.Stash.Add(PrototypeContent.ScrapMetal, 8);
            session.Stash.Add(PrototypeContent.CircuitBoard, 1);
            Assert.That(PrototypeContent.UpgradeWateringCan(session), Is.EqualTo(CraftBlock.WorkbenchLevel));
            Assert.That(session.Stash.CountOf(PrototypeContent.ScrapMetal.Id), Is.EqualTo(8));

            session.Workshop.Restore(2, null);
            Assert.That(PrototypeContent.UpgradeWateringCan(session), Is.EqualTo(CraftBlock.None));
            Assert.That(session.WateringCan.Capacity, Is.EqualTo(12));
            Assert.That(PrototypeContent.UpgradeWateringCan(session), Is.EqualTo(CraftBlock.MaxLevel));
        }

        [Test]
        public void Save_KeepsTheCanLevel()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.WateringCan.Upgrade();

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out report);

            Assert.That(loaded.WateringCan.Level, Is.EqualTo(2));
            Assert.That(loaded.WateringCan.Water, Is.EqualTo(9));
        }
    }
}
