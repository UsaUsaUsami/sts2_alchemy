"""Cuts the parts sheet from scripts/rig-parts.py into one transparent PNG per part.

Usage: python scripts/rig-cut.py [--tag v1]
Reads assets/art/character/rig/alchemist/parts-<tag>.png, writes assets/art/character/rig/alchemist/parts/<PART>.png
and artifacts/rig-custom/cut-<tag>.png (every cut part on grey with its name, to check by eye).
The magenta background is keyed out (soft edge, magenta spill pulled back to grey); each connected shape is a part.
Parts are named by their place on the sheet: the top row left to right, then the bottom row, as rig-parts.py asks.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
DIR = ROOT / "assets/art/character/rig/alchemist"
TOP = ["HOOD", "TORSO", "SKIRT", "CAPE", "STAFF"]
BOTTOM = ["UPPER_ARM", "FOREARM", "HAND_OPEN", "HAND_GRIP", "BOOT"]


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


tag = arg("--tag", "v1")
# --names A,B,...: a one-row sheet (rig-parts.py --set forearms), parts named left to right.
# --scale: resize the cut parts (the forearm sheet was drawn larger than the first; 0.6 matches its sleeves).
names = arg("--names", "").split(",") if arg("--names") else None
scale = float(arg("--scale", "1"))
if names:
    TOP, BOTTOM = names, []
rgb = np.asarray(Image.open(DIR / f"parts-{tag}.png").convert("RGB")).astype(np.float32) / 255
r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
# Magenta-ness: red and blue high, green low. 1 on the background, 0 on the drawing.
key = np.clip((np.minimum(r, b) - g - 0.25) / 0.35, 0, 1)
alpha = 1 - key
# Pull the magenta fringe back: where red and blue exceed green, lower them towards green.
spill = np.clip(np.minimum(r, b) - g, 0, None)
fringe = np.where(alpha < 1, spill * 0.9, 0)
out = rgb.copy()
out[..., 0] -= fringe
out[..., 2] -= fringe
out = np.clip(out, 0, 1)

solid = alpha > 0.5
labels, count = ndimage.label(ndimage.binary_closing(solid, iterations=2))
sizes = ndimage.sum(solid, labels, range(1, count + 1))
keep = [i + 1 for i in np.argsort(sizes)[::-1][:len(TOP) + len(BOTTOM)]]
boxes = {i: ndimage.find_objects((labels == i).astype(int))[0] for i in keep}
mid = rgb.shape[0] * 0.62  # the bottom row starts about here on a 1024-high sheet
top = sorted((i for i in keep if names or boxes[i][0].start < mid * 0.6), key=lambda i: boxes[i][1].start)
bottom = sorted((i for i in keep if i not in top), key=lambda i: boxes[i][1].start)
if len(top) != len(TOP) or len(bottom) != len(BOTTOM):
    sys.exit(f"部品の数が合いません: 上段 {len(top)} / 下段 {len(bottom)}")

(DIR / "parts").mkdir(exist_ok=True)
rgba = np.dstack([out, alpha]) * 255
cuts = []
for name, i in list(zip(TOP, top)) + list(zip(BOTTOM, bottom)):
    ys, xs = boxes[i]
    pad = 4
    y0, y1 = max(ys.start - pad, 0), min(ys.stop + pad, rgb.shape[0])
    x0, x1 = max(xs.start - pad, 0), min(xs.stop + pad, rgb.shape[1])
    part = rgba[y0:y1, x0:x1].copy()
    part[..., 3] *= ndimage.binary_dilation(labels[y0:y1, x0:x1] == i, iterations=3)  # only this shape
    im = Image.fromarray(part.astype(np.uint8), "RGBA")
    if scale != 1:
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
    im.save(DIR / "parts" / f"{name}.png")
    cuts.append((name, im))
    print(f"{name}: {im.size} at ({x0},{y0})")

w = sum(im.width for _, im in cuts) + 20 * len(cuts)
h = max(im.height for _, im in cuts) + 30
sheet = Image.new("RGBA", (w, h), (58, 58, 68, 255))
d = ImageDraw.Draw(sheet)
x = 10
for name, im in cuts:
    sheet.alpha_composite(im, (x, 26))
    d.text((x, 6), name, fill=(255, 255, 255, 255))
    x += im.width + 20
(ROOT / "artifacts/rig-custom").mkdir(parents=True, exist_ok=True)
sheet.save(ROOT / f"artifacts/rig-custom/cut-{tag}.png")
