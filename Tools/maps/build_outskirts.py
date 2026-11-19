"""
Builds Resources/Maps/outskirts.txt, the 260x170 outskirts raid map, from the layout of the
concept image: houses top left, the school top centre, the gas station top right, the main
road across the middle, paddies and the market bottom left, the radio tower hill bottom right.

    python Tools/maps/build_outskirts.py            # writes the map and a preview image
    python Tools/maps/build_outskirts.py --seed 7   # same layout, different details

The output is ordinary map text, so it can be opened and edited in the Raid Map Editor or,
later, imported into a Tilemap. Rerunning this script overwrites hand edits.
"""

import argparse
import os
import random
import sys
from collections import deque

W, H = 260, 170

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
MAP_PATH = os.path.join(ROOT, "Assets", "_Project", "Resources", "Maps", "outskirts.txt")
PREVIEW_PATH = os.path.join(HERE, "outskirts_preview.png")

WALL, FLOOR, GRASS, ROAD, DIRT, PADDY = "#", ".", ",", "=", ":", "~"
GROUND = FLOOR + GRASS + ROAD + DIRT + PADDY

HEADER = """; Ban do raid: Vung ven thi tran (260 x 170)
; Sinh boi Tools/maps/build_outskirts.py theo anh y tuong. Chay lai script se ghi de.
;
; Khu: xom nha ong (tren trai), truong hoc (tren giua), tram xang (tren phai),
;      ruong lua va cho que (duoi trai), doi thap radio (duoi phai).
; Nen: . san be tong   , co   = duong nhua   : dat   ~ ruong nuoc
; Dieu kien: X cau dao   J loi ra can bat cau dao   U loi ra can ha trum
"""


class Grid:
    def __init__(self, rng):
        self.rng = rng
        self.c = [[GRASS] * W for _ in range(H)]
        self.zone = [[""] * W for _ in range(H)]

    def inside(self, x, y):
        return 0 <= x < W and 0 <= y < H

    def get(self, x, y):
        return self.c[y][x] if self.inside(x, y) else WALL

    def set(self, x, y, s):
        if self.inside(x, y):
            self.c[y][x] = s

    def fill(self, x0, y0, x1, y1, s):
        for y in range(max(0, y0), min(H, y1 + 1)):
            for x in range(max(0, x0), min(W, x1 + 1)):
                self.c[y][x] = s

    def tag(self, x0, y0, x1, y1, name):
        for y in range(max(0, y0), min(H, y1 + 1)):
            for x in range(max(0, x0), min(W, x1 + 1)):
                self.zone[y][x] = name

    def rect_walls(self, x0, y0, x1, y1):
        for x in range(x0, x1 + 1):
            self.set(x, y0, WALL)
            self.set(x, y1, WALL)
        for y in range(y0, y1 + 1):
            self.set(x0, y, WALL)
            self.set(x1, y, WALL)

    def gap(self, cells, s=FLOOR):
        for (x, y) in cells:
            self.set(x, y, s)

    def free(self, x, y):
        return self.inside(x, y) and self.c[y][x] in GROUND

    def blob(self, cx, cy, radius, s=WALL, only=GROUND):
        for y in range(cy - radius, cy + radius + 1):
            for x in range(cx - radius, cx + radius + 1):
                if (x - cx) ** 2 + (y - cy) ** 2 <= radius * radius + self.rng.random() * radius:
                    if self.inside(x, y) and self.c[y][x] in only:
                        self.c[y][x] = s


def building(g, x0, y0, x1, y1, doors, inner_walls=()):
    """A building with an outer wall, a floor inside, door gaps, and optional inner walls."""
    g.fill(x0, y0, x1, y1, FLOOR)
    g.rect_walls(x0, y0, x1, y1)
    for (wx0, wy0, wx1, wy1, door) in inner_walls:
        for y in range(wy0, wy1 + 1):
            for x in range(wx0, wx1 + 1):
                g.set(x, y, WALL)
        if door:
            g.gap(door)
    g.gap(doors)


# ---------------------------------------------------------------------------------------
# Layout
# ---------------------------------------------------------------------------------------

ROAD_TOP, ROAD_BOTTOM = 71, 78          # the main road across the map
WEST_STREET = (98, 103)                 # street between the houses and the school
EAST_STREET = (179, 184)                # street between the school and the gas station
SOUTH_ROAD_X = 136                      # left edge of the road down to the south edge


def south_road_left(y):
    """The south road bends gently, like the one in the picture."""
    if y < 100:
        return SOUTH_ROAD_X
    if y < 130:
        return SOUTH_ROAD_X + (y - 100) // 6
    return SOUTH_ROAD_X + 5 - (y - 130) // 10


def roads(g):
    g.fill(0, ROAD_TOP - 1, W - 1, ROAD_TOP - 1, DIRT)
    g.fill(0, ROAD_BOTTOM + 1, W - 1, ROAD_BOTTOM + 1, DIRT)
    g.fill(0, ROAD_TOP, W - 1, ROAD_BOTTOM, ROAD)
    g.fill(WEST_STREET[0], 1, WEST_STREET[1], ROAD_TOP, ROAD)
    g.fill(EAST_STREET[0], 1, EAST_STREET[1], ROAD_TOP, ROAD)
    for y in range(ROAD_BOTTOM, H):
        left = south_road_left(y)
        g.fill(left - 1, y, left - 1, y, DIRT)
        g.fill(left, y, left + 6, y, ROAD)
        g.fill(left + 7, y, left + 7, y, DIRT)


def residential(g, rng):
    """Rows of narrow tube houses with alleys between them."""
    g.fill(1, 1, WEST_STREET[0] - 1, ROAD_TOP - 2, DIRT)
    g.tag(1, 1, WEST_STREET[0] - 1, ROAD_TOP - 2, "houses")
    rows = [(3, 18), (24, 39), (46, 62)]
    for (top, bottom) in rows:
        x = 3
        while x < WEST_STREET[0] - 10:
            width = rng.choice([8, 9, 10, 11])
            if x + width > WEST_STREET[0] - 3:
                break
            if rng.random() < 0.12:
                # an empty lot with a little garden
                g.fill(x, top, x + width - 1, bottom, GRASS)
                g.blob(x + width // 2, (top + bottom) // 2, 1)
                x += width + 3
                continue
            x1 = x + width - 1
            mid = (top + bottom) // 2 + rng.randint(-2, 2)
            front_door = [(x + width // 2, bottom), (x + width // 2 + 1, bottom)]
            back_door = [(x + 2, top)] if rng.random() < 0.5 else []
            split = (x + 1, mid, x1 - 1, mid, [(x + 2 + rng.randint(0, width - 5), mid), (x + 3 + rng.randint(0, width - 6), mid)])
            building(g, x, top, x1, bottom, front_door + back_door, [split])
            x += width + rng.choice([2, 3, 3, 4])
    # a back fence along the top of the road so the houses read as a block
    for x in range(1, WEST_STREET[0] - 1):
        if g.get(x, ROAD_TOP - 2) == DIRT and rng.random() < 0.35:
            g.set(x, ROAD_TOP - 2, WALL)


def school(g, rng):
    x0, y0, x1, y1 = WEST_STREET[1] + 3, 4, EAST_STREET[0] - 3, ROAD_TOP - 3
    g.fill(x0, y0, x1, y1, FLOOR)
    g.tag(x0, y0, x1, y1, "school")
    g.rect_walls(x0, y0, x1, y1)
    gate = (x0 + x1) // 2
    g.gap([(gate + d, y1) for d in range(-3, 4)])
    g.gap([(x0, 40), (x0, 41), (x0, 42), (x1, 40), (x1, 41), (x1, 42)])

    # The two-storey block: classrooms along a corridor.
    bx0, by0, bx1, by1 = x0 + 6, y0 + 3, x1 - 6, y0 + 18
    corridor = by1 - 3
    building(g, bx0, by0, bx1, by1, [((bx0 + bx1) // 2 + d, by1) for d in (-1, 0, 1)] + [(bx0, corridor), (bx1, corridor)])
    for x in range(bx0 + 1, bx1):
        g.set(x, corridor - 1, WALL)
    # Classrooms: dividers every few cells, each room with a door onto the corridor.
    dividers = list(range(bx0 + 9, bx1 - 5, 9))
    for x in dividers:
        for y in range(by0 + 1, corridor - 1):
            g.set(x, y, WALL)
    edges = [bx0] + dividers + [bx1]
    for left, right in zip(edges, edges[1:]):
        door = (left + right) // 2
        g.set(door, corridor - 1, FLOOR)
        g.set(door + 1 if door + 1 < right else door - 1, corridor - 1, FLOOR)

    # Football pitch: an open field in the yard.
    fx0, fy0, fx1, fy1 = x0 + 12, by1 + 6, x1 - 20, y1 - 6
    g.fill(fx0, fy0, fx1, fy1, GRASS)
    g.tag(fx0, fy0, fx1, fy1, "pitch")
    for gy in range((fy0 + fy1) // 2 - 2, (fy0 + fy1) // 2 + 3):
        g.set(fx0 - 1, gy, WALL)
        g.set(fx1 + 1, gy, WALL)

    # The shed with the blue roof, right of the pitch.
    building(g, x1 - 14, by1 + 8, x1 - 5, by1 + 22, [(x1 - 14, by1 + 15), (x1 - 14, by1 + 16)])
    # Gatehouse.
    building(g, gate - 3, y1 - 6, gate + 3, y1 - 2, [(gate, y1 - 2)])
    # trees along the fence
    for _ in range(14):
        tx = rng.randint(x0 + 2, x1 - 2)
        ty = rng.choice([y0 + 21, y1 - 3]) if rng.random() < 0.6 else rng.randint(by1 + 3, y1 - 3)
        if g.get(tx, ty) == FLOOR and not (fx0 - 2 <= tx <= fx1 + 2 and fy0 - 2 <= ty <= fy1 + 2):
            g.blob(tx, ty, 1, only=FLOOR)
    return (bx0, by0, bx1, by1, corridor), (fx0, fy0, fx1, fy1), (x1 - 14, by1 + 8, x1 - 5, by1 + 22)


def gas_station(g, rng):
    x0, y0, x1, y1 = EAST_STREET[1] + 3, 4, W - 3, ROAD_TOP - 3
    g.fill(x0, y0, x1, y1, FLOOR)
    g.tag(x0, y0, x1, y1, "gas")
    g.rect_walls(x0, y0, x1, y1)
    # wide entrances from the road, like a forecourt
    g.gap([(x, y1) for x in range(x0 + 6, x0 + 22)])
    g.gap([(x, y1) for x in range(x1 - 20, x1 - 8)])
    g.gap([(x0, y) for y in range(46, 51)])

    # The garage with three bays, back against the north fence.
    gx0, gy0, gx1, gy1 = x0 + 26, y0 + 2, x1 - 3, y0 + 18
    building(g, gx0, gy0, gx1, gy1, [])
    bay = (gx1 - gx0) // 3
    for i in range(3):
        door_x = gx0 + bay * i + bay // 2
        g.gap([(door_x + d, gy1) for d in range(-2, 3)])
        if i > 0:
            for y in range(gy0 + 1, gy1 - 4):
                g.set(gx0 + bay * i, y, WALL)

    # The station shop.
    sx0, sy0, sx1, sy1 = x0 + 4, y0 + 22, x0 + 26, y0 + 34
    building(g, sx0, sy0, sx1, sy1, [(sx0 + 10, sy1), (sx0 + 11, sy1)],
             [(sx0 + 15, sy0 + 1, sx0 + 15, sy1 - 1, [(sx0 + 15, sy0 + 5), (sx0 + 15, sy0 + 6)])])

    # Pump islands under the canopy.
    for i in range(4):
        px = x0 + 34 + i * 7
        g.fill(px, y0 + 42, px + 1, y0 + 43, WALL)
    return (gx0, gy0, gx1, gy1), (sx0, sy0, sx1, sy1)


def paddies(g, rng):
    """Rice paddies divided by raised dykes, with a strip of vegetable beds by the road."""
    x0, y0, x1, y1 = 1, ROAD_BOTTOM + 3, 70, H - 2
    g.tag(x0, y0, x1, y1, "paddies")
    y = y0
    while y < y1:
        height = rng.randint(11, 15)
        x = x0
        while x < x1:
            width = rng.randint(18, 24)
            g.fill(x, y, min(x + width - 1, x1), min(y + height - 1, y1), PADDY)
            x += width + 2
        y += height + 2
    # Vegetable beds: rows of dirt and green.
    for yy in range(y0, y0 + 30):
        for xx in range(44, 72):
            g.set(xx, yy, DIRT if yy % 2 == 0 else GRASS)
    # A few field huts.
    building(g, 30, y0 + 52, 36, y0 + 57, [(33, y0 + 52)])
    building(g, 58, y0 + 66, 64, y0 + 71, [(58, y0 + 68)])
    # A pond.
    for _ in range(3):
        g.blob(rng.randint(76, 84), rng.randint(150, 160), 3, s=WALL, only=GROUND)
    return [(33, y0 + 54), (61, y0 + 69)]


def market(g, rng):
    x0, y0, x1, y1 = 74, ROAD_BOTTOM + 4, SOUTH_ROAD_X - 6, 140
    g.fill(x0, y0, x1, y1, FLOOR)
    g.tag(x0, y0, x1, y1, "market")
    # a low wall on two sides, open toward the road and the paddies
    for x in range(x0, x1 + 1):
        if rng.random() < 0.8:
            g.set(x, y1, WALL)
    for y in range(y0, y1 + 1):
        if rng.random() < 0.8:
            g.set(x1, y, WALL)
    stalls = []
    for row in range(5):
        for col in range(5):
            sx = x0 + 4 + col * 10
            sy = y0 + 4 + row * 10
            if sx + 5 > x1 - 2 or sy + 3 > y1 - 2:
                continue
            if rng.random() < 0.15:
                continue
            g.fill(sx, sy, sx + 4, sy + 1, WALL)       # the counter
            stalls.append((sx + 2, sy + 3))           # where the goods are left
    # The covered market hall, a building with stalls inside.
    building(g, x0 + 2, y1 - 14, x0 + 22, y1 - 3, [(x0 + 12, y1 - 14), (x0 + 13, y1 - 14), (x0 + 22, y1 - 8)],
             [(x0 + 12, y1 - 12, x0 + 12, y1 - 4, [(x0 + 12, y1 - 9)])])
    return stalls, (x0 + 2, y1 - 14, x0 + 22, y1 - 3)


def hill(g, rng):
    """The radio tower hill: rocky slopes, one winding trail up, the fenced tower on top."""
    x0, y0, x1, y1 = SOUTH_ROAD_X + 14, ROAD_BOTTOM + 3, W - 2, H - 2
    g.tag(x0, y0, x1, y1, "hill")
    # Rocks along the foot of the hill, leaving the trail mouth and a scramble gap open.
    for y in range(y0, y1 + 1, 2):
        g.blob(x0 + rng.randint(0, 2), y, rng.randint(1, 2), only=GRASS + DIRT)
    for x in range(x0, x1 + 1, 2):
        g.blob(x, y0 + rng.randint(0, 2), rng.randint(1, 2), only=GRASS + DIRT)
    # Rock outcrops on the slopes.
    for _ in range(30):
        g.blob(rng.randint(x0 + 8, x1 - 6), rng.randint(y0 + 8, y1 - 6), rng.randint(1, 3), only=GRASS)

    # The trail: from the main road up to the tower, then down the back to the far exit.
    trail = []
    x, y = x0 + 6, y0 - 1
    target_points = [(x0 + 22, y0 + 30), (x0 + 50, y0 + 16), (x0 + 70, y0 + 36), (x0 + 82, y0 + 22),
                     (x0 + 84, y0 + 46), (x0 + 96, y0 + 70), (x1 - 6, y1 - 4)]
    for (tx, ty) in target_points:
        while (x, y) != (tx, ty):
            if abs(tx - x) > abs(ty - y) or (abs(tx - x) == abs(ty - y) and rng.random() < 0.5):
                x += 1 if tx > x else -1
            else:
                y += 1 if ty > y else -1
            trail.append((x, y))
    for (tx, ty) in trail:
        for dy in range(-1, 2):
            for dx in range(-1, 2):
                if g.inside(tx + dx, ty + dy) and y0 - 2 <= ty + dy:
                    g.set(tx + dx, ty + dy, DIRT)
    # A second, rougher way up from the south road.
    for yy in range(y0 + 60, y0 + 64):
        for xx in range(x0 - 2, x0 + 18):
            if g.get(xx, yy) == WALL:
                g.set(xx, yy, GRASS)

    # The fenced tower compound on the summit.
    cx0, cy0, cx1, cy1 = x0 + 74, y0 + 10, x0 + 100, y0 + 30
    g.fill(cx0, cy0, cx1, cy1, DIRT)
    g.tag(cx0, cy0, cx1, cy1, "tower")
    g.rect_walls(cx0, cy0, cx1, cy1)
    g.gap([(cx0 + 8 + d, cy1) for d in range(4)], DIRT)
    g.gap([(cx0, cy0 + 12), (cx0, cy0 + 13), (cx0, cy0 + 14)], DIRT)
    g.gap([(cx1, cy0 + 16), (cx1, cy0 + 17), (cx1, cy0 + 18)], DIRT)
    tower = (cx0 + 14, cy0 + 6)
    g.fill(tower[0], tower[1], tower[0] + 3, tower[1] + 3, WALL)
    hut = (cx0 + 3, cy0 + 3, cx0 + 10, cy0 + 9)
    building(g, *hut, [(cx0 + 10, cy0 + 6)])
    return (cx0, cy0, cx1, cy1), hut, tower


def scenery(g, rng):
    # Trees on open grass.
    for _ in range(260):
        x, y = rng.randint(2, W - 3), rng.randint(2, H - 3)
        if g.get(x, y) == GRASS and all(g.get(x + dx, y + dy) == GRASS for dx in (-2, 0, 2) for dy in (-2, 0, 2)):
            g.blob(x, y, 1, only=GRASS)
    # Wrecks, barricades and litter are not part of the map: the game scatters them
    # (Scripts/Game/World/DecorLayer.cs), with pictures when they exist.


# ---------------------------------------------------------------------------------------
# Markers
# ---------------------------------------------------------------------------------------

class Placer:
    def __init__(self, g, rng, spawn):
        self.g, self.rng, self.spawn = g, rng, spawn
        self.counts = {}

    def ok(self, x, y, near_spawn=24):
        if not self.g.free(x, y):
            return False
        if self.g.get(x, y) not in GROUND:
            return False
        return (x - self.spawn[0]) ** 2 + (y - self.spawn[1]) ** 2 >= near_spawn ** 2

    def at(self, x, y, s):
        if self.g.free(x, y):
            self.g.set(x, y, s)
            self.counts[s] = self.counts.get(s, 0) + 1
            return True
        return False

    def in_rect(self, rect, s, n, near_spawn=24, ground=None, against_wall=False):
        x0, y0, x1, y1 = rect
        placed, tries = 0, 0
        while placed < n and tries < n * 400:
            tries += 1
            x, y = self.rng.randint(x0, x1), self.rng.randint(y0, y1)
            if not self.ok(x, y, near_spawn):
                continue
            if ground and self.g.get(x, y) not in ground:
                continue
            if against_wall and not any(self.g.get(x + dx, y + dy) == WALL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                continue
            self.at(x, y, s)
            placed += 1
        return placed

    def in_zone(self, zone, s, n, **kw):
        cells = [(x, y) for y in range(H) for x in range(W) if self.g.zone[y][x] == zone]
        xs = [c[0] for c in cells]
        ys = [c[1] for c in cells]
        return self.in_rect((min(xs), min(ys), max(xs), max(ys)), s, n, **kw)


def rooms_of(g, zone):
    """Indoor floor cells (plain floor with walls on two sides or more) in a zone, for cabinets."""
    out = []
    for y in range(1, H - 1):
        for x in range(1, W - 1):
            if g.zone[y][x] != zone or g.c[y][x] != FLOOR:
                continue
            walls = sum(g.c[y + dy][x + dx] == WALL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if walls >= 1:
                out.append((x, y))
    return out


def place_markers(g, rng, school_parts, gas_parts, huts, market_parts, hill_parts):
    spawn = (south_road_left(H - 4) + 3, H - 4)
    g.set(spawn[0], spawn[1], "P")
    p = Placer(g, rng, spawn)

    # Exits: the bus at the west end, the north end of the school street, the garage (needs
    # power), and the trail behind the hill (needs the boss dead).
    p.at(4, ROAD_TOP + 5, "E")
    p.at(WEST_STREET[0] + 2, 2, "E")
    (gx0, gy0, gx1, gy1), (sx0, sy0, sx1, sy1) = gas_parts
    p.at(gx1 - 4, gy0 + 3, "J")
    p.at(sx0 + 19, sy0 + 3, "X")
    p.at(W - 5, H - 5, "U")

    # Loot.
    house_rooms = rooms_of(g, "houses")
    rng.shuffle(house_rooms)
    for (x, y) in house_rooms[:30]:
        p.at(x, y, "C")
    for (x, y) in house_rooms[30:34]:
        p.at(x, y, "M")
    (bx0, by0, bx1, by1, corridor), pitch, shed = school_parts
    p.in_rect((bx0 + 1, by0 + 1, bx1 - 1, corridor - 2), "M", 8, against_wall=True)
    p.in_rect((bx0 + 1, by0 + 1, bx1 - 1, corridor - 2), "C", 6, against_wall=True)
    p.in_rect((shed[0] + 1, shed[1] + 1, shed[2] - 1, shed[3] - 1), "W", 1)
    p.in_rect((shed[0] + 1, shed[1] + 1, shed[2] - 1, shed[3] - 1), "C", 2)
    p.in_rect((gx0 + 1, gy0 + 1, gx1 - 1, gy1 - 2), "W", 5, against_wall=True)
    p.in_rect((gx0 + 1, gy0 + 1, gx1 - 1, gy1 - 2), "C", 3, against_wall=True)
    p.in_rect((sx0 + 1, sy0 + 1, sx1 - 1, sy1 - 1), "C", 3, against_wall=True)
    p.in_rect((sx0 + 1, sy0 + 1, sx1 - 1, sy1 - 1), "M", 1, against_wall=True)
    stalls, hall = market_parts
    rng.shuffle(stalls)
    for (x, y) in stalls[:6]:
        p.at(x, y, "S")
    for (x, y) in stalls[6:10]:
        p.at(x, y, "C")
    p.in_rect((hall[0] + 1, hall[1] + 1, hall[2] - 1, hall[3] - 1), "S", 2)
    for (x, y) in huts:
        p.at(x, y, "S")
    compound, hut, tower = hill_parts
    p.in_rect((hut[0] + 1, hut[1] + 1, hut[2] - 1, hut[3] - 1), "V", 2)
    p.in_rect((hut[0] + 1, hut[1] + 1, hut[2] - 1, hut[3] - 1), "M", 1)
    p.in_rect((compound[0] + 1, compound[1] + 1, compound[2] - 1, compound[3] - 1), "W", 2)

    # The boss stands by the tower; armoured guards around the compound.
    p.at(tower[0] + 6, tower[1] + 2, "B")
    p.in_rect((compound[0] + 1, compound[1] + 1, compound[2] - 1, compound[3] - 1), "A", 3)

    # Zombies, zone by zone.
    plan = {
        "houses": [("Z", 34), ("R", 6), ("G", 2)],
        "school": [("Z", 20), ("R", 5), ("G", 2)],
        "pitch": [("Z", 12)],
        "gas": [("Z", 16), ("R", 4), ("A", 3)],
        "market": [("Z", 14), ("R", 3), ("G", 1)],
        "paddies": [("Z", 12), ("R", 3)],
        "hill": [("Z", 14), ("R", 4), ("A", 2), ("G", 1)],
    }
    for zone, items in plan.items():
        for (s, n) in items:
            p.in_zone(zone, s, n)
    # Stragglers on the roads.
    p.in_rect((2, ROAD_TOP, W - 3, ROAD_BOTTOM), "Z", 22, ground=ROAD)
    p.in_rect((2, ROAD_TOP, W - 3, ROAD_BOTTOM), "R", 4, ground=ROAD)
    return spawn, p.counts


# ---------------------------------------------------------------------------------------
# Checks and output
# ---------------------------------------------------------------------------------------

def unreachable(g, spawn):
    seen = [[False] * W for _ in range(H)]
    q = deque([spawn])
    seen[spawn[1]][spawn[0]] = True
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < W and 0 <= ny < H and not seen[ny][nx] and g.c[ny][nx] != WALL:
                seen[ny][nx] = True
                q.append((nx, ny))
    bad = []
    for y in range(H):
        for x in range(W):
            s = g.c[y][x]
            if s not in GROUND and s != WALL and not seen[y][x]:
                bad.append((s, x, y))
    return bad


PREVIEW_COLORS = {
    WALL: (58, 52, 48), FLOOR: (128, 124, 116), GRASS: (78, 104, 62), ROAD: (60, 62, 66),
    DIRT: (122, 98, 70), PADDY: (74, 104, 96),
    "C": (210, 150, 60), "M": (235, 235, 240), "S": (140, 200, 80), "W": (60, 120, 60), "V": (90, 110, 170),
    "Z": (200, 40, 40), "R": (240, 120, 40), "G": (170, 70, 210), "A": (150, 160, 180), "B": (255, 0, 0),
    "E": (60, 230, 110), "J": (240, 220, 40), "U": (240, 120, 200), "X": (255, 255, 0), "P": (40, 140, 255),
}


def preview(g, path, scale=4):
    try:
        from PIL import Image
    except ImportError:
        print("Chua co Pillow, bo qua anh xem truoc (pip install pillow).")
        return
    img = Image.new("RGB", (W * scale, H * scale))
    px = img.load()
    for y in range(H):
        for x in range(W):
            color = PREVIEW_COLORS.get(g.c[y][x], (255, 0, 255))
            big = g.c[y][x] not in GROUND and g.c[y][x] != WALL
            for yy in range(scale):
                for xx in range(scale):
                    if big or True:
                        px[x * scale + xx, y * scale + yy] = color
    # Make markers easier to see: draw them as bigger dots.
    for y in range(H):
        for x in range(W):
            s = g.c[y][x]
            if s in GROUND or s == WALL:
                continue
            r = 4 if s in "EJUXPB" else 2
            for yy in range(-r, r + 1):
                for xx in range(-r, r + 1):
                    X, Y = x * scale + scale // 2 + xx, y * scale + scale // 2 + yy
                    if 0 <= X < W * scale and 0 <= Y < H * scale and xx * xx + yy * yy <= r * r:
                        px[X, Y] = PREVIEW_COLORS.get(s, (255, 0, 255))
    img.save(path)


def build(seed):
    rng = random.Random(seed)
    g = Grid(rng)
    roads(g)
    residential(g, rng)
    school_parts = school(g, rng)
    gas_parts = gas_station(g, rng)
    huts = paddies(g, rng)
    market_parts = market(g, rng)
    hill_parts = hill(g, rng)
    scenery(g, rng)
    roads_again = [(x, y) for y in range(H) for x in range(W)]
    # The map border.
    for x in range(W):
        g.set(x, 0, WALL)
        g.set(x, H - 1, WALL)
    for y in range(H):
        g.set(0, y, WALL)
        g.set(W - 1, y, WALL)
    spawn, counts = place_markers(g, rng, school_parts, gas_parts, huts, market_parts, hill_parts)
    return g, spawn, counts


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--seed", type=int, default=20261120)
    parser.add_argument("--out", default=MAP_PATH)
    parser.add_argument("--preview", default=PREVIEW_PATH)
    args = parser.parse_args()

    g, spawn, counts = build(args.seed)
    bad = unreachable(g, spawn)
    if bad:
        print("Co %d vi tri khong toi duoc:" % len(bad))
        for item in bad[:20]:
            print("  %s o cot %d, hang %d" % item)
        sys.exit(1)

    text = HEADER + "\n".join("".join(row) for row in g.c) + "\n"
    with open(args.out, "w", newline="\n", encoding="utf-8") as handle:
        handle.write(text)
    preview(g, args.preview)
    print("Da ghi %s (%dx%d)" % (args.out, W, H))
    print("Dem:", ", ".join("%s=%d" % kv for kv in sorted(counts.items())))


if __name__ == "__main__":
    main()
