#!/usr/bin/env python3
# Voxel fish-tank app icon renderer: voxel model -> isometric SVG. export.py turns it into
# the Icon.App.* assets.
#
# Axes: +x goes screen lower-right, +y goes screen lower-left, +z goes up.
# The camera sits at the (+x, +y, +z) corner, so only +x / +y / +z faces are ever visible.
#
# Glass has no edge material. Coplanar glass faces merge into one region per plane, and each
# region is drawn with its own fill-opacity, so where glass planes overlap on screen (rim,
# lip over shoulder, front wall over back wall) the tint stacks up. Thickness reads as density,
# not as outlines. Form comes from per-orientation tint: bright tops, mid left, cool right.
#
# Draw order: back glass -> water -> fish -> front glass -> highlights.

import math
import sys
from collections import defaultdict
from pathlib import Path

S = 16  # pixels per voxel edge
COS30 = math.cos(math.radians(30))

# Tank
W = 14  # outer size along x and y
WALL_TOP = 11  # walls occupy z = 1..WALL_TOP
SHOULDER_Z = 12
NECK_Z = (13, 14)
LIP_Z = 15
HOLE = (4, 9)  # opening spans x, y in [4, 9]
NECK = (3, 10)  # neck ring outer bound
LIP = (2, 11)  # lip outer bound
WATER_TOP = 10

# Fish profile in the y-z plane, seen through the right (+x) face.
# Rows from top (z = FISH_TOP) down; columns from tail (y = 1) to nose (y = 12).
# B body, S stripe, D fin, T tail, E eye
FISH = [
    "......DD....",
    "TT..BBSDBB..",
    "TTTBBBSBBBB.",
    "TTTBBBSBBSBB",
    ".TTBBBSBBSEB",
    ".TTBBBSBBSBB",
    "TTTBBBSBBSBB",
    "TTTBBBSBBBB.",
    "TT..BBSBBB..",
    ".......DD...",
]
FISH_TOP = 10
FISH_X_BODY = (5, 6, 7)  # body thickness along x, centered between the x walls
FISH_X_FIN = (6,)  # fins and tail are one voxel thin

FISH_COLORS = {
    "B": "#F2AE40",
    "S": "#FAF6EC",
    "D": "#D2601E",
    "T": "#E5821F",
    "E": "#1A1410",
}
# Glass tint and fill-opacity per visible face orientation.
GLASS = {"z": ("#EAFBFB", 0.5), "y": ("#A6DCDF", 0.22), "x": ("#86C4CB", 0.26)}
HIGHLIGHT = ("#FFFFFF", 0.6)
WATER = ("#3F76E4", 0.78)
SHADE = {"z": 1.0, "y": 0.84, "x": 0.68}  # top, left (+y), right (+x) for opaque materials


def build_glass():
    g = set()
    for x in range(W):
        for y in range(W):
            g.add((x, y, 0))
            if x in (0, W - 1) or y in (0, W - 1):
                for z in range(1, WALL_TOP + 1):
                    g.add((x, y, z))
            in_hole = HOLE[0] <= x <= HOLE[1] and HOLE[0] <= y <= HOLE[1]
            if not in_hole:
                g.add((x, y, SHOULDER_Z))
            in_neck = NECK[0] <= x <= NECK[1] and NECK[0] <= y <= NECK[1]
            if in_neck and not in_hole:
                for z in NECK_Z:
                    g.add((x, y, z))
            in_lip = LIP[0] <= x <= LIP[1] and LIP[0] <= y <= LIP[1]
            if in_lip and not in_hole:
                g.add((x, y, LIP_Z))
    return g


def build_fish():
    f = {}
    for r, row in enumerate(FISH):
        z = FISH_TOP - r
        for c, ch in enumerate(row):
            if ch == ".":
                continue
            xs = FISH_X_FIN if ch in "DT" else FISH_X_BODY
            for x in xs:
                f[(x, c + 1, z)] = ch
    return f


def highlights():
    h = set()
    # Diagonal streaks on the left (y = W-1) and right (x = W-1) faces.
    for k in range(4):
        h.add((2 + k, W - 1, 4 + k))
    for k in range(2):
        h.add((2 + k, W - 1, 2 + k))
    for k in range(4):
        h.add((W - 1, 4 - k, 7 + k))
    return h


def project(p):
    x, y, z = p
    return ((x - y) * COS30 * S, (x + y) * 0.5 * S - z * S)


def faces(v, solid):
    # Visible faces as (axis, plane, quad); quads of one axis share winding so shared edges cancel.
    x, y, z = v
    out = []
    if (x, y, z + 1) not in solid:
        out.append(("z", z + 1, [(x, y, z + 1), (x + 1, y, z + 1), (x + 1, y + 1, z + 1), (x, y + 1, z + 1)]))
    if (x + 1, y, z) not in solid:
        out.append(("x", x + 1, [(x + 1, y, z), (x + 1, y + 1, z), (x + 1, y + 1, z + 1), (x + 1, y, z + 1)]))
    if (x, y + 1, z) not in solid:
        out.append(("y", y + 1, [(x, y + 1, z), (x + 1, y + 1, z), (x + 1, y + 1, z + 1), (x, y + 1, z + 1)]))
    return out


def merge(quads):
    # Union coplanar unit quads by cancelling shared edges, then chain the boundary into loops.
    edges = set()
    for q in quads:
        for i in range(4):
            a, b = q[i], q[(i + 1) % 4]
            if (b, a) in edges:
                edges.remove((b, a))
            else:
                edges.add((a, b))
    nxt = defaultdict(list)
    for a, b in edges:
        nxt[a].append(b)
    loops = []
    while nxt:
        start = next(iter(nxt))
        loop, cur = [start], start
        while True:
            b = nxt[cur].pop()
            if not nxt[cur]:
                del nxt[cur]
            if b == start:
                break
            loop.append(b)
            cur = b
        loops.append(loop)
    return loops


def shade(hex_color, k):
    r, g, b = (int(hex_color[i : i + 2], 16) for i in (1, 3, 5))
    return "#{:02X}{:02X}{:02X}".format(*(round(c * k) for c in (r, g, b)))


class Canvas:
    def __init__(self):
        self.items = []
        self.pts = []

    def _path(self, loops):
        parts = []
        for loop in loops:
            pts = [project(p) for p in loop]
            self.pts.extend(pts)
            parts.append("M" + " L".join(f"{px:.2f},{py:.2f}" for px, py in pts) + " Z")
        return " ".join(parts)

    def regions(self, voxels, solid, style_of):
        # One merged region per (axis, plane, style), each with its own fill-opacity.
        buckets = defaultdict(list)
        for v in voxels:
            for axis, plane, quad in faces(v, solid):
                buckets[(axis, plane, style_of(v, axis))].append(quad)
        regs = []
        for (axis, plane, (color, alpha)), quads in buckets.items():
            depth = sum(sum(p) for q in quads for p in q) / (4 * len(quads))
            regs.append((depth, color, alpha, merge(quads)))
        for _, color, alpha, loops in sorted(regs, key=lambda r: r[0]):
            self.items.append(f'<path d="{self._path(loops)}" fill="{color}" fill-opacity="{alpha}"/>')

    def solid_group(self, voxels, solid, color_of, opacity):
        # Opaque, depth-sorted faces under one group opacity: seamless, no internal edges.
        polys = []
        for v in sorted(voxels, key=lambda v: (sum(v), v[2])):
            for axis, _, quad in faces(v, solid):
                pts = [project(p) for p in quad]
                self.pts.extend(pts)
                color = shade(color_of(v), SHADE[axis])
                d = " ".join(f"{px:.2f},{py:.2f}" for px, py in pts)
                polys.append(f'<polygon points="{d}" fill="{color}" stroke="{color}"/>')
        self.items.append(f'<g opacity="{opacity}" stroke-width="0.6" stroke-linejoin="round">')
        self.items.extend(polys)
        self.items.append("</g>")

    def svg(self, pad=S):
        xs = [p[0] for p in self.pts]
        ys = [p[1] for p in self.pts]
        x0, y0 = min(xs) - pad, min(ys) - pad
        w, h = max(xs) - min(xs) + 2 * pad, max(ys) - min(ys) + 2 * pad
        side = max(w, h)
        x0 -= (side - w) / 2
        y0 -= (side - h) / 2
        head = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{x0:.2f} {y0:.2f} {side:.2f} {side:.2f}">'
        return "\n".join([head, *self.items, "</svg>"])


def build_svg(with_highlights=True):
    # Small icon sizes drop the highlight streaks; at a few pixels per voxel they read as noise.
    glass = build_glass()
    hl = highlights() & glass if with_highlights else set()
    fish = build_fish()
    water = {
        (x, y, z)
        for x in range(1, W - 1)
        for y in range(1, W - 1)
        for z in range(1, WATER_TOP + 1)
    }

    def is_back(v):
        x, y, z = v
        return z < SHOULDER_Z and (x == 0 or y == 0 or z == 0)

    c = Canvas()
    c.regions({v for v in glass if is_back(v)}, glass, lambda v, axis: GLASS[axis])
    c.solid_group(water, water, lambda v: WATER[0], WATER[1])
    c.solid_group(set(fish), set(fish), lambda v: FISH_COLORS[fish[v]], 1)
    c.regions({v for v in glass if not is_back(v)}, glass, lambda v, axis: GLASS[axis])
    if hl:
        c.regions(hl, glass, lambda v, axis: HIGHLIGHT)
    return c.svg()


if __name__ == "__main__":
    Path(sys.argv[1] if len(sys.argv) > 1 else "icon.svg").write_text(build_svg())
