# ProjectBnhon4.0

Tên mã của một game extraction top-down chơi đơn kết hợp farming sim, bối cảnh hậu tận thế zombie. Gameloop và góc nhìn theo Escape From Duckov (ra ngoài loot, chiến đấu, mang đồ về nâng cấp, trả nhiệm vụ); trồng trọt, quan hệ với NPC và cốt truyện theo Stardew Valley.

- Engine: Unity 6.3 LTS (6000.3.x), URP 2D, Input System
- Hình ảnh: 2D pixel art, góc nhìn từ trên xuống
- Thiết kế: [Game Design Document](https://claude.ai/artifact/VXEWyrbmhv4SPPz5QaMHfW) (riêng tư cho tới khi chủ dự án chia sẻ)
- Kế hoạch: [docs/ROADMAP.md](docs/ROADMAP.md)

## Trạng thái

Mốc M0 (khởi động). Repo hiện có:

| Phần | Trạng thái |
| --- | --- |
| `Bnhon.Core`: luật chơi bằng C# thuần | Đã biên dịch và chạy 141 unit test đạt |
| `Bnhon.Game`: lớp Unity (điều khiển, AI, tương tác, HUD) | Chạy được trong Unity 6000.3.28f1; code mới được kiểm tra kiểu với API giả lập trước khi đẩy lên |

## Thiết lập lần đầu

1. Cài Unity 6.3 LTS (repo đang dùng 6000.3.28f1) qua Unity Hub và cài Git LFS (`git lfs install`).
2. Clone repo này.
3. Unity Hub > Add > Add project from disk > chọn thư mục repo, rồi mở.

## Chạy prototype

Mở scene `Assets/_Project/Scenes/Prototype.unity` và nhấn Play.

`PrototypeBootstrap` dựng toàn bộ màn chơi thử bằng các ô vuông màu, không cần prefab hay asset:

- **Căn cứ:** 9 ô ruộng, 3 cư dân (Lan, Minh, bà Tư), kho, bàn chế tạo, quầy mua bán, giếng nước, bếp, giường, cổng ra ngoài.
- **Trồng trọt:** 6 loại cây theo mùa (mùa đông chỉ trồng được bắp cải). Bình tưới chứa 6 lần tưới, múc lại ở giếng. Cây không tưới quá 2 ngày liền sẽ chết, và chết khi sang mùa không hợp.
- **Độ khó tăng dần:** cứ hai ngày mức đe dọa tăng một bậc (tối đa 10); zombie thường trên bản đồ dần bị thay bằng zombie chạy và zombie bọc thép, đợt tăng viện đông hơn. Zombie bọc thép cầm tấm chắn chặn 75% sát thương từ phía trước; bắn đủ thì tấm chắn vỡ, hoặc vòng ra sau để khỏi phải phá.
- **Hai khu raid:** vùng ven (`outskirts.txt`, 260×170 ô: xóm nhà ống, trường học, trạm xăng, ruộng và chợ, đồi tháp radio có trùm Gã thợ điện) và trung tâm thị trấn (`town.txt`, mở sau khi nâng bàn chế tạo lên cấp 2), chọn ở cổng. Trung tâm có két sắt và trùm Đồ tể; hạ rồi thì nó không xuất hiện lại.
- **Mở đầu:** game mới bắt đầu trong bệnh viện (`Resources/Maps/hospital.txt`): lục tủ lấy dao găm, tìm đường tới căn tin, chạy khỏi đàn zombie tới chỗ Minh. Chết trong màn này thì làm lại từ giường bệnh, không mất gì. Sau đó dòng "Mục tiêu" trên HUD dẫn qua những ngày đầu ở căn cứ. File lưu cũ bỏ qua màn mở đầu; muốn xem thì xóa file lưu bằng menu **Bnhon > Save**.
- **Giải cứu và việc của cư dân:** Chú Sáu chờ trong nhà lồng chợ quê ở vùng ven (từ ngày thứ hai). Đưa chú băng gạc, chú đi theo bạn (zombie cũng nhắm vào chú); rút lui cùng nhau thì chú về căn cứ. Quầy mua bán chỉ mở sau khi làm việc đầu tiên của chú; 4 việc sau mở thêm hàng, giảm giá, bán đồ được giá hơn. Ký hiệu `Y` trên bản đồ là chỗ người chờ cứu.
- **Cư dân:** nhấn E để mở màn hình trò chuyện: câu thoại đổi theo số tim, buổi và chỗ đứng; chọn quà từ balo; nhận và giao nhiệm vụ. Cư dân đổi chỗ đứng giữa buổi sáng (trước raid) và buổi tối (sau raid). Lan có hai cảnh tim ở mốc 2 và 4 tim.
- **Nấu ăn:** bếp nấu 4 món từ nông sản. Mỗi món cho một hiệu ứng (hồi thể lực nhanh, chậm đói khát, hồi máu dần, chạy nhanh); thời gian chỉ chạy khi ở ngoài và hết khi đi ngủ.
- **Khu raid:** dựng từ bản đồ file chữ 104×64 ô: 21 công trình (nhà, phòng khám, trường học, cửa hàng, nhà kho, bãi xe), 45 thùng đồ, 65 zombie và ba vùng rút lui màu xanh lá.

| Phím | Hành động |
| --- | --- |
| WASD | Di chuyển |
| Shift | Chạy (tốn thể lực) |
| Space | Lăn né theo hướng đang đi, không bị thương khi đang lăn (tốn 25 thể lực) |
| Chuột | Ngắm, chuột trái tấn công |
| 1 / 2 / 3 | Dao (luôn có, im lặng, tốn thể lực) / hai ô súng đang trang bị |
| R | Nạp đạn từ balo; xoay vật phẩm khi đang kéo trong balo |
| Q | Dùng nhanh một món hồi máu trong balo |
| E | Tương tác: lục đồ, mở kho, cuốc, gieo, tưới, thu hoạch, nói chuyện, ngủ, ra cổng |
| F | Khi đứng ở ô đất đã cuốc: đổi loại hạt sẽ gieo |
| B | Ở căn cứ: bật hoặc tắt chế độ xây dựng. Bấm vào công trình để nhấc lên, bấm lần nữa để đặt ở chỗ viền xanh, chuột phải để trả về chỗ cũ |
| Tab | Mở hoặc đóng balo. Kéo chuột trái để xếp đồ, chuột phải để chuyển nhanh, F để dùng hoặc trang bị món đang rê chuột, Esc để đóng. Kéo súng vào ô vũ khí để trang bị, bấm vào súng đang trang bị để cất về balo |

Đói và khát chỉ giảm khi đang raid; về 0 thì mất máu dần và thể lực hồi chậm. Ngủ hồi đầy nước và một phần no.

Một vòng chơi thử: nói chuyện với cư dân để nhận nhiệm vụ, cuốc đất và gieo khoai, ra cổng để raid, lục thùng, đứng trong vùng xanh 3 giây để rút lui, cất đồ, trả nhiệm vụ, ngủ sang ngày mới. Chết trong raid thì mất đồ trong balo. Khi nói chuyện, cư dân tự nhận món quà đầu tiên có trong balo.

## Lưu game

Game tự lưu vào một file `save.json` tại ba thời điểm: khi bắt đầu raid, khi raid kết thúc (rút lui hoặc chết), và khi ngủ. Mở game lại là tiếp tục từ lần lưu gần nhất. Thoát game giữa chuyến raid được tính như chết ngoài đó: mất đồ trong balo và vũ khí đang mang.

Trong Unity, menu **Bnhon > Save** có hai lệnh: xóa file lưu để chơi lại từ đầu, và mở thư mục chứa file lưu. Nếu file lưu hỏng, game bắt đầu ván mới và giữ file cũ lại với đuôi `.bad`.

## Sửa bản đồ raid

Khu raid được dựng từ file chữ trong `Assets/_Project/Resources/Maps/`, mỗi ký tự là một ô rộng 1 đơn vị.

Bản đồ vùng ven (`outskirts.txt`) được sinh bằng `python Tools/maps/build_outskirts.py`, script này cũng xuất ảnh xem trước `Tools/maps/outskirts_preview.png`. Sửa tay trong file thì đừng chạy lại script, vì nó ghi đè. Bản đồ vùng ven cũ vẫn còn ở `outskirts_old.txt`.

Cách tiện nhất là dùng trình sửa bản đồ trong Unity: menu **Bnhon > Raid Map Editor**. Nó vẽ bản đồ thành hình có màu, cho chọn ký hiệu rồi kéo chuột trái để vẽ (chuột phải để lấy ký hiệu tại ô đang trỏ), báo ngay nếu bản đồ sai hoặc có chỗ bị bít kín, và bấm Lưu để ghi lại file. Cũng có thể mở file bằng Notepad, sửa, lưu, rồi Play.

| Ký tự | Ý nghĩa |
| --- | --- |
| `#` | Tường (chặn đường đi, đạn và tầm nhìn) |
| `.` `,` `=` `:` `~` | Nền đi được: sàn bê tông, cỏ, đường nhựa, đất, ruộng nước (chỉ khác màu) |
| `P` | Chỗ người chơi vào (đúng một cái) |
| `E` | Điểm rút lui, vùng 3×3 quanh ô đó (ít nhất một cái) |
| `C` `M` `S` `W` | Tủ nhà, tủ thuốc, kho hạt giống, hòm vũ khí |
| `V` | Két sắt |
| `Z` `R` `G` `A` `B` | Zombie thường, chạy, gào, bọc thép; trùm của khu |
| `X` | Cầu dao máy phát (bật lên thì máy kêu, kéo zombie tới) |
| `J` | Lối rút lui chỉ mở khi đã bật cầu dao |
| `U` | Lối rút lui chỉ mở khi đã hạ trùm của khu |

Dòng bắt đầu bằng `;` là ghi chú. Nếu file sai định dạng, game báo lỗi trên màn hình và dùng một bản đồ dự phòng nhỏ. Nếu có thùng đồ hay điểm rút lui bị tường bít kín, game cũng cảnh báo khi bắt đầu. Để dùng bản đồ khác, tạo file mới cùng thư mục và đổi ô `Map Name` trên `PrototypeBootstrap`.

## Thay ô vuông màu bằng hình thật

Thả file PNG vào `Assets/_Project/Resources/Sprites/` với đúng tên dưới đây. Món nào có file thì dùng hình, món nào chưa có vẫn là ô vuông màu.

| Tên file | Dùng cho | Cách đặt |
| --- | --- | --- |
| `wall.png` | Tường khu raid | Kéo giãn vừa một ô (nên là ảnh vuông). Nền khu raid giờ được vẽ theo loại đất của bản đồ nên `floor.png` không còn dùng |
| `extraction.png` | Vùng rút lui | Kéo giãn vừa 3×3 ô |
| `decor_*.png` (vết nứt, máu, vết cháy, vũng nước, lá, rác, kính vỡ, cỏ dại, dây leo) | Chi tiết nằm trên nền khu raid, game tự rải | Chỉ hiện khi có hình |
| `prop_*.png` (xe cộ, bao cát, rào, thùng phuy, rác, cây, bụi...) | Đồ bỏ lại và cây cối, game tự rải; xe, bao cát, thùng phuy chắn đường và đạn | Không có hình thì là khối màu |
| `power_switch.png` | Cầu dao máy phát | Co theo bề ngang, giữ tỉ lệ |
| `crate_house.png`, `crate_medical.png`, `crate_seed.png`, `crate_weapon.png` | Bốn loại thùng đồ | Co theo bề ngang, giữ tỉ lệ |
| `zombie_walker.png`, `zombie_runner.png`, `zombie_screamer.png`, `corpse.png` | Zombie và xác | Co theo bề ngang, giữ tỉ lệ |
| `player.png`, `npc_lan.png`, `npc_minh.png`, `npc_ba_tu.png` | Nhân vật | Co theo bề ngang, giữ tỉ lệ |
| `bed.png`, `stash.png`, `gate.png`, `workbench.png`, `shop.png`, `well.png`, `kitchen.png` | Đồ trong căn cứ | Co vừa khung, giữ tỉ lệ |
| `base_floor.png`, `plot_dry.png`, `plot_wet.png` | Nền căn cứ, ô ruộng khô và ướt | Kéo giãn vừa một ô |
| `crop_sprout.png`, `crop_<tên cây>.png` (ví dụ `crop_corn.png`) | Cây mới nhú và cây đã lớn trên ô ruộng | Co theo độ lớn của cây |
| `item_<id>.png` (ví dụ `item_pistol.png`, `item_bandage.png`) | Icon vật phẩm trong balo, kho, hòm | Co vừa các ô của vật phẩm; chưa có hình thì hiện tên |

Ảnh trong thư mục này được Unity tự đặt thành sprite pixel art (lọc Point, không nén) nhờ `Assets/_Project/Scripts/Editor/SpriteImportSettings.cs`, nên không cần chỉnh Inspector. Ảnh được co giãn theo kích thước vật thể nên độ phân giải nào cũng chạy, nhưng nên giữ mật độ điểm ảnh đồng đều giữa các món.

Ảnh tạo bằng workflow ComfyUI trong `Tools/ComfyUI/` có thể xử lý tự động bằng `Tools/sprite_prep.py` (cần Python, `pip install pillow numpy`):

```
python Tools/sprite_prep.py bnhon__00002_.png crate_house
python Tools/sprite_prep.py san.png floor --tile
```

Script xóa nền sáng, thu về lưới pixel thật (mặc định mỗi điểm ảnh 8 px), cắt sát vật thể, lưu thẳng vào `Resources/Sprites/<tên>.png` và tạo một ảnh xem trước phóng to cạnh file gốc. Dùng `--tile` cho sàn và tường (giữ nền, không cắt), `--keep-shadow` nếu muốn giữ bóng dưới chân.

## Thay âm thanh

Mọi hiệu ứng âm thanh hiện được sinh bằng code (`SoundSynth`). Để thay bằng file thật, thả file `.wav` hoặc `.ogg` vào `Assets/_Project/Resources/Audio/` với đúng tên; hiệu ứng nào có file thì dùng file.

Tên hợp lệ: `pistol_shot`, `shotgun_shot`, `knife_swing`, `reload`, `empty_click`, `footstep`, `roll`, `player_hurt`, `player_death`, `zombie_growl`, `zombie_scream`, `zombie_hit`, `zombie_death`, `loot_open`, `item_reveal`, `use_item`, `extract`.

Âm lượng chung chỉnh ở ô `Master Volume` của object `Sfx` khi đang Play.

## Cấu trúc

```
Assets/_Project/
  Scripts/
    Core/     Bnhon.Core   - luật chơi, không phụ thuộc UnityEngine
      Items, Inventory (lưới kiểu Tarkov), Loot, Calendar, Farming,
      Relationships, Quests, Economy, Raid, Content (dữ liệu mẫu), GameSession
    Game/     Bnhon.Game   - MonoBehaviour nối Core với scene
      Player, Combat, AI, Interaction, Raid, UI, PrototypeBootstrap
  Tests/EditMode/          - unit test NUnit cho Core
  Art/ Audio/ Data/ Prefabs/ Scenes/   - chỗ cho asset, hiện trống
Tools/CoreTests/           - chạy test Core bằng dotnet, không cần Unity
docs/                      - roadmap
```

Quy tắc: luật chơi nằm trong `Core` và phải có test; `Game` chỉ đọc input, hiển thị và gọi vào `Core`. `Core` không được tham chiếu `UnityEngine` (asmdef đã bật `noEngineReferences`).

## Chạy test

- Trong Unity: Window > General > Test Runner > EditMode > Run All.
- Không cần Unity: `dotnet test Tools/CoreTests` (cần .NET SDK 8 và truy cập nuget.org). GitHub Actions chạy lệnh này ở mỗi lần push; workflow này chưa được chạy thử lần nào.

## Quy ước git

- Nhánh chính `main`; làm việc trên nhánh `feature/<tên>` và gộp qua pull request.
- Ảnh, âm thanh, font đi qua Git LFS (đã khai báo trong `.gitattributes`).
- Luôn commit file `.meta` cùng asset của nó.
- Để gộp scene và prefab, cấu hình UnityYAMLMerge một lần trên máy:
  `git config merge.unityyamlmerge.driver "'<đường dẫn Unity>/Editor/Data/Tools/UnityYAMLMerge' merge -p %O %B %A %A"`
