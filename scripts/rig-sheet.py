"""Contact sheet of the frames scripts/rig-probe.ps1 saved: one row per animation, background grey like the game.
Usage: python scripts/rig-sheet.py artifacts/rig-probe/<name> [tag] [scale]"""
import re
import sys
from pathlib import Path

from PIL import Image

d = Path(sys.argv[1]); tag = sys.argv[2] if len(sys.argv) > 2 else None; s = float(sys.argv[3]) if len(sys.argv) > 3 else 0.5
rows = {}
for p in sorted(d.glob("*.png")):
    m = re.match(r"(.+?)_(.+)_(\d+)\.png$", p.name)
    if m and (tag is None or m[1] == tag):
        rows.setdefault(f"{m[1]} {m[2]}", []).append(p)
frames = [[Image.open(p) for p in ps] for ps in rows.values()]
w, h = frames[0][0].size
cw, ch = int(w * s), int(h * s)
sheet = Image.new("RGBA", (cw * max(map(len, frames)), ch * len(frames)), (58, 58, 68, 255))
for r, ims in enumerate(frames):
    for c, im in enumerate(ims):
        sheet.alpha_composite(im.resize((cw, ch)), (c * cw, r * ch))
sheet.save(d / "sheet.png")
print(d / "sheet.png", list(rows))
