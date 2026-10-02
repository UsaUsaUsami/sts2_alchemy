"""Design candidates for the homunculus pet: four ideas in one Codex image call (2x2 sheet), cut into four files.

Usage: python scripts/homunculus-concepts.py [--tag v1] [--note "extra direction"]
Writes assets/concepts/homunculus/hm-<tag>.png (the sheet) and hm-<tag>-<id>.png per panel.
Why one call: the user's ChatGPT usage allows only a few images at a time (2026-09-29). The user asked for a more
pop design than the eerie jar idea; a golem is fine too, so two golems and two homunculi.
"""
import shutil
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
REFS = [ROOT / ".research/sts2-ref/lineup_small.png", ROOT / "assets/concepts/alchemist-character-v3.png"]
OUT = ROOT / "assets/concepts/homunculus"

PANELS = [
    ("golem-stone", "top-left", "GOLEM 1 - a chibi stone golem: a chunky, rounded, head-and-body-in-one shape made of "
     "a few smooth ochre stones, short stubby arms and legs. No face; a softly glowing gold alchemical sigil (circle "
     "and triangle, like the alchemist's) is its only 'eye'. Thin lines in the four element colours along the stone "
     "seams."),
    ("golem-flask", "top-right", "GOLEM 2 - a small brass-and-stone golem whose head is a round glass flask with a "
     "cork; inside the flask, teal liquid with two big round cute eyes floating in it. Short sturdy body, oversized "
     "hands."),
    ("homunculus-jar", "bottom-left", "HOMUNCULUS 1 - a small, cute pale-teal humanoid with a big round head and big "
     "round eyes, sitting inside an open glass jar with a brass rim, hands on the rim, peeking out cheerfully. Not "
     "eerie, not foetus-like."),
    ("homunculus-sprout", "bottom-right", "HOMUNCULUS 2 - a small, soft, bean-shaped teal homunculus floating in the "
     "air, big dot eyes, a tiny gold sigil on its forehead, a little sprout or wisp of mist on its head, tiny arms."),
]
BRIEF = (
    "Create a character design sheet for the companion of the Alchemist, a playable character in a Slay the Spire 2 "
    "mod. The second attached image is the Alchemist (hooded charcoal robe with gold trim, glowing gold sigil face); "
    "the companion must look like it belongs to them. The first attached image is the base game's cast, for visual "
    "language only (do not copy).\n"
    "Style: POP and cute, but in Slay the Spire 2's painterly look: big simple rounded shapes, strong readable "
    "silhouette, minimal thin line work, flat colour masses with soft brush texture, saturated accents. The companion "
    "is small (about knee-high to the Alchemist) and stands next to them on the battlefield; it shields the Alchemist "
    "from attacks. Each design shown alone, full body, three-quarter view, centred, plain flat mid-grey background, "
    "no text, no labels, no Alchemist in the picture.\n")


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


tag = arg("--tag", "v1")
note = arg("--note", "")
codex = shutil.which("codex") or shutil.which("codex.cmd")
if not codex:
    sys.exit("codex が見つかりません。")
missing = [str(r) for r in REFS if not r.exists()]
if missing:
    sys.exit("参考画像がありません: " + ", ".join(missing))
OUT.mkdir(parents=True, exist_ok=True)
sheet = OUT / f"hm-{tag}.png"
task = ("Use your built-in image generation tool to create exactly one image, square (1024x1024). It is a 2x2 sheet "
        "of FOUR different design candidates: each panel is exactly one quarter, same grey background, no gaps, "
        "borders, frames or text, nothing crossing a panel edge.\nThen save it as " + sheet.relative_to(ROOT).as_posix()
        + " (overwrite if it exists). Do not edit any other file. Reply with the saved path only.\n\n" + BRIEF + "\n"
        + "\n".join(f"PANEL {pos}: {desc}" for _, pos, desc in PANELS)
        + (f"\nExtra direction: {note}" if note else "") + "\n")
cmd = [codex, "exec", "--ephemeral", "-s", "workspace-write", "-C", str(ROOT), "-c", 'model_reasoning_effort="low"']
for ref in REFS:
    cmd += ["-i", str(ref)]
cmd.append("-")
started = time.time()
log = ROOT / "artifacts/art-gen" / f"hm-{tag}.log"
log.parent.mkdir(parents=True, exist_ok=True)
with open(log, "w", encoding="utf-8") as f:
    subprocess.run(cmd, input=task, text=True, encoding="utf-8", stdout=f, stderr=subprocess.STDOUT, timeout=900,
                   cwd=ROOT)
if not (sheet.exists() and sheet.stat().st_mtime >= started - 1):
    sys.exit(f"生成に失敗（ログ: {log}）")
image = Image.open(sheet).convert("RGB")
hw, hh = image.width // 2, image.height // 2
for i, (key, _, _) in enumerate(PANELS):
    x, y = (i % 2) * hw, (i // 2) * hh
    image.crop((x, y, x + hw, y + hh)).save(OUT / f"hm-{tag}-{key}.png")
print(f"{time.time() - started:.0f}秒: {sheet}")
