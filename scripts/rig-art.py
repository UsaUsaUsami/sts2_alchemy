"""Draw the mod's own pieces for the alchemist's combat rig (NecroRig.cs), per donor rig: for the Necrobinder the long
staff that replaces the scythe, the gold sigil on the face and the robe's element band; for the Silent the sigil and
the band (on its grey hanging cloth). Everything here is drawn from shapes in
this script; nothing of the base game is copied. Each file is one atlas part, upright, at the part's packed size.

Usage: python scripts/rig-art.py   -> assets/art/character/rig/<donor>/*.png
The pole line was measured on the scythe part (docs/research.md 2026-10-01): it runs from the top-left to the
bottom-right corner, x = x0 + (y - y0) * slope.
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
BASE = ROOT / "assets/art/character/rig"
OUT = BASE / "necrobinder"
S = 4  # supersampling
GOLD, GOLD_HI, GOLD_LO = (226, 181, 82, 255), (255, 232, 160, 255), (138, 96, 30, 255)
WOOD, WOOD_HI = (52, 36, 24, 255), (104, 74, 48, 255)


def canvas(w, h):
    return Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))


def save(im, name):
    OUT.mkdir(parents=True, exist_ok=True)
    im.resize((im.width // S, im.height // S), Image.LANCZOS).save(OUT / f"{name}.png")


def staff(name, size, x0, y0, slope, ring_at, ring, bottom_y, dark=False, head=True):
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
    if not head:  # the big ring rides on the bone instead (staff_head); the pole runs on to the top of the heel
        top = (px(56), 56 * S)
        d.line([top, a], fill=WOOD, width=int(width))
        d.line([(top[0] - width, top[1] - 2 * S), (top[0] + width, top[1] + 2 * S)], fill=GOLD, width=int(width * 1.2))
        a = top
    else:
        d.line([(cx + r * 0.6, cy + r * 0.6), a], fill=shade if dark else GOLD, width=int(width * 1.1))  # the collar
    if not dark:
        d.line([(a[0] - width * 0.25, a[1]), (b[0] - width * 0.25, b[1])], fill=WOOD_HI, width=max(1, int(width * 0.3)))
        for t in (0.12, 0.5):  # brass bands on the pole
            y = pole_top + (bottom_y - pole_top) * t
            d.line([(px(y) - width, (y - 3) * S), (px(y) + width, (y + 3) * S)], fill=GOLD, width=int(width * 0.9))
    if not head:
        save(im, name)
        return
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=shade if dark else GOLD, width=int(max(2, ring / 3.5) * S))
    if not dark:
        d.ellipse([cx - r * 0.72, cy - r * 0.72, cx + r * 0.72, cy + r * 0.72], outline=GOLD_LO, width=S)
        tri = [(cx + r * 0.6 * math.cos(math.radians(t)), cy + r * 0.6 * math.sin(math.radians(t))) for t in (-90, 30, 150)]
        d.polygon(tri, outline=GOLD_HI, width=int(1.5 * S))
        d.ellipse([cx - r * 0.16, cy - r * 0.16, cx + r * 0.16, cy + r * 0.16], fill=GOLD_HI)
    save(im, name)


staff("scythe", (215, 434), 67, 120, 0.4661, (25, 84), 13, 432, head=False)
# The dissolving scythe (death) is the plain staff too; the big ring rides on the bone only with "scythe" (2026-10-02).
staff("sythe_dissolve", (213, 432), 65, 120, 0.466, (25, 84), 13, 430, head=False)
# scythe_glow (the glowing copy in the big cast) has no drawing: the part is dropped, the cast's sparks stay.
(OUT / "scythe_glow.png").unlink(missing_ok=True)

def sigil(name, size, centre, radius):
    """The gold alchemical sigil of the key visual (circle, triangle, dot, short rays) for the dark hood face."""
    im = canvas(*size)
    d = ImageDraw.Draw(im)
    cx, cy, r = centre[0] * S, centre[1] * S, radius * S
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=GOLD_HI, width=int(1.4 * S))
    tri = [(cx + r * 0.75 * math.cos(math.radians(a)), cy + r * 0.75 * math.sin(math.radians(a))) for a in (-90, 30, 150)]
    d.polygon(tri, outline=GOLD_HI, width=int(1.2 * S))
    d.ellipse([cx - r * 0.22, cy + r * 0.02, cx + r * 0.38, cy + r * 0.42], outline=GOLD_HI, width=S)
    for ang in (-90, -60, -120, 90):
        c, s_ = math.cos(math.radians(ang)), math.sin(math.radians(ang))
        d.line([(cx + c * r * 1.2, cy + s_ * r * 1.2), (cx + c * r * 1.45, cy + s_ * r * 1.45)], fill=GOLD, width=S)
    save(im, name)


def band(name, size, left, right, marks_y, mark=4.2):
    """The robe band: two gold borders and the four element marks (earth, water, fire, air) top to bottom."""
    w, h = size
    im = canvas(w, h)
    d = ImageDraw.Draw(im)
    for x in (left, right):
        d.line([(x * S, 4 * S), (x * S, (h - 6) * S)], fill=GOLD, width=int(1.3 * S))
    cx = (left + right) / 2 * S
    k = mark * S
    for y, kind in zip(marks_y, ("earth", "water", "fire", "air")):
        y *= S
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
    save(im, name)


sigil("head", (47, 50), (30, 24), 9.5)
band("skirt_flap", (25, 127), 6, 19, (22, 46, 70, 94))

def hood(name, w=200, h=230):
    """The key visual's hood for the Necrobinder rig, drawn upright (peak up and back, opening facing right). It
    rides on the head bone in front of the head; the opening is left empty so the dark face and its sigil show."""
    im = canvas(w, h)
    d = ImageDraw.Draw(im)
    P = lambda pts: [(x * S, y * S) for x, y in pts]
    outline = [(70, 8), (40, 40), (22, 90), (18, 140), (30, 190), (60, 222), (120, 228), (170, 212), (188, 180),
               (178, 150), (160, 128), (150, 96), (140, 60), (115, 30)]
    d.polygon(P(outline), fill=(30, 29, 35, 255))
    d.polygon(P([(70, 8), (40, 40), (26, 92), (24, 140), (40, 170), (60, 120), (70, 70)]), fill=(20, 19, 24, 255))  # shade
    d.polygon(P([(130, 205), (170, 212), (188, 180), (178, 150), (150, 180)]), fill=(46, 44, 52, 255))  # light fold
    opening = [118, 92, 186, 178]
    d.ellipse([v * S for v in opening], fill=(0, 0, 0, 0))
    d.arc([v * S for v in (114, 88, 190, 182)], 0, 360, fill=GOLD, width=int(3 * S))
    d.line(P([(60, 222), (120, 228), (170, 212)]), fill=GOLD, width=int(3 * S))
    save(im, name)


def ring_head(name, size=120):
    """A big brass sigil ring for the staff head (rides on the scythe bone, outside the scythe's own outline)."""
    im = canvas(size, size)
    d = ImageDraw.Draw(im)
    c, r = size / 2 * S, size * 0.42 * S
    d.ellipse([c - r, c - r, c + r, c + r], outline=GOLD, width=int(size / 14 * S))
    d.ellipse([c - r * 0.8, c - r * 0.8, c + r * 0.8, c + r * 0.8], outline=GOLD_LO, width=int(size / 60 * S))
    tri = [(c + r * 0.7 * math.cos(math.radians(a)), c + r * 0.7 * math.sin(math.radians(a))) for a in (-90, 30, 150)]
    d.polygon(tri, outline=GOLD_HI, width=int(size / 30 * S))
    d.ellipse([c - r * 0.16, c - r * 0.16, c + r * 0.16, c + r * 0.16], fill=GOLD_HI)
    for a in range(0, 360, 45):
        x, y = math.cos(math.radians(a)), math.sin(math.radians(a))
        d.line([(c + x * r * 1.05, c + y * r * 1.05), (c + x * r * 1.18, c + y * r * 1.18)], fill=GOLD_HI, width=int(size / 40 * S))
    save(im, name)


hood("hood")
ring_head("staff_head")

# The Silent (2026-10-01 trial): the face is the dark right half of its head part; the band on the hanging cloth.
OUT = BASE / "silent"
sigil("head", (31, 42), (21.5, 22), 6.5)
band("dress", (53, 156), 19, 35, (36, 64, 92, 120), mark=4.5)
print("written:", sorted(str(p.relative_to(BASE)) for p in BASE.rglob("*.png")))
