"""Generate card art with Codex's image generation, in parallel, into assets/art/incoming.

Usage: python scripts/generate-art.py [--only fire_spark,earth_bedrock] [--kind レア] [--limit 10] [--jobs 3] [--force]
Needs the Codex CLI logged in with ChatGPT (`codex login status`) and assets/art/prompts.json (scripts/art-prompts.py).
Each card is one `codex exec` run that generates one image and copies it to assets/art/incoming/<slug>.png; logs go
to artifacts/art-gen/<slug>.log. Cards that already have art (assets/art/cards) or an incoming file are skipped
unless --force. Review the images, then run scripts/import-art.py. See docs/art-pipeline.md.
"""
import json
import shutil
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
INCOMING = ROOT / "assets/art/incoming"
LOGS = ROOT / "artifacts/art-gen"
KEY_VISUAL = ROOT / "assets/concepts/alchemist-character-v3.png"


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


only = set(filter(None, (arg("--only") or "").split(",")))
kind = arg("--kind")
limit = int(arg("--limit", "0"))
jobs = int(arg("--jobs", "3"))
force = "--force" in sys.argv
feedback = arg("--note", "")  # extra instruction for this batch, e.g. after a review

codex = shutil.which("codex") or shutil.which("codex.cmd")
if not codex:
    sys.exit("codex が見つかりません（npm install -g @openai/codex）。")
entries = json.loads((ROOT / "assets/art/prompts.json").read_text(encoding="utf-8"))
done = {p.stem for p in (ROOT / "assets/art/cards").glob("*.png")}
todo = []
for e in entries:
    if only and e["slug"] not in only:
        continue
    if kind and e["kind"] != kind:
        continue
    if not force and (e["slug"] in done or (INCOMING / e["file"]).exists()):
        continue
    todo.append(e)
if limit:
    todo = todo[:limit]
if not todo:
    sys.exit("生成するカードがありません。")
INCOMING.mkdir(parents=True, exist_ok=True)
LOGS.mkdir(parents=True, exist_ok=True)


def run(e):
    target = INCOMING / e["file"]
    task = (
        "Use your built-in image generation tool to create exactly one image from the prompt below. "
        f"Then save that generated image file as assets/art/incoming/{e['file']} in the current repository "
        "(create the folder if needed, overwrite if it exists). Do not edit any other file. "
        "When done, reply with the saved path only.\n\n"
        + (f"EXTRA DIRECTION: {feedback}\n\n" if feedback else "")
        + f"PROMPT:\n{e['prompt']}\n")
    started = time.time()
    with open(LOGS / f"{e['slug']}.log", "w", encoding="utf-8") as log:
        try:
            # The key visual is attached so the alchemist looks the same in every card.
            subprocess.run([codex, "exec", "-s", "workspace-write", "-C", str(ROOT), "-i", str(KEY_VISUAL), "-"],
                           input=task, text=True,
                           encoding="utf-8", stdout=log, stderr=subprocess.STDOUT, timeout=900, cwd=ROOT)
        except subprocess.TimeoutExpired:
            log.write("\nTIMEOUT\n")
    ok = target.exists() and target.stat().st_mtime >= started - 1
    return e, ok, time.time() - started


print(f"{len(todo)}枚を{jobs}並列で生成します。")
results = []
with ThreadPoolExecutor(max_workers=jobs) as pool:
    for e, ok, secs in pool.map(run, todo):
        results.append((e, ok))
        print(f"{'OK ' if ok else 'NG '} {e['slug']:<32} {e['title']}（{secs:.0f}秒）", flush=True)
failed = [e["slug"] for e, ok in results if not ok]
print(f"\n成功 {len(results) - len(failed)} / 失敗 {len(failed)}")
if failed:
    print("失敗（ログは artifacts/art-gen/）:", ",".join(failed))
    print("再実行: python scripts/generate-art.py --only " + ",".join(failed))
