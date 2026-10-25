# /// script
# requires-python = ">=3.10"
# dependencies = ["pillow==13.0.0"]
# ///
# Generate the Icon.App.* assets from the voxel fish-tank model.
#
#   uv run scripts/VoxelIcon/export.py [output-dir]
#
# Requires rsvg-convert (librsvg) and, for the .icns, macOS iconutil.
# Icon.App.png and Icon.App.ico stay transparent; only Icon.App.icns carries a backplate, since
# macOS boxes non-squircle artwork into a system plate otherwise.

import math
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from render import build_svg  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "src" / "Polymerium.Avalonia" / "Assets"

SMALL_MAX = 48  # sizes up to this use the variant without highlight streaks
ICO_SIZES = [16, 24, 32, 48, 64, 128, 256]
ICNS_SIZES = [16, 32, 128, 256, 512]

# macOS plate on the 1024 grid: 824 body centered, continuous-corner superellipse.
CANVAS = 1024
BODY = 824
TANK = 640
PLATE_TOP = "#2B2F36"
PLATE_BOTTOM = "#16181C"


def superellipse(cx, cy, r, n=5, steps=256):
    pts = []
    for i in range(steps):
        t = 2 * math.pi * i / steps
        c, s = math.cos(t), math.sin(t)
        x = cx + r * math.copysign(abs(c) ** (2 / n), c)
        y = cy + r * math.copysign(abs(s) ** (2 / n), s)
        pts.append(f"{x:.2f},{y:.2f}")
    return "M" + " L".join(pts) + " Z"


def plated(tank_svg):
    off = (CANVAS - TANK) / 2
    inner = re.sub(r"^<svg ", f'<svg x="{off}" y="{off}" width="{TANK}" height="{TANK}" ', tank_svg, count=1)
    plate = superellipse(CANVAS / 2, CANVAS / 2, BODY / 2)
    return "\n".join(
        [
            f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {CANVAS} {CANVAS}">',
            "<defs>",
            '<linearGradient id="plate" x1="0" y1="0" x2="0" y2="1">',
            f'<stop offset="0" stop-color="{PLATE_TOP}"/><stop offset="1" stop-color="{PLATE_BOTTOM}"/>',
            "</linearGradient>",
            '<filter id="shadow" x="-20%" y="-20%" width="140%" height="140%">',
            '<feDropShadow dx="0" dy="10" stdDeviation="14" flood-color="#000" flood-opacity="0.35"/>',
            "</filter>",
            "</defs>",
            f'<path d="{plate}" fill="url(#plate)" filter="url(#shadow)"/>',
            inner,
            "</svg>",
        ]
    )


def rasterize(svg_path, size, out_path):
    subprocess.run(
        ["rsvg-convert", "-w", str(size), "-h", str(size), "-o", str(out_path), str(svg_path)],
        check=True,
    )
    return out_path


def main(out_dir):
    out_dir.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        def write(name, svg):
            path = tmp / f"{name}.svg"
            path.write_text(svg)
            return path

        full_svg, small_svg = build_svg(with_highlights=True), build_svg(with_highlights=False)
        full, small = write("full", full_svg), write("small", small_svg)
        plate_full, plate_small = write("plate_full", plated(full_svg)), write("plate_small", plated(small_svg))

        rasterize(full, 1024, out_dir / "Icon.App.png")

        frames = [
            Image.open(rasterize(small if s <= SMALL_MAX else full, s, tmp / f"ico_{s}.png")).convert("RGBA")
            for s in ICO_SIZES
        ]
        largest = frames[-1]
        largest.save(
            out_dir / "Icon.App.ico",
            format="ICO",
            sizes=[(s, s) for s in ICO_SIZES],
            append_images=frames[:-1],
            bitmap_format="bmp",
        )

        if shutil.which("iconutil"):
            iconset = tmp / "Icon.App.iconset"
            iconset.mkdir()
            for s in ICNS_SIZES:
                for scale, suffix in ((1, ""), (2, "@2x")):
                    px = s * scale
                    src = plate_small if px <= SMALL_MAX else plate_full
                    rasterize(src, px, iconset / f"icon_{s}x{s}{suffix}.png")
            subprocess.run(
                ["iconutil", "-c", "icns", str(iconset), "-o", str(out_dir / "Icon.App.icns")],
                check=True,
            )
        else:
            print("iconutil not found; skipped Icon.App.icns", file=sys.stderr)


if __name__ == "__main__":
    main(Path(sys.argv[1]) if len(sys.argv) > 1 else ASSETS)
