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
| `Bnhon.Core`: luật chơi bằng C# thuần | Đã biên dịch và chạy 53 unit test đạt |
| `Bnhon.Game`: lớp Unity (điều khiển, AI, tương tác, HUD) | Chạy được trong Unity 6000.3.28f1; code mới được kiểm tra kiểu với API giả lập trước khi đẩy lên |

## Thiết lập lần đầu

1. Cài Unity 6.3 LTS (repo đang dùng 6000.3.28f1) qua Unity Hub và cài Git LFS (`git lfs install`).
2. Clone repo này.
3. Unity Hub > Add > Add project from disk > chọn thư mục repo, rồi mở.

## Chạy prototype

Mở scene `Assets/_Project/Scenes/Prototype.unity` và nhấn Play.

`PrototypeBootstrap` dựng toàn bộ màn chơi thử bằng các ô vuông màu, không cần prefab hay asset:

- **Căn cứ:** 9 ô ruộng, 3 cư dân (Lan, Minh, bà Tư), kho, giường, cổng ra ngoài.
- **Khu raid:** dựng từ bản đồ file chữ: nhà, phòng khám, cửa hàng, nhà kho, với thùng đồ, zombie và hai vùng rút lui màu xanh lá.

| Phím | Hành động |
| --- | --- |
| WASD | Di chuyển |
| Shift | Chạy (tốn thể lực) |
| Space | Lăn né theo hướng đang đi, không bị thương khi đang lăn (tốn 25 thể lực) |
| Chuột | Ngắm, chuột trái tấn công |
| 1 / 2 / 3 | Dao (im lặng, tốn thể lực) / súng ngắn / shotgun (rất ồn) |
| R | Nạp đạn từ balo; xoay vật phẩm khi đang kéo trong balo |
| Q | Dùng nhanh một món hồi máu trong balo |
| E | Tương tác: lục đồ, mở kho, cuốc, gieo, tưới, thu hoạch, nói chuyện, ngủ, ra cổng |
| Tab | Mở hoặc đóng balo. Kéo chuột trái để xếp đồ, chuột phải để chuyển nhanh, F để dùng món đang rê chuột, Esc để đóng |

Đói và khát chỉ giảm khi đang raid; về 0 thì mất máu dần và thể lực hồi chậm. Ngủ hồi đầy nước và một phần no.

Một vòng chơi thử: nói chuyện với cư dân để nhận nhiệm vụ, cuốc đất và gieo khoai, ra cổng để raid, lục thùng, đứng trong vùng xanh 3 giây để rút lui, cất đồ, trả nhiệm vụ, ngủ sang ngày mới. Chết trong raid thì mất đồ trong balo. Khi nói chuyện, cư dân tự nhận món quà đầu tiên có trong balo.

## Sửa bản đồ raid

Khu raid được dựng từ file chữ `Assets/_Project/Resources/Maps/outskirts.txt`, mỗi ký tự là một ô rộng 1 đơn vị. Mở bằng Notepad, sửa, lưu, rồi Play.

| Ký tự | Ý nghĩa |
| --- | --- |
| `#` | Tường (chặn đường đi, đạn và tầm nhìn) |
| `.` | Sàn |
| `P` | Chỗ người chơi vào (đúng một cái) |
| `E` | Điểm rút lui, vùng 3×3 quanh ô đó (ít nhất một cái) |
| `C` `M` `S` `W` | Tủ nhà, tủ thuốc, kho hạt giống, hòm vũ khí |
| `Z` `R` `G` | Zombie thường, zombie chạy, zombie gào |

Dòng bắt đầu bằng `;` là ghi chú. Nếu file sai định dạng, game báo lỗi trên màn hình và dùng một bản đồ dự phòng nhỏ. Nếu có thùng đồ hay điểm rút lui bị tường bít kín, game cũng cảnh báo khi bắt đầu. Để dùng bản đồ khác, tạo file mới cùng thư mục và đổi ô `Map Name` trên `PrototypeBootstrap`.

## Thay ô vuông màu bằng hình thật

Thả file PNG vào `Assets/_Project/Resources/Sprites/` với đúng tên dưới đây. Món nào có file thì dùng hình, món nào chưa có vẫn là ô vuông màu.

| Tên file | Dùng cho | Cách đặt |
| --- | --- | --- |
| `floor.png`, `wall.png` | Sàn và tường khu raid | Kéo giãn vừa một ô (nên là ảnh vuông) |
| `extraction.png` | Vùng rút lui | Kéo giãn vừa 3×3 ô |
| `crate_house.png`, `crate_medical.png`, `crate_seed.png`, `crate_weapon.png` | Bốn loại thùng đồ | Co theo bề ngang, giữ tỉ lệ |
| `zombie_walker.png`, `zombie_runner.png`, `zombie_screamer.png`, `corpse.png` | Zombie và xác | Co theo bề ngang, giữ tỉ lệ |
| `player.png`, `npc_lan.png`, `npc_minh.png`, `npc_ba_tu.png` | Nhân vật | Co theo bề ngang, giữ tỉ lệ |
| `bed.png`, `stash.png`, `gate.png` | Đồ trong căn cứ | Co theo bề ngang, giữ tỉ lệ |

Ảnh trong thư mục này được Unity tự đặt thành sprite pixel art (lọc Point, không nén) nhờ `Assets/_Project/Scripts/Editor/SpriteImportSettings.cs`, nên không cần chỉnh Inspector. Ảnh được co giãn theo kích thước vật thể nên độ phân giải nào cũng chạy, nhưng nên giữ mật độ điểm ảnh đồng đều giữa các món. Ô ruộng và nền căn cứ chưa hỗ trợ thay hình.

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
