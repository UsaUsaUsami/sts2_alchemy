"""Generate card art four cards at a time: one Codex image call makes a 2x2 sheet, which is cut into four files.

Usage: python scripts/generate-art-grid.py [--only a,b,c,d | --limit 4] [--per 4] [--reasoning low] [--force]
Why: every `codex exec` costs model tokens plus one image generation. Card portraits are 250x190, so a quarter of a
1536x1024 sheet (768x512) is plenty; four cards per call cuts the calls to a quarter (user, 2026-09-29: the ChatGPT
usage limit was being eaten by one image per call).
Writes assets/art/incoming/<slug>.png for each card (then scripts/import-art.py) and keeps the sheet in
artifacts/art-gen/grid-<first slug>.png. Logs: artifacts/art-gen/grid-<first slug>.log.
"""
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
INCOMING = ROOT / "assets/art/incoming"
LOGS = ROOT / "artifacts/art-gen"
KEY_VISUAL = ROOT / "assets/concepts/alchemist-character-v3.png"
POSITIONS = ["top-left", "top-right", "bottom-left", "bottom-right"]
# Composition rules that stop the four panels (and the whole set) from repeating "alchemist reaching out at the left".
# Exactly one panel per sheet may show the alchemist, and only as hands or a small/back view; the others show no person.
SHOTS = ["a wide establishing shot of the environment, the effect small in the middle distance",
         "a tight close-up of one object or one effect, filling the frame",
         "a low-angle or top-down view of the effect hitting the ground or an unseen target",
         "a symmetrical, emblem-like centred composition",
         "a diagonal action composition with the effect crossing the frame edge to edge"]


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


only = [s for s in (arg("--only") or "").split(",") if s]
limit = int(arg("--limit", "4"))
reasoning = arg("--reasoning", "low")
force = "--force" in sys.argv
codex = shutil.which("codex") or shutil.which("codex.cmd")
if not codex:
    sys.exit("codex が見つかりません（npm install -g @openai/codex）。")
entries = json.loads((ROOT / "assets/art/prompts.json").read_text(encoding="utf-8"))
by_slug = {e["slug"]: e for e in entries}
if only:
    missing = [s for s in only if s not in by_slug]
    if missing:
        sys.exit("prompts.json にないカード: " + ",".join(missing))
    todo = [by_slug[s] for s in only]
else:
    todo = [e for e in entries if not e["has_art"] and not (INCOMING / e["file"]).exists()
            and e["kind"] in ("コモン", "アンコモン", "レア", "初期デッキ", "工房の錬成")]
if not force:
    todo = [e for e in todo if not e["has_art"] and not (INCOMING / e["file"]).exists()]
todo = todo[:min(limit, 4)]
if not todo:
    sys.exit("生成するカードがありません。")
if len(todo) < 4:
    print(f"注意: {len(todo)}枚だけです。1回の生成で4枚に満たない分の枠は無駄になります。")

style = todo[0]["prompt"].split("\n\nCard:", 1)[0]
panels = []
seed = sum(ord(c) for e in todo for c in e["slug"])  # deterministic for a given set of cards
alch_panel = seed % len(todo)
for i, (pos, e) in enumerate(zip(POSITIONS, todo)):
    body = e["prompt"].split("\n\nCard:", 1)[1].rsplit("\nAspect ratio", 1)[0]
    shot = SHOTS[(seed + i * 2) % len(SHOTS)]
    who = ("The alchemist MAY appear here, but only as a pair of hands or a small figure seen from behind, never "
           "large, never reaching in from the left edge." if i == alch_panel else
           "NO person, no hands and no alchemist in this panel.")
    panels.append(f"PANEL {pos}: Card:{body.strip()}\nComposition: {shot}. {who}")

LOGS.mkdir(parents=True, exist_ok=True)
INCOMING.mkdir(parents=True, exist_ok=True)
sheet = LOGS / f"grid-{todo[0]['slug']}.png"
task = (
    "Use your built-in image generation tool to create exactly one image, landscape 3:2 (1536x1024). Use the attached "
    "key visual only as the reference for how the alchemist looks. The image is a 2x2 contact sheet of FOUR separate "
    "card illustrations: each panel fills exactly one quarter of the image, the four panels touch with NO gaps, "
    "borders, gutters, frames or text, and nothing may cross a panel edge. Every panel is its own complete scene; "
    "keep each panel's main subject well inside its centre. Same painting style in all four panels.\n"
    "Then save the generated image as " + sheet.relative_to(ROOT).as_posix() + " in the current repository "
    "(overwrite if it exists). Do not edit any other file. Reply with the saved path only.\n\n"
    "COMMON STYLE FOR EVERY PANEL:\n" + style + "\n\n" + "\n\n".join(panels) + "\n")
cmd = [codex, "exec", "-s", "workspace-write", "-C", str(ROOT), "-c", f'model_reasoning_effort="{reasoning}"',
       "-i", str(KEY_VISUAL), "-"]
started = time.time()
print(f"{len(todo)}枚（{', '.join(e['title'] for e in todo)}）を1回で生成します。")
with open(LOGS / f"grid-{todo[0]['slug']}.log", "w", encoding="utf-8") as log:
    try:
        subprocess.run(cmd, input=task, text=True, encoding="utf-8", stdout=log, stderr=subprocess.STDOUT,
                       timeout=900, cwd=ROOT)
    except subprocess.TimeoutExpired:
        log.write("\nTIMEOUT\n")
if not (sheet.exists() and sheet.stat().st_mtime >= started - 1):
    sys.exit(f"生成に失敗（ログ: {LOGS / ('grid-' + todo[0]['slug'] + '.log')}）")

image = Image.open(sheet).convert("RGB")
w, h = image.size
hw, hh = w // 2, h // 2
trim = max(2, w // 200)  # drop the seam pixels where two panels meet
for i, e in enumerate(todo):
    x, y = (i % 2) * hw, (i // 2) * hh
    image.crop((x + trim, y + trim, x + hw - trim, y + hh - trim)).save(INCOMING / e["file"])
    print(f"OK {e['slug']:<28} {e['title']}")
print(f"{time.time() - started:.0f}秒。シート: {sheet}。確認: python scripts/art-sheet.py incoming --cols 4")
