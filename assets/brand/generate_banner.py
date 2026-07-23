# /// script
# requires-python = ">=3.10"
# dependencies = ["fonttools>=4.50", "uharfbuzz>=0.45"]
# ///
"""Generate the Polymerium README banners (banner-{dark,light}{,.zh}.svg).

Usage: uv run generate_banner.py

Text is shaped with Geist (Latin) and Noto Sans SC (Chinese tagline), both
downloaded from Google Fonts into the system temp dir on first run, and
emitted as vector paths, so the banners render identically everywhere with
no font dependency.
"""

import re
import tempfile
import urllib.request
from pathlib import Path

import uharfbuzz as hb
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen

HERE = Path(__file__).parent
FONT_DIR = Path(tempfile.gettempdir()) / "polymerium-geist-fonts"
FONT_URLS = {
    400: "https://fonts.gstatic.com/s/geist/v5/gyBhhwUxId8gMGYQMKR3pzfaWI_RnOM4nQ.ttf",
    500: "https://fonts.gstatic.com/s/geist/v5/gyBhhwUxId8gMGYQMKR3pzfaWI_RruM4nQ.ttf",
    600: "https://fonts.gstatic.com/s/geist/v5/gyBhhwUxId8gMGYQMKR3pzfaWI_RQuQ4nQ.ttf",
}
CJK_FONT_URL = "https://fonts.gstatic.com/s/notosanssc/v40/k3kCo84MPvpLmixcA63oeAL7Iqp5IZJF9bmaG9_FnYw.ttf"
CJK_FONT_NAME = "NotoSansSC-400.ttf"

GOLD = "#EEA93B"
BROWN = "#3A2E1C"
OFFWHITE = "#E9E4D8"

WIDTH, HEIGHT = 1280, 640
TEXT_X = 104
WORDMARK = ("Polymerium.", 600, 112, 292, -0.02)
EYEBROW = ("MINECRAFT LAUNCHER", 500, 21, 190, 0.28)
TAGLINE = [
    ("Your instance is a recipe,", 400, 38, 384),
    ("not a pile of copied files.", 400, 38, 442),
]
TAGLINE_ZH = [
    ("实例是一份可编辑的配置，", 400, 38, 384),
    ("不是一堆复制的文件。", 400, 38, 442),
]
LOGO_TRANSFORM = "translate(1155 308) rotate(45) scale(4.6)"  # incl. 0.95 brand scale

LOGO_DEFS = """\
    <rect id="link" x="-30" y="-30" width="60" height="60" rx="16" fill="none" stroke-width="12"/>
    <clipPath id="weave">
      <rect x="24" y="-10" width="12" height="12"/>
    </clipPath>"""


def logo_group(neutral: str, transform: str, opacity: float | None = None) -> str:
    attr = f' opacity="{opacity}"' if opacity is not None else ""
    return f"""<g transform="{transform}"{attr}>
    <g transform="translate(-17 17)">
      <use href="#link" stroke="{neutral}"/>
    </g>
    <g transform="translate(17 -17)">
      <use href="#link" stroke="{GOLD}"/>
    </g>
    <g transform="translate(-17 17)">
      <g clip-path="url(#weave)">
        <use href="#link" stroke="{neutral}"/>
      </g>
    </g>
  </g>"""


def ensure_fonts() -> None:
    FONT_DIR.mkdir(parents=True, exist_ok=True)
    for weight, url in FONT_URLS.items():
        target = FONT_DIR / f"Geist-{weight}.ttf"
        if not target.exists():
            print(f"downloading Geist {weight}")
            urllib.request.urlretrieve(url, target)
    cjk = FONT_DIR / CJK_FONT_NAME
    if not cjk.exists():
        print("downloading Noto Sans SC 400")
        urllib.request.urlretrieve(CJK_FONT_URL, cjk)


def round_d(d: str) -> str:
    return re.sub(r"-?\d+\.\d+", lambda m: f"{float(m.group(0)):.1f}", d)


def shape_line(font_path: Path, text: str, size: float, tracking_em: float = 0.0):
    """Shape one line; return list of (path_d, x_advance_in_font_units, cluster)."""
    blob = hb.Blob.from_file_path(str(font_path))
    face = hb.Face(blob)
    hb_font = hb.Font(face)
    tt = TTFont(str(font_path))
    upm = tt["head"].unitsPerEm
    hb_font.scale = (upm, upm)
    buf = hb.Buffer()
    buf.add_str(text)
    buf.guess_segment_properties()
    hb.shape(hb_font, buf, {"kern": True, "liga": True})
    glyph_order = tt.getGlyphOrder()
    glyph_set = tt.getGlyphSet()
    tracking_units = tracking_em * size / size * upm * (size / upm)  # resolves to tracking_em * upm... keep explicit below
    tracking_units = tracking_em * upm
    out = []
    pen_x = 0.0
    for info, pos in zip(buf.glyph_infos, buf.glyph_positions):
        name = glyph_order[info.codepoint]
        pen = SVGPathPen(glyph_set)
        try:
            glyph_set[name].draw(pen)
        except Exception:
            pen.getCommands()  # space and friends: no outline
        d = round_d(pen.getCommands())
        out.append((d, pen_x + pos.x_offset, info.cluster))
        pen_x += pos.x_advance + tracking_units
    return out, upm


def text_paths(font_path: Path, text: str, size: float, x: float, y: float,
               fill: str, tracking_em: float = 0.0, split_last: str | None = None) -> str:
    """Emit one <path> per glyph, baseline at (x, y). If split_last is set,
    glyphs whose cluster belongs to that trailing character use split fill."""
    glyphs, upm = shape_line(font_path, text, size, tracking_em)
    s = size / upm
    last_cluster = len(text) - 1 if split_last else -1
    parts = []
    for d, gx, cluster in glyphs:
        if not d:
            continue
        color = GOLD if cluster == last_cluster else fill
        parts.append(
            f'<path transform="translate({x + gx * s:.2f} {y:.2f}) scale({s:.4f} {-s:.4f})" '
            f'd="{d}" fill="{color}"/>'
        )
    return "\n  ".join(parts)


def banner(dark: bool, zh: bool = False) -> str:
    if dark:
        bg_top, bg_bottom = "#1E1B16", "#131110"
        wordmark_fill, tagline_fill, eyebrow_fill = "#F2EDE3", "#A39E93", GOLD
        logo_neutral = OFFWHITE
        glow_opacity, texture_opacity = "0.17", 0.05
        edge = 'stroke="#FFFFFF" stroke-opacity="0.07"'
    else:
        bg_top, bg_bottom = "#FCFAF5", "#F3EFE5"
        wordmark_fill, tagline_fill, eyebrow_fill = BROWN, "#857A68", "#A0731B"
        logo_neutral = BROWN
        glow_opacity, texture_opacity = "0.13", 0.06
        edge = 'stroke="#3A2E1C" stroke-opacity="0.10"'

    font600 = FONT_DIR / "Geist-600.ttf"
    font500 = FONT_DIR / "Geist-500.ttf"
    font400 = FONT_DIR / "Geist-400.ttf"

    eyebrow_text, ew, es, ey, et = EYEBROW
    wm_text, ww, ws, wy, wt = WORDMARK
    blocks = [
        text_paths(font500, eyebrow_text, es, TEXT_X, ey, eyebrow_fill, et),
        text_paths(font600, wm_text, ws, TEXT_X, wy, wordmark_fill, wt, split_last="."),
    ]
    for line, weight, size, baseline in (TAGLINE_ZH if zh else TAGLINE):
        font = FONT_DIR / (CJK_FONT_NAME if zh else f"Geist-{weight}.ttf")
        blocks.append(text_paths(font, line, size, TEXT_X, baseline, tagline_fill))

    return f"""<svg width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{bg_top}"/>
      <stop offset="1" stop-color="{bg_bottom}"/>
    </linearGradient>
    <radialGradient id="glow" cx="0.89" cy="0.49" r="0.62">
      <stop offset="0" stop-color="{GOLD}" stop-opacity="{glow_opacity}"/>
      <stop offset="1" stop-color="{GOLD}" stop-opacity="0"/>
    </radialGradient>
{LOGO_DEFS}
  </defs>
  <rect width="{WIDTH}" height="{HEIGHT}" fill="url(#bg)"/>
  <rect width="{WIDTH}" height="{HEIGHT}" fill="url(#glow)"/>
  <rect x="0.5" y="0.5" width="{WIDTH - 1}" height="{HEIGHT - 1}" fill="none" {edge}/>
  {logo_group(GOLD, "translate(-30 730) rotate(45) scale(3.23)", opacity=texture_opacity)}
  {logo_group(logo_neutral, LOGO_TRANSFORM)}
  {blocks[0]}
  {blocks[1]}
  {blocks[2]}
  {blocks[3]}
</svg>
"""


def main() -> None:
    ensure_fonts()
    for dark in (True, False):
        for zh in (False, True):
            name = f"banner-{'dark' if dark else 'light'}{'.zh' if zh else ''}.svg"
            (HERE / name).write_text(banner(dark=dark, zh=zh))
    print("wrote banner-{dark,light}{,.zh}.svg")


if __name__ == "__main__":
    main()
