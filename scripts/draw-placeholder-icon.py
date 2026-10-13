"""Draw a flat placeholder power icon with PIL when a Codex sheet is not worth it for one icon (2026-10-13).

Usage: python scripts/draw-placeholder-icon.py golden_form_power
Writes assets/art/incoming/icons/<slug>.png; then python scripts/import-icons.py <slug>.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
S = 512


def golden_form_power(d):
    # A gold ingot: a trapezoid top face over a darker front, with a light rim like the FLAT style.
    top = [(150, 190), (362, 190), (420, 250), (92, 250)]
    front = [(92, 250), (420, 250), (420, 330), (92, 330)]
    rim = (255, 240, 190, 255)
    d.polygon(front, fill=(196, 132, 28, 255), outline=rim, width=10)
    d.polygon(top, fill=(246, 196, 64, 255), outline=rim, width=10)
    d.line([(175, 215), (300, 215)], fill=(255, 250, 220, 255), width=12)


DRAW = {"golden_form_power": golden_form_power}

slug = sys.argv[1]
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
DRAW[slug](ImageDraw.Draw(img))
shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
shadow.putalpha(img.getchannel("A").filter(ImageFilter.GaussianBlur(6)).point(lambda a: a // 3))
out = Image.alpha_composite(shadow, img)
path = ROOT / "assets/art/incoming/icons" / f"{slug}.png"
path.parent.mkdir(parents=True, exist_ok=True)
out.save(path)
print(path)
