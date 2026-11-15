using System;
using System.Collections.Generic;
using Bnhon.Core.Farming;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;

namespace Bnhon.Core.Saving
{
    /// <summary>What happened while applying a save, for the game to tell the player.</summary>
    public sealed class LoadReport
    {
        /// <summary>The save was written at the start of a raid that never ended: the game was quit outside.</summary>
        public bool AbandonedRaid;

        /// <summary>Saved things that no longer exist in the game's content, and were dropped.</summary>
        public readonly List<string> Dropped = new List<string>();
    }

    /// <summary>Copies a <see cref="GameSession"/> into a <see cref="SaveData"/> and back.</summary>
    public static class SaveMapper
    {
        public static SaveData Capture(GameSession session, bool inRaid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var data = new SaveData
            {
                playerGender = (int)session.Relationships.PlayerGender,
                totalDays = session.Clock.TotalDays,
                money = session.Wallet.Money,
                hunger = session.Needs.Hunger,
                thirst = session.Needs.Thirst,
                raidedToday = session.RaidedToday,
                inRaid = inRaid,
                stash = CaptureGrid(session.Stash),
                backpack = CaptureGrid(session.Backpack),
                securePouch = CaptureGrid(session.SecurePouch)
            };

            for (int slot = 0; slot < Combat.Loadout.SlotCount; slot++)
            {
                ItemDefinition weapon = session.Loadout.GetWeapon(slot);
                data.loadout.Add(new WeaponSlotSave
                {
                    id = weapon != null ? weapon.Id : string.Empty,
                    loaded = session.Loadout.GetLoaded(slot)
                });
            }

            foreach (FarmPlot plot in session.Farm.Plots)
            {
                data.farm.Add(new PlotSave
                {
                    state = (int)plot.State,
                    cropId = plot.Crop != null ? plot.Crop.Id : string.Empty,
                    growthDays = plot.GrowthDays,
                    dryDays = plot.DryDays,
                    watered = plot.IsWatered
                });
            }

            foreach (Relationship relationship in session.Relationships.All)
            {
                data.relationships.Add(new RelationshipSave
                {
                    npcId = relationship.Npc.Id,
                    points = relationship.Points,
                    talkedToday = relationship.TalkedToday,
                    giftedToday = relationship.GiftedToday,
                    giftsThisWeek = relationship.GiftsThisWeek,
                    status = (int)relationship.Status
                });
            }

            foreach (DeliveryQuest quest in session.Quests.All)
            {
                data.quests.Add(new QuestSave { id = quest.Id, state = (int)quest.State });
            }

            data.workbenchLevel = session.Workshop.Level;
            foreach (string blueprint in session.Workshop.KnownBlueprints) data.blueprints.Add(blueprint);

            data.shopSaved = true;
            foreach (Trading.ShopEntry entry in session.Shop.Entries)
            {
                data.shopStock.Add(new ShopStockSave { id = entry.Item.Id, stock = entry.Stock });
            }

            return data;
        }

        /// <summary>
        /// Overwrites a freshly created session with saved state. The session must already
        /// have its NPCs and quests registered (see PrototypeContent.CreateNewSession); the
        /// save only carries their progress. Ids that no longer exist are skipped and reported.
        /// </summary>
        public static LoadReport Apply(
            SaveData data,
            GameSession session,
            Func<string, ItemDefinition> findItem,
            Func<string, CropDefinition> findCrop)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (data.version > SaveData.CurrentVersion)
            {
                throw new FormatException("Save version " + data.version + " is newer than this game understands (" + SaveData.CurrentVersion + ").");
            }

            var report = new LoadReport();

            session.Clock.Restore(data.totalDays);
            session.Wallet.Restore(data.money);
            session.Needs.Restore(data.hunger, data.thirst);
            session.Stamina.Refill();
            session.RestoreRaidedToday(data.raidedToday);

            ApplyGrid(data.stash, session.Stash, findItem, report);
            ApplyGrid(data.backpack, session.Backpack, findItem, report);
            ApplyGrid(data.securePouch, session.SecurePouch, findItem, report);

            session.Loadout.Clear();
            for (int slot = 0; slot < Combat.Loadout.SlotCount && data.loadout != null && slot < data.loadout.Count; slot++)
            {
                WeaponSlotSave saved = data.loadout[slot];
                if (saved == null || string.IsNullOrEmpty(saved.id)) continue;

                ItemDefinition weapon = findItem(saved.id);
                if (weapon == null || weapon.Category != ItemCategory.Weapon)
                {
                    report.Dropped.Add("weapon " + saved.id);
                    continue;
                }
                session.Loadout.Set(slot, weapon, saved.loaded);
            }

            if (data.farm != null)
            {
                if (data.farm.Count > session.Farm.Plots.Count) session.Farm.AddPlots(data.farm.Count - session.Farm.Plots.Count);
                for (int i = 0; i < data.farm.Count; i++)
                {
                    PlotSave saved = data.farm[i];
                    if (saved == null) continue;

                    CropDefinition crop = string.IsNullOrEmpty(saved.cropId) ? null : findCrop(saved.cropId);
                    if (crop == null && !string.IsNullOrEmpty(saved.cropId)) report.Dropped.Add("crop " + saved.cropId);
                    session.Farm.Plots[i].Restore((PlotState)saved.state, crop, saved.growthDays, saved.dryDays, saved.watered);
                }
            }

            if (data.relationships != null)
            {
                foreach (RelationshipSave saved in data.relationships)
                {
                    if (saved == null) continue;
                    if (!session.Relationships.Restore(saved.npcId, saved.points, saved.talkedToday, saved.giftedToday, saved.giftsThisWeek, (RelationshipStatus)saved.status))
                    {
                        report.Dropped.Add("npc " + saved.npcId);
                    }
                }
            }

            if (data.quests != null)
            {
                foreach (QuestSave saved in data.quests)
                {
                    if (saved == null) continue;
                    if (!session.Quests.Restore(saved.id, (QuestState)saved.state)) report.Dropped.Add("quest " + saved.id);
                }
            }

            session.Workshop.Restore(data.workbenchLevel, data.blueprints);

            // A save from before the shop existed leaves it as a new game has it: fully stocked.
            if (data.shopSaved && data.shopStock != null)
            {
                foreach (ShopStockSave saved in data.shopStock)
                {
                    if (saved != null) session.Shop.RestoreStock(saved.id, saved.stock);
                }
            }

            if (data.inRaid)
            {
                // Quitting outside counts as dying there: otherwise closing the game would be a free escape.
                report.AbandonedRaid = true;
                session.Backpack.Clear();
                session.Loadout.Clear();
                session.RestoreRaidedToday(true);
            }

            return report;
        }

        private static GridSave CaptureGrid(GridInventory grid)
        {
            var save = new GridSave();
            foreach (PlacedItem placed in grid.Items)
            {
                save.items.Add(new ItemSave
                {
                    id = placed.Item.Id,
                    count = placed.Count,
                    x = placed.X,
                    y = placed.Y,
                    rotated = placed.Rotated
                });
            }
            return save;
        }

        private static void ApplyGrid(GridSave save, GridInventory grid, Func<string, ItemDefinition> findItem, LoadReport report)
        {
            grid.Clear();
            if (save == null || save.items == null) return;

            foreach (ItemSave saved in save.items)
            {
                if (saved == null) continue;

                ItemDefinition item = findItem(saved.id);
                if (item == null || saved.count < 1)
                {
                    report.Dropped.Add("item " + saved.id);
                    continue;
                }

                int count = Math.Min(saved.count, item.MaxStack);
                // Put it back where it was. If the item's size changed since the save, fit it anywhere.
                if (grid.TryPlace(item, count, saved.x, saved.y, saved.rotated) == null && grid.Add(item, count) != 0)
                {
                    report.Dropped.Add("item " + saved.id + " (no room)");
                }
            }
        }
    }
}
