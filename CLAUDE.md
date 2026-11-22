# CLAUDE.md

Ghi chú cho Claude khi làm việc trên repo này. Đọc file này trước, rồi `README.md` (tính năng đang có) và `docs/ROADMAP.md` (việc đã xong, việc còn lại).

## Cách làm việc với chủ dự án

- Luôn trả lời bằng **tiếng Việt**. Code, tên biến, chú thích trong code và commit message viết bằng tiếng Anh. Chữ hiện trong game là tiếng Việt.
- Chủ dự án chơi thử trong Unity rồi phản hồi (thường kèm ảnh chụp). Claude sửa, kiểm tra, commit, push lên `main`, rồi báo lại.
- Luôn nói rõ: code lớp Game **mới được kiểm tra kiểu với API giả lập, chưa chạy trong Unity**. Không nói "đã chạy" nếu chưa chạy.
- Khi báo lại: những gì đã đổi, cách thử, chỗ cần chủ dự án để ý khi chơi. Không kể lại từng bước đã làm.
- Hình ảnh hiện tại là ảnh AI làm tạm. Animation để sau khi có hình thật.

## Dự án

Game Unity 6.3 LTS, URP, Input System, 2D pixel art góc nhìn từ trên xuống. Thể loại: extraction (vòng lặp như Escape From Duckov: ra ngoài raid, loot, chết là mất đồ mang theo) kết hợp farming và quan hệ NPC (như Stardew Valley). Bối cảnh: thị trấn Việt Nam sau đại dịch zombie.

- Repo: `theanhhzz1533/bnhon4`, nhánh `main`.
- GDD NPC (Google Sheet "Project Bnhon 4 GDD", sheet `NPC`): https://docs.google.com/spreadsheets/d/1A7c3MI3-KGDRknEtXfi7s4vh7tpbW7Nut_1H90ATYXA/edit
  Các cột: Tên, Mô tả, Hình ảnh, Hẹn hò, Công dụng, Mở khóa, Chuỗi nhiệm vụ, Cảnh tim, Prompt.

## Cấu trúc code

- `Assets/_Project/Scripts/Core` (assembly `Bnhon.Core`): luật chơi bằng C# thuần, không dùng UnityEngine. Mọi luật mới nên nằm ở đây và có test.
- `Assets/_Project/Scripts/Game` (assembly `Bnhon.Game`): MonoBehaviour, AI, va chạm, giao diện IMGUI.
- `Assets/_Project/Tests/EditMode`: test NUnit cho Core.
- `Assets/_Project/Scripts/Core/Content/PrototypeContent.cs`: toàn bộ dữ liệu mẫu (vật phẩm, NPC, giải cứu, chuỗi nhiệm vụ, bùa, cửa hàng, cốt truyện mở đầu).
- `Assets/_Project/Scripts/Game/PrototypeBootstrap.cs`: dựng toàn bộ màn chơi bằng code lúc chạy (căn cứ, khu raid, NPC, camera). Không có prefab.
- `Assets/_Project/Resources/Maps/*.txt`: bản đồ raid dạng ký tự. `Assets/_Project/Resources/Sprites/*.png`: hình.
- `Tools/generate_sprites.py`: danh sách hình cần tạo và prompt (chạy `python Tools/generate_sprites.py --pick key=n`).
- `Tools/maps/build_outskirts.py`: sinh bản đồ vùng ven 260x170 và ảnh xem trước.

## Kiểm tra trước khi commit

Chạy `bash Tools/checks/run.sh` (cần .NET 8 SDK). Gồm:

- **core**: biên dịch Core cùng test EditMode, chạy test (bộ NUnit thu gọn trong `Tools/checks/core/Shim.cs`), rồi kiểm tra mọi điểm đánh dấu trên mọi bản đồ đều đi tới được.
- **game**: kiểm tra kiểu lớp Game với Unity giả lập (`Tools/checks/game/UnityStubs.cs`). Chỉ biên dịch, không chạy.
- **editor**: kiểm tra kiểu script Editor.

Kết quả mong đợi: test đều đạt, `unreachable 0` cho mọi bản đồ, `0 Error(s)` cho game và editor.

Khi game hoặc editor báo lỗi chỉ vì thiếu một API **có thật** của Unity trong stub, thêm API đó vào stub. Không sửa code game cho vừa stub.

Giới hạn của bộ test thu gọn: chỉ có `Assert.That(x, Is.EqualTo(...) / Is.True / Is.False)`. Không có `Assert.Throws` (dùng try/catch) và không có `.Within`. Code dùng C# 9.

## Quy ước

- **File .meta**: mỗi file `.cs` mới cần `.meta` đi kèm. Sao chép `Assets/_Project/Scripts/Game/UI/GameHud.cs.meta` và thay `guid` bằng một uuid4 hex mới. Thư mục mới: sao chép meta của một thư mục có sẵn (ví dụ `Scripts/Core/Crafting.meta`), cũng thay guid.
- **Git**: `git pull --rebase origin main` trước khi push. Hình trong `Assets/_Project/Resources/Sprites/*.png` và `Tools/maps/*.png` không đi qua LFS (đã khai trong `.gitattributes`).
- **File lưu**: `SaveData.CurrentVersion` (hiện là 9). Khi đổi dữ liệu lưu, tăng phiên bản và thêm chuyển đổi trong `PrototypeContent.LoadSession` để file cũ vẫn tải được.
- **Cờ cốt truyện** (`StoryFlags`): cư dân đã về căn cứ là `resident_<id>`; các bước chuỗi nhiệm vụ và dịch vụ đã mở cũng lưu bằng cờ.
- **Ký hiệu bản đồ**: `#` tường, `. , = : ~` các loại nền, `P` điểm vào, `E` lối ra, `C M S W V` hòm đồ, `Z R G A B` các loại zombie, `K T H N` dùng cho bản đồ cốt truyện (tủ đồ cố định, điểm kích hoạt bầy zombie, chỗ bầy xuất hiện, người đến cứu), `Y` người cần cứu (đánh số từ trên xuống, trái sang phải), `X` cầu dao, `J` lối ra cần điện, `U` lối ra sau khi hạ trùm, `I` điểm Vy thả xuống (3 điểm mỗi bản đồ). Xem `RaidMap.cs` để biết chính xác.
- Khu raid đặt tại `RaidCenter` (400,0). Căn cứ là lưới 20x14 (`BaseLayout`), phím B để xây.

## Thiết kế đã chốt (đừng đổi khi chưa hỏi)

- **Chuỗi nhiệm vụ khác cảnh tim.** Chuỗi nhiệm vụ mở tính năng của NPC và cho phần thưởng (cửa hàng, dịch vụ…). Cảnh tim (mốc 2/4/6/8/10 tim) chỉ dành cho NPC hẹn hò được, dẫn tới hẹn hò và cưới.
- **Quân** là nhà khoa học. Bùa giống Duckov: 8 mẫu vật quay ra 1 bùa ngẫu nhiên, đeo 2 bùa (mở rộng lên 3), không mất khi chết trong raid.
- **Vy** là thợ máy, đồ điện và radio, chạy xe phân khối lớn: thả người chơi ở 1 trong 3 điểm xuất phát của bản đồ (ngẫu nhiên, sau đó chọn giữa 2, rồi chọn bất kỳ). Cửa hàng: radio mồi, can xăng, pháo hiệu.
- Cư dân đã có: Lan (y tế), Minh (chế tạo), Bà Tư (trồng trọt), Chú Sáu (cửa hàng), Quân (bùa), Vy (chở đi raid). Chưa làm: Hùng (vũ khí), Khang (nấu ăn), Tuấn (tự thu hoạch), Thảo (kỹ năng), Ngọc (chợ đen).
- **Không giảm đạn.** Khiên của zombie bọc thép và trùm phá được bằng đạn.
- **Hình ảnh:** 2D, góc nhìn 3/4 (tường lộ cả mặt trên và mặt trước), ánh sáng ấm, zoom gần. Không chuyển sang 3D.

## Việc tiếp theo

Xem `docs/ROADMAP.md`. Các việc đang chờ gần nhất:

- Chủ dự án chơi thử bản đổi hình gần nhất (chỉnh màu bằng URP, bóng mềm, mặt trước của tường, zoom gần) và báo lại.
- Sửa bản đồ: đồ trang trí không được nằm trên ô tường; nền cần đường đi, viền chuyển tiếp giữa các loại nền, cỏ dày hơn.
- Hùng, Khang, Tuấn, Thảo theo sheet NPC.
- Hẹn hò và cảnh tim.
- Xây lại thị trấn (đồn công an, quán ăn, thư viện); kết thúc hồi 1 ở tháp radio.
