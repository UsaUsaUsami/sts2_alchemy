"""Draw the mod's own pieces for the alchemist's combat rig (NecroRig.cs): the long staff that replaces the
Necrobinder's scythe, the gold sigil on the face and the robe's element band. Everything here is drawn from shapes in
this script; nothing of the base game is copied. Each file is one atlas part, upright, at the part's packed size.

Usage: python scripts/necro-rig-art.py   -> assets/art/character/rig/*.png
The pole line was measured on the scythe part (docs/research.md 2026-10-01): it runs from the top-left to the
bottom-right corner, x = x0 + (y - y0) * slope.
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "assets/art/character/rig"
OUT.mkdir(parents=True, exist_ok=True)
S = 4  # supersampling
GOLD, GOLD_HI, GOLD_LO = (226, 181, 82, 255), (255, 232, 160, 255), (138, 96, 30, 255)
WOOD, WOOD_HI = (52, 36, 24, 255), (104, 74, 48, 255)


def canvas(w, h):
    return Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))


def save(im, name):
    im.resize((im.width // S, im.height // S), Image.LANCZOS).save(OUT / f"{name}.png")


def staff(name, size, x0, y0, slope, ring_at, ring, bottom_y, dark=False):
    """The pole on the scythe's pole line, joined to a brass ring that sits in the blade's thick heel. The game draws
    the part as a mesh shaped like the scythe, so everything must stay inside that outline (measured upright: the
    heel spans about x 6-40 at y 64-104 on the 215x434 part); the crescent tip is left empty."""
    w, h = size
    im = canvas(w, h)
    d = ImageDraw.Draw(im)
    px = lambda y: (x0 + (y - y0) * slope) * S
    width = max(2.5, w / 60) * S
    pole_top = ring_at[1] + ring * 0.9
    a, b = (px(pole_top), pole_top * S), (px(bottom_y), bottom_y * S)
    cx, cy, r = ring_at[0] * S, ring_at[1] * S, ring * S
    shade = (10, 8, 6, 255)
    d.line([a, b], fill=shade if dark else WOOD, width=int(width))
    d.line([(cx + r * 0.6, cy + r * 0.6), a], fill=shade if dark else GOLD, width=int(width * 1.1))  # the collar
    if not dark:
        d.line([(a[0] - width * 0.25, a[1]), (b[0] - width * 0.25, b[1])], fill=WOOD_HI, width=max(1, int(width * 0.3)))
        for t in (0.12, 0.5):  # brass bands on the pole
            y = pole_top + (bottom_y - pole_top) * t
            d.line([(px(y) - width, (y - 3) * S), (px(y) + width, (y + 3) * S)], fill=GOLD, width=int(width * 0.9))
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=shade if dark else GOLD, width=int(max(2, ring / 3.5) * S))
    if not dark:
        d.ellipse([cx - r * 0.72, cy - r * 0.72, cx + r * 0.72, cy + r * 0.72], outline=GOLD_LO, width=S)
        tri = [(cx + r * 0.6 * math.cos(math.radians(t)), cy + r * 0.6 * math.sin(math.radians(t))) for t in (-90, 30, 150)]
        d.polygon(tri, outline=GOLD_HI, width=int(1.5 * S))
        d.ellipse([cx - r * 0.16, cy - r * 0.16, cx + r * 0.16, cy + r * 0.16], fill=GOLD_HI)
    save(im, name)


staff("scythe", (215, 434), 67, 120, 0.4661, (25, 84), 13, 432)
staff("sythe_dissolve", (213, 432), 65, 120, 0.466, (25, 84), 13, 430, dark=True)
staff("scythe_glow", (120, 229), 64.5, 120, 0.475, (14, 45), 7, 228, dark=True)

# Face: the gold alchemical sigil of the key visual (circle, triangle, dot, short rays) on the dark hood face.
im = canvas(47, 50)
d = ImageDraw.Draw(im)
cx, cy, r = 30 * S, 24 * S, 9.5 * S
d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=GOLD_HI, width=int(1.4 * S))
tri = [(cx + r * 0.75 * math.cos(math.radians(a)), cy + r * 0.75 * math.sin(math.radians(a))) for a in (-90, 30, 150)]
d.polygon(tri, outline=GOLD_HI, width=int(1.2 * S))
d.ellipse([cx - r * 0.22, cy + r * 0.02, cx + r * 0.38, cy + r * 0.42], outline=GOLD_HI, width=S)
for ang in (-90, -60, -120, 90):
    c, s = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    d.line([(cx + c * r * 1.2, cy + s * r * 1.2), (cx + c * r * 1.45, cy + s * r * 1.45)], fill=GOLD, width=S)
save(im, "head")

# Robe band (the Necrobinder's skirt flap): gold borders and the four element marks, top to bottom.
w, h = 25, 127
im = canvas(w, h)
d = ImageDraw.Draw(im)
for x in (6, 19):
    d.line([(x * S, 4 * S), ((x - 1) * S, (h - 6) * S)], fill=GOLD, width=int(1.3 * S))
cx = 12.5 * S
def mark(y, kind):
    y *= S
    k = 4.2 * S
    if kind == "earth":
        d.polygon([(cx, y - k), (cx + k, y + k * 0.8), (cx - k, y + k * 0.8)], outline=GOLD_HI, width=S)
    elif kind == "water":
        d.polygon([(cx, y - k), (cx + k * 0.7, y + k * 0.3), (cx, y + k), (cx - k * 0.7, y + k * 0.3)], outline=GOLD_HI, width=S)
    elif kind == "fire":
        d.line([(cx - k * 0.6, y + k), (cx, y - k), (cx + k * 0.6, y + k)], fill=GOLD_HI, width=S)
        d.line([(cx - k * 0.2, y + k), (cx, y), (cx + k * 0.2, y + k)], fill=GOLD_HI, width=S)
    else:
        d.arc([cx - k, y - k, cx + k, y + k], 0, 300, fill=GOLD_HI, width=S)
        d.arc([cx - k * 0.45, y - k * 0.45, cx + k * 0.45, y + k * 0.45], 120, 420, fill=GOLD_HI, width=S)
for y, kind in ((22, "earth"), (46, "water"), (70, "fire"), (94, "air")):
    mark(y, kind)
d.line([(cx, 104 * S), (cx, 118 * S)], fill=GOLD, width=S)
save(im, "skirt_flap")
print("written:", sorted(p.name for p in OUT.glob("*.png")))
