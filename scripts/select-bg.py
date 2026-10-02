"""Generate the character select background for the alchemist with Codex (one image call).

Usage: python scripts/select-bg.py [--note "extra direction"] [--crop-only]
Writes assets/concepts/select-bg.png (the raw image) and assets/art/character/select_bg.png (1920x1080, what the
mod shows; CharacterArt.SelectBg). The base game's select screens put the character right of centre and keep the
left side calm for the character list and description (2026-09-30). Second version (user, same day: the workshop was
too busy and the golem sat under the ascension panel): a plain backdrop, the golem at the far right. At 1920x1080 the
game's UI covers the info panel (x 296-822, y 345-779), the ascension panel (x 643-1277, y 738-855), the character
buttons (y 851-1005, centred) and the confirm button (x 1760-, y 726-836), from character_select_screen.tscn.
"""
import shutil
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
REFS = [ROOT / ".research/sts2-ref/lineup_small.png", ROOT / "assets/concepts/alchemist-character-v3.png"]
RAW = ROOT / "assets/concepts/select-bg.png"
OUT = ROOT / "assets/art/character/select_bg.png"


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


note = arg("--note", "")
codex = shutil.which("codex") or shutil.which("codex.cmd")
if not codex:
    sys.exit("codex が見つかりません。")
brief = (
    "Use your built-in image generation tool to create exactly one image, landscape 3:2 (1536x1024). Then save it as "
    + RAW.relative_to(ROOT).as_posix() + " (overwrite if it exists). Do not edit any other file. Reply with the saved "
    "path only.\n\n"
    "It is the character select screen background for the Alchemist, a playable character in a Slay the Spire 2 mod. "
    "The first attached image is the base game's cast (visual language only, do not copy). The second is the "
    "Alchemist: draw them exactly like it (hooded charcoal robe with gold trim, a glowing gold alchemical sigil as the "
    "face, a vertical band of the four element marks, belt with two vials). Draw ONLY the Alchemist: no golem, no "
    "companion, no creature (the golem is added afterwards).\n"
    "Background: SIMPLE, like the base game's select screens: no room, no furniture, no architecture, no props. Only "
    "a dark charcoal-to-deep-teal backdrop with soft painterly brush texture and a large faint gold transmutation "
    "circle glowing behind the figures, plus four small wisps of the element colours (ochre earth, teal water, orange "
    "fire, pale cyan air) near the Alchemist's raised hand.\n"
    "Composition (the game's UI covers parts of the screen, keep them plain): the Alchemist stands large in the right "
    "half, centred at about 60% of the width (the figure spans roughly 45% to 75%), three-quarter view, one hand "
    "raised towards the upper left, the hood's top near the upper edge. The area from 76% to 100% of the width in the "
    "lower 60% of the height is EMPTY plain backdrop with a dim gold glow on the floor (a companion is pasted there "
    "later). The left 40% and the "
    "bottom-centre band (the lowest 30% between 25% and 70% of the width, where the character buttons and the "
    "ascension selector sit) contain only the plain backdrop.\n"
    "Style: Slay the Spire 2 painterly look, minimal thin line work, large flat colour masses with soft brush "
    "texture, dramatic lighting. No text, no logo, no frame, no UI.\n" + (f"Extra direction: {note}\n" if note else ""))
cmd = [codex, "exec", "--ephemeral", "-s", "workspace-write", "-C", str(ROOT), "-c", 'model_reasoning_effort="low"']
for ref in REFS:
    cmd += ["-i", str(ref)]
cmd.append("-")
started = time.time()
if "--crop-only" not in sys.argv:  # --crop-only: redo the 1920x1080 crop of the existing raw image
    log = ROOT / "artifacts/art-gen/select-bg.log"
    log.parent.mkdir(parents=True, exist_ok=True)
    with open(log, "w", encoding="utf-8") as f:
        subprocess.run(cmd, input=brief, text=True, encoding="utf-8", stdout=f, stderr=subprocess.STDOUT,
                       timeout=900, cwd=ROOT)
    if not (RAW.exists() and RAW.stat().st_mtime >= started - 1):
        sys.exit(f"生成に失敗（ログ: {log}）")
image = Image.open(RAW).convert("RGB")
# Cover 1920x1080: scale to the width, crop the extra height mostly from the bottom (keeps the hood's tip).
scaled = image.resize((1920, round(image.height * 1920 / image.width)), Image.LANCZOS)
top = max(0, (scaled.height - 1080) * 3 // 10)
picture = scaled.crop((0, top, 1920, top + 1080)).convert("RGBA")
# The golem is pasted from the combat sprite: a generated one never matched it (user, 2026-09-30: "the golem is
# different"). The game shows this picture 4% larger on each side (AlchemistSelectBg.VisibleRect), so a picture
# pixel p lands at p*1.08 - 4% of the screen. Screen box: x 1390-1740, feet at y 1040 -- right of the alchemist and
# the ascension panel (ends x 1277), left of the confirm button (x 1760-).
golem = Image.open(ROOT / "assets/art/pets/golem.png").convert("RGBA")
px = golem.load()
for y in range(golem.height * 3 // 4, golem.height):  # the concept's grey floor shadow (opaque neutral grey) -> soft dark
    for x in range(golem.width):
        r, g, b, a = px[x, y]
        if a and max(r, g, b) - min(r, g, b) < 8 and 75 < r < 135:
            px[x, y] = (0, 0, 0, 110)
alpha = golem.getchannel("A")
golem = Image.eval(golem.convert("RGB"), lambda v: int(v * 0.75)).convert("RGBA")  # the painting is darker
golem.putalpha(alpha)
gw = round(350 / 1.08)
golem = golem.resize((gw, round(golem.height * gw / golem.width)), Image.LANCZOS)
gx, feet = round((1390 + 0.04 * 1920) / 1.08), round((1040 + 0.04 * 1080) / 1.08)
shadow = Image.new("RGBA", picture.size, (0, 0, 0, 0))
ImageDraw.Draw(shadow).ellipse((gx + gw * 0.08, feet - 26, gx + gw * 0.92, feet + 14), fill=(0, 0, 0, 150))
picture.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(10)))
sole = golem.getchannel("A").point(lambda a: 255 if a > 200 else 0).getbbox()[3]  # feet, not the shadow below
picture.alpha_composite(golem, (gx, feet - sole))
picture.convert("RGB").save(OUT)
print(f"{time.time() - started:.0f}秒: {OUT.relative_to(ROOT)}")
