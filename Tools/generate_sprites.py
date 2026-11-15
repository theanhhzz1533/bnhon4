"""Generate every sprite the game is still missing, using a running ComfyUI.

    python Tools/generate_sprites.py                 generate all missing sprites
    python Tools/generate_sprites.py --list          show which sprites exist and which are missing
    python Tools/generate_sprites.py --only wall floor
    python Tools/generate_sprites.py --redo zombie_walker      regenerate one that already exists
    python Tools/generate_sprites.py --pick zombie_walker=3    use candidate 3 instead of the auto pick

For each missing sprite the script queues the SDXL + Pixel Art XL workflow in
ComfyUI, downloads the candidates, turns each into a clean sprite with
sprite_prep, picks the most usable one, and writes it to
Assets/_Project/Resources/Sprites/<key>.png. All candidates are kept in
Tools/ComfyUI/candidates/, with a contact sheet (candidates.png) to review.

ComfyUI must be open. Needs Python 3 with Pillow and numpy:
    pip install pillow numpy
"""

import argparse
import io
import json
import os
import random
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

import numpy as np
from PIL import Image, ImageDraw

import sprite_prep

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(REPO, "Assets", "_Project", "Resources", "Sprites")
CANDIDATES = os.path.join(REPO, "Tools", "ComfyUI", "candidates")

STYLE = ("pixel art, top-down 3/4 view, post-apocalyptic Vietnamese small town, muted palette, "
         "simple, flat colors, clean outlines")
OBJECT_STYLE = STYLE + ", single object centered, plain white background, game asset"
TILE_STYLE = STYLE + ", seamless tileable texture, top-down, flat, no objects, fills the whole image"
NEGATIVE = ("photo, photorealistic, realistic, 3d render, blurry, gradient, depth of field, soft shading, "
            "text, watermark, signature, side view, multiple objects, frame, border")
TILE_NEGATIVE = NEGATIVE.replace("multiple objects, ", "") + ", white background, object, perspective"

# key -> (subject, is_tile). Keys match the file names PlaceholderArt looks for.
ASSETS = [
    ("floor", "cracked grey concrete ground", True),
    ("wall", "weathered red brick wall top", True),
    ("crate_house", "old wooden kitchen cabinet", False),
    ("crate_medical", "white metal medical cabinet with a red cross", False),
    ("crate_seed", "open wooden crate full of burlap seed bags", False),
    ("crate_weapon", "olive green military ammo crate", False),
    ("zombie_walker", "shambling zombie in torn grey clothes, full body, facing the viewer", False),
    ("zombie_runner", "lean fast zombie in a torn orange tracksuit, full body, facing the viewer", False),
    ("zombie_screamer", "thin pale zombie with a wide open screaming mouth, purple rags, full body, facing the viewer", False),
    ("corpse", "one dead zombie body lying on its back with arms spread, isolated, nothing around it, no ground, no floor", False),
    ("player", "young male survivor with a backpack and a blue jacket, full body, facing the viewer", False),
    ("npc_lan", "young Vietnamese woman survivor in a pink shirt, full body, facing the viewer", False),
    ("npc_minh", "young Vietnamese man mechanic in light blue overalls, full body, facing the viewer", False),
    ("npc_ba_tu", "elderly Vietnamese woman in a conical hat and brown clothes, full body, facing the viewer", False),
    ("bed", "simple camp bed with a red blanket, seen from above", False),
    ("stash", "large wooden storage chest with metal bands", False),
    ("gate", "rusty corrugated metal gate with barbed wire", False),
    ("workbench", "sturdy wooden workbench with a vise, tools and scrap metal on top", False),
    ("shop", "small market stall with a tarp roof, crates and canned goods on the counter", False),
]


class ComfyError(Exception):
    pass


def http_json(url, payload=None, timeout=30):
    data = json.dumps(payload).encode("utf-8") if payload is not None else None
    request = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        body = error.read().decode("utf-8", "replace")
        raise ComfyError("ComfyUI tra ve loi %s: %s" % (error.code, body[:800]))


def find_server(url):
    """Returns the base URL of a reachable ComfyUI. Tries the usual ports when none is given."""
    tried = [url] if url else ["http://127.0.0.1:8188", "http://127.0.0.1:8000"]
    for base in tried:
        try:
            http_json(base.rstrip("/") + "/system_stats", timeout=4)
            return base.rstrip("/")
        except (urllib.error.URLError, OSError, ValueError, ComfyError):
            continue
    raise ComfyError(
        "Khong ket noi duoc ComfyUI o %s.\n"
        "Hay mo ComfyUI truoc. Neu no chay o dia chi khac (xem thanh dia chi hoac Settings > Server),\n"
        "them tham so --url, vi du: --url http://127.0.0.1:8000" % ", ".join(tried))


def options_of(server, node, field):
    """The choices ComfyUI offers for a dropdown input, e.g. the installed checkpoints."""
    info = http_json(server + "/object_info/" + node)[node]["input"]["required"][field]
    first = info[0]
    if isinstance(first, list):
        return first
    if len(info) > 1 and isinstance(info[1], dict) and "options" in info[1]:
        return info[1]["options"]
    return []


def choose(options, wanted, hints, what):
    if wanted:
        for option in options:
            if option == wanted or wanted.lower() in option.lower():
                return option
        raise ComfyError("Khong thay %s '%s'. Dang co: %s" % (what, wanted, ", ".join(options) or "(trong)"))
    for hint in hints:
        for option in options:
            if hint in option.lower():
                return option
    raise ComfyError("Khong tu chon duoc %s. Dang co: %s. Hay chi dinh bang tham so." % (what, ", ".join(options) or "(trong)"))


def build_graph(checkpoint, lora, strength, positive, negative, seed, count, steps, cfg):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": checkpoint}},
        "2": {"class_type": "LoraLoader", "inputs": {
            "model": ["1", 0], "clip": ["1", 1], "lora_name": lora,
            "strength_model": strength, "strength_clip": strength}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["2", 1], "text": positive}},
        "4": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["2", 1], "text": negative}},
        "5": {"class_type": "EmptyLatentImage", "inputs": {"width": 1024, "height": 1024, "batch_size": count}},
        "6": {"class_type": "KSampler", "inputs": {
            "model": ["2", 0], "positive": ["3", 0], "negative": ["4", 0], "latent_image": ["5", 0],
            "seed": seed, "steps": steps, "cfg": cfg,
            "sampler_name": "dpmpp_2m", "scheduler": "karras", "denoise": 1.0}},
        "7": {"class_type": "VAEDecode", "inputs": {"samples": ["6", 0], "vae": ["1", 2]}},
        "8": {"class_type": "SaveImage", "inputs": {"images": ["7", 0], "filename_prefix": "bnhon_auto"}},
    }


def run_graph(server, graph, timeout):
    """Queues a workflow and waits for it. Returns the generated images as RGBA arrays."""
    client_id = uuid.uuid4().hex
    queued = http_json(server + "/prompt", {"prompt": graph, "client_id": client_id})
    if queued.get("node_errors"):
        raise ComfyError("Workflow bi tu choi: %s" % json.dumps(queued["node_errors"])[:800])
    prompt_id = queued["prompt_id"]

    deadline = time.time() + timeout
    while time.time() < deadline:
        history = http_json(server + "/history/" + prompt_id)
        entry = history.get(prompt_id)
        if entry:
            status = entry.get("status", {})
            if status.get("status_str") == "error":
                raise ComfyError("ComfyUI bao loi khi tao anh: %s" % json.dumps(status.get("messages", ""))[:800])
            images = []
            for output in entry.get("outputs", {}).values():
                for image in output.get("images", []):
                    query = urllib.parse.urlencode({
                        "filename": image["filename"],
                        "subfolder": image.get("subfolder", ""),
                        "type": image.get("type", "output")})
                    with urllib.request.urlopen(server + "/view?" + query, timeout=60) as response:
                        images.append(np.asarray(Image.open(io.BytesIO(response.read())).convert("RGBA")))
            if images or status.get("completed"):
                return images
        time.sleep(1.5)
    raise ComfyError("Qua %d giay ma ComfyUI chua tao xong. Thu tang --timeout." % timeout)


def score(sprite, is_tile):
    """
    Higher is more usable. Tiles: prefer even texture (little contrast between the four quarters).
    Objects: one solid shape of a sensible size; empty or nearly full frames mean the background
    removal failed or the subject was cut off.
    """
    if is_tile:
        rgb = sprite[:, :, :3].astype(float)
        half_y, half_x = rgb.shape[0] // 2, rgb.shape[1] // 2
        quarters = [rgb[:half_y, :half_x], rgb[:half_y, half_x:], rgb[half_y:, :half_x], rgb[half_y:, half_x:]]
        means = np.array([q.mean(axis=(0, 1)) for q in quarters])
        return -float(means.std(axis=0).sum())

    height, width = sprite.shape[:2]
    opaque = sprite[:, :, 3] > 0
    if height < 12 or width < 12 or not opaque.any():
        return -1000.0
    fill = float(opaque.mean())
    too_big = max(height, width) > 118
    return fill - (2.0 if too_big else 0.0) - (1.0 if fill < 0.35 else 0.0)


def contact_sheet(rows, path):
    """One row per sprite: its candidates on the raid floor colour, the chosen one outlined."""
    cell, label_width = 136, 150
    columns = max(len(candidates) for _, candidates, _ in rows)
    sheet = Image.new("RGBA", (label_width + columns * cell, len(rows) * cell), (64, 66, 71, 255))
    draw = ImageDraw.Draw(sheet)
    for row, (key, candidates, chosen) in enumerate(rows):
        draw.text((8, row * cell + 8), key, fill=(255, 255, 255, 255))
        for column, sprite in enumerate(candidates):
            image = Image.fromarray(sprite, "RGBA")
            scale = max(1, min(120 // image.width, 120 // image.height))
            image = image.resize((image.width * scale, image.height * scale), Image.NEAREST)
            if image.width > 120 or image.height > 120:
                image.thumbnail((120, 120), Image.NEAREST)
            x = label_width + column * cell + (cell - image.width) // 2
            y = row * cell + (cell - image.height) // 2
            sheet.alpha_composite(image, (x, y))
            draw.text((label_width + column * cell + 4, row * cell + 4), str(column + 1), fill=(255, 255, 255, 255))
            if column == chosen:
                draw.rectangle(
                    [label_width + column * cell + 1, row * cell + 1, label_width + (column + 1) * cell - 2, (row + 1) * cell - 2],
                    outline=(90, 220, 110, 255), width=2)
    sheet.save(path)


def candidate_path(key, index):
    return os.path.join(CANDIDATES, "%s_%d.png" % (key, index + 1))


def apply_pick(spec):
    key, _, number = spec.partition("=")
    source = candidate_path(key, int(number) - 1)
    if not os.path.exists(source):
        raise ComfyError("Khong co ung vien %s. Xem thu muc %s" % (spec, CANDIDATES))
    Image.open(source).save(os.path.join(SPRITES, key + ".png"))
    print("da chon %s -> %s.png" % (os.path.basename(source), key))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--url", help="dia chi ComfyUI (mac dinh thu cong 8188 roi 8000)")
    parser.add_argument("--checkpoint", help="ten (hoac mot phan ten) checkpoint SDXL")
    parser.add_argument("--lora", help="ten (hoac mot phan ten) LoRA pixel art")
    parser.add_argument("--strength", type=float, default=1.1)
    parser.add_argument("--count", type=int, default=4, help="so ung vien moi sprite (giam neu het bo nho)")
    parser.add_argument("--steps", type=int, default=30)
    parser.add_argument("--cfg", type=float, default=6.0)
    parser.add_argument("--seed", type=int, help="seed co dinh de tao lai dung ket qua cu")
    parser.add_argument("--timeout", type=int, default=900, help="so giay cho moi sprite")
    parser.add_argument("--only", nargs="+", metavar="KEY", help="chi lam cac sprite nay")
    parser.add_argument("--redo", nargs="+", metavar="KEY", help="tao lai du da co file")
    parser.add_argument("--pick", nargs="+", metavar="KEY=N", help="doi sang ung vien khac da tao, khong can ComfyUI")
    parser.add_argument("--list", action="store_true", help="chi liet ke, khong tao")
    args = parser.parse_args()

    os.makedirs(SPRITES, exist_ok=True)
    os.makedirs(CANDIDATES, exist_ok=True)
    known = [key for key, _, _ in ASSETS]

    try:
        if args.pick:
            for spec in args.pick:
                apply_pick(spec)
            return 0

        for key in (args.only or []) + (args.redo or []):
            if key not in known:
                raise ComfyError("Khong co sprite ten '%s'. Cac ten hop le: %s" % (key, ", ".join(known)))

        todo = []
        for key, subject, is_tile in ASSETS:
            exists = os.path.exists(os.path.join(SPRITES, key + ".png"))
            if args.list:
                print("%-16s %s" % (key, "co roi" if exists else "THIEU"))
                continue
            if args.only and key not in args.only and key not in (args.redo or []):
                continue
            if exists and key not in (args.redo or []):
                continue
            todo.append((key, subject, is_tile))
        if args.list:
            return 0
        if not todo:
            print("Khong con sprite nao thieu. Dung --redo <ten> de tao lai.")
            return 0

        server = find_server(args.url)
        checkpoint = choose(options_of(server, "CheckpointLoaderSimple", "ckpt_name"), args.checkpoint, ["xl"], "checkpoint")
        lora = choose(options_of(server, "LoraLoader", "lora_name"), args.lora, ["pixel"], "LoRA")
        print("ComfyUI: %s\ncheckpoint: %s\nLoRA: %s\nse tao %d sprite: %s\n" % (
            server, checkpoint, lora, len(todo), ", ".join(key for key, _, _ in todo)))

        rows = []
        failed = []
        for number, (key, subject, is_tile) in enumerate(todo, 1):
            seed = args.seed if args.seed is not None else random.randrange(1, 2 ** 31)
            positive = subject + ", " + (TILE_STYLE if is_tile else OBJECT_STYLE)
            negative = TILE_NEGATIVE if is_tile else NEGATIVE
            print("[%d/%d] %s (seed %d) ..." % (number, len(todo), key, seed), flush=True)

            try:
                started = time.time()
                graph = build_graph(checkpoint, lora, args.strength, positive, negative, seed, args.count, args.steps, args.cfg)
                images = run_graph(server, graph, args.timeout)
                if not images:
                    raise ComfyError("ComfyUI khong tra ve anh nao.")

                candidates = [sprite_prep.process(image, tile=is_tile) for image in images]
                for index, sprite in enumerate(candidates):
                    Image.fromarray(sprite, "RGBA").save(candidate_path(key, index))
                scores = [score(sprite, is_tile) for sprite in candidates]
                chosen = int(np.argmax(scores))
                Image.fromarray(candidates[chosen], "RGBA").save(os.path.join(SPRITES, key + ".png"))
                rows.append((key, candidates, chosen))
                print("        xong sau %ds, chon ung vien %d (%dx%d)" % (
                    time.time() - started, chosen + 1, candidates[chosen].shape[1], candidates[chosen].shape[0]))
            except ComfyError as error:
                failed.append(key)
                print("        LOI: %s" % error)

        if rows:
            sheet = os.path.join(CANDIDATES, "candidates.png")
            contact_sheet(rows, sheet)
            print("\nBang ung vien: %s" % sheet)
            print("Khung xanh la cai da chon. Muon doi: python Tools/generate_sprites.py --pick <ten>=<so>")
        if failed:
            print("\nChua tao duoc: %s. Chay lai lenh de thu lai rieng nhung cai nay." % ", ".join(failed))
            return 1
        return 0
    except ComfyError as error:
        print("LOI: %s" % error)
        return 2


if __name__ == "__main__":
    sys.exit(main())
