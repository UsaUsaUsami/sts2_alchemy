"""Generate key-visual (character design) candidates for the alchemist with Codex, using StS2 art as reference.

Usage: python scripts/keyvisual.py [--only flask,beak] [--jobs 3] [--note "extra direction"] [--tag v3]
Needs .research/sts2-ref (python scripts/extract-sts2-refs.py) and the Codex CLI logged in with ChatGPT.
Writes assets/concepts/keyvisual/kv-<tag>-<id>.png; logs in artifacts/art-gen/kv-<tag>-<id>.log.
Why the references: without them Codex drew a heavily outlined handsome face (user, 2026-09-29). StS2's cast never
shows a face and keeps line work minimal, with large flat colour masses.
"""
import shutil
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
REFS = [ROOT / ".research/sts2-ref/lineup_small.png", ROOT / ".research/sts2-ref/characterselect_silent.png"]
OUT = ROOT / "assets/concepts/keyvisual"
LOGS = ROOT / "artifacts/art-gen"

BRIEF = (
    "Design an ORIGINAL playable character for a Slay the Spire 2 mod: the Alchemist. The attached images are the "
    "base game's own cast, given only as a visual-language reference. Match that language, do not copy any of those "
    "characters, costumes, masks or poses.\n"
    "Match these traits of the reference: the face is never shown (hidden by a mask, helmet, hood or a non-human head); "
    "minimal line work (thin, sparse outlines at most, no heavy black inking, no anime or comic linework); large flat "
    "colour masses with soft painterly brush texture inside them; a strong, simple, readable silhouette; one dominant "
    "colour per character; mostly muted cloth with a few saturated accents; no glamour, no pretty face, no handsome "
    "hero features.\n"
    "Character identity (show it through props and shapes): a travelling alchemist who fights by shifting between four "
    "elemental phases - earth (iron, ochre), water (herbs, teal), fire (black powder, orange) and air (ether, pale "
    "cyan) - harvests materials with a portable furnace, and grows a small homunculus companion.\n"
    "Output: one full-body character, standing three-quarter view, portrait orientation (2:3), plain flat mid-grey "
    "background, no text, no logo, no frame, no extra characters unless the direction asks for the homunculus.\n")

DIRECTIONS = {
    "flask": "Direction: the head itself is a large round glass flask with a cork/brass stopper; inside it four "
             "coloured liquids (ochre, teal, orange, pale cyan) swirl slowly. Long dark coat, leather gloves, belt of "
             "reagent vials. Dominant colour: deep indigo.",
    "beak": "Direction: an alchemist-physician wearing a smooth brass beak mask with round smoked-glass eyes (not a "
            "copy of a historical plague doctor: simplified, graphic shapes), wide-brimmed hat, long herb-stuffed "
            "satchel, apron. Dominant colour: moss green.",
    "furnace": "Direction: a stocky artisan who carries a small iron furnace on the back like a backpack, glowing "
               "orange vents; the face is hidden behind a full welding-style mask with one horizontal smoked-glass "
               "slit; thick apron, heavy tongs. Dominant colour: rust red.",
    "homunculus": "Direction: a slim alchemist in a deep hood that hides the face in shadow, holding up a glass jar in "
                  "which a small, cute but eerie homunculus floats and presses its hands to the glass. The homunculus "
                  "is part of the character's identity. Dominant colour: violet.",
    "sigil": "Direction: a robed figure whose face is replaced by a softly glowing alchemical sigil (a circle with a "
             "triangle and an ouroboros) floating inside the hood; the robe is patterned with faint transmutation "
             "circles in the four element colours. Dominant colour: charcoal with gold.",
}


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


only = set(filter(None, (arg("--only") or "").split(",")))
jobs = int(arg("--jobs", "3"))
note = arg("--note", "")
tag = arg("--tag", "v3")
codex = shutil.which("codex") or shutil.which("codex.cmd")
if not codex:
    sys.exit("codex が見つかりません（npm install -g @openai/codex）。")
missing = [str(r) for r in REFS if not r.exists()]
if missing:
    sys.exit("参考画像がありません。先に python scripts/extract-sts2-refs.py: " + ", ".join(missing))
OUT.mkdir(parents=True, exist_ok=True)
LOGS.mkdir(parents=True, exist_ok=True)
todo = [(k, v) for k, v in DIRECTIONS.items() if not only or k in only]


def run(item):
    key, direction = item
    name = f"kv-{tag}-{key}"
    target = OUT / f"{name}.png"
    rel = target.relative_to(ROOT).as_posix()
    task = ("Use your built-in image generation tool to create exactly one image. Use the attached images as style "
            "reference as described. Then save the generated image as " + rel + " in the current repository "
            "(overwrite if it exists). Do not edit any other file. Reply with the saved path only.\n\n"
            + BRIEF + "\n" + direction + (f"\nExtra direction: {note}" if note else "") + "\n")
    cmd = [codex, "exec", "-s", "workspace-write", "-C", str(ROOT)]
    for ref in REFS:
        cmd += ["-i", str(ref)]
    cmd.append("-")
    started = time.time()
    with open(LOGS / f"{name}.log", "w", encoding="utf-8") as log:
        try:
            subprocess.run(cmd, input=task, text=True, encoding="utf-8", stdout=log, stderr=subprocess.STDOUT,
                           timeout=900, cwd=ROOT)
        except subprocess.TimeoutExpired:
            log.write("\nTIMEOUT\n")
    return key, target.exists() and target.stat().st_mtime >= started - 1, time.time() - started


print(f"{len(todo)}案を{jobs}並列で生成します。")
with ThreadPoolExecutor(max_workers=jobs) as pool:
    for key, ok, secs in pool.map(run, todo):
        print(f"{'OK' if ok else 'NG'} {key}（{secs:.0f}秒）", flush=True)
