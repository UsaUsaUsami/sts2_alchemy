"""Bring generated card art into the mod: name check, centre crop to the card's aspect, resize, save as PNG.

Usage: python scripts/import-art.py [--scale 2] [--keep]
Reads images from assets/art/incoming (png/jpg/jpeg/webp) and writes assets/art/cards/<slug>.png, which
scripts/build.ps1 ships as art/cards in the mod (CardArt.cs loads them). The file name must start with a slug from
assets/art/prompts.json ("fire_spark.png", "fire_spark_v2.png" and "fire_spark (1).png" all work; the longest
matching slug wins). Processed originals move to assets/art/incoming/done unless --keep. --scale 2 stores twice the
base game's size (sharper when zoomed, if the game scales it down; not checked in game yet).
"""
import json
import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
INCOMING = ROOT / "assets/art/incoming"
CARDS = ROOT / "assets/art/cards"
scale = int(sys.argv[sys.argv.index("--scale") + 1]) if "--scale" in sys.argv else 1
keep = "--keep" in sys.argv

prompts = ROOT / "assets/art/prompts.json"
if not prompts.exists():
    sys.exit("assets/art/prompts.json がありません。先に scripts/art-prompts.py を実行してください。")
# Every card, including ones that already have art, so re-imports work: regenerate the sheet with --all if needed.
sizes = {e["slug"]: tuple(e["size"]) for e in json.loads(prompts.read_text(encoding="utf-8"))}
for p in CARDS.glob("*.png"):
    sizes.setdefault(p.stem, (250, 190))


def match(stem):
    stem = stem.lower()
    found = [s for s in sizes if stem == s or stem.startswith(s + "_") or stem.startswith(s + " ") or stem.startswith(s + "-")]
    return max(found, key=len) if found else None


def fit(image, w, h):
    """Centre crop to w:h, then resize to w×h."""
    image = image.convert("RGBA")
    sw, sh = image.size
    if sw * h > sh * w:
        nw = sh * w // h
        image = image.crop(((sw - nw) // 2, 0, (sw - nw) // 2 + nw, sh))
    else:
        nh = sw * h // w
        image = image.crop((0, (sh - nh) // 2, sw, (sh - nh) // 2 + nh))
    return image.resize((w, h), Image.LANCZOS)


CARDS.mkdir(parents=True, exist_ok=True)
files = [p for p in sorted(INCOMING.glob("*")) if p.suffix.lower() in (".png", ".jpg", ".jpeg", ".webp")]
if not files:
    print(f"{INCOMING} に画像がありません。")
imported, unknown = [], []
for p in files:
    s = match(p.stem)
    if s is None:
        unknown.append(p.name)
        continue
    w, h = sizes[s]
    with Image.open(p) as image:
        fit(image, w * scale, h * scale).save(CARDS / f"{s}.png")
    imported.append(f"{p.name} -> {s}.png ({w * scale}x{h * scale})")
    if not keep:
        (INCOMING / "done").mkdir(exist_ok=True)
        shutil.move(str(p), INCOMING / "done" / p.name)
for line in imported:
    print("取り込み:", line)
for name in unknown:
    print("対応するカードがない（ファイル名を確認）:", name)
print(f"{len(imported)}枚取り込み、{len(unknown)}枚は未対応。次は scripts/build.ps1 → scripts/update.ps1")
