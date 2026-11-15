"""Turn a 1024x1024 image from the ComfyUI pixel-art workflow into a game sprite.

    python Tools/sprite_prep.py <input.png> <sprite_key> [--tile] [--block 8] [--keep-shadow]

Steps:
  1. Remove the plain light background: every near-white or light-grey pixel
     connected to the image border becomes transparent (skipped with --tile).
     With --tile the result is instead a seamless square tile, made by
     mirroring a patch from the middle of the image.
  2. Find how the image's big "pixels" line up (block size, default 8, because
     Pixel Art XL draws on roughly a 128x128 grid at 1024x1024) and sample one
     colour per block, which gives a clean pixel-art image at true resolution.
  3. Crop to the visible content (skipped with --tile).
  4. Save to Assets/_Project/Resources/Sprites/<sprite_key>.png, plus a x8
     preview next to the input for checking by eye.

Needs Python 3 with Pillow and numpy:  pip install pillow numpy
"""

import argparse
import os
import sys
from collections import deque

import numpy as np
from PIL import Image

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(REPO, "Assets", "_Project", "Resources", "Sprites")


def flood_from_border(mask):
    """Cells of `mask` connected to the image border."""
    height, width = mask.shape
    seen = np.zeros_like(mask)
    queue = deque()
    for x in range(width):
        for y in (0, height - 1):
            if mask[y, x] and not seen[y, x]:
                seen[y, x] = True
                queue.append((y, x))
    for y in range(height):
        for x in (0, width - 1):
            if mask[y, x] and not seen[y, x]:
                seen[y, x] = True
                queue.append((y, x))

    while queue:
        y, x = queue.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < height and 0 <= nx < width and mask[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                queue.append((ny, nx))
    return seen


def remove_background(rgba, keep_shadow):
    """
    Makes the plain background transparent. The background colour is read from the image
    border, so it works for white as well as coloured backdrops (a coloured backdrop is how
    white objects such as bandages are generated without being erased).
    """
    rgb = rgba[:, :, :3].astype(int)
    border = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    background_colour = np.median(border, axis=0)
    brightness = rgb.mean(axis=2)
    saturation = rgb.max(axis=2) - rgb.min(axis=2)

    if background_colour.mean() > 200 and background_colour.max() - background_colour.min() < 40:
        # White backdrop: also clear the soft grey shadow models like to draw under objects.
        near_white = (brightness > 225) & (saturation < 30)
        light_grey = (brightness > 140) & (saturation < 22)
        background = near_white if keep_shadow else (near_white | light_grey)
    else:
        distance = np.abs(rgb - background_colour).sum(axis=2)
        background = distance < 90

    out = rgba.copy()
    out[flood_from_border(background), 3] = 0
    return out


def best_offset(rgb, block):
    """The grid offset (0..block-1) whose block borders line up best with colour changes."""
    scores = []
    diff_x = np.abs(np.diff(rgb, axis=1)).sum(axis=2).sum(axis=0)
    diff_y = np.abs(np.diff(rgb, axis=0)).sum(axis=2).sum(axis=1)
    for offset in range(block):
        edges_x = diff_x[offset + block - 1::block].sum()
        edges_y = diff_y[offset + block - 1::block].sum()
        scores.append(edges_x + edges_y)
    return int(np.argmax(scores))


def downsample(rgba, block):
    """One colour per block: the most common opaque colour among the block's inner pixels."""
    rgb = rgba[:, :, :3].astype(int)
    offset = best_offset(rgb, block)
    height, width = rgba.shape[:2]
    rows = (height - offset) // block
    cols = (width - offset) // block
    out = np.zeros((rows, cols, 4), dtype=np.uint8)
    margin = max(1, block // 4)

    for r in range(rows):
        for c in range(cols):
            y0 = offset + r * block
            x0 = offset + c * block
            cell = rgba[y0 + margin:y0 + block - margin, x0 + margin:x0 + block - margin].reshape(-1, 4)
            opaque = cell[cell[:, 3] > 0]
            if len(opaque) * 2 < len(cell):
                continue
            # Quantise lightly so near-identical shades vote together.
            keys = (opaque[:, :3] // 8).astype(np.int32)
            packed = keys[:, 0] * 1024 + keys[:, 1] * 32 + keys[:, 2]
            values, counts = np.unique(packed, return_counts=True)
            winner = values[np.argmax(counts)]
            pick = opaque[packed == winner][:, :3].mean(axis=0)
            out[r, c, :3] = pick.round().astype(np.uint8)
            out[r, c, 3] = 255
    return out


def crop_to_content(rgba, padding=1):
    alpha = rgba[:, :, 3]
    ys, xs = np.nonzero(alpha)
    if len(ys) == 0:
        return rgba
    y0 = max(0, ys.min() - padding)
    y1 = min(rgba.shape[0], ys.max() + 1 + padding)
    x0 = max(0, xs.min() - padding)
    x1 = min(rgba.shape[1], xs.max() + 1 + padding)
    return rgba[y0:y1, x0:x1]


def make_tile(small, cells):
    """
    A square tile that repeats without visible seams: take a clean patch from the middle
    of the image and mirror it left-right and top-bottom, so every edge meets its own copy.
    """
    half = cells // 2
    height, width = small.shape[:2]
    y0 = max(0, (height - half) // 2)
    x0 = max(0, (width - half) // 2)
    patch = small[y0:y0 + half, x0:x0 + half].copy()
    patch[:, :, 3] = 255
    top = np.concatenate([patch, patch[:, ::-1]], axis=1)
    return np.concatenate([top, top[::-1]], axis=0)


def process(rgba, tile=False, block=8, keep_shadow=False, tile_cells=64, orient=None):
    """
    Full pipeline on an RGBA array; returns the finished sprite as an RGBA array.
    `orient` is "wide" or "tall" for inventory icons whose slot is not square: if the model
    drew the item the other way round, it is turned 90 degrees to match.
    """
    rgba = rgba.copy()
    if tile:
        return make_tile(downsample(rgba, block), tile_cells)

    rgba = remove_background(rgba, keep_shadow)
    sprite = crop_to_content(downsample(rgba, block))

    height, width = sprite.shape[:2]
    if orient == "wide" and height > width * 1.15:
        sprite = np.rot90(sprite, k=-1).copy()
    elif orient == "tall" and width > height * 1.15:
        sprite = np.rot90(sprite, k=1).copy()
    return sprite


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("input")
    parser.add_argument("key", help="sprite name, e.g. crate_house")
    parser.add_argument("--block", type=int, default=8, help="size of one art pixel in the input (default 8)")
    parser.add_argument("--tile", action="store_true", help="floor or wall: output a seamless square tile")
    parser.add_argument("--tile-cells", type=int, default=64, help="tile size in art pixels (default 64)")
    parser.add_argument("--keep-shadow", action="store_true", help="keep light-grey ground shadows")
    parser.add_argument("--out-dir", default=SPRITES)
    args = parser.parse_args()

    rgba = np.asarray(Image.open(args.input).convert("RGBA"))
    small = process(rgba, args.tile, args.block, args.keep_shadow, args.tile_cells)

    os.makedirs(args.out_dir, exist_ok=True)
    out_path = os.path.join(args.out_dir, args.key + ".png")
    Image.fromarray(small, "RGBA").save(out_path)

    preview = Image.fromarray(small, "RGBA").resize((small.shape[1] * 8, small.shape[0] * 8), Image.NEAREST)
    preview_path = os.path.splitext(args.input)[0] + "_preview.png"
    preview.save(preview_path)

    print("sprite  %s  (%dx%d)" % (out_path, small.shape[1], small.shape[0]))
    print("preview %s" % preview_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
