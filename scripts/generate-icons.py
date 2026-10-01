"""Generate small icons (relics, powers, materials, workshop) twelve at a time: one Codex image call draws a 4x3 sheet
on a flat magenta background, which is cut into twelve transparent PNGs.

Usage: python scripts/generate-icons.py <group> [--reasoning low] [--tolerance 60]
       python scripts/generate-icons.py <group> --cut-only   (re-cut an existing sheet, no generation)
Groups are the keys of GROUPS below. Writes assets/art/incoming/icons/<slug>.png (cropped to the icon, square, with
a transparent margin) and keeps the sheet in artifacts/art-gen/icons-<group>.png. Then scripts/import-icons.py.
Why twelve: relic and power icons are 256x256 in the base game, so a 384x341 cell of a 1536x1024 sheet is enough,
and one call costs the same as the four-card sheets (about 4% of the 5-hour ChatGPT window, user 2026-09-29).
The style reference handed to Codex is artifacts/art-gen/vanilla-icons-ref.png (base-game icons cut from the local
pck; git-ignored, never shipped, AGENTS.md 3).
"""
import shutil
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
INCOMING = ROOT / "assets/art/incoming/icons"
LOGS = ROOT / "artifacts/art-gen"
REFERENCE = LOGS / "vanilla-icons-ref.png"
COLS, ROWS = 4, 3
KEY = (255, 0, 255)
INK = (59, 42, 30)

RELIC = ("Relic icon in the style of Slay the Spire 2 (see the reference sheet, top row): ONE simple object, as plain "
         "as the reference: big soft shapes, only three to five flat colour masses with gentle painterly shading, NO "
         "fine detail, NO engraving, NO ornament, NO texture noise, NO small symbols, NO sparkles. A dark outline and a "
         "thin light-grey sticker rim around the whole object. It must read at 40 pixels. Fills about 70% of its cell.")
# User, 2026-10-01: the first sheet was too busy, the phase-transition relics most of all. Keep subjects to one idea.
MAP = ("Map legend symbol like the base game's map icons: drawn only in dark brown ink (#3b2a1e) with a few bold "
       "sketchy strokes, little or no hatching, no colour fill, no rim. Fills about 60% of its cell.")
FLAT = ("Flat emblem like the base game's power and enchantment icons (reference sheet, bottom row): one simple bold "
        "shape, two or three flat colours, a soft lighter outline, no background scene. Fills about 70% of its cell.")

GROUPS = {
    # Trial (user, 2026-10-01: try one sheet first). DarvCrucible is the ancient relic kept for old saves.
    "relics": [
        ("material_box", RELIC, "素材ボックス, the starter relic: a small plain wooden box with the lid open, a few coloured "
         "lumps inside"),
        ("refined_material_box", RELIC, "精錬された素材ボックス, the same small box upgraded: dark wood with gold edges, a warm "
         "orange glow from inside"),
        ("darv_crucible", RELIC, "ダーヴの坩堝, an ancient squat green-bronze crucible with glowing embers inside"),
        ("phase_compass", RELIC, "方位盤, a plain round brass compass seen from the front, one gold needle, no markings"),
        ("blood_chalice", RELIC, "血の杯, a plain silver chalice with one red drop running down its side"),
        ("pulsing_core", RELIC, "脈打つ核石, a round ochre stone with one glowing gold crack, like a heart"),
        ("quadrant", RELIC, "四分儀, a plain brass quarter-circle instrument (a quadrant), nothing else"),
        ("great_crucible", RELIC, "大坩堝, a large plain black iron pot on three legs, filled with glowing gold liquid"),
        ("wardens_foundation", RELIC, "番人の礎, a single square ochre foundation stone block, slightly cracked"),
        ("large_material_bag", RELIC, "大きな素材鞄, a big bulging brown leather sack with one buckle"),
        ("workshop_map", MAP, "工房 (alchemist's workshop node on the map): an alembic flask standing on a small anvil"),
        ("workshop_modify", FLAT, "改造 (workshop modification of a card): a small plain hammer, flat grey and gold"),
    ],
}


def arg(name, default):
    return type(default)(sys.argv[sys.argv.index(name) + 1]) if name in sys.argv else default


def cut(sheet_path, icons, tolerance):
    """Key out the magenta, despill the fringe, crop to a padded square. Every magenta pixel goes, not only those
    reached from the border: gaps enclosed by the drawing (the map symbol's ink, a chalice's stem) are background too,
    and the icons are told never to use magenta themselves."""
    sheet = Image.open(sheet_path).convert("RGB")
    w, h = sheet.size
    cw, ch = w // COLS, h // ROWS
    trim = max(2, w // 300)
    close = lambda c: sum(abs(a - b) for a, b in zip(c, KEY)) <= tolerance * 3
    for i, (slug, style, _) in enumerate(icons):
        x0, y0 = (i % COLS) * cw, (i // COLS) * ch
        cell = sheet.crop((x0 + trim, y0 + trim, x0 + cw - trim, y0 + ch - trim)).convert("RGBA")
        cwid, chei = cell.size
        px = cell.load()
        if style == MAP:
            # The ink comes out tinted toward the magenta, so a colour key loses most strokes. Opacity is taken from
            # how dark a pixel is against the magenta (red + blue), and every stroke is painted the one ink colour.
            ink = Image.new("RGBA", cell.size, INK + (0,))
            ap = ink.load()
            for y in range(chei):
                for x in range(cwid):
                    r, _, b, _ = px[x, y]
                    a = round((440 - r - b) * 255 / 300)
                    ap[x, y] = INK + (max(0, min(255, a)) if a > 20 else 0,)
            cell, mask = ink, ink.getchannel("A")
        else:
            mask = Image.new("L", cell.size, 255)
            mp = mask.load()
            for y in range(chei):
                for x in range(cwid):
                    if close(px[x, y][:3]):
                        mp[x, y] = 0
            mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
            # Despill: pink left in anti-aliased edge pixels is pulled toward the pixel's own green level.
            for y in range(chei):
                for x in range(cwid):
                    r, g, b, _ = px[x, y]
                    if r > g + 40 and b > g + 40:
                        m = max(g, min(r, b) - 60)
                        px[x, y] = (min(r, m + 40), g, min(b, m + 40), 255)
            cell.putalpha(mask)
        box = mask.getbbox()
        if not box:
            print(f"NG {slug}: 空のセル")
            continue
        icon = cell.crop(box)
        side = round(max(icon.size) * 1.08)
        square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        square.paste(icon, ((side - icon.width) // 2, (side - icon.height) // 2))
        square.save(INCOMING / f"{slug}.png")
        print(f"OK {slug:<24} {icon.width}x{icon.height}")


group = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("--") else None
if group not in GROUPS:
    sys.exit("グループを指定してください: " + ", ".join(GROUPS))
icons = GROUPS[group]
if len(icons) > COLS * ROWS:
    sys.exit(f"{group}: {len(icons)}個は多すぎます（1回{COLS * ROWS}個まで）。")
LOGS.mkdir(parents=True, exist_ok=True)
INCOMING.mkdir(parents=True, exist_ok=True)
sheet = LOGS / f"icons-{group}.png"
tolerance = arg("--tolerance", 60)

if "--cut-only" not in sys.argv:
    codex = shutil.which("codex") or shutil.which("codex.cmd")
    if not codex:
        sys.exit("codex が見つかりません（npm install -g @openai/codex）。")
    positions = [f"row {i // COLS + 1}, column {i % COLS + 1}" for i in range(len(icons))]
    cells = "\n".join(f"CELL {pos}: {style}\nSubject: {subject}" for pos, (_, style, subject) in zip(positions, icons))
    task = (
        "Use your built-in image generation tool to create exactly one image, landscape 3:2 (1536x1024). It is a "
        f"{COLS}x{ROWS} grid of {len(icons)} separate game icons, one icon centred in each equal cell, read left to "
        "right, top to bottom. The WHOLE background, in every cell, is one flat pure magenta (#FF00FF): no gradient, "
        "no texture, no floor, no cast shadow, no glow on the background, no grid lines, no borders, no text or "
        "letters anywhere. Icons never touch or cross a cell edge and never use magenta or pink themselves. The "
        "attached sheet shows base-game icons for STYLE ONLY: do not copy those objects.\n"
        "Then save the generated image as " + sheet.relative_to(ROOT).as_posix() + " in the current repository "
        "(overwrite if it exists). Do not edit any other file. Reply with the saved path only.\n\n" + cells + "\n")
    cmd = [codex, "exec", "-s", "workspace-write", "-C", str(ROOT), "-c", f'model_reasoning_effort="{arg("--reasoning", "low")}"']
    if REFERENCE.exists():
        cmd += ["-i", str(REFERENCE)]
    cmd.append("-")
    started = time.time()
    print(f"{len(icons)}個（{group}）を1回で生成します。")
    with open(LOGS / f"icons-{group}.log", "w", encoding="utf-8") as log:
        try:
            subprocess.run(cmd, input=task, text=True, encoding="utf-8", stdout=log, stderr=subprocess.STDOUT,
                           timeout=900, cwd=ROOT)
        except subprocess.TimeoutExpired:
            log.write("\nTIMEOUT\n")
    if not (sheet.exists() and sheet.stat().st_mtime >= started - 1):
        sys.exit(f"生成に失敗（ログ: {LOGS / ('icons-' + group + '.log')}）")
    print(f"{time.time() - started:.0f}秒。シート: {sheet}")

cut(sheet, icons, tolerance)
