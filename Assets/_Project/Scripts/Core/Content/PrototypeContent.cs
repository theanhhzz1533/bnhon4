using System.Collections.Generic;
using Bnhon.Core.Crafting;
using Bnhon.Core.Farming;
using Bnhon.Core.Items;
using Bnhon.Core.Loot;
using Bnhon.Core.Npcs;
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
            RoastedSweetPotato, SpinachSoup, MugwortPorridge, CabbageStirFry
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

        // Places in the base a resident can stand. The scene maps each to a position.
        public const string SpotClinic = "clinic";
        public const string SpotWorkshop = "workshop";
        public const string SpotPorch = "porch";
        public const string SpotKitchen = "kitchen";
        public const string SpotWell = "well";
        public const string SpotGate = "gate";
        public const string SpotFarm = "farm";

        public static readonly IReadOnlyList<string> BaseSpots = new[] { SpotClinic, SpotWorkshop, SpotPorch, SpotKitchen, SpotWell, SpotGate, SpotFarm };

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
                .Says("Lan", "Hộp trà này của mẹ em. Em mang theo từ hôm chạy khỏi thị trấn, ba năm rồi chưa dám pha.")
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

        public static readonly IReadOnlyList<NpcLife> NpcLives = new[] { CreateLan(), CreateMinh(), CreateBaTu() };

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
            shop.AddEntry(ShotgunShell, 4, 32, 2);
            shop.AddEntry(Bandage, 1, 22, 3);
            shop.AddEntry(CannedFood, 1, 26, 3);
            shop.AddEntry(WaterBottle, 1, 18, 3);
            shop.AddEntry(SweetPotatoSeed, 3, 18, 3);
            shop.AddEntry(CornSeed, 3, 24, 2);
            shop.AddEntry(MugwortSeed, 2, 28, 2);
            shop.AddEntry(TomatoSeed, 3, 26, 2);
            shop.AddEntry(WaterSpinachSeed, 3, 16, 2);
            shop.AddEntry(CabbageSeed, 2, 20, 2);
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
            SetUpKitchen(session.Kitchen);
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
            report = SaveMapper.Apply(data, session, FindItem, FindCrop, FindBuff);
            if (report.AbandonedRaid) GrantFallbackWeapon(session);
            return session;
        }
    }
}
