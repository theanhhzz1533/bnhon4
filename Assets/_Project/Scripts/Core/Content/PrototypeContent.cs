using System.Collections.Generic;
using Bnhon.Core.Crafting;
using Bnhon.Core.Farming;
using Bnhon.Core.Items;
using Bnhon.Core.Loot;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
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

        // Weapons. Their combat stats live with the game layer, keyed by these ids.
        public static readonly ItemDefinition Pistol = new ItemDefinition("pistol", "Súng ngắn", ItemCategory.Weapon, ItemRarity.Uncommon, 2, 1, 1, 120);
        public static readonly ItemDefinition Shotgun = new ItemDefinition("shotgun", "Shotgun", ItemCategory.Weapon, ItemRarity.Rare, 3, 1, 1, 260);
        public const int PistolMagazine = 8;

        public static readonly ItemDefinition MedKit = new ItemDefinition("med_kit", "Bộ sơ cứu", ItemCategory.Medicine, ItemRarity.Uncommon, 1, 2, 2, 60);

        // Blueprints: found on raids, registered at the workbench to unlock a recipe for good.
        public static readonly ItemDefinition BlueprintPistol = new ItemDefinition("bp_pistol", "Bản vẽ: Súng ngắn", ItemCategory.Blueprint, ItemRarity.Rare, 1, 1, 1, 90);
        public static readonly ItemDefinition BlueprintShotgun = new ItemDefinition("bp_shotgun", "Bản vẽ: Shotgun", ItemCategory.Blueprint, ItemRarity.Epic, 1, 1, 1, 180);
        public static readonly ItemDefinition BlueprintMedKit = new ItemDefinition("bp_med_kit", "Bản vẽ: Bộ sơ cứu", ItemCategory.Blueprint, ItemRarity.Uncommon, 1, 1, 1, 50);

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
            Wood, ScrapMetal, Cloth, CircuitBoard, CannedFood, Bandage, PistolAmmo, ShotgunShell, WaterBottle, Pistol, Shotgun, MedKit, BlueprintPistol, BlueprintShotgun, BlueprintMedKit,
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
            new ConsumableDefinition(CannedFood.Id, hunger: 40f, thirst: 5f, useSeconds: 3f),
            new ConsumableDefinition(WaterBottle.Id, thirst: 50f),
            new ConsumableDefinition(SweetPotato.Id, hunger: 25f),
            new ConsumableDefinition(Corn.Id, hunger: 20f, thirst: 5f),
            new ConsumableDefinition(Bandage.Id, heal: 30f, useSeconds: 3.5f),
            new ConsumableDefinition(Mugwort.Id, heal: 15f, useSeconds: 2.5f),
            new ConsumableDefinition(MedKit.Id, heal: 65f, useSeconds: 5f)
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

        public static CropDefinition FindCrop(string cropId)
        {
            foreach (var crop in Crops)
            {
                if (crop.Id == cropId) return crop;
            }
            return null;
        }

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
            .Add(Bandage, 32)
            .Add(Cloth, 40, 1, 2)
            .Add(MugwortSeed, 20, 1, 2)
            .Add(BlueprintMedKit, 8);

        public static readonly LootTable SeedStore = new LootTable("seed_store")
            .Add(SweetPotatoSeed, 40, 2, 4)
            .Add(CornSeed, 35, 2, 4)
            .Add(MugwortSeed, 25, 1, 3);

        public static readonly LootTable WeaponCrate = new LootTable("weapon_crate")
            .Add(PistolAmmo, 60, 6, 14)
            .Add(ShotgunShell, 35, 2, 5)
            .Add(Pistol, 9)
            .Add(Shotgun, 6)
            .Add(BlueprintPistol, 7)
            .Add(BlueprintShotgun, 4)
            .Add(ScrapMetal, 25, 2, 4)
            .Add(CircuitBoard, 15);

        // What zombies carry. Searched on their corpses.
        public static readonly LootTable WalkerCorpse = new LootTable("corpse_walker")
            .Add(Cloth, 35, 1, 2)
            .Add(ScrapMetal, 25, 1, 2)
            .Add(CannedFood, 15)
            .Add(PistolAmmo, 15, 2, 4)
            .Add(Bandage, 4);

        public static readonly LootTable RunnerCorpse = new LootTable("corpse_runner")
            .Add(WaterBottle, 28)
            .Add(Cloth, 30, 1, 2)
            .Add(Bandage, 8)
            .Add(PistolAmmo, 20, 2, 5)
            .Add(ShotgunShell, 14, 1, 2);

        public static readonly LootTable ScreamerCorpse = new LootTable("corpse_screamer")
            .Add(TeaTin, 30)
            .Add(MugwortSeed, 30, 1, 3)
            .Add(CircuitBoard, 25)
            .Add(OldCassette, 15);

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

        private static ItemAmount A(ItemDefinition item, int count)
        {
            return new ItemAmount(item, count);
        }

        /// <summary>Recipes, workbench upgrade costs and what dismantling gives.</summary>
        public static void SetUpWorkshop(Workshop workshop)
        {
            // Level 1: the basics that keep a broke player able to go out again.
            workshop.AddRecipe(new Recipe("r_bandage", Bandage, 1, 1, null, A(Cloth, 2)));
            workshop.AddRecipe(new Recipe("r_pistol_ammo", PistolAmmo, 6, 1, null, A(ScrapMetal, 2)));

            // Level 2
            workshop.AddRecipe(new Recipe("r_shotgun_shell", ShotgunShell, 4, 2, null, A(ScrapMetal, 2), A(Cloth, 1)));
            workshop.AddRecipe(new Recipe("r_med_kit", MedKit, 1, 2, BlueprintMedKit, A(Bandage, 2), A(Mugwort, 1)));
            workshop.AddRecipe(new Recipe("r_pistol", Pistol, 1, 2, BlueprintPistol, A(ScrapMetal, 6), A(Wood, 2)));

            // Level 3
            workshop.AddRecipe(new Recipe("r_shotgun", Shotgun, 1, 3, BlueprintShotgun, A(ScrapMetal, 8), A(Wood, 3), A(CircuitBoard, 1)));

            workshop.SetUpgradeCost(2, A(Wood, 6), A(ScrapMetal, 6), A(Cloth, 4));
            workshop.SetUpgradeCost(3, A(Wood, 10), A(ScrapMetal, 12), A(CircuitBoard, 3));

            workshop.SetDismantle(Pistol, A(ScrapMetal, 3));
            workshop.SetDismantle(Shotgun, A(ScrapMetal, 4), A(Wood, 1));
            workshop.SetDismantle(Bandage, A(Cloth, 1));
            workshop.SetDismantle(TeaTin, A(ScrapMetal, 1));
            workshop.SetDismantle(OldCassette, A(ScrapMetal, 1));
        }

        /// <summary>What the base shop sells. Prices are well above what it pays, so trading is never free money.</summary>
        public static void SetUpShop(Trading.Shop shop)
        {
            shop.AddEntry(PistolAmmo, 10, 30, 4);
            shop.AddEntry(ShotgunShell, 4, 32, 2);
            shop.AddEntry(Bandage, 1, 22, 3);
            shop.AddEntry(CannedFood, 1, 26, 3);
            shop.AddEntry(WaterBottle, 1, 18, 3);
            shop.AddEntry(SweetPotatoSeed, 3, 18, 3);
            shop.AddEntry(CornSeed, 3, 24, 2);
            shop.AddEntry(MugwortSeed, 2, 28, 2);
            shop.AddEntry(Pistol, 1, 220, 1, 0.35);
            shop.AddEntry(BlueprintMedKit, 1, 120, 1, 0.25);
        }

        /// <summary>A new save: NPCs, quests and a small starter kit in the stash and backpack.</summary>
        public static GameSession CreateNewSession(Gender playerGender, int seed)
        {
            var session = new GameSession(playerGender, seed);

            foreach (var npc in CreateNpcs()) session.Relationships.Register(npc);
            foreach (var quest in CreateQuests()) session.Quests.Add(quest);

            SetUpWorkshop(session.Workshop);
            SetUpShop(session.Shop);
            session.Shop.FillStock();
            session.Loadout.Set(0, Pistol, PistolMagazine);
            session.Wallet.Add(50);
            session.Stash.Add(SweetPotatoSeed, 4);
            session.Backpack.Add(PistolAmmo, 16);
            session.Backpack.Add(ShotgunShell, 4);
            session.Backpack.Add(Bandage, 1);
            session.Backpack.Add(CannedFood, 1);
            session.Backpack.Add(WaterBottle, 1);
            return session;
        }

        /// <summary>
        /// Keeps a death from leaving the player with only a knife forever: if they own no
        /// firearm anywhere, an old pistol and one magazine of ammo appear in the stash.
        /// Returns true if it granted one.
        /// </summary>
        public static bool GrantFallbackWeapon(GameSession session)
        {
            if (session.Loadout.HasAny) return false;
            foreach (var grid in new[] { session.Backpack, session.Stash, session.SecurePouch })
            {
                foreach (var placed in grid.Items)
                {
                    if (placed.Item.Category == ItemCategory.Weapon) return false;
                }
            }

            if (session.Stash.Add(Pistol, 1) != 0) return false;
            session.Stash.Add(PistolAmmo, PistolMagazine);
            return true;
        }

        /// <summary>Rebuilds a session from a save: a new session with the saved progress applied on top.</summary>
        public static GameSession LoadSession(SaveData data, int seed, out LoadReport report)
        {
            var session = CreateNewSession((Gender)data.playerGender, seed);
            report = SaveMapper.Apply(data, session, FindItem, FindCrop);
            if (report.AbandonedRaid) GrantFallbackWeapon(session);
            return session;
        }
    }
}
