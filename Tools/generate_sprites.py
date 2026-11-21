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
ITEM_STYLE = ("pixel art, game inventory icon, single item centered, muted palette, clean outlines, "
              "simple, flat colors, plain white background")
TILE_STYLE = STYLE + ", seamless tileable texture, top-down, flat, no objects, fills the whole image, low contrast, muted colours, subtle even texture, large plain areas"
NEGATIVE = ("photo, photorealistic, realistic, 3d render, blurry, gradient, depth of field, soft shading, "
            "text, watermark, signature, side view, multiple objects, frame, border")
TILE_NEGATIVE = NEGATIVE.replace("multiple objects, ", "") + ", white background, object, perspective, high contrast, busy pattern, bright highlights, strong outlines"

# key -> (subject, is_tile). Keys match the file names PlaceholderArt looks for.
ASSETS = [
    ("floor", "plain dark grey concrete ground with a few faint cracks", True),
    ("wall", "top of a weathered dull brown brick wall, evenly worn", True),
    ("wall_face", "front side of a weathered dull brown brick wall seen straight on, rows of bricks, darker toward the ground", True),
    ("crate_house", "old wooden kitchen cabinet", False),
    ("crate_medical", "white metal medical cabinet with a red cross", False),
    ("crate_seed", "open wooden crate full of burlap seed bags", False),
    ("crate_weapon", "olive green military ammo crate", False),
    ("zombie_walker", "shambling zombie in torn grey clothes, full body, facing the viewer", False),
    ("zombie_runner", "lean fast zombie in a torn orange tracksuit, full body, facing the viewer", False),
    ("zombie_screamer", "thin pale zombie with a wide open screaming mouth, purple rags, full body, facing the viewer", False),
    ("zombie_armored", "zombie wearing rusty riot armour and a helmet, holding a metal shield", False),
    ("zombie_boss", "huge fat butcher zombie in a bloody apron holding a cleaver", False),
    ("crate_safe", "small grey steel safe with a dial lock", False),
    ("corpse", "one dead zombie body lying on its back with arms spread, isolated, nothing around it, no ground, no floor", False),
    ("player", "young male survivor with a backpack and a blue jacket, full body, facing the viewer", False),
    ("npc_lan", "young Vietnamese woman survivor in a pink shirt, full body, facing the viewer", False),
    ("npc_minh", "young Vietnamese man mechanic in light blue overalls, full body, facing the viewer", False),
    ("npc_ba_tu", "elderly Vietnamese woman in a conical hat and brown clothes, full body, facing the viewer", False),
    # Residents still to be rescued. Same wording as the Prompt row of the NPC sheet.
    ("npc_chu_sau", "middle-aged Vietnamese man, 55 years old, market shopkeeper, slightly chubby, balding on top, reading glasses low on his nose, pen behind his ear, short-sleeved checkered shirt, khaki trousers, waist pouch, one leg bandaged, leaning on a bamboo walking stick, full body, facing the viewer", False),
    ("npc_hung", "Vietnamese man, 29 years old, militia soldier, muscular, buzz cut, stern angular face, worn green militia uniform, old bulletproof vest, red armband, rifle slung over his shoulder, combat boots, full body, facing the viewer", False),
    ("npc_vy", "young Vietnamese woman, 26 years old, electrician and biker, tall, short pixie haircut dyed faded red, patched black leather biker jacket, fingerless gloves, ripped jeans, tall boots, safety goggles on her forehead, tool belt with electrical tape, confident grin, full body, facing the viewer", False),
    ("npc_thao", "Vietnamese woman, 29 years old, school teacher, slender, long black hair held with a clip, round glasses, white blouse, long grey skirt, thin cardigan, holding a stack of books, calm warm expression, full body, facing the viewer", False),
    ("npc_khang", "Vietnamese man, 32 years old, restaurant chef, stocky, cheerful wide smile, stained white chef jacket, apron, bandana over his hair, kitchen towel over one shoulder, full body, facing the viewer", False),
    ("npc_tuan", "young Vietnamese man, 23 years old, motorbike delivery rider, skinny, slicked-back hair, green delivery jacket with a peeling logo, half-face helmet, cargo shorts, sneakers, large square delivery backpack, cheeky grin, full body, facing the viewer", False),
    ("npc_ngoc", "young Vietnamese woman, 27 years old, former raider, lean, long black hair in a high ponytail, long scar across one cheek, dark brown leather jacket, checkered scarf around her neck, black gloves, dagger and crossbow at her hip, cold sharp eyes, full body, facing the viewer", False),
    ("npc_quan", "middle-aged Vietnamese man, 46 years old, research scientist, thin, neatly combed salt-and-pepper hair, thin metal-rimmed glasses, white lab coat over a buttoned shirt with a loose tie, latex gloves, holding a test tube, calm unreadable expression, full body, facing the viewer", False),
    ("bed", "simple camp bed with a red blanket, seen from above", False),
    ("stash", "large wooden storage chest with metal bands", False),
    ("gate", "rusty corrugated metal gate with barbed wire", False),
    ("workbench", "sturdy wooden workbench with a vise, tools and scrap metal on top", False),
    ("base_floor", "trampled dirt yard with sparse patches of grass", True),
    ("plot_dry", "tilled dry brown farm soil with straight furrows", True),
    ("plot_wet", "tilled dark wet farm soil with straight furrows", True),
    ("crop_sprout", "tiny green seedling sprout with two leaves", False),
    ("crop_sweet_potato", "sweet potato plant, low green vines with heart-shaped leaves", False),
    ("crop_corn", "tall corn stalk with a yellow ear of corn", False),
    ("crop_mugwort", "bushy mugwort herb plant with jagged grey-green leaves", False),
    ("crop_tomato", "tomato plant on a stake with red tomatoes", False),
    ("crop_water_spinach", "clump of water spinach with long green stalks and narrow leaves", False),
    ("crop_cabbage", "round green cabbage growing in the ground", False),
    ("well", "round stone water well with a wooden bucket", False),
    ("bench_vy", "electrician workbench with wires, a car battery, a soldering iron and a parked black motorbike beside it", False),
    ("lab", "makeshift science lab table with glass flasks, test tubes and a microscope", False),
    ("kitchen", "small brick cooking stove with a steaming pot on top", False),
    ("shop", "small market stall with a tarp roof, crates and canned goods on the counter", False),
]

# Inventory icons: item id -> (subject, slot shape, backdrop).
# The slot shape matches the item's size in the grid, so the icon fills its cells.
# White items get a dark backdrop, otherwise background removal would erase them.
DARK = "plain solid dark blue background"
ITEMS = [
    ("wood", "bundle of wooden planks tied with rope, upright", "tall", None),
    ("scrap_metal", "pile of rusty scrap metal pieces and bolts", None, None),
    ("cloth", "folded piece of grey-brown cloth fabric", None, DARK),
    ("circuit_board", "green electronic circuit board", None, None),
    ("canned_food", "tin can of food with a paper label", None, None),
    ("bandage", "roll of white medical gauze bandage", None, DARK),
    ("med_kit", "red first aid kit bag with a white cross, upright", "tall", None),
    ("water_bottle", "clear plastic water bottle with a blue cap, upright", "tall", DARK),
    ("pistol_ammo", "small cardboard box of pistol ammunition with brass bullets beside it", None, None),
    ("shotgun_shell", "three red shotgun shells standing together", None, None),
    ("pistol", "black semi-automatic pistol, side view, pointing right", "wide", None),
    ("shotgun", "pump-action shotgun with a wooden stock, side view, horizontal, pointing right", "wide", None),
    ("bp_pistol", "rolled blue blueprint paper showing a technical drawing of a pistol", None, None),
    ("bp_shotgun", "rolled blue blueprint paper showing a technical drawing of a shotgun", None, None),
    ("bp_med_kit", "rolled blue blueprint paper showing a technical drawing of a first aid kit", None, None),
    ("seed_sweet_potato", "small burlap seed bag with a purple sweet potato drawn on it", None, None),
    ("seed_corn", "small burlap seed bag with a yellow corn cob drawn on it", None, None),
    ("seed_mugwort", "small burlap seed bag with a green herb leaf drawn on it", None, None),
    ("sweet_potato", "purple-skinned sweet potato", None, None),
    ("corn", "ear of yellow corn with green husk", None, None),
    ("mugwort", "bunch of green mugwort herb leaves tied together", None, None),
    ("seed_tomato", "small burlap seed bag with a red tomato drawn on it", None, None),
    ("seed_water_spinach", "small burlap seed bag with long green leaves drawn on it", None, None),
    ("seed_cabbage", "small burlap seed bag with a green cabbage drawn on it", None, None),
    ("tomato", "ripe red tomato with a green stem", None, None),
    ("water_spinach", "bunch of long green water spinach stalks tied together", None, None),
    ("cabbage", "round green cabbage head", None, None),
    ("meal_roasted_sweet_potato", "roasted sweet potato split open showing orange flesh", None, None),
    ("meal_spinach_soup", "bowl of clear green vegetable soup", None, DARK),
    ("meal_mugwort_porridge", "bowl of rice porridge with green herbs on top", None, DARK),
    ("meal_cabbage_stir_fry", "plate of stir-fried cabbage with red tomato pieces", None, DARK),
    ("fuel_can", "red metal jerry can for petrol", "tall", None),
    ("radio_decoy", "small battered portable radio with an antenna and duct tape", None, None),
    ("flare", "red emergency road flare stick", "tall", None),
    ("sample", "small sealed glass vial with dark green zombie tissue inside, a paper label", "tall", None),
    ("core_electrician", "glowing blue crystal-like organic core with sparks, held in a jar", None, DARK),
    ("core_butcher", "pulsing dark red organic core, veined, in a jar", None, None),
    ("dagger", "combat dagger with a black handle, upright, blade pointing up", "tall", None),
    ("tea_tin", "small decorated metal tea tin", None, None),
    ("old_cassette", "old audio cassette tape", None, None),
]


# The look of the abandoned world. "flat" things lie on the ground and are seen from straight
# above; "prop" things stand up and are drawn like the crates and characters.
APOCALYPSE = "abandoned, weathered, rusty, dusty, post-apocalyptic, muted desaturated colors"
FLAT_STYLE = ("pixel art, top-down view from directly above, lying flat on the ground, single decal, isolated, "
              + APOCALYPSE + ", plain white background, game asset")
PROP_STYLE = OBJECT_STYLE + ", " + APOCALYPSE

# key -> (subject, "flat" or "prop", background or None for white)
DECOR = [
    ("decor_crack_1", "network of dark cracks in grey asphalt, thin lines only", "flat", None),
    ("decor_crack_2", "single long jagged crack with small branches, dark lines", "flat", None),
    ("decor_blood_pool", "dried dark red blood stain splatter", "flat", None),
    ("decor_blood_drag", "long smeared dark red blood drag trail", "flat", None),
    ("decor_scorch", "black burn scorch mark with soot", "flat", None),
    ("decor_puddle", "dark muddy rain puddle with ripples", "flat", None),
    ("decor_leaves", "scattered dry brown and yellow fallen leaves", "flat", None),
    ("decor_trash", "scattered crumpled paper, plastic bags and cans", "flat", None),
    ("decor_glass", "shattered broken window glass shards", "flat", DARK),
    ("decor_weeds_1", "small tuft of green weeds growing through a crack", "flat", None),
    ("decor_weeds_2", "patch of dry yellow grass and small weeds", "flat", None),
    ("decor_vines", "tangle of green ivy vines and leaves", "flat", None),
    ("prop_car_rust_red", "rusty abandoned red sedan car with broken windows, seen from above at an angle, side facing right", "prop", None),
    ("prop_car_rust_blue", "rusty abandoned blue hatchback car with flat tyres, seen from above at an angle, side facing right", "prop", None),
    ("prop_car_burnt", "burnt out car wreck, charred black metal frame, no paint", "prop", None),
    ("prop_motorbike", "fallen rusty scooter motorbike lying on its side", "prop", None),
    ("prop_bicycle", "old bicycle lying on the ground", "prop", None),
    ("prop_ambulance", "abandoned white ambulance van with a red stripe, doors open, dirty", "prop", None),
    ("prop_bus", "long abandoned rusty yellow city bus, broken windows, side view from above", "prop", None),
    ("prop_concrete_barrier", "cracked grey concrete road barrier block", "prop", None),
    ("prop_sandbags", "low wall of stacked khaki sandbags", "prop", None),
    ("prop_barbed_wire", "coil of rusty barbed wire on wooden stakes", "prop", None),
    ("prop_barricade_furniture", "makeshift barricade of broken chairs, tables and planks", "prop", None),
    ("prop_barrel", "rusty metal oil drum barrel", "prop", None),
    ("prop_barrel_fire", "rusty oil drum with fire burning inside", "prop", DARK),
    ("prop_trash_bin", "overturned green plastic wheelie bin", "prop", None),
    ("prop_rubble", "pile of broken bricks and concrete rubble", "prop", None),
    ("prop_tire", "old worn car tyre lying flat", "prop", None),
    ("prop_shopping_cart", "rusty abandoned shopping cart", "prop", None),
    ("prop_power_pole_fallen", "fallen concrete power pole with tangled wires, lying horizontally", "prop", None),
    ("prop_sign_fallen", "fallen bent yellow road warning sign", "prop", None),
    ("prop_suitcase", "abandoned open suitcase with clothes spilling out", "prop", None),
    ("prop_tarp_body", "body covered by a blue plastic tarp lying on the ground", "prop", None),
    ("prop_tent_collapsed", "collapsed green relief tent with a red cross", "prop", None),
    ("prop_tree_green", "leafy green tree canopy seen from above, round", "prop", None),
    ("prop_tree_dead", "dead leafless grey tree seen from above, bare branches", "prop", None),
    ("prop_bush_1", "small round green bush", "prop", None),
    ("prop_bush_2", "overgrown tangled shrub with dry leaves", "prop", None),
    ("power_switch", "grey electrical breaker box with a big lever on a wall, warning sticker", "prop", None),
]


def all_jobs():
    """Every sprite as (file key, positive prompt, negative prompt, is_tile, orient)."""
    jobs = []
    for key, subject, is_tile in ASSETS:
        style = TILE_STYLE if is_tile else OBJECT_STYLE
        jobs.append((key, subject + ", " + style, TILE_NEGATIVE if is_tile else NEGATIVE, is_tile, None))
    for key, subject, kind, backdrop in DECOR:
        style = FLAT_STYLE if kind == "flat" else PROP_STYLE
        if backdrop is not None:
            style = style.replace("plain white background", backdrop)
        jobs.append((key, subject + ", " + style, NEGATIVE, False, None))
    for item_id, subject, orient, backdrop in ITEMS:
        style = ITEM_STYLE if backdrop is None else ITEM_STYLE.replace("plain white background", backdrop)
        jobs.append(("item_" + item_id, subject + ", " + style, NEGATIVE, False, orient))
    return jobs


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
    cell, label_width = 136, 190
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
    jobs = all_jobs()
    known = [job[0] for job in jobs]

    try:
        if args.pick:
            for spec in args.pick:
                apply_pick(spec)
            return 0

        for key in (args.only or []) + (args.redo or []):
            if key not in known:
                raise ComfyError("Khong co sprite ten '%s'. Cac ten hop le: %s" % (key, ", ".join(known)))

        todo = []
        for key, positive, negative, is_tile, orient in jobs:
            exists = os.path.exists(os.path.join(SPRITES, key + ".png"))
            if args.list:
                print("%-24s %s" % (key, "co roi" if exists else "THIEU"))
                continue
            if args.only and key not in args.only and key not in (args.redo or []):
                continue
            if exists and key not in (args.redo or []):
                continue
            todo.append((key, positive, negative, is_tile, orient))
        if args.list:
            return 0
        if not todo:
            print("Khong con sprite nao thieu. Dung --redo <ten> de tao lai.")
            return 0

        server = find_server(args.url)
        checkpoint = choose(options_of(server, "CheckpointLoaderSimple", "ckpt_name"), args.checkpoint, ["xl"], "checkpoint")
        lora = choose(options_of(server, "LoraLoader", "lora_name"), args.lora, ["pixel"], "LoRA")
        print("ComfyUI: %s\ncheckpoint: %s\nLoRA: %s\nse tao %d sprite: %s\n" % (
            server, checkpoint, lora, len(todo), ", ".join(job[0] for job in todo)))

        rows = []
        failed = []
        for number, (key, positive, negative, is_tile, orient) in enumerate(todo, 1):
            seed = args.seed if args.seed is not None else random.randrange(1, 2 ** 31)
            print("[%d/%d] %s (seed %d) ..." % (number, len(todo), key, seed), flush=True)

            try:
                started = time.time()
                graph = build_graph(checkpoint, lora, args.strength, positive, negative, seed, args.count, args.steps, args.cfg)
                images = run_graph(server, graph, args.timeout)
                if not images:
                    raise ComfyError("ComfyUI khong tra ve anh nao.")

                candidates = [sprite_prep.process(image, tile=is_tile, orient=orient) for image in images]
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
