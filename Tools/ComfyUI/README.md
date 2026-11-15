# Workflow ComfyUI tạo sprite pixel art

`bnhon_pixel_sprite.json`: SDXL + LoRA Pixel Art XL, tạo 4 ảnh 1024×1024 mỗi lần chạy.

Cần có:
- Một checkpoint SDXL trong `models/checkpoints` (Juggernaut XL hoặc `sd_xl_base_1.0`)
- LoRA [Pixel Art XL](https://huggingface.co/nerijs/pixel-art-xl) trong `models/loras`

Cách dùng: kéo thả file JSON vào cửa sổ ComfyUI. Ô nào hiện đỏ nghĩa là tên file model trên máy bạn khác; bấm vào ô đó và chọn lại. Hướng dẫn chi tiết và danh sách prompt nằm trong ô ghi chú màu nâu của workflow.

Sau khi chọn ảnh: thu nhỏ 1024 → 128 bằng Nearest Neighbor, xóa nền, lưu PNG đúng tên file trong bảng ở README gốc, rồi chép vào `Assets/_Project/Resources/Sprites/`.

## Tạo hàng loạt mọi sprite còn thiếu

Mở ComfyUI, rồi trong thư mục repo chạy:

```
pip install pillow numpy
python Tools/generate_sprites.py
```

Script tự tìm ComfyUI (cổng 8188 hoặc 8000), tự chọn checkpoint có chữ "xl" và LoRA có chữ "pixel", rồi với mỗi sprite chưa có file: tạo 4 ứng viên, xử lý thành sprite, chọn cái dùng được nhất và lưu vào `Assets/_Project/Resources/Sprites/`. Mọi ứng viên nằm trong `Tools/ComfyUI/candidates/`, kèm bảng `candidates.png` để xem lại.

| Lệnh | Tác dụng |
| --- | --- |
| `--list` | Liệt kê sprite nào có, sprite nào thiếu |
| `--only wall floor` | Chỉ làm những sprite này |
| `--redo zombie_walker` | Tạo lại sprite đã có |
| `--pick zombie_walker=3` | Đổi sang ứng viên số 3 đã tạo (không cần ComfyUI) |
| `--count 2` | Ít ứng viên hơn, dùng khi card báo hết bộ nhớ |
| `--url http://127.0.0.1:8000` | Chỉ định địa chỉ ComfyUI |

Prompt của từng sprite nằm trong danh sách `ASSETS` ở đầu `generate_sprites.py`. Sàn và tường được ghép đối xứng từ một mảng giữa ảnh để lát liền không lộ mép.
