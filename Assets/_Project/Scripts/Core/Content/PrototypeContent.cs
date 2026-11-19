using System.Collections.Generic;
using Bnhon.Core.Charms;
using Bnhon.Core.Crafting;
using Bnhon.Core.Farming;
using Bnhon.Core.Items;
using Bnhon.Core.Layout;
using Bnhon.Core.Loot;
using Bnhon.Core.Npcs;
using Bnhon.Core.Quests;
using Bnhon.Core.Raid;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using Bnhon.Core.Story;
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

        // What Quân brews charms from: tissue taken from zombie bodies, and the cores of bosses.
        public static readonly ItemDefinition Sample = new ItemDefinition("sample", "Mẫu vật", ItemCategory.Material, ItemRarity.Uncommon, 1, 1, 30, 8);
        public static readonly ItemDefinition CoreElectrician = new ItemDefinition("core_electrician", "Lõi Gã thợ điện", ItemCategory.Quest, ItemRarity.Epic, 1, 1, 1, 0);
        public static readonly ItemDefinition CoreButcher = new ItemDefinition("core_butcher", "Lõi Đồ tể", ItemCategory.Quest, ItemRarity.Epic, 1, 1, 1, 0);

        // Found in the cabinet by the hospital bed. Picking it up gives the player their knife for good.
        public static readonly ItemDefinition Dagger = new ItemDefinition("dagger", "Dao găm", ItemCategory.Quest, ItemRarity.Uncommon, 1, 2, 1, 0);

        // More crops, so every season has something to grow.
        public static readonly ItemDefinition TomatoSeed = new ItemDefinition("seed_tomato", "Hạt cà chua", ItemCategory.Seed, ItemRarity.Common, 1, 1, 20, 7);
        public static readonly ItemDefinition WaterSpinachSeed = new ItemDefinition("seed_water_spinach", "Hạt rau muống", ItemCategory.Seed, ItemRarity.Common, 1, 1, 20, 4);
        public static readonly ItemDefinition CabbageSeed = new ItemDefinition("seed_cabbage", "Hạt bắp cải", ItemCategory.Seed, ItemRarity.Common, 1, 1, 20, 7);
        public static readonly ItemDefinition Tomato = new ItemDefinition("tomato", "Cà chua", ItemCategory.Produce, ItemRarity.Common, 1, 1, 10, 12);
        public static readonly ItemDefinition WaterSpinach = new ItemDefinition("water_spinach", "Rau muống", ItemCategory.Produce, ItemRarity.Common, 1, 1, 10, 7);
        public static readonly ItemDefinition Cabbage = new ItemDefinition("cabbage", "Bắp cải", ItemCategory.Produce, ItemRarity.Common, 1, 1, 5, 22);

        // Cooked in the kitchen. Each feeds well and gives one buff for the day's raid.
        public static readonly ItemDefinition RoastedSweetPotato = new ItemDefinition("meal_roasted_sweet_potato", "Khoai nướng", ItemCategory.Food, ItemRarity.Uncommon, 1, 1, 3, 40);
        public static readonly ItemDefinition SpinachSoup = new ItemDefinition("meal_spinach_soup", "Canh rau muống", ItemCategory.Food, ItemRarity.Uncommon, 1, 1, 3, 35);
        public static readonly ItemDefinition MugwortPorridge = new ItemDefinition("meal_mugwort_porridge", "Cháo ngải cứu", ItemCategory.Food, ItemRarity.Uncommon, 1, 1, 3, 50);
        public static readonly ItemDefinition CabbageStirFry = new ItemDefinition("meal_cabbage_stir_fry", "Bắp cải xào cà chua", ItemCategory.Food, ItemRarity.Uncommon, 1, 1, 3, 55);

        public static readonly BuffDefinition BuffEndurance = new BuffDefinition("buff_endurance", "Dẻo dai: thể lực hồi nhanh hơn 30%", BuffStat.StaminaRegen, 0.3f, 240f);
        public static readonly BuffDefinition BuffHydrated = new BuffDefinition("buff_hydrated", "Mát ruột: đói và khát chậm hơn 40%", BuffStat.NeedsDrain, -0.4f, 300f);
        public static readonly BuffDefinition BuffMending = new BuffDefinition("buff_mending", "Ấm bụng: hồi 0,5 máu mỗi giây", BuffStat.HealthRegen, 0.5f, 120f);
        public static readonly BuffDefinition BuffNimble = new BuffDefinition("buff_nimble", "Nhanh nhẹn: di chuyển nhanh hơn 8%", BuffStat.MoveSpeed, 0.08f, 240f);

        public static readonly IReadOnlyList<BuffDefinition> Buffs = new[] { BuffEndurance, BuffHydrated, BuffMending, BuffNimble };

        public static BuffDefinition FindBuff(string buffId)
        {
            foreach (var buff in Buffs)
            {
                if (buff.Id == buffId) return buff;
            }
            return null;
        }

        public static readonly IReadOnlyList<ItemDefinition> Items = new[]
        {
            Wood, ScrapMetal, Cloth, CircuitBoard, CannedFood, Bandage, PistolAmmo, ShotgunShell, WaterBottle, Pistol, Shotgun, MedKit, BlueprintPistol, BlueprintShotgun, BlueprintMedKit,
            SweetPotatoSeed, CornSeed, MugwortSeed, SweetPotato, Corn, Mugwort, TeaTin, OldCassette,
            TomatoSeed, WaterSpinachSeed, CabbageSeed, Tomato, WaterSpinach, Cabbage,
            RoastedSweetPotato, SpinachSoup, MugwortPorridge, CabbageStirFry, Dagger,
            Sample, CoreElectrician, CoreButcher
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
            new ConsumableDefinition(MedKit.Id, heal: 65f, useSeconds: 5f),
            new ConsumableDefinition(Tomato.Id, hunger: 12f, thirst: 12f),
            new ConsumableDefinition(WaterSpinach.Id, hunger: 10f, thirst: 5f),
            new ConsumableDefinition(Cabbage.Id, hunger: 25f, thirst: 5f),
            new ConsumableDefinition(RoastedSweetPotato.Id, hunger: 60f, useSeconds: 3f, buff: BuffEndurance),
            new ConsumableDefinition(SpinachSoup.Id, hunger: 25f, thirst: 60f, useSeconds: 3f, buff: BuffHydrated),
            new ConsumableDefinition(MugwortPorridge.Id, heal: 20f, hunger: 45f, thirst: 15f, useSeconds: 3f, buff: BuffMending),
            new ConsumableDefinition(CabbageStirFry.Id, hunger: 55f, thirst: 10f, useSeconds: 3f, buff: BuffNimble)
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

        public static readonly CropDefinition TomatoCrop = new CropDefinition(
            "crop_tomato", "Cà chua", TomatoSeed, Tomato, 5,
            SeasonMask.Spring | SeasonMask.Summer, 2, 1, 2);

        public static readonly CropDefinition WaterSpinachCrop = new CropDefinition(
            "crop_water_spinach", "Rau muống", WaterSpinachSeed, WaterSpinach, 3,
            SeasonMask.Summer | SeasonMask.Autumn, 2, 2, 3);

        public static readonly CropDefinition CabbageCrop = new CropDefinition(
            "crop_cabbage", "Bắp cải", CabbageSeed, Cabbage, 5,
            SeasonMask.Autumn | SeasonMask.Winter, 0, 1, 1);

        public static readonly IReadOnlyList<CropDefinition> Crops = new[] { SweetPotatoCrop, CornCrop, MugwortCrop, TomatoCrop, WaterSpinachCrop, CabbageCrop };

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
            .Add(MugwortSeed, 25, 1, 3)
            .Add(TomatoSeed, 30, 2, 4)
            .Add(WaterSpinachSeed, 30, 2, 4)
            .Add(CabbageSeed, 25, 2, 3);

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

        /// <summary>Safes: few, deep inside dangerous buildings, and worth the trip.</summary>
        public static readonly LootTable Safe = new LootTable("safe")
            .Add(CircuitBoard, 30, 1, 2)
            .Add(PistolAmmo, 25, 8, 14)
            .Add(ShotgunShell, 20, 3, 6)
            .Add(MedKit, 18)
            .Add(Shotgun, 10)
            .Add(BlueprintShotgun, 8)
            .Add(BlueprintPistol, 8)
            .Add(TeaTin, 12)
            .Add(OldCassette, 12);

        public static readonly LootTable ArmoredCorpse = new LootTable("corpse_armored")
            .Add(ScrapMetal, 50, 2, 3)
            .Add(PistolAmmo, 25, 2, 5)
            .Add(ShotgunShell, 15, 1, 3)
            .Add(CircuitBoard, 10);

        /// <summary>What the boss of the town centre always carries.</summary>
        public static readonly IReadOnlyList<ItemAmount> ButcherLoot = new[] { A(Shotgun, 1), A(ShotgunShell, 6), A(MedKit, 1), A(CircuitBoard, 2), A(CoreButcher, 1), A(Sample, 6) };

        public static readonly IReadOnlyList<LootTable> RaidLootTables = new[] { HouseCabinet, MedicalCabinet, SeedStore, WeaponCrate, Safe };

        public const string AreaOutskirtsId = "outskirts";
        public const string AreaTownId = "town";
        public const string FlagButcherDead = "boss_town";

        public const string FlagTowerBossDead = "boss_outskirts";

        public static readonly RaidArea AreaOutskirts = new RaidArea(AreaOutskirtsId, "Vùng ven thị trấn", "outskirts",
            "Xóm nhà ống, trường học, trạm xăng, ruộng và chợ quê. Rộng, nhiều ngả. Trên đồi phía đông nam, Gã thợ điện canh tháp radio.",
            null, FlagTowerBossDead)
        {
            BossName = "Gã thợ điện",
            BossHealth = 600f,
            BossLoot = new[] { A(CircuitBoard, 3), A(PistolAmmo, 16), A(MedKit, 1), A(BlueprintPistol, 1), A(CoreElectrician, 1), A(Sample, 5) }
        };

        public static readonly RaidArea AreaTown = new RaidArea(AreaTownId, "Trung tâm thị trấn", "town",
            "Phố xá chật, zombie đông và có cả loại bọc thép. Két sắt và hòm vũ khí nhiều hơn hẳn. Đồ tể chiếm trụ sở ở giữa.",
            "step_workbench", FlagButcherDead)
        {
            BossName = "Đồ tể",
            BossLoot = ButcherLoot
        };

        public static readonly IReadOnlyList<RaidArea> RaidAreas = new[] { AreaOutskirts, AreaTown };

        public static RaidArea FindArea(string areaId)
        {
            foreach (var area in RaidAreas)
            {
                if (area.Id == areaId) return area;
            }
            return null;
        }

        public const string NpcLan = "lan";
        public const string NpcMinh = "minh";
        public const string NpcBaTu = "ba_tu";
        public const string NpcChuSau = "chu_sau";
        public const string NpcQuan = "quan";

        /// <summary>The residents who are at the base from the very start.</summary>
        public static readonly IReadOnlyList<string> Founders = new[] { NpcLan, NpcMinh, NpcBaTu };

        public static NpcProfile[] CreateNpcs()
        {
            return new[]
            {
                new NpcProfile(NpcLan, "Lan", Gender.Female, true)
                    .Loves(TeaTin.Id, Mugwort.Id).Likes(Corn.Id, Cloth.Id).Dislikes(ScrapMetal.Id),
                new NpcProfile(NpcMinh, "Minh", Gender.Male, true)
                    .Loves(OldCassette.Id, CircuitBoard.Id).Likes(CannedFood.Id, SweetPotato.Id).Dislikes(Cloth.Id),
                new NpcProfile(NpcBaTu, "Bà Tư", Gender.Female, false)
                    .Loves(SweetPotato.Id).Likes(TeaTin.Id, Mugwort.Id),
                new NpcProfile(NpcChuSau, "Chú Sáu", Gender.Male, false)
                    .Loves(OldCassette.Id, TeaTin.Id).Likes(CannedFood.Id, Corn.Id).Dislikes(Mugwort.Id),
                new NpcProfile(NpcQuan, "Quân", Gender.Male, false)
                    .Loves(CircuitBoard.Id, Sample.Id).Likes(TeaTin.Id, MedKit.Id).Dislikes(CannedFood.Id, OldCassette.Id)
            };
        }

        // Places in the base a resident can stand. The scene maps each to a position.
        public const string SpotClinic = "clinic";
        public const string SpotWorkshop = "workshop";
        public const string SpotPorch = "porch";
        public const string SpotKitchen = "kitchen";
        public const string SpotWell = "well";
        public const string SpotGate = "gate";
        public const string SpotFarm = "farm";
        public const string SpotShop = "shop";
        public const string SpotLab = "lab";

        public static readonly IReadOnlyList<string> BaseSpots = new[] { SpotClinic, SpotWorkshop, SpotPorch, SpotKitchen, SpotWell, SpotGate, SpotFarm, SpotShop, SpotLab };

        public const string EventLanSupplies = "lan_2_supplies";
        public const string EventLanTea = "lan_4_tea";

        /// <summary>Lan: the base's medic, a nursing student when the outbreak began. The one fully written resident so far.</summary>
        private static NpcLife CreateLan()
        {
            var schedule = new NpcSchedule(SpotClinic)
                .On(DayPhase.Evening, SpotKitchen)
                .OnDay(6, DayPhase.Morning, SpotWell);

            var book = new DialogueBook
            {
                FirstMeeting = "Em là Lan, lo tủ thuốc của hợp tác xã. {P} mới tới thì nhớ một điều thôi: bị cắn hay bị xước gì cũng phải báo em, đừng giấu.",
                AlreadyTalked = "Mình nói chuyện rồi mà. {P} đi làm việc đi, em còn phải kiểm thuốc.",
                AcceptsDating = "...Em tưởng {p} không bao giờ hỏi. Vâng."
            };
            book.SayIn(DayPhase.Morning, "{P} ra ngoài nhớ mang băng gạc. Em không muốn tối nay phải khâu cho ai đâu.", 0, 1)
                .SayIn(DayPhase.Morning, "Tủ thuốc còn đúng nửa lọ cồn. Thấy tủ y tế ngoài kia thì {p} lục kỹ giúp em.", 0, 1)
                .SayIn(DayPhase.Evening, "{P} về rồi à. Xoay một vòng em xem có vết thương nào không.", 0, 1)
                .SayIn(DayPhase.Evening, "Tối nào em cũng đếm người. Đủ thì mới ngủ được.", 0, 1)
                .SayIn(DayPhase.Morning, "Hồi trước em học điều dưỡng, mới năm ba thì dịch nổ ra. Giờ cả căn cứ gọi em là bác sĩ, nghe mà run.", 2, 3)
                .SayIn(DayPhase.Morning, "Ngải cứu {p} trồng tốt lắm. Đắp vết bầm còn nhanh hơn thuốc em có.", 2, 3)
                .SayIn(DayPhase.Evening, "Hôm nay ngoài đó có đông không? {P} kể đi, em nghe.", 2, 3)
                .SayIn(DayPhase.Evening, "Em nấu dư một bát. Không phải phần của {p} đâu, nhưng {p} ăn đi.", 2, 3)
                .SayIn(DayPhase.Morning, "Dạo này nghe tiếng cổng mở buổi chiều là em biết {p} về. Lạ ghê.", 4)
                .SayIn(DayPhase.Morning, "Em để dành cho {p} một gói trà. Uống cho ấm rồi hẵng đi.", 4)
                .SayIn(DayPhase.Evening, "Ngồi xuống đây một lát đi. Hôm nay em không hỏi vết thương nữa, em hỏi {p} có mệt không.", 4)
                .SayIn(DayPhase.Evening, "Hồi mới tới đây em sợ buổi tối lắm. Giờ thì buổi tối là lúc em thích nhất.", 4)
                .SayAt(SpotWell, "Chủ nhật em tự cho mình nghỉ nửa buổi. Ngồi cạnh giếng nghe tiếng nước, đỡ nhớ biển.")
                .OnGift(GiftTaste.Loved, "Trời, {p} tìm đâu ra vậy? Em thích lắm, cảm ơn {p}!")
                .OnGift(GiftTaste.Liked, "Cảm ơn {p} nhé, em dùng được đấy.")
                .OnGift(GiftTaste.Neutral, "Cảm ơn {p}.")
                .OnGift(GiftTaste.Disliked, "Ờ... em cảm ơn. Chắc em để tạm ở góc kho.");

            var supplies = new HeartEvent(EventLanSupplies, "Nửa lọ cồn", 2, DayPhase.Evening)
                .Narrate("Buổi tối, Lan ngồi trước tủ thuốc mở toang, đếm đi đếm lại mấy cuộn băng.")
                .Says("Lan", "Em đếm ba lần rồi mà vẫn ra từng này. Nếu tuần này có ai bị thương nặng, em không biết phải làm sao.")
                .Says("Lan", "Hồi ở trường, thiếu gì thì chạy xuống kho xin. Giờ kho là mấy cái tủ ngoài kia, giữa bầy zombie.")
                .Says("Lan", "Em xin lỗi, em không định than với {p}. Chỉ là... ở đây ai cũng nghĩ em biết hết.")
                .Choice("Em không phải gánh một mình. Thiếu gì cứ viết ra, {p} đi tìm.", 60,
                    "...{P} nói thật nhé. Vậy em viết danh sách. Mà {p} phải về đủ tay đủ chân thì em mới nhận.")
                .Choice("Ai ở đây cũng phải cố thôi.", 15,
                    "Vâng, {p} nói đúng. Em đếm lại lần nữa vậy.");

            var tea = new HeartEvent(EventLanTea, "Hộp trà của mẹ", 4)
                .Narrate("Lan gọi bạn lại, tay cầm một hộp trà móp méo và hai cái cốc sắt.")
                .Says("Lan", "Hộp trà này của mẹ em. Em mang theo từ hôm chạy khỏi thị trấn, tới giờ vẫn chưa dám pha.")
                .Says("Lan", "Mẹ bảo trà ngon thì phải uống với người mình muốn ngồi lâu. Em nghĩ mãi, thấy giữ thêm nữa cũng chẳng để làm gì.")
                .Narrate("Nước giếng đun sôi, trà đã cũ nhưng vẫn thơm. Hai người ngồi nhìn ruộng khoai một lúc lâu.")
                .Says("Lan", "Hết dịch, {p} định làm gì?")
                .Choice("Ở lại đây, trồng cho kín mấy luống này đã.", 60,
                    "Em cũng nghĩ vậy. Ở đây có ruộng, có người chờ cửa. Vậy mai {p} nhớ về sớm, em pha ấm nữa.")
                .Choice("Chưa nghĩ tới. Sống qua từng ngày đã.", 25,
                    "Ừ, cũng phải. Vậy thì sống qua hôm nay cho tử tế: uống hết cốc này đã.");

            return new NpcLife(NpcLan, schedule, book) { AddressMale = "anh", AddressFemale = "chị" }
                .Add(supplies)
                .Add(tea);
        }

        private static NpcLife CreateMinh()
        {
            var book = new DialogueBook
            {
                FirstMeeting = "Minh. Tôi lo máy móc với hàng rào. Súng kẹt đạn thì mang lại đây, đừng đập.",
                AlreadyTalked = "Nói rồi mà. Tôi đang dở tay."
            };
            book.SayIn(DayPhase.Morning, "Tôi đang gom sắt vụn dựng lại hàng rào. Ra ngoài thấy thì nhặt giúp.")
                .SayIn(DayPhase.Morning, "Bàn chế tạo lên cấp thì tôi chỉ cho bạn vài thứ hay hơn băng gạc.")
                .SayIn(DayPhase.Evening, "Nghe tiếng súng vọng về là tôi biết bạn còn sống.")
                .SayIn(DayPhase.Evening, "Tối tôi canh cổng. Bạn ngủ đi, mai còn đi.")
                .OnGift(GiftTaste.Loved, "Cái này mà bạn cũng kiếm được à? Quý đấy. Cảm ơn.")
                .OnGift(GiftTaste.Liked, "Được đấy, cảm ơn.")
                .OnGift(GiftTaste.Neutral, "Ừ, cảm ơn.")
                .OnGift(GiftTaste.Disliked, "Tôi biết làm gì với thứ này bây giờ...");

            return new NpcLife(NpcMinh, new NpcSchedule(SpotWorkshop).On(DayPhase.Evening, SpotGate), book);
        }

        private static NpcLife CreateBaTu()
        {
            var book = new DialogueBook
            {
                FirstMeeting = "Bà là bà Tư. Bà ở hợp tác xã này từ hồi nó còn có loa phát thanh. Cháu cứ coi đây như nhà.",
                AlreadyTalked = "Bà nói rồi đó, cháu đi làm đi kẻo nắng."
            };
            book.SayIn(DayPhase.Morning, "Khoai lang phải tưới sáng sớm cháu ạ, tưới trưa là xót rễ.")
                .SayIn(DayPhase.Morning, "Sang đông chỉ còn bắp cải chịu được rét. Cháu nhớ để dành hạt.")
                .SayIn(DayPhase.Evening, "Về rồi hả cháu. Ăn gì chưa?")
                .SayIn(DayPhase.Evening, "Ngày xưa giờ này cả xóm ra sân phơi hóng gió. Giờ còn mấy bà cháu mình.")
                .OnGift(GiftTaste.Loved, "Ôi, cháu còn nhớ bà thích món này. Bà cảm ơn.")
                .OnGift(GiftTaste.Liked, "Quý hóa quá, bà cảm ơn cháu.")
                .OnGift(GiftTaste.Neutral, "Ừ, bà xin.")
                .OnGift(GiftTaste.Disliked, "Bà già rồi, không dùng tới thứ này đâu cháu.");

            return new NpcLife(NpcBaTu, new NpcSchedule(SpotPorch).On(DayPhase.Morning, SpotFarm), book) { AddressMale = "cháu", AddressFemale = "cháu" };
        }

        private static NpcLife CreateChuSau()
        {
            var book = new DialogueBook
            {
                FirstMeeting = "Chú là Sáu, bán tạp hóa ở chợ quê ba chục năm. Giờ chợ không còn, nhưng buôn bán thì người ta vẫn cần.",
                AlreadyTalked = "Nói rồi mà con. Mua gì thì ghé quầy."
            };
            book.Say("Ngày xưa sáng nào chú cũng mở sạp từ năm giờ. Giờ thì mở lúc nào có hàng.", 0, 3)
                .Say("Đồ hộp là vua đó con. Giữ được lâu, đổi được nhiều.", 0, 3)
                .SayIn(DayPhase.Morning, "Hôm nay ra ngoài nhớ để ý mấy cái tủ bếp. Người ta hay giấu đồ ngon trong đó.", 0, 5)
                .SayIn(DayPhase.Evening, "Về rồi hả? Có món gì hay thì đem ra chú xem giá.", 0, 5)
                .Say("Cái chân này còn đau, nhưng ngồi quầy thì không cần chạy.", 0, 2)
                .Say("Con biết không, ba chục năm buôn bán, chú chưa từng lỗ với khách quen. Con là khách quen rồi đó.", 4)
                .SayIn(DayPhase.Evening, "Hồi đó tối nào chợ cũng có người hát cải lương. Nhớ ghê.", 4)
                .SayIn(DayPhase.Morning, "Chú để dành cho con mấy món tốt ở dưới quầy. Đừng nói ai.", 6)
                .OnGift(GiftTaste.Loved, "Trời đất, cái này quý lắm đó con. Chú cảm ơn!")
                .OnGift(GiftTaste.Liked, "Được đó, chú nhận.")
                .OnGift(GiftTaste.Neutral, "Ừ, chú cảm ơn.")
                .OnGift(GiftTaste.Disliked, "Cái này bán không ai mua đâu con...");

            return new NpcLife(NpcChuSau, new NpcSchedule(SpotShop), book) { AddressMale = "con", AddressFemale = "con" };
        }

        private static NpcLife CreateQuan()
        {
            var book = new DialogueBook
            {
                FirstMeeting = "Quân. Tôi làm nghiên cứu ở bệnh viện thị trấn. Đừng nhìn tôi như thế, tôi không phải bác sĩ chữa cảm cúm.",
                AlreadyTalked = "Tôi đang ghi chép. Có mẫu vật thì để lên bàn."
            };
            book.Say("Cấu trúc mô của chúng thay đổi theo từng con. Thú vị hơn anh nghĩ đấy.", 0, 3)
                .Say("Mẫu vật lấy ở xác còn tươi thì tốt hơn. Đừng để quá lâu.", 0, 3)
                .SayIn(DayPhase.Morning, "Ra ngoài thì lấy giúp tôi ít mẫu. Con chạy nhanh và con bọc thép cho mẫu tốt hơn.", 0, 6)
                .SayIn(DayPhase.Evening, "Về rồi à. Có mẫu mới không?", 0, 6)
                .Say("Cô Lan hỏi tôi về thứ tôi tiêm cho anh. Tôi bảo cô ấy là vitamin.", 2, 5)
                .SayIn(DayPhase.Evening, "Có những câu hỏi mà câu trả lời còn tệ hơn việc không biết.", 4)
                .Say("Anh tin tôi được bao nhiêu? Đừng trả lời vội.", 5)
                .SayIn(DayPhase.Morning, "Hôm qua tôi lại mơ về viện nghiên cứu. Lần này có cả anh trong đó.", 7)
                .OnGift(GiftTaste.Loved, "Cái này... đúng thứ tôi cần. Cảm ơn.")
                .OnGift(GiftTaste.Liked, "Dùng được. Cảm ơn.")
                .OnGift(GiftTaste.Neutral, "Ừm.")
                .OnGift(GiftTaste.Disliked, "Tôi không có thời gian cho thứ này.");
            book.SayIn(DayPhase.Morning, "Hôm nay tôi thử một mẻ mới. Nếu anh thấy chóng mặt thì báo tôi.", 0, 10);
            book.SayIn(DayPhase.Evening, "Đêm ở đây yên hơn bệnh viện. Ở đó, đêm nào cũng có tiếng gõ cửa.", 0, 10);

            return new NpcLife(NpcQuan, new NpcSchedule(SpotLab), book) { AddressMale = "anh", AddressFemale = "chị" };
        }

        public static readonly IReadOnlyList<NpcLife> NpcLives = new[] { CreateLan(), CreateMinh(), CreateBaTu(), CreateChuSau(), CreateQuan() };

        // ---------------------------------------------------------------------------------
        // Charms
        // ---------------------------------------------------------------------------------

        public const string FlagLabOpen = "svc_quan_lab";
        public const string FlagLabBreakdown = "svc_quan_breakdown";
        public const string FlagLabCores = "svc_quan_cores";
        public const string FlagLabGroups = "svc_quan_groups";
        public const string FlagLabThirdSlot = "svc_quan_slot3";
        public const string FlagLabFirstBrewUsed = "lab_first_brew_used";

        private static CharmDefinition Charm(string id, string name, CharmRarity rarity, CharmGroup group, CharmStat stat, float amount, string description)
        {
            return new CharmDefinition(id, name, rarity, group, stat, amount, description);
        }

        public static readonly CharmLab Lab = new CharmLab(Sample)
            .Add(Charm("charm_corpse_scent", "Mùi tử thi", CharmRarity.Common, CharmGroup.Stealth, CharmStat.ZombieSight, 0.2f, "Zombie nhìn thấy bạn ở tầm gần hơn 20%."))
            .Add(Charm("charm_light_step", "Bước chân nhẹ", CharmRarity.Common, CharmGroup.Stealth, CharmStat.Noise, 0.3f, "Chạy và lăn ồn ít hơn 30%."))
            .Add(Charm("charm_antibody", "Kháng thể", CharmRarity.Common, CharmGroup.Survival, CharmStat.DamageTaken, 0.1f, "Bị đánh mất ít máu hơn 10%."))
            .Add(Charm("charm_slow_metabolism", "Trao đổi chất chậm", CharmRarity.Common, CharmGroup.Survival, CharmStat.NeedsDrain, 0.25f, "Đói và khát chậm hơn 25%."))
            .Add(Charm("charm_quick_hands", "Tay nhanh", CharmRarity.Common, CharmGroup.Stealth, CharmStat.SearchSpeed, 0.3f, "Lục thùng và lục xác nhanh hơn 30%."))
            .Add(Charm("charm_strong_lungs", "Phổi khỏe", CharmRarity.Common, CharmGroup.Survival, CharmStat.StaminaRegen, 0.2f, "Thể lực hồi nhanh hơn 20%."))
            .Add(Charm("charm_adrenaline", "Adrenaline", CharmRarity.Rare, CharmGroup.Survival, CharmStat.LowHealthSpeed, 0.2f, "Dưới 25% máu thì chạy nhanh hơn 20%."))
            .Add(Charm("charm_scent", "Đánh hơi", CharmRarity.Rare, CharmGroup.Stealth, CharmStat.LootSense, 10f, "Thùng đồ chưa lục trong vòng 10 ô sáng lên xuyên tường."))
            .Add(Charm("charm_weak_point", "Đọc điểm yếu", CharmRarity.Rare, CharmGroup.Combat, CharmStat.ShieldBreak, 0.5f, "Tấm chắn của zombie bọc thép và trùm vỡ nhanh gấp rưỡi."))
            .Add(Charm("charm_reflex", "Phản xạ", CharmRarity.Rare, CharmGroup.Combat, CharmStat.RollRecovery, 0.5f, "Lăn hồi nhanh gấp đôi."))
            .Add(Charm("charm_recovery", "Hồi phục", CharmRarity.Rare, CharmGroup.Survival, CharmStat.Regeneration, 0.35f, "Không bị đánh 5 giây thì hồi máu dần."))
            .Add(Charm("charm_thick_skin", "Da dày", CharmRarity.Epic, CharmGroup.Survival, CharmStat.MaxHealth, 25f, "Thêm 25 máu tối đa."))
            .Add(Charm("charm_shock", "Xung điện", CharmRarity.Epic, CharmGroup.Combat, CharmStat.MeleeStun, 1.5f, "Đòn dao làm zombie đứng hình 1,5 giây."))
            .Add(Charm("charm_cold_blood", "Máu lạnh", CharmRarity.Epic, CharmGroup.Combat, CharmStat.Recoil, 0.4f, "Súng giật ít hơn 40%."));

        public static CharmDefinition FindCharm(string charmId) => Lab.Find(charmId);

        public static bool LabOpen(GameSession session) => session.Story.Has(FlagLabOpen);

        public static bool FirstBrewFree(GameSession session) => LabOpen(session) && !session.Story.Has(FlagLabFirstBrewUsed);

        /// <summary>Quân brews a charm: the first one is on the house, then 8 samples each (12 to pick the group).</summary>
        public static CharmDefinition BrewCharm(GameSession session, CharmGroup? group = null)
        {
            if (!LabOpen(session)) return null;
            if (group != null && !session.Story.Has(FlagLabGroups)) return null;

            bool free = group == null && FirstBrewFree(session);
            CharmDefinition charm = Lab.Brew(session.Charms, session.Stash, session.Backpack, session.Rng, free, group);
            if (charm != null && free) session.Story.Set(FlagLabFirstBrewUsed);
            return charm;
        }

        public static CharmDefinition BrewFromCore(GameSession session, ItemDefinition core)
        {
            if (!session.Story.Has(FlagLabCores)) return null;
            return Lab.BrewFromCore(session.Charms, core, session.Stash, session.Backpack, session.Rng);
        }

        public static int BreakDownCharm(GameSession session, CharmDefinition charm)
        {
            if (!session.Story.Has(FlagLabBreakdown)) return 0;
            return Lab.BreakDown(session.Charms, charm, session.Stash, session.Backpack);
        }

        /// <summary>Samples a zombie's body holds, by kind: "walker", "runner", "screamer", "armored", "boss".</summary>
        public static int SamplesFromBody(string kind, System.Random rng)
        {
            switch (kind)
            {
                case "runner":
                case "screamer":
                    return rng.NextDouble() < 0.45 ? 1 : 0;
                case "armored":
                    return rng.NextDouble() < 0.7 ? 1 + rng.Next(2) : 0;
                case "boss":
                    return 0; // a boss carries its own fixed loot, samples included
                default:
                    return rng.NextDouble() < 0.28 ? 1 : 0;
            }
        }

        // ---------------------------------------------------------------------------------
        // Rescues and resident tasks
        // ---------------------------------------------------------------------------------

        public const string FlagShopOpen = "svc_sau_shop";
        public const string FlagShopStock = "svc_sau_stock";
        public const string FlagShopRare = "svc_sau_rare";
        public const string FlagShopDiscount = "svc_sau_discount";
        public const string FlagShopCart = "svc_sau_cart";

        public static readonly IReadOnlyList<RescueDefinition> Rescues = new[]
        {
            new RescueDefinition(NpcQuan, AreaOutskirtsId, 0, session => !session.InIntro && session.Story.Has(StoryIds.Extracted))
            {
                ClearRadius = 12f,
                Plea = "Đứng yên. Anh còn sống thật à? Tôi sẽ không bước ra khỏi phòng này chừng nào bầy ngoài sân còn đó. Dọn chúng đi.",
                Thanks = "Được rồi. Anh đi trước, tôi theo. Đừng để tôi phải chạy.",
                Health = 80f,
                Speed = 3.6f,
                Arrival = new[]
                {
                    new SceneLine(null, "Quân đứng ở cổng hợp tác xã, nhìn mọi người như nhìn mẫu vật, rồi gật đầu một cái."),
                    new SceneLine("Quân", "Tôi cần một cái bàn, một chỗ khô ráo, và mẫu vật. Thật nhiều mẫu vật."),
                    new SceneLine("Quân", "Đổi lại, tôi pha cho anh thứ giúp anh sống sót ngoài kia. Tám mẫu vật một mũi. Đừng hỏi trong đó có gì."),
                    new SceneLine(null, "Ông nhìn sang chỗ trống cạnh bếp. Phòng thí nghiệm chỉ cần dựng lên là dùng được.")
                }
            },
            new RescueDefinition(NpcChuSau, AreaOutskirtsId, 1, session => !session.InIntro)
            {
                Needs = A(Bandage, 1),
                Plea = "Ai đó? Ơ, người sống! Con ơi, chân chú bị kẹt dưới sạp, chảy máu quá. Có băng gạc không con?",
                Thanks = "Đỡ rồi, đỡ rồi. Con đi trước, chú lết theo. Đừng đi nhanh quá nghe.",
                Health = 90f,
                Speed = 3.3f,
                Arrival = new[]
                {
                    new SceneLine(null, "Chú Sáu tập tễnh bước qua cổng, nhìn ruộng khoai, nhìn bếp lửa, rồi ngồi phịch xuống."),
                    new SceneLine("Chú Sáu", "Có ruộng, có bếp, có người. Vậy là còn làm ăn được."),
                    new SceneLine("Chú Sáu", "Quầy mua bán của mấy đứa bỏ không đó hả? Để chú. Có điều quầy trống thì bán cái gì: con kiếm giúp chú ít hàng làm vốn đã.")
                }
            }
        };

        public static RescueDefinition FindRescue(string areaId, int markerIndex)
        {
            foreach (var rescue in Rescues)
            {
                if (rescue.AreaId == areaId && rescue.MarkerIndex == markerIndex) return rescue;
            }
            return null;
        }

        public static readonly QuestChain SauChain =
            new QuestChain(NpcChuSau)
                .Add(new ChainStep("Mở lại quầy", "Quầy trống trơn. Chú cần ít hàng làm vốn: 2 đồ hộp và 2 chai nước.", FlagShopOpen)
                    .Bring(A(CannedFood, 2), A(WaterBottle, 2))
                    .Rewards("Quầy mua bán mở cửa: đạn, băng gạc, đồ ăn, nước, hạt giống."))
                .Add(new ChainStep("Hàng tồn kho", "Chú muốn đóng lại cái kệ cho đàng hoàng: 4 sắt vụn và 3 vải.", FlagShopStock)
                    .Bring(A(ScrapMetal, 4), A(Cloth, 3))
                    .Rewards("Quầy bán thêm đạn shotgun và bộ sơ cứu."))
                .Add(new ChainStep("Mạng lưới buôn", "Chú có mối buôn cũ ở thị trấn, cần cái bộ đàm để liên lạc: 2 bảng mạch.", FlagShopRare)
                    .Bring(A(CircuitBoard, 2))
                    .Rewards("Thỉnh thoảng có súng ngắn, shotgun và bản vẽ bộ sơ cứu."))
                .Add(new ChainStep("Sổ nợ", "Chú tìm cuốn băng cải lương hồi xưa hay nghe. Thân thiết hơn chút nữa chú mới dám nhờ.", FlagShopDiscount)
                    .NeedsHearts(3)
                    .Bring(A(OldCassette, 1))
                    .Rewards("Khách quen: mọi món giảm giá 10%."))
                .Add(new ChainStep("Xe ba gác", "Có xe ba gác thì chú chở được nhiều hàng hơn: 10 gỗ và 8 sắt vụn.", FlagShopCart)
                    .Bring(A(Wood, 10), A(ScrapMetal, 8))
                    .Rewards("Mỗi món về thêm 1 suất mỗi ngày, và bán đồ được 60% giá trị thay vì 50%."));

        public static readonly QuestChain QuanChain = new QuestChain(NpcQuan)
            .Add(new ChainStep("Phòng thí nghiệm", "Tôi cần một cái bàn và đồ thủy tinh: 6 gỗ, 4 sắt vụn, 1 bảng mạch.", FlagLabOpen)
                .Bring(A(Wood, 6), A(ScrapMetal, 4), A(CircuitBoard, 1))
                .Rewards("Mở pha bùa: 8 mẫu vật đổi 1 bùa ngẫu nhiên. Lần pha đầu miễn phí."))
            .Add(new ChainStep("Kính hiển vi", "Ráp được kính hiển vi thì tôi tách được mẫu từ bùa dư: 2 bảng mạch, 3 vải.", FlagLabBreakdown)
                .Bring(A(CircuitBoard, 2), A(Cloth, 3))
                .Rewards("Phân rã bùa trùng thành mẫu vật."))
            .Add(new ChainStep("Mẫu đặc biệt", "Lõi của con trùm trên tháp radio. Tôi cần xem nó.", FlagLabCores)
                .Bring(A(CoreElectrician, 1))
                .Rewards("Pha bùa từ lõi trùm: luôn ra bùa hiếm hoặc quý."))
            .Add(new ChainStep("Hồ sơ bệnh viện", "Tôi cần nhiều mẫu để đối chiếu, và cần biết anh có giữ mồm được không.", FlagLabGroups)
                .NeedsHearts(4)
                .Bring(A(Sample, 20))
                .Rewards("Pha nhắm theo nhóm: chọn ra bùa Lén lút, Sinh tồn hoặc Chiến đấu (12 mẫu vật)."))
            .Add(new ChainStep("Viện nghiên cứu", "Lõi của Đồ tể. Và lần này, tôi sẽ kể anh nghe thứ tôi đã làm ở viện.", FlagLabThirdSlot)
                .NeedsHearts(8)
                .Bring(A(CoreButcher, 1))
                .Rewards("Mở ô bùa thứ ba."));

        public static readonly IReadOnlyList<QuestChain> Chains = new[] { SauChain, QuanChain };

        public static QuestChain FindChain(string npcId)
        {
            foreach (var chain in Chains)
            {
                if (chain.NpcId == npcId) return chain;
            }
            return null;
        }

        public static bool ShopOpen(GameSession session) => session.Story.Has(FlagShopOpen);

        /// <summary>
        /// Brings every resident service in line with the story flags: what the shop sells and
        /// at what price. Called after loading and whenever a task is finished.
        /// </summary>
        public static void ApplyServices(GameSession session)
        {
            Trading.Shop shop = session.Shop;
            shop.PriceFactor = session.Story.Has(FlagShopDiscount) ? 0.9 : 1.0;
            shop.SellRate = session.Story.Has(FlagShopCart) ? 0.6 : Trading.Shop.SellFactor;
            shop.ExtraStock = session.Story.Has(FlagShopCart) ? 1 : 0;
            session.Charms.Slots = session.Story.Has(FlagLabThirdSlot) ? 3 : CharmCollection.BaseSlots;
        }

        /// <summary>Completes a resident's current task and updates their services. Returns the step, or null.</summary>
        public static ChainStep CompleteTask(GameSession session, string npcId)
        {
            QuestChain chain = FindChain(npcId);
            ChainStep step = chain != null ? chain.Complete(session) : null;
            if (step != null) ApplyServices(session);
            return step;
        }

        public static NpcLife FindNpcLife(string npcId)
        {
            foreach (var life in NpcLives)
            {
                if (life.NpcId == npcId) return life;
            }
            return null;
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

        public static readonly BuildingType BuildingShop = new BuildingType("shop", "Quầy mua bán", 2, 1);
        public static readonly BuildingType BuildingWorkbench = new BuildingType("workbench", "Bàn chế tạo", 2, 1);
        public static readonly BuildingType BuildingStash = new BuildingType("stash", "Kho", 2, 1);
        public static readonly BuildingType BuildingBed = new BuildingType("bed", "Giường", 1, 2);
        public static readonly BuildingType BuildingKitchen = new BuildingType("kitchen", "Bếp", 2, 1);
        public static readonly BuildingType BuildingWell = new BuildingType("well", "Giếng", 1, 1);
        public static readonly BuildingType BuildingLab = new BuildingType("lab", "Phòng thí nghiệm", 2, 1);

        /// <summary>
        /// The yard: where nothing may be built, and where each building starts. Cell (0,0) is
        /// the bottom-left corner of the yard; the scene puts the yard's centre at the origin.
        /// </summary>
        public static void SetUpBase(BaseLayout layout)
        {
            layout.Reserve(2, 5, 4, 5);    // the farm
            layout.Reserve(1, 7);          // Bà Tư by the farm
            layout.Reserve(18, 2, 2, 4);   // the gate
            layout.Reserve(9, 4, 2, 2);    // where the player appears
            layout.Reserve(7, 11, 2, 1);   // Lan's medicine corner
            layout.Reserve(10, 11);        // Minh at the workshop
            layout.Reserve(4, 11, 2, 1);   // Bà Tư's porch
            layout.Reserve(17, 1);         // Minh's watch by the gate

            layout.Add(BuildingShop, 1, 11);
            layout.Add(BuildingWorkbench, 11, 11);
            layout.Add(BuildingStash, 14, 11);
            layout.Add(BuildingBed, 17, 10);
            layout.Add(BuildingKitchen, 11, 1);
            layout.Add(BuildingWell, 4, 3);
            layout.Add(BuildingLab, 14, 1);
        }

        /// <summary>What the next watering can level costs, and the workbench level it needs. Null at the top level.</summary>
        public static ItemAmount[] CanUpgradeCost(int currentLevel, out int workbenchLevel)
        {
            workbenchLevel = currentLevel;
            if (currentLevel == 1) return new[] { A(ScrapMetal, 4), A(Cloth, 2) };
            if (currentLevel == 2) return new[] { A(ScrapMetal, 8), A(CircuitBoard, 1) };
            return null;
        }

        /// <summary>Upgrades the watering can if the workbench is good enough and the materials are there.</summary>
        public static CraftBlock UpgradeWateringCan(GameSession session)
        {
            ItemAmount[] cost = CanUpgradeCost(session.WateringCan.Level, out int workbenchLevel);
            if (cost == null) return CraftBlock.MaxLevel;
            if (session.Workshop.Level < workbenchLevel) return CraftBlock.WorkbenchLevel;
            if (!Workshop.TrySpend(cost, session.Stash, session.Backpack)) return CraftBlock.Materials;
            session.WateringCan.Upgrade();
            return CraftBlock.None;
        }

        /// <summary>What the kitchen cooks. All at level 1 with no blueprint: the kitchen has no upgrades yet.</summary>
        public static void SetUpKitchen(Workshop kitchen)
        {
            kitchen.AddRecipe(new Recipe("k_roasted_sweet_potato", RoastedSweetPotato, 1, 1, null, A(SweetPotato, 2), A(Wood, 1)));
            kitchen.AddRecipe(new Recipe("k_spinach_soup", SpinachSoup, 1, 1, null, A(WaterSpinach, 2), A(WaterBottle, 1)));
            kitchen.AddRecipe(new Recipe("k_mugwort_porridge", MugwortPorridge, 1, 1, null, A(Mugwort, 1), A(Corn, 1)));
            kitchen.AddRecipe(new Recipe("k_cabbage_stir_fry", CabbageStirFry, 1, 1, null, A(Cabbage, 1), A(Tomato, 1)));
        }

        /// <summary>What the base shop sells. Prices are well above what it pays, so trading is never free money.</summary>
        public static void SetUpShop(Trading.Shop shop)
        {
            shop.AddEntry(PistolAmmo, 10, 30, 4);
            shop.AddEntry(ShotgunShell, 4, 32, 2).RequiredFlag = FlagShopStock;
            shop.AddEntry(MedKit, 1, 90, 1).RequiredFlag = FlagShopStock;
            shop.AddEntry(Bandage, 1, 22, 3);
            shop.AddEntry(CannedFood, 1, 26, 3);
            shop.AddEntry(WaterBottle, 1, 18, 3);
            shop.AddEntry(SweetPotatoSeed, 3, 18, 3);
            shop.AddEntry(CornSeed, 3, 24, 2);
            shop.AddEntry(MugwortSeed, 2, 28, 2);
            shop.AddEntry(TomatoSeed, 3, 26, 2);
            shop.AddEntry(WaterSpinachSeed, 3, 16, 2);
            shop.AddEntry(CabbageSeed, 2, 20, 2);
            shop.AddEntry(Pistol, 1, 220, 1, 0.5).RequiredFlag = FlagShopRare;
            shop.AddEntry(Shotgun, 1, 420, 1, 0.3).RequiredFlag = FlagShopRare;
            shop.AddEntry(BlueprintMedKit, 1, 120, 1, 0.35).RequiredFlag = FlagShopRare;
        }

        /// <summary>What is in the cabinet by the hospital bed.</summary>
        public static readonly IReadOnlyList<ItemAmount> IntroCabinet = new[] { A(Dagger, 1), A(Bandage, 2) };

        /// <summary>Narration when the game opens in the hospital ward.</summary>
        public static readonly IReadOnlyList<SceneLine> IntroWakeScene = new[]
        {
            new SceneLine(null, "Bạn mở mắt. Trần nhà loang ố, ống truyền dịch treo bên giường đã cạn từ lúc nào."),
            new SceneLine(null, "Điều cuối cùng bạn nhớ là ánh đèn pha, tiếng phanh rít và mặt đường lao tới. Một vụ tai nạn. Còn sau đó bao lâu thì bạn không biết."),
            new SceneLine(null, "Bệnh viện im lặng một cách kỳ lạ. Không tiếng máy, không tiếng người."),
            new SceneLine(null, "Rồi một tiếng hét dội lại từ cuối hành lang, và tắt ngang."),
            new SceneLine(null, "Phải ra khỏi đây. Trước hết, tìm thứ gì đó để tự vệ: thử cái tủ cạnh giường.")
        };

        /// <summary>Minh pulls the player out of the canteen and takes them to the base.</summary>
        public static readonly IReadOnlyList<SceneLine> IntroRescueScene = new[]
        {
            new SceneLine(null, "Một bàn tay túm lấy cổ áo bạn, kéo giật qua cánh cửa sắt. Cửa sập lại ngay trước mặt đàn zombie."),
            new SceneLine("Minh", "Im. Đừng thở mạnh. Chúng nó nghe đấy."),
            new SceneLine(null, "Tiếng cào cửa thưa dần. Người đàn ông hạ cây xà beng xuống, nhìn bộ đồ bệnh nhân của bạn."),
            new SceneLine("Minh", "Ba tháng rồi tôi mới thấy có người đi ra từ cái bệnh viện đó mà còn biết chạy. Tôi là Minh."),
            new SceneLine("Minh", "Đi được không? Chỗ bọn tôi cách đây nửa buổi đường. Một hợp tác xã cũ, có tường, có ruộng, có người."),
            new SceneLine(null, "Bạn theo Minh qua những con phố vắng. Trời sập tối thì tới nơi: một cánh cổng tôn, sau đó là ánh lửa bếp."),
            new SceneLine("Minh", "Tới rồi. Tôi để trong kho cho bạn khẩu súng ngắn cũ với ít đạn. Đi chào mọi người đi, rồi ngủ một giấc. Mai tính.")
        };

        private static bool HasSownCrop(GameSession session)
        {
            foreach (FarmPlot plot in session.Farm.Plots)
            {
                if (plot.State == PlotState.Ready) return true;
                if (plot.State == PlotState.Growing && (plot.IsWatered || plot.GrowthDays > 0)) return true;
            }
            return false;
        }

        /// <summary>The opening chain: out of the hospital, then the first days at the base.</summary>
        public static readonly Storyline OpeningStory = new Storyline()
            .Add(new StoryStep("wake", "Tìm thứ gì đó để tự vệ. Lục cái tủ cạnh giường (E).",
                session => session.HasKnife)
                .Says("Bạn giắt con dao găm vào thắt lưng. Phím 1 để cầm, chuột trái để chém."))
            .Add(new StoryStep("escape", "Tìm đường ra khỏi bệnh viện.",
                session => session.Story.Has(StoryIds.ReachedCanteen) || !session.InIntro))
            .Add(new StoryStep("run", "CHẠY! Băng qua căn tin, ra cửa phía tây. Đừng đứng lại đánh cả đàn.",
                session => !session.InIntro))
            .Add(new StoryStep("meet", "Làm quen với Lan và bà Tư trong căn cứ.",
                session => session.Story.Has("met_" + NpcLan) && session.Story.Has("met_" + NpcBaTu))
                .Says("Mọi người đã biết mặt bạn."))
            .Add(new StoryStep("rest", "Lấy đồ Minh để trong kho, rồi lên giường ngủ một giấc.",
                session => session.Clock.TotalDays >= 1)
                .Says("Ngày đầu tiên ở hợp tác xã."))
            .Add(new StoryStep("farm", "Bà Tư nhờ gieo vụ đầu: cuốc một ô, gieo hạt (có sẵn trong kho) và tưới.",
                HasSownCrop)
                .Gives(0, A(MugwortSeed, 2))
                .Says("Bà Tư dúi cho bạn 2 hạt ngải cứu: \"Trồng lấy mà đắp vết thương.\""))
            .Add(new StoryStep("first_raid", "Ra cổng đi chuyến đầu tiên, và trở về an toàn.",
                session => session.Story.Has(StoryIds.Extracted))
                .Gives(60)
                .Says("Minh gật đầu: \"Về được là giỏi rồi.\" Nhận 60 tem phiếu."))
            .Add(new StoryStep("rescue_sau", "Bà Tư nhắc: Chú Sáu bán tạp hóa có thể còn kẹt trong nhà lồng chợ quê (vùng ven, phía tây nam). Mang theo băng gạc.",
                session => Residents.LivesAtBase(session, NpcChuSau))
                .Says("Chú Sáu đã về căn cứ. Nói chuyện với chú để mở lại quầy mua bán."))
            .Add(new StoryStep("rescue_quan", "Minh thấy đèn sáng ban đêm ở dãy lớp học của trường (vùng ven, phía bắc). Có người sống trong đó. Dọn sạch zombie quanh đó rồi đưa họ về.",
                session => Residents.LivesAtBase(session, NpcQuan))
                .Says("Quân đã về căn cứ. Giúp ông dựng phòng thí nghiệm để pha bùa."))
            .Add(new StoryStep("workbench", "Gom vật liệu nâng bàn chế tạo lên cấp 2.",
                session => session.Workshop.Level >= 2)
                .Gives(0, A(BlueprintMedKit, 1))
                .Says("Lan gửi bạn bản vẽ bộ sơ cứu (trong kho). Minh chỉ đường vào trung tâm thị trấn: ra cổng là chọn được."))
            .Add(new StoryStep("town", "Vào trung tâm thị trấn và hạ Đồ tể đang chiếm trụ sở. Mang shotgun, và đừng bắn chính diện bọn bọc thép.",
                session => session.Story.Has(FlagButcherDead))
                .Gives(150, A(BlueprintShotgun, 1))
                .Says("Đồ tể đã gục. Cả căn cứ ra cổng đón bạn. Nhận 150 tem phiếu và bản vẽ shotgun."));

        /// <summary>
        /// A brand new game: the session of <see cref="CreateNewSession"/>, but the player wakes
        /// in the hospital with nothing. The starter kit comes with <see cref="FinishIntro"/>.
        /// </summary>
        public static GameSession CreateNewGame(Gender playerGender, int seed)
        {
            GameSession session = CreateNewSession(playerGender, seed);
            session.Backpack.Clear();
            session.Loadout.Clear();
            return session;
        }

        /// <summary>Ends the opening: the player now lives at the base, has met Minh, and finds his kit in the stash.</summary>
        public static void FinishIntro(GameSession session)
        {
            if (!session.InIntro) return;

            session.Story.Set(StoryIds.IntroDone);
            session.Story.Set("met_" + NpcMinh);
            session.Stash.Add(Pistol, 1);
            session.Stash.Add(PistolAmmo, 16);
            session.Stash.Add(Bandage, 1);
            session.Stash.Add(CannedFood, 1);
            session.Stash.Add(WaterBottle, 1);
        }

        /// <summary>A session past the opening with a small starter kit. Tests and loading build on this; a new game uses <see cref="CreateNewGame"/>.</summary>
        public static GameSession CreateNewSession(Gender playerGender, int seed)
        {
            var session = new GameSession(playerGender, seed);

            foreach (var npc in CreateNpcs()) session.Relationships.Register(npc);
            foreach (var quest in CreateQuests()) session.Quests.Add(quest);

            SetUpWorkshop(session.Workshop);
            SetUpShop(session.Shop);
            SetUpKitchen(session.Kitchen);
            foreach (string founder in Founders) Residents.MoveIn(session, founder);
            SetUpBase(session.Base);
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
            report = SaveMapper.Apply(data, session, FindItem, FindCrop, FindBuff, FindCharm);
            if (data.version < 5)
            {
                // Saves from before the opening existed are already living at the base.
                session.Story.Set(StoryIds.IntroDone);
                OpeningStory.SkipAll(session);
            }
            if (data.version < 7)
            {
                // Before rescues existed, Chú Sáu's shop was simply open from the start.
                foreach (string founder in Founders) Residents.MoveIn(session, founder);
                Residents.MoveIn(session, NpcChuSau);
                session.Story.Set(FlagShopOpen);
            }
            ApplyServices(session);

            // Worn charms go back on once the number of slots is known.
            if (data.wornCharms != null)
            {
                for (int slot = 0; slot < data.wornCharms.Count; slot++)
                {
                    CharmDefinition worn = FindCharm(data.wornCharms[slot]);
                    if (worn != null) session.Charms.Wear(worn, slot);
                }
            }

            if (report.AbandonedRaid) GrantFallbackWeapon(session);
            return session;
        }
    }
}
