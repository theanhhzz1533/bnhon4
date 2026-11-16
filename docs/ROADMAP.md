# Roadmap ProjectBnhon4.0

Năm mốc, mỗi mốc có một điều kiện xong. Chưa gắn ngày vì chưa chốt quy mô đội. Mốc quan trọng nhất là M1: nếu một chuyến raid graybox chưa vui thì chưa làm tiếp farming hay NPC.

## M0 - Khởi động

Xong khi: mở được project trong Unity và test Core chạy xanh.

- [x] Game Design Document bản đầu
- [x] Repo git: `.gitignore`, `.gitattributes` (LFS), cấu trúc thư mục
- [x] `Bnhon.Core`: inventory lưới, bảng loot, lịch, ruộng, quan hệ, nhiệm vụ, phiên raid, kèm unit test
- [x] `Bnhon.Game`: điều khiển, súng, zombie, thùng đồ, vùng rút lui, ruộng, NPC, HUD debug, `PrototypeBootstrap`
- [x] Đẩy repo lên GitHub
- [x] Sinh `Packages/` và `ProjectSettings/` bằng Unity 6.3 LTS, commit cùng file `.meta`
- [x] Mở project, sửa lỗi biên dịch nếu có, chạy được scene prototype
- [ ] Chạy test EditMode trong Unity và workflow `core-tests` trên GitHub
- [ ] Chốt các câu hỏi mở trong GDD mục 12

## M1 - Prototype lõi (xong)

Xong khi: chơi trọn một chuyến raid 10 phút và thấy căng thẳng.

- [x] Một bản đồ graybox có bố cục, đọc từ file chữ `Resources/Maps/outskirts.txt`
- [x] Hình ảnh thay được từng món bằng cách thả file PNG vào `Resources/Sprites`
- [x] Fog of war theo hình nón tầm nhìn (bản đầu: lưới tối + ẩn zombie ngoài tầm nhìn)
- [x] Lục thùng đồ có thời gian, vật phẩm hiện dần
- [x] UI inventory dạng lưới: kéo thả, xoay vật phẩm, chuyển giữa balo, thùng và kho
- [x] Túi an toàn có UI riêng
- [x] Vũ khí: cận chiến và shotgun, đổi vũ khí bằng phím 1-3, độ tản đạn
- [x] Độ giật khi bắn: đạn tản rộng dần, tâm ngắm mở ra, camera giật
- [x] Chạy và lăn gây tiếng động thu hút zombie ở gần; đi bộ thì im lặng
- [x] Vũ khí là vật phẩm: hai ô trang bị, loot được trong hòm vũ khí, mất khi chết
- [x] Lăn né và thể lực
- [x] Zombie: thêm loại chạy nhanh và loại gào gọi bầy
- [x] Zombie tìm đường quanh tường (A* trên lưới), đi tới vị trí cuối cùng nhìn thấy người chơi
- [x] Đói và khát trong raid; ăn uống và băng bó từ balo
- [x] Xác zombie lục được, đồ rơi theo loại zombie
- [x] Âm thanh tạm: 17 hiệu ứng sinh bằng code, thay được bằng file trong `Resources/Audio`
- [x] Playtest lần 1: chuyến đi dưới 5 phút, dọn sạch được bản đồ, hồi máu quá dễ, zombie kẹt ở cửa
- [x] Sau playtest 1: sửa tìm đường qua cửa, zombie tăng viện theo thời gian, dùng vật phẩm mất thời gian, giảm đồ hồi máu, lục đồ chậm hơn
- [x] Playtest lần 2 trên bản đồ lớn: một chuyến khoảng 10 phút, có căng thẳng

## M2 - Vertical slice (đang làm)

Xong khi: vòng lặp một ngày trọn vẹn, người ngoài chơi thử 30 phút.

- [ ] Tách căn cứ và bản đồ raid thành hai scene, có màn chuyển cảnh
- [x] Lưu và tải game bằng JSON: tự lưu khi bắt đầu raid, kết thúc raid và khi ngủ; thoát giữa raid tính là chết
- [ ] Chuyển dữ liệu mẫu trong `PrototypeContent` sang ScriptableObject
- [ ] Đặt và dời công trình trên lưới: bàn chế tạo, kho, bếp
- [x] Chế tạo kiểu Escape From Duckov: bàn chế tạo 3 cấp, bản vẽ phải mang về đăng ký, tháo dỡ vật phẩm
- [x] Farming: bình tưới có dung tích và giếng nước, 6 loại cây theo mùa, chọn hạt để gieo, bếp nấu 4 món có buff
- [ ] Farming, phần còn lại: cuốc và bình tưới nâng cấp được, hình cây lớn dần trên ô ruộng
- [x] Một NPC hoàn chỉnh (Lan): lịch sinh hoạt theo buổi và ngày trong tuần, hội thoại theo số tim, tặng quà có UI, 2 cảnh tim có lựa chọn
- [x] Chuỗi cốt truyện mở đầu: tỉnh dậy trong bệnh viện, được Minh cứu về căn cứ, rồi 5 mục tiêu cho những ngày đầu
- [x] Quầy mua bán: hàng giới hạn về lại mỗi ngày, bán đồ được nửa giá trị
- [ ] Pixel art thật cho căn cứ và một khu raid; Pixel Perfect Camera; ánh sáng 2D
- [x] HUD dạng khối thay cho HUD debug: thanh máu và thể lực, vũ khí, mục tiêu, nhiệm vụ; giao diện co theo cửa sổ; hộp thoại cốt truyện
- [ ] Chuyển HUD và menu từ IMGUI sang UI Toolkit

## M3 - Alpha

Xong khi: chơi từ đầu đến hết hồi 1 không cần công cụ debug.

- [ ] 3 bản đồ raid, mỗi bản đồ một trùm
- [ ] 6 NPC, trong đó 4 hẹn hò được; hẹn hò và kết hôn
- [ ] 4 mùa, ảnh hưởng của mùa lên cây trồng và raid
- [ ] Cây kỹ năng 3 nhánh
- [ ] Kẻ cướp có súng, biết nấp
- [ ] Độ bền và phụ kiện vũ khí
- [ ] Ngày và đêm trong raid
- [ ] Cân bằng kinh tế vòng đầu
- [ ] Bản địa hóa: tách toàn bộ chuỗi ký tự ra bảng

## M4 - Early Access

Xong khi: bản build ổn định, đủ để phát hành Early Access.

- [ ] Nội dung hồi 1 và hồi 2
- [ ] Hướng dẫn chơi đầu game
- [ ] Tùy chọn: phím, âm thanh, đồ họa, độ khó
- [ ] Tối ưu hiệu năng và sửa lỗi
- [ ] Trang Steam, trailer, bản demo
- [ ] Tiếng Việt và tiếng Anh

## Ngoài phạm vi bản đầu

PvP, co-op, bản đồ sinh ngẫu nhiên, chăn nuôi, console và mobile.
