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
| `Bnhon.Core`: luật chơi bằng C# thuần | Đã biên dịch và chạy 38 unit test đạt |
| `Bnhon.Game`: lớp Unity (điều khiển, AI, tương tác, HUD) | Chạy được trong Unity 6000.3.28f1; code mới được kiểm tra kiểu với API giả lập trước khi đẩy lên |

## Thiết lập lần đầu

1. Cài Unity 6.3 LTS (repo đang dùng 6000.3.28f1) qua Unity Hub và cài Git LFS (`git lfs install`).
2. Clone repo này.
3. Unity Hub > Add > Add project from disk > chọn thư mục repo, rồi mở.

## Chạy prototype

Mở scene `Assets/_Project/Scenes/Prototype.unity` và nhấn Play.

`PrototypeBootstrap` dựng toàn bộ màn chơi thử bằng các ô vuông màu, không cần prefab hay asset:

- **Căn cứ:** 9 ô ruộng, 3 cư dân (Lan, Minh, bà Tư), kho, giường, cổng ra ngoài.
- **Khu raid:** thùng đồ, zombie, tường chắn tầm nhìn, vùng rút lui màu xanh lá.

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
