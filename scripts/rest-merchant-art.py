"""Generate the alchemist's rest site and merchant figures with Codex (one image call, two panels).

Usage: python scripts/rest-merchant-art.py [--note "extra direction"] [--cut-only]
Writes assets/concepts/rest-merchant.png (the raw sheet) and assets/art/character/rest_site.png and merchant.png
(transparent, cropped to the figure), which RestMerchantArt in CardArt.cs loads. In the base game both are Spine
animations (the Ironclad sits by the fire; at the shop he stands left of the merchant facing right); the mod shows one
still picture fitted into the Ironclad's figure bounds instead (2026-10-01).
"""
import shutil
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
REFS = [ROOT / "assets/concepts/alchemist-character-v3.png", ROOT / "assets/concepts/homunculus-v1.png"]
RAW = ROOT / "assets/concepts/rest-merchant.png"
OUT = ROOT / "assets/art/character"
KEY = (255, 0, 255)


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


if "--cut-only" not in sys.argv:
    codex = shutil.which("codex") or shutil.which("codex.cmd")
    if not codex:
        sys.exit("codex が見つかりません。")
    brief = (
        "Use your built-in image generation tool to create exactly one image, landscape 3:2 (1536x1024). Then save it "
        "as " + RAW.relative_to(ROOT).as_posix() + " (overwrite if it exists). Do not edit any other file. Reply with "
        "the saved path only.\n\n"
        "Two separate full-body figures side by side for a Slay the Spire 2 mod, each in its own half of the image, on "
        "ONE flat pure magenta (#FF00FF) background everywhere: no floor, no ground line, no cast shadow, no campfire, "
        "no props on the ground, no text, nothing magenta or pink on the figures. Nothing crosses the vertical centre "
        "line. Same hand-painted style as the first attached image, which shows the Alchemist: draw them exactly like it "
        "(faceless, deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil, long charcoal "
        "robe with gold border lines and a vertical band of the four element marks, brown belt with two small vials, "
        "dark gloves and boots). The second attached image is their companion, a chunky rounded stone golem.\n"
        "LEFT HALF (rest site): the Alchemist sitting on the ground resting, three-quarter view facing RIGHT (toward a "
        "campfire that is NOT drawn), knees up, holding a small steaming cup in both hands, relaxed. The stone golem "
        "sits curled up beside them on their left (the viewer's left), half their sitting height, dozing. The pair fills "
        "most of the left half, bottoms resting on the same invisible ground line near the bottom edge.\n"
        "RIGHT HALF (shop): the Alchemist standing, full body, three-quarter view facing RIGHT toward a shopkeeper who "
        "is NOT drawn, relaxed, one hand on a vial at the belt, curious. Feet near the bottom edge, head near the top. "
        "No golem in this half.\n" + (arg("--note", "") or ""))
    cmd = [codex, "exec", "-s", "workspace-write", "-C", str(ROOT), "-c", 'model_reasoning_effort="low"']
    for ref in REFS:
        cmd += ["-i", str(ref)]
    cmd.append("-")
    started = time.time()
    log_path = ROOT / "artifacts/art-gen/rest-merchant.log"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    with open(log_path, "w", encoding="utf-8") as log:
        subprocess.run(cmd, input=brief, text=True, encoding="utf-8", stdout=log, stderr=subprocess.STDOUT,
                       timeout=900, cwd=ROOT)
    if not (RAW.exists() and RAW.stat().st_mtime >= started - 1):
        sys.exit(f"生成に失敗（ログ: {log_path}）")
    print(f"{time.time() - started:.0f}秒。シート: {RAW}")

sheet = Image.open(RAW).convert("RGBA")
w, h = sheet.size
px = sheet.load()
mask = Image.new("L", sheet.size, 255)
mp = mask.load()
for y in range(h):
    for x in range(w):
        r, g, b, _ = px[x, y]
        if abs(r - KEY[0]) + abs(g - KEY[1]) + abs(b - KEY[2]) <= 180:
            mp[x, y] = 0
        elif r > g + 40 and b > g + 40:  # despill the pink fringe
            m = max(g, min(r, b) - 60)
            px[x, y] = (min(r, m + 40), g, min(b, m + 40), 255)
mask = mask.filter(ImageFilter.MinFilter(3))
# The sitting figure's feet may reach past the centre line, so the halves are split by connected pieces: each piece
# goes to the side its centre of mass is on.
side = Image.new("L", sheet.size, 0)  # 1 = left (rest site), 2 = right (merchant)
sp, seen = side.load(), bytearray(w * h)
for sy in range(h):
    for sx in range(w):
        if seen[sy * w + sx] or mp[sx, sy] == 0:
            continue
        piece, stack = [], [(sx, sy)]
        seen[sy * w + sx] = 1
        while stack:
            x, y = stack.pop()
            piece.append((x, y))
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx] and mp[nx, ny]:
                    seen[ny * w + nx] = 1
                    stack.append((nx, ny))
        label = 1 if sum(x for x, _ in piece) / len(piece) < w / 2 else 2
        for x, y in piece:
            sp[x, y] = label
mask = mask.filter(ImageFilter.GaussianBlur(0.8))
OUT.mkdir(parents=True, exist_ok=True)
for name, label in (("rest_site.png", 1), ("merchant.png", 2)):
    own = side.point(lambda v, label=label: 255 if v == label else 0).filter(ImageFilter.MaxFilter(3))
    alpha = Image.composite(mask, Image.new("L", sheet.size, 0), own)
    figure = sheet.copy()
    figure.putalpha(alpha)
    figure = figure.crop(alpha.getbbox())
    figure.save(OUT / name)
    print(f"{name}: {figure.width}x{figure.height}")
