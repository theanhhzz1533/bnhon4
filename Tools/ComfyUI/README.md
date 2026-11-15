# Workflow ComfyUI tạo sprite pixel art

`bnhon_pixel_sprite.json`: SDXL + LoRA Pixel Art XL, tạo 4 ảnh 1024×1024 mỗi lần chạy.

Cần có:
- Một checkpoint SDXL trong `models/checkpoints` (Juggernaut XL hoặc `sd_xl_base_1.0`)
- LoRA [Pixel Art XL](https://huggingface.co/nerijs/pixel-art-xl) trong `models/loras`

Cách dùng: kéo thả file JSON vào cửa sổ ComfyUI. Ô nào hiện đỏ nghĩa là tên file model trên máy bạn khác; bấm vào ô đó và chọn lại. Hướng dẫn chi tiết và danh sách prompt nằm trong ô ghi chú màu nâu của workflow.

Sau khi chọn ảnh: thu nhỏ 1024 → 128 bằng Nearest Neighbor, xóa nền, lưu PNG đúng tên file trong bảng ở README gốc, rồi chép vào `Assets/_Project/Resources/Sprites/`.
