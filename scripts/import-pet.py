"""Turn a pet concept (a figure on a plain grey background) into the in-game sprite: background made transparent,
cropped to the figure, scaled to a fixed height.

Usage: python scripts/import-pet.py [source.png] [--height 360] [--tolerance 12] [--out assets/art/pets/golem.png]
Default source: assets/concepts/homunculus-v1.png (the stone golem, 2026-09-29). Writes assets/art/pets/golem.png,
which HomunculusPet loads from art/pets next to the DLL. The background is flood-filled from the borders, so grey
inside the figure stays.
"""
import sys
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
args = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith("--") and not sys.argv[i - 1].startswith("--")]


def arg(name, default):
    return type(default)(sys.argv[sys.argv.index(name) + 1]) if name in sys.argv else default


source = Path(args[0]) if args else ROOT / "assets/concepts/homunculus-v1.png"
height = arg("--height", 360)
tolerance = arg("--tolerance", 12)
out = ROOT / arg("--out", "assets/art/pets/golem.png")

image = Image.open(source).convert("RGBA")
w, h = image.size
px = image.load()
# The background colour: median of the border pixels.
border = sorted(px[x, y][:3] for x in range(w) for y in (0, h - 1)) + sorted(px[x, y][:3] for y in range(h) for x in (0, w - 1))
bg = border[len(border) // 2]
close = lambda c: sum(abs(a - b) for a, b in zip(c[:3], bg)) <= tolerance * 3
seen = bytearray(w * h)
queue = deque((x, y) for x in range(w) for y in (0, h - 1)) + deque((x, y) for y in range(h) for x in (0, w - 1))
mask = Image.new("L", (w, h), 255)
mp = mask.load()
while queue:
    x, y = queue.popleft()
    if seen[y * w + x] or not close(px[x, y]):
        continue
    seen[y * w + x] = 1
    mp[x, y] = 0
    for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
        if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx]:
            queue.append((nx, ny))
mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1))  # soften the cut edge
image.putalpha(mask)
image = image.crop(mask.getbbox())
image = image.resize((round(image.width * height / image.height), height), Image.LANCZOS)
out.parent.mkdir(parents=True, exist_ok=True)
image.save(out)
print(f"{source.name} -> {out.relative_to(ROOT)} ({image.width}x{image.height}, 背景色 {bg})")
