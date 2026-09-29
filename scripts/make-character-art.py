"""Make the alchemist's icons from the cut-out key visual, at the base game's sizes.

Usage: python scripts/make-character-art.py
Input: assets/art/character/alchemist.png (python scripts/import-pet.py assets/concepts/alchemist-character-v3.png
--height 1000 --out assets/art/character/alchemist.png). Writes into assets/art/character/:
  icon.png          85x85   top panel, run history, ancients' dialogue (the hooded head with the sigil)
  icon_outline.png  85x85   white silhouette of icon.png, drawn behind it
  char_select.png   132x195 character select button (bust on a teal panel)
  map_marker.png    49x64   map position marker (a chevron, like the base game's)
char_select and map_marker are also written as .ctex: the game types those two as CompressedTexture2D, which only
loads Godot's own texture file (header "GST2" + WebP, the same layout as the base game's imported files).
Sizes are the Ironclad's own files (2026-09-30). CharacterArt in CardArt.cs loads them.
"""
import io
import struct
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
DIR = ROOT / "assets/art/character"
TEAL, DARK, GOLD = (115, 214, 178), (22, 54, 50), (214, 170, 84)

body = Image.open(DIR / "alchemist.png").convert("RGBA")
w, h = body.size
# Head: the hood's box, found from the key visual (566x1000): the sigil sits at about (0.67w, 0.11h).
head = body.crop((int(w * 0.36), 0, int(w * 0.94), int(w * 0.58)))
# Fade the cut at the shoulders so the icon reads as a head, not a cropped rectangle.
fade = Image.linear_gradient("L").resize(head.size).point(lambda v: 255 if v < 150 else max(0, 255 - (v - 150) * 255 // 105))
head.putalpha(Image.composite(head.getchannel("A"), Image.new("L", head.size, 0), fade))
icon = Image.new("RGBA", (85, 85))
head.thumbnail((81, 81), Image.LANCZOS)
icon.alpha_composite(head, ((85 - head.width) // 2, (85 - head.height) // 2))
icon.save(DIR / "icon.png")
alpha = icon.getchannel("A").point(lambda a: 255 if a > 40 else 0).filter(ImageFilter.MaxFilter(5))
outline = Image.new("RGBA", icon.size, (255, 255, 255, 0))
outline.putalpha(alpha)
outline.save(DIR / "icon_outline.png")

# Character select: head and shoulders on a flat panel, like the base game's buttons.
cw, ch = 132, 195
bust = body.crop((int(w * 0.32), 0, int(w * 0.94), int(w * 0.62 * ch / cw)))
bust = bust.resize((cw, round(bust.height * cw / bust.width)), Image.LANCZOS)
panel = Image.new("RGBA", (cw, ch), DARK + (255,))
glow = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
ImageDraw.Draw(glow).ellipse((-20, 10, cw + 20, ch + 60), fill=TEAL + (110,))
panel.alpha_composite(glow.filter(ImageFilter.GaussianBlur(24)))
panel.alpha_composite(bust.crop((0, 0, cw, ch)), (0, 8))
ImageDraw.Draw(panel).rectangle((0, 0, cw - 1, ch - 1), outline=GOLD + (255,), width=2)
panel.save(DIR / "char_select.png")

# Map marker: a downward chevron, gold rim around teal.
marker = Image.new("RGBA", (49 * 4, 64 * 4))
d = ImageDraw.Draw(marker)
outer = [(4, 8), (98, 44), (192, 8), (98, 248)]
inner = [(30, 30), (98, 60), (166, 30), (98, 214)]
d.polygon(outer, fill=GOLD + (255,))
d.polygon(inner, fill=TEAL + (255,))
d.polygon([(98, 60), (166, 30), (98, 214)], fill=(70, 160, 135, 255))  # shaded half, like the vanilla marker
marker.resize((49, 64), Image.LANCZOS).save(DIR / "map_marker.png")


def save_ctex(image, path):
    """Godot 4 CompressedTexture2D: GST2 v1, one WebP (lossless) image in RGBA8, no mipmaps."""
    buf = io.BytesIO()
    image.save(buf, "WEBP", lossless=True)
    data = buf.getvalue()
    w, h = image.size
    header = struct.pack("<4sIIIIi3I", b"GST2", 1, w, h, 0x0D000000, -1, 0, 0, 0)
    path.write_bytes(header + struct.pack("<IHHII", 2, w, h, 0, 5) + struct.pack("<I", len(data)) + data)


save_ctex(panel, DIR / "char_select.ctex")
save_ctex(Image.open(DIR / "map_marker.png").convert("RGBA"), DIR / "map_marker.ctex")
print("icon.png, icon_outline.png, char_select.png, map_marker.png ->", DIR.relative_to(ROOT))
