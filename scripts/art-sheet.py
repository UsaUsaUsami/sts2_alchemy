"""Tile generated card art into one labelled sheet for review (what Claude looks at before importing).

Usage: python scripts/art-sheet.py [incoming|cards] [--cols 4]
Writes artifacts/art-gen/sheet.png. Each tile is the image cropped to the card's aspect, as import-art.py would.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
source = ROOT / "assets/art" / (sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("--") else "incoming")
cols = int(sys.argv[sys.argv.index("--cols") + 1]) if "--cols" in sys.argv else 4
titles = {e["slug"]: e["title"] for e in json.loads((ROOT / "assets/art/prompts.json").read_text(encoding="utf-8"))}
files = sorted(p for p in source.glob("*") if p.suffix.lower() in (".png", ".jpg", ".jpeg", ".webp"))
if not files:
    sys.exit(f"{source} に画像がありません。")
TW, TH, LABEL = 375, 285, 26
try:
    font = ImageFont.truetype("C:/Windows/Fonts/meiryo.ttc", 18)
except OSError:
    font = ImageFont.load_default()
rows = (len(files) + cols - 1) // cols
sheet = Image.new("RGB", (cols * TW, rows * (TH + LABEL)), (24, 24, 28))
draw = ImageDraw.Draw(sheet)
for i, p in enumerate(files):
    with Image.open(p) as image:
        image = image.convert("RGB")
        sw, sh = image.size
        if sw * TH > sh * TW:
            nw = sh * TW // TH
            image = image.crop(((sw - nw) // 2, 0, (sw - nw) // 2 + nw, sh))
        else:
            nh = sw * TH // TW
            image = image.crop((0, (sh - nh) // 2, sw, (sh - nh) // 2 + nh))
        x, y = (i % cols) * TW, (i // cols) * (TH + LABEL)
        sheet.paste(image.resize((TW, TH), Image.LANCZOS), (x, y + LABEL))
    slug = p.stem.lower()
    draw.text((x + 6, y + 3), f"{slug}  {titles.get(slug, '')}", fill=(230, 230, 230), font=font)
out = ROOT / "artifacts/art-gen/sheet.png"
out.parent.mkdir(parents=True, exist_ok=True)
sheet.save(out)
print(f"{len(files)}枚 -> {out}")
