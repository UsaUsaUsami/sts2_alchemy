"""Asks Codex (codex exec, the user's ChatGPT login) to look at images and prints only its answer, so Claude reads a
short reply instead of the image. Read-only sandbox, no session kept. Spends the user's ChatGPT quota.

Usage: python scripts/ask-codex.py "question" image.png [image2.png ...] [--effort low|medium|high]
"""
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
args = sys.argv[1:]
effort = "medium"
if "--effort" in args:
    i = args.index("--effort")
    effort = args[i + 1]
    del args[i:i + 2]
if len(args) < 2:
    sys.exit(__doc__)
prompt, images = args[0], args[1:]
codex = shutil.which("codex") or shutil.which("codex.cmd")
if not codex:
    sys.exit("codex が見つかりません。")
for image in images:
    if not Path(image).exists():
        sys.exit(f"画像がありません: {image}")
with tempfile.TemporaryDirectory() as tmp:
    answer = Path(tmp) / "answer.txt"
    cmd = [codex, "exec", "-s", "read-only", "--ephemeral", "--skip-git-repo-check", "-C", str(ROOT),
           "-c", f'model_reasoning_effort="{effort}"', "-o", str(answer)]
    for image in images:
        cmd += ["-i", str(Path(image).resolve())]
    cmd.append("-")
    task = ("Look only at the attached image(s); do not run commands or read files. Answer in Japanese, briefly.\n\n"
            + prompt)
    run = subprocess.run(cmd, input=task, text=True, encoding="utf-8", capture_output=True, timeout=900, cwd=ROOT)
    if not answer.exists():
        sys.exit(f"Codex の応答がありません:\n{run.stdout[-2000:]}\n{run.stderr[-2000:]}")
    sys.stdout.reconfigure(encoding="utf-8")
    print(answer.read_text(encoding="utf-8").strip())
