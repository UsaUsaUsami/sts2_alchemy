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

# 2026-10-02: the attack pushes the open palm at the enemy (user). Drawn as it is seen in the strike, sleeve
# horizontal; rig-build.py turns it so the elbow is at the top like the other forearms, and swaps it in during attack.
PUSH = [
    ("FOREARM_PUSH_L", "the same wide bell sleeve as the FOREARM parts in the attached sheets, lying HORIZONTAL: the "
     "elbow end on the LEFT, the open cuff on the RIGHT, gold trim stripes at the cuff, mustard lining. Out of the "
     "INSIDE of the cuff comes a dark brown leather gloved LEFT hand, the wrist bent sharply UP: the fingers point "
     "straight UP, the palm faces RIGHT as if pushing a spell forward, seen from the thumb side (the thumb towards "
     "the viewer, lying along the index finger). The wrist disappears into the dark inside of the sleeve, the front "
     "edge of the cuff overlaps the wrist."),
]

# 2026-10-02: the cast raises the open hand (user). Drawn as seen in the cast, forearm rising at 45 degrees;
# rig-build.py turns it so the elbow is at the top and swaps it in during cast.
RAISE = [
    ("FOREARM_RAISE_L", "the same wide bell sleeve as the FOREARM parts in the attached sheets, rising DIAGONALLY at 45 "
     "degrees: the elbow end at the LOWER LEFT, the open cuff at the UPPER RIGHT, gold trim stripes at the cuff, "
     "mustard lining. Out of the INSIDE of the cuff comes a dark brown leather gloved LEFT hand raised towards the "
     "sky, open, fingers spread, pointing up and to the right, the palm facing UP, seen from the thumb side (the "
     "thumb towards the viewer). The wrist disappears into the dark inside of the sleeve, the front edge of the cuff "
     "overlaps the wrist."),
]

# 2026-10-02: the golem's own rig (user: the golem too, with a hit reaction). Parts after assets/art/pets/golem.png.
GOLEM = [
    ("GOLEM_BODY", "the golem's body: one big rounded boulder that is head and torso in one, made of a few smooth "
     "ochre stones, facing RIGHT in three-quarter view, the glowing gold alchemical sigil (circle, triangle, small "
     "circle) on its front as its face, thin glowing seams in the four element colours (red, blue, green, white) "
     "between the stones, a gold band or two. NO arms and NO legs: flat stone sockets where they attach."),
    ("GOLEM_ARM", "one golem arm hanging straight down, shoulder at the top: a round shoulder stone with a gold band, "
     "a thick forearm stone, and a big round stone fist at the bottom."),
    ("GOLEM_LEG", "one short stubby golem leg, straight and vertical, hip end at the top: a thick stone with a gold "
     "band at the ankle and a flat stone foot pointing RIGHT."),
    ("PEBBLES", "three small loose ochre stone chips of different sizes, side by side, for flying debris."),
]

# 2026-10-02: the rest site (user). The alchemist sits by the fire, facing RIGHT, with a steaming cup, the golem
# resting beside; the hood is the standing rig's HOOD.
REST = [
    ("REST_BODY", "the Alchemist SITTING on the ground facing RIGHT in three-quarter view, NO head and NO arms: the "
     "charcoal robe over the torso with the scarf and gold ring clasp at the neck, the vertical front band with "
     "element symbols, brown sash with the two small flasks, and the legs folded with knees up in front, robe "
     "draped over them with the gold-striped hem on the ground, dark brown boots showing at the front. Flat "
     "shoulder area where arms attach."),
    ("REST_ARMS", "both of the Alchemist's forearms and gloved hands together, held in front of the chest, wide "
     "charcoal bell sleeves with gold-striped cuffs, the dark brown gloved hands coming out of the cuffs holding a "
     "small round wooden cup with a thin wisp of steam, seen from the side facing RIGHT. No upper arms, no body."),
    ("GOLEM_REST", "the stone golem companion (the one in the attached golem image) RESTING: sitting slumped on the "
     "ground with its stone arms around its knees, the gold sigil face glowing softly, facing RIGHT. Whole, alone."),
]

# 2026-10-02: the first rest body had flat sockets at the shoulders that no arm piece covered convincingly; the
# seated figure is drawn whole (arms and cup included), only the head separate.
REST2 = [
    ("REST_SEATED", "the Alchemist SITTING on the ground facing RIGHT in three-quarter view, complete EXCEPT the "
     "head: no head and no hood, the neck ends in the charcoal scarf with the gold ring clasp (an empty dark neck "
     "opening on top, where the separate hood will sit). Both arms are drawn: wide charcoal bell sleeves with "
     "gold-striped cuffs, the dark brown gloved hands coming out of the cuffs and holding a small round wooden cup "
     "in front of the chest, resting on the raised knees. Charcoal robe with the vertical front band and element "
     "symbols, brown sash with the two small flasks, the robe draped over the folded legs with the gold-striped hem "
     "on the ground, dark brown boots at the front. No sockets, no cut ends: everything that shows is finished."),
]

# 2026-10-02 (v2): the first golem parts made it tall and long-legged; the user wants it to follow the original
# picture closely (assets/art/pets/golem.png): squat, the body a huge round boulder as wide as tall, stubby legs
# that barely show, short arms hanging from the middle of the body with big fists near the ground.
GOLEM2 = [
    ("GOLEM_BODY", "the golem's body exactly as in the FIRST attached image: one huge round boulder, about as wide as "
     "it is tall, that is head and torso in one, built of large smooth ochre stones with glowing seams (orange on "
     "the left, green low in the middle, blue on the right), the big glowing gold sigil (circle, triangle, small "
     "circle) on its front-right. Same stone shapes, colours and soft painterly style as that image. WITHOUT the "
     "arms and WITHOUT the legs: where they were, just more of the boulder's stones (no sockets, no holes, no flat "
     "discs), the bottom of the boulder rounded."),
    ("GOLEM_ARM", "one golem arm exactly as in the FIRST attached image, hanging straight down, shoulder at the top: "
     "a short upper stone with the gold band and triangular gold plate, then a big round stone fist made of a few "
     "chunky stones. Short and chunky, about half the height of the body."),
    ("GOLEM_LEG", "one golem leg exactly as in the FIRST attached image: a very short stubby stone, with the gold "
     "band and triangular gold plate at its top, ending in a rounded stone foot pointing RIGHT. About a quarter of "
     "the body's height."),
    ("PEBBLES", "three small loose ochre stone chips of different sizes, side by side, for flying debris."),
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
elif arg("--set") == "rest2":
    PARTS = REST2
    REFS = REFS + [OUT / "parts-v1.png", OUT / "parts-rest-v1.png", ROOT / "assets/art/character/rest_site.png"]
elif arg("--set") == "rest":
    PARTS = REST
    REFS = REFS + [OUT / "parts-v1.png", ROOT / "assets/art/pets/golem.png", ROOT / "assets/art/character/rest_site.png"]
elif arg("--set") == "golem2":
    PARTS = GOLEM2
    REFS = [ROOT / "assets/art/pets/golem.png", ROOT / "artifacts/hs-alchemist-page.png"]
elif arg("--set") == "golem":
    PARTS = GOLEM
    REFS = [ROOT / "assets/art/pets/golem.png", ROOT / "artifacts/hs-alchemist-page.png", OUT / "parts-v1.png"]
elif arg("--set") == "raise":
    PARTS = RAISE
    REFS = REFS + [OUT / "parts-v1.png", OUT / "parts-push-v1.png"]
elif arg("--set") == "push":
    PARTS = PUSH
    REFS = REFS + [OUT / "parts-v1.png", OUT / "parts-left-v1.png"]
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
