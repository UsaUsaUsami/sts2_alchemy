"""Turn the cut icons in assets/art/incoming/icons/ into the game's sizes, written as Godot .ctex files that the mod
hands to the game's ResourceLoader by absolute path (IconArt in CardArt.cs).

Usage: python scripts/import-icons.py [slug ...]   (default: every icon in assets/art/incoming/icons/)
Sizes follow the base game (read from its pck, 2026-10-01):
  relic  -> <slug>.ctex 256x256 (big), <slug>_packed.ctex 85x85, <slug>_outline.ctex 85x85 (white silhouette grown
            by a few pixels, like relic_outline_atlas)
  flat   -> <slug>.ctex 256x256 (big, powers), <slug>_packed.ctex 64x64 (power bar, enchantments)
  map    -> <slug>_packed.ctex 128x128, the ink drawing about 72 px tall in the middle (map_shop's padded size)
The kind comes from the icon's style in scripts/generate-icons.py. Also writes a preview sheet to
artifacts/art-gen/icons-imported.png.
"""
import importlib.util
import io
import struct
import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
INCOMING = ROOT / "assets/art/incoming/icons"
OUT = ROOT / "assets/art/icons"

spec = importlib.util.spec_from_file_location("gen", ROOT / "scripts/generate-icons.py")
gen = importlib.util.module_from_spec(spec)
sys.argv, argv = [sys.argv[0], "--import"], sys.argv  # generate-icons exits on an unknown group; keep it quiet
try:
    spec.loader.exec_module(gen)
except SystemExit:
    pass
sys.argv = argv
KIND = {slug: ("relic" if style == gen.RELIC else "map" if style == gen.MAP else "flat")
        for icons in gen.GROUPS.values() for slug, style, _ in icons}


def save_ctex(image, path):
    """Godot 4 CompressedTexture2D: GST2 v1, one lossless WebP in RGBA8, no mipmaps (as make-character-art.py)."""
    buf = io.BytesIO()
    image.save(buf, "WEBP", lossless=True)
    data = buf.getvalue()
    w, h = image.size
    header = struct.pack("<4sIIIIi3I", b"GST2", 1, w, h, 0x0D000000, -1, 0, 0, 0)
    path.write_bytes(header + struct.pack("<IHHII", 2, w, h, 0, 5) + struct.pack("<I", len(data)) + data)


def fit(icon, side, inner):
    """The icon scaled so its longer edge is `inner`, centred on a transparent side x side square."""
    box = icon.getbbox() or (0, 0, icon.width, icon.height)
    icon = icon.crop(box)
    scale = inner / max(icon.size)
    icon = icon.resize((max(1, round(icon.width * scale)), max(1, round(icon.height * scale))), Image.LANCZOS)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.paste(icon, ((side - icon.width) // 2, (side - icon.height) // 2))
    return square


def outline(packed, grow=3):
    alpha = packed.getchannel("A").point(lambda a: 255 if a > 40 else 0).filter(ImageFilter.MaxFilter(grow * 2 + 1))
    white = Image.new("RGBA", packed.size, (255, 255, 255, 0))
    white.putalpha(alpha.filter(ImageFilter.GaussianBlur(0.6)))
    return white


slugs = sys.argv[1:] or sorted(p.stem for p in INCOMING.glob("*.png"))
OUT.mkdir(parents=True, exist_ok=True)
previews = []
for slug in slugs:
    src = INCOMING / f"{slug}.png"
    if not src.exists() or slug not in KIND:
        print(f"NG {slug}: {'ファイルなし' if not src.exists() else 'generate-icons.pyにない'}")
        continue
    icon = Image.open(src).convert("RGBA")
    kind = KIND[slug]
    if kind == "relic":
        big, packed = fit(icon, 256, 236), fit(icon, 85, 78)
        save_ctex(big, OUT / f"{slug}.ctex")
        save_ctex(packed, OUT / f"{slug}_packed.ctex")
        save_ctex(outline(packed), OUT / f"{slug}_outline.ctex")
    elif kind == "flat":
        big, packed = fit(icon, 256, 236), fit(icon, 64, 60)
        save_ctex(big, OUT / f"{slug}.ctex")
        save_ctex(packed, OUT / f"{slug}_packed.ctex")
    else:
        big = packed = fit(icon, 128, 72)
        save_ctex(packed, OUT / f"{slug}_packed.ctex")
    previews.append((big, packed))
    print(f"OK {slug:<24} {kind}")

if previews:
    sheet = Image.new("RGBA", (len(previews) * 270, 360), (60, 60, 70, 255))
    for i, (big, packed) in enumerate(previews):
        sheet.paste(big, (i * 270 + 7, 7), big)
        sheet.paste(packed, (i * 270 + 7, 270), packed)
    sheet.save(ROOT / "artifacts/art-gen/icons-imported.png")
    print(f"{len(previews)}個 -> {OUT.relative_to(ROOT)}。プレビュー: artifacts/art-gen/icons-imported.png")
