using System.Collections.Generic;
using Bnhon.Core.Farming;
using Bnhon.Core.Items;
using Bnhon.Core.Loot;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;
using Bnhon.Core.Vitals;

namespace Bnhon.Core.Content
{
    /// <summary>
    /// Hard-coded sample data so the prototype runs before any
    /// ScriptableObject pipeline exists. Replace with authored data in M2.
    /// </summary>
    public static class PrototypeContent
    {
        // Materials
        public static readonly ItemDefinition Wood = new ItemDefinition("wood", "Gỗ", ItemCategory.Material, ItemRarity.Common, 1, 2, 5, 8);
        public static readonly ItemDefinition ScrapMetal = new ItemDefinition("scrap_metal", "Sắt vụn", ItemCategory.Material, ItemRarity.Common, 1, 1, 10, 6);
        public static readonly ItemDefinition Cloth = new ItemDefinition("cloth", "Vải", ItemCategory.Material, ItemRarity.Common, 1, 1, 10, 5);
        public static readonly ItemDefinition CircuitBoard = new ItemDefinition("circuit_board", "Bảng mạch", ItemCategory.Material, ItemRarity.Rare, 1, 1, 3, 60);

        // Consumables
        public static readonly ItemDefinition CannedFood = new ItemDefinition("canned_food", "Đồ hộp", ItemCategory.Food, ItemRarity.Common, 1, 1, 3, 15);
        public static readonly ItemDefinition Bandage = new ItemDefinition("bandage", "Băng gạc", ItemCategory.Medicine, ItemRarity.Common, 1, 1, 5, 12);
        public static readonly ItemDefinition PistolAmmo = new ItemDefinition("pistol_ammo", "Đạn súng ngắn", ItemCategory.Ammo, ItemRarity.Common, 1, 1, 30, 2);

        public static readonly ItemDefinition ShotgunShell = new ItemDefinition("shotgun_shell", "Đạn shotgun", ItemCategory.Ammo, ItemRarity.Uncommon, 1, 1, 12, 5);
        public static readonly ItemDefinition WaterBottle = new ItemDefinition("water_bottle", "Chai nước", ItemCategory.Food, ItemRarity.Common, 1, 2, 1, 10);

        // Seeds and produce
        public static readonly ItemDefinition SweetPotatoSeed = new ItemDefinition("seed_sweet_potato", "Dây khoai lang", ItemCategory.Seed, ItemRarity.Common, 1, 1, 20, 4);
        public static readonly ItemDefinition CornSeed = new ItemDefinition("seed_corn", "Hạt ngô", ItemCategory.Seed, ItemRarity.Common, 1, 1, 20, 6);
        public static readonly ItemDefinition MugwortSeed = new ItemDefinition("seed_mugwort", "Hạt ngải cứu", ItemCategory.Seed, ItemRarity.Uncommon, 1, 1, 20, 10);
        public static readonly ItemDefinition SweetPotato = new ItemDefinition("sweet_potato", "Khoai lang", ItemCategory.Produce, ItemRarity.Common, 1, 1, 10, 12);
        public static readonly ItemDefinition Corn = new ItemDefinition("corn", "Ngô", ItemCategory.Produce, ItemRarity.Common, 1, 1, 10, 14);
        public static readonly ItemDefinition Mugwort = new ItemDefinition("mugwort", "Ngải cứu", ItemCategory.Produce, ItemRarity.Uncommon, 1, 1, 10, 20);

        // Gifts
        public static readonly ItemDefinition TeaTin = new ItemDefinition("tea_tin", "Hộp trà", ItemCategory.Gift, ItemRarity.Uncommon, 1, 1, 1, 40);
        public static readonly ItemDefinition OldCassette = new ItemDefinition("old_cassette", "Băng cassette cũ", ItemCategory.Gift, ItemRarity.Rare, 1, 1, 1, 70);

        public static readonly IReadOnlyList<ItemDefinition> Items = new[]
        {
            Wood, ScrapMetal, Cloth, CircuitBoard, CannedFood, Bandage, PistolAmmo, ShotgunShell, WaterBottle,
            SweetPotatoSeed, CornSeed, MugwortSeed, SweetPotato, Corn, Mugwort, TeaTin, OldCassette
        };

        public static ItemDefinition FindItem(string itemId)
        {
            foreach (var item in Items)
            {
                if (item.Id == itemId) return item;
            }
            return null;
        }

        public static readonly IReadOnlyList<ConsumableDefinition> Consumables = new[]
        {
            new ConsumableDefinition(CannedFood.Id, hunger: 40f, thirst: 5f),
            new ConsumableDefinition(WaterBottle.Id, thirst: 50f),
            new ConsumableDefinition(SweetPotato.Id, hunger: 25f),
            new ConsumableDefinition(Corn.Id, hunger: 20f, thirst: 5f),
            new ConsumableDefinition(Bandage.Id, heal: 35f),
            new ConsumableDefinition(Mugwort.Id, heal: 15f)
        };

        public static ConsumableDefinition FindConsumable(string itemId)
        {
            foreach (var consumable in Consumables)
            {
                if (consumable.ItemId == itemId) return consumable;
            }
            return null;
        }

        public static readonly CropDefinition SweetPotatoCrop = new CropDefinition(
            "crop_sweet_potato", "Khoai lang", SweetPotatoSeed, SweetPotato, 4,
            SeasonMask.Spring | SeasonMask.Summer | SeasonMask.Autumn, 0, 2, 3);

        public static readonly CropDefinition CornCrop = new CropDefinition(
            "crop_corn", "Ngô", CornSeed, Corn, 6,
            SeasonMask.Spring | SeasonMask.Summer, 3, 1, 2);

        public static readonly CropDefinition MugwortCrop = new CropDefinition(
            "crop_mugwort", "Ngải cứu", MugwortSeed, Mugwort, 3,
            SeasonMask.Spring | SeasonMask.Summer | SeasonMask.Autumn, 0, 1, 2);

        public static readonly IReadOnlyList<CropDefinition> Crops = new[] { SweetPotatoCrop, CornCrop, MugwortCrop };

        public static readonly LootTable HouseCabinet = new LootTable("house_cabinet")
            .Add(Wood, 20, 1, 2)
            .Add(Cloth, 25, 1, 3)
            .Add(CannedFood, 20)
            .Add(WaterBottle, 18)
            .Add(ScrapMetal, 20, 1, 3)
            .Add(TeaTin, 8)
            .Add(OldCassette, 3)
            .Add(SweetPotatoSeed, 12, 1, 3);

        public static readonly LootTable MedicalCabinet = new LootTable("medical_cabinet")
            .Add(Bandage, 60, 1, 2)
            .Add(Cloth, 25, 1, 2)
            .Add(MugwortSeed, 15, 1, 2);

        public static readonly LootTable SeedStore = new LootTable("seed_store")
            .Add(SweetPotatoSeed, 40, 2, 4)
            .Add(CornSeed, 35, 2, 4)
            .Add(MugwortSeed, 25, 1, 3);

        public static readonly LootTable WeaponCrate = new LootTable("weapon_crate")
            .Add(PistolAmmo, 60, 6, 14)
            .Add(ShotgunShell, 35, 2, 5)
            .Add(ScrapMetal, 25, 2, 4)
            .Add(CircuitBoard, 15);

        public static readonly IReadOnlyList<LootTable> RaidLootTables = new[] { HouseCabinet, MedicalCabinet, SeedStore, WeaponCrate };

        public const string NpcLan = "lan";
        public const string NpcMinh = "minh";
        public const string NpcBaTu = "ba_tu";

        public static NpcProfile[] CreateNpcs()
        {
            return new[]
            {
                new NpcProfile(NpcLan, "Lan", Gender.Female, true)
                    .Loves(TeaTin.Id, Mugwort.Id).Likes(Corn.Id, Cloth.Id).Dislikes(ScrapMetal.Id),
                new NpcProfile(NpcMinh, "Minh", Gender.Male, true)
                    .Loves(OldCassette.Id, CircuitBoard.Id).Likes(CannedFood.Id, SweetPotato.Id).Dislikes(Cloth.Id),
                new NpcProfile(NpcBaTu, "Bà Tư", Gender.Female, false)
                    .Loves(SweetPotato.Id).Likes(TeaTin.Id, Mugwort.Id)
            };
        }

        public static DeliveryQuest[] CreateQuests()
        {
            return new[]
            {
                new DeliveryQuest("q_fence", "Dựng lại hàng rào", NpcMinh, Wood.Id, 3, 60, 100),
                new DeliveryQuest("q_first_harvest", "Vụ khoai đầu tiên", NpcBaTu, SweetPotato.Id, 2, 40, 150),
                new DeliveryQuest("q_first_aid", "Tủ thuốc cho căn cứ", NpcLan, Bandage.Id, 2, 50, 100)
            };
        }

        /// <summary>A new save: NPCs, quests and a small starter kit in the stash and backpack.</summary>
        public static GameSession CreateNewSession(Gender playerGender, int seed)
        {
            var session = new GameSession(playerGender, seed);

            foreach (var npc in CreateNpcs()) session.Relationships.Register(npc);
            foreach (var quest in CreateQuests()) session.Quests.Add(quest);

            session.Wallet.Add(50);
            session.Stash.Add(SweetPotatoSeed, 4);
            session.Backpack.Add(PistolAmmo, 16);
            session.Backpack.Add(ShotgunShell, 4);
            session.Backpack.Add(Bandage, 1);
            session.Backpack.Add(CannedFood, 1);
            session.Backpack.Add(WaterBottle, 1);
            return session;
        }
    }
}
