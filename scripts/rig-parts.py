"""Body parts for the alchemist's own combat rig (2026-10-02, user: the repainted Necrobinder is too far from the
character icon). One Codex image call draws every part on one sheet, separated on a flat magenta background, in the
look of the key visual; scripts/rig-build.py cuts them out and builds the skeleton.

Usage: python scripts/rig-parts.py [--tag v1] [--note "extra direction"]
Writes assets/art/character/rig/alchemist/parts-<tag>.png (the raw sheet). Spends one image of the user's ChatGPT
quota. References: the key visual, and a parts page of another mod's rig (HelloSpire, MIT) for the layout only.
"""
import shutil
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
REFS = [ROOT / "assets/concepts/alchemist-character-v3.png", ROOT / "artifacts/hs-alchemist-page.png"]
OUT = ROOT / "assets/art/character/rig/alchemist"

# Parts, in reading order on the sheet. Limb pieces are drawn straight and vertical with the joint at the top, so
# the rig can hang them from their bones.
PARTS = [
    ("HOOD", "the hooded head seen from the side, facing RIGHT (three-quarter profile): the deep charcoal hood with "
     "its gold edge, the face opening completely dark, and in it the glowing gold sigil (circle, triangle, small "
     "circle, short rays) seen slightly from the side. No neck, no shoulders."),
    ("TORSO", "the upper body from the shoulders down to the waist, facing RIGHT, NO arms and NO head: charcoal robe, "
     "the charcoal scarf wrapped round the neck with the gold ring clasp, the vertical charcoal band with gold edges "
     "down the front showing the earth triangle and water drop symbols, the brown sash at the waist with two small "
     "round potion flasks (amber and teal) hanging from it."),
    ("SKIRT", "the lower robe from the waist down to the hem, facing RIGHT, NO legs: long flared charcoal robe with "
     "two gold stripes near the hem, the vertical front band continuing with the fire and spiral symbols."),
    ("CAPE", "the long scarf-cape on its own, hanging from the top edge and flowing down and back to the LEFT, "
     "charcoal outside with mustard-gold lining showing at the folds, ragged ends."),
    ("UPPER_ARM", "one upper sleeve of the robe, straight and vertical, shoulder end at the top, charcoal."),
    ("FOREARM", "one wide bell sleeve of the robe, straight and vertical, elbow end at the top, opening at the bottom "
     "with two gold trim stripes at the cuff, charcoal."),
    ("HAND_OPEN", "one dark brown leather glove, open relaxed hand, wrist at the top, fingers pointing down."),
    ("HAND_GRIP", "one dark brown leather glove as a closed fist, wrist at the top, seen from the side, with an empty "
     "round gap through the fist where a staff will pass horizontally."),
    ("BOOT", "one dark brown boot with a gold strap and a short piece of dark trouser above it, seen from the side, "
     "toe pointing RIGHT."),
    ("STAFF", "a long, thin, straight wooden staff, vertical, with brass bands; at the top a brass ring holding the "
     "same gold sigil (triangle and circle) as the face."),
]

# 2026-10-02: forearms with the hand already coming out of the cuff. Separate hands had to be fitted to the slanted
# cuff by calculation and kept looking stuck on from outside (user, Codex); drawn as one piece there is no seam.
FOREARMS = [
    ("FOREARM_OPEN", "one wide bell sleeve of the robe, straight and vertical, elbow end at the top, the opening at the "
     "bottom with two gold trim stripes at the cuff, charcoal outside and mustard lining inside, exactly like the "
     "FOREARM in the attached parts sheet. A dark brown leather gloved RIGHT hand comes out from INSIDE the cuff "
     "opening, relaxed and hanging down, back of the hand towards the viewer, thumb pointing to the RIGHT; the wrist "
     "disappears into the dark inside of the sleeve, the front edge of the cuff overlaps the wrist."),
    ("FOREARM_GRIP", "the same bell sleeve, straight and vertical, elbow end at the top. A dark brown leather gloved "
     "LEFT hand comes out from INSIDE the cuff opening and is closed in a fist, palm side towards the viewer, fingers "
     "curled round towards the viewer, thumb wrapped over the fingers; through the fist runs an empty round "
     "vertical gap where a staff will pass. The wrist disappears into the dark inside of the sleeve, the front edge "
     "of the cuff overlaps the wrist. No staff drawn."),
]

# 2026-10-02: the first FOREARM_GRIP read as a right hand making an OK sign with the staff through a hole facing the
# viewer (user). Redrawn as a left fist closed round a vertical pole that is not drawn; the staff is a separate part
# drawn behind the fist, so it shows above and below the fingers.
GRIP = [
    ("FOREARM_GRIP", "the same wide bell sleeve as the FOREARM parts in the attached sheets, straight and vertical, "
     "elbow end at the top, gold trim stripes at the cuff, mustard lining. Out of the INSIDE of the cuff comes a dark "
     "brown leather gloved LEFT hand clenched in a fist around a VERTICAL round pole about as thick as a finger. The "
     "pole is NOT drawn. Seen from the palm side: the four curled fingers lie horizontally across the front of the "
     "(invisible) pole, one above the other, their middle knuckles towards the viewer; the thumb comes round from the "
     "LEFT and rests over the index finger at the top. The top and bottom of the fist curve round where the pole "
     "enters and leaves, leaving a small notch of background above and below the fingers where the pole would be. "
     "The wrist disappears into the dark inside of the sleeve."),
]

# 2026-10-02: the staff is dropped (user: the fist never looked like it held it; the key visual has no staff). The
# far arm gets an open LEFT hand, seen from the palm side so it does not read as a second right hand.
LEFT_OPEN = [
    ("FOREARM_OPEN_L", "the same wide bell sleeve as the FOREARM parts in the attached sheets, straight and vertical, "
     "elbow end at the top, gold trim stripes at the cuff, mustard lining. Out of the INSIDE of the cuff comes a dark "
     "brown leather gloved LEFT hand, open and relaxed, hanging down, fingers slightly curled, seen from the PALM "
     "side (the palm and the inside of the fingers towards the viewer), thumb pointing to the RIGHT. It is the mirror "
     "partner of the right hand in the attached forearm sheet, not a copy of it. The wrist disappears into the dark "
     "inside of the sleeve, the front edge of the cuff overlaps the wrist."),
]

BRIEF = (
    "Create a CHARACTER PARTS SHEET for a 2D skeletal (cut-out) animation rig of the Alchemist, a playable character "
    "in a Slay the Spire 2 mod. The first attached image is the Alchemist's key visual: match its design, colours "
    "and painterly style exactly (charcoal hooded robe, mustard-gold trim, dark brown gloves and boots, glowing gold "
    "sigil face, dark-on-grey value range, soft brush texture, clean dark outline). The second attached image is "
    "another rig's parts page, ONLY to show the idea of a parts sheet: every body part drawn separately, whole, "
    "not overlapping. Do not copy its character or style.\n"
    "Rules: every part fully visible and separate, at least 40 px of background between parts, nothing touching the "
    "image edge, one consistent scale (as if the full assembled character were about 900 px tall), consistent light "
    "from the upper left. Background: a single flat pure magenta (#FF00FF) everywhere, no gradient, no shadows on "
    "the background, no ground, no text, no labels, no numbers, no frames.\n"
    "Draw these parts:\n")


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


tag = arg("--tag", "v1")
# --set forearms: only the FOREARMS above, with the first parts sheet as a reference so the style and scale match.
if arg("--set") == "forearms":
    PARTS = FOREARMS
    REFS = REFS + [OUT / "parts-v1.png"]
elif arg("--set") == "left":
    PARTS = LEFT_OPEN
    REFS = REFS + [OUT / "parts-v1.png", OUT / "parts-forearms-v1.png"]
elif arg("--set") == "grip":
    PARTS = GRIP
    REFS = REFS + [OUT / "parts-v1.png", OUT / "parts-forearms-v1.png"]
note = arg("--note", "")
codex = shutil.which("codex") or shutil.which("codex.cmd")
if not codex:
    sys.exit("codex が見つかりません。")
missing = [str(r) for r in REFS if not r.exists()]
if missing:
    sys.exit("参考画像がありません: " + ", ".join(missing))
OUT.mkdir(parents=True, exist_ok=True)
sheet = OUT / f"parts-{tag}.png"
task = ("Use your built-in image generation tool to create exactly one image, landscape (1536x1024). Then save it as "
        + sheet.relative_to(ROOT).as_posix() + " (overwrite if it exists). Do not edit any other file. Reply with the "
        "saved path only.\n\n" + BRIEF + "\n".join(f"- {name}: {desc}" for name, desc in PARTS)
        + (f"\nExtra direction: {note}" if note else "") + "\n")
cmd = [codex, "exec", "-s", "workspace-write", "-C", str(ROOT), "-c", 'model_reasoning_effort="low"']
for ref in REFS:
    cmd += ["-i", str(ref)]
cmd.append("-")
started = time.time()
log = ROOT / "artifacts/art-gen" / f"rig-parts-{tag}.log"
log.parent.mkdir(parents=True, exist_ok=True)
with open(log, "w", encoding="utf-8") as f:
    subprocess.run(cmd, input=task, text=True, encoding="utf-8", stdout=f, stderr=subprocess.STDOUT, timeout=900,
                   cwd=ROOT)
if not (sheet.exists() and sheet.stat().st_mtime >= started - 1):
    sys.exit(f"生成に失敗（ログ: {log}）")
print(f"{sheet} ({time.time() - started:.0f} s)")
