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
        "shape, two or three flat colours, a soft lighter outline, no background scene, NO fine detail. Fills about 70% "
        "of its cell.")
# Materials: painted like relics but imported at power sizes (256 and 64), since they sit in lists, not a relic bar.
ITEM = RELIC.replace("Relic icon", "Item icon")
# Element colours (user, 2026-10-01): earth brown, water blue, fire red, air light blue. The marks are the four on the
# alchemist's robe in the key visual.
EARTH, WATER, FIRE, AIR = "earth brown", "deep blue", "red", "light sky blue"

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
    # Powers of the phase-transition axis. Slugs are the class names (IconArt.Slug).
    "powers-phase": [
        ("flame_heart_power", FLAT, f"焔の心 (gain strength on entering fire): a {FIRE} flame shaped like a heart"),
        ("sky_power", FLAT, "オーバードライブ (energy next turn after many transitions): a gold lightning bolt over a small "
         "circular arrow"),
        ("wind_afterimage_power", FLAT, f"残風の刃 (damage per transition at end of turn): a {AIR} crescent blade with two "
         "faded copies trailing behind it"),
        ("earth_king_power", FLAT, f"大地の王 (block is kept): a {EARTH} stone crown"),
        ("preparation_power", FLAT, "励起 (next transition is stronger): a gold upward chevron with a small glow"),
        ("synergy_power", FLAT, "ダブルシフト (next transition fires twice): two gold curved arrows chasing each other"),
        ("philosophers_stone_power", FLAT, "賢者の石 (all transitions stronger): a single faceted red gemstone"),
        ("earth_core_power", FLAT, f"大地の心核: a round {EARTH} core with a plain triangle on it"),
        ("water_core_power", FLAT, f"流水の心核: a round {WATER} core with a plain water drop on it"),
        ("fire_core_power", FLAT, f"劫火の心核: a round {FIRE} core with a plain flame on it"),
        ("air_core_power", FLAT, f"疾風の心核: a round {AIR} core with a plain swirl on it"),
        ("phase_wheel_power", FLAT, f"四相輪転 (moves to the next phase): a wheel split in four quarters, {EARTH}, {WATER}, "
         f"{FIRE}, {AIR}, with one arrow around it"),
    ],
    # Powers of the life axis (drain and the golem) and the rest.
    "powers-life": [
        ("crucible_power", FLAT, "エレメント・リローデッド (a material each turn): a small grey-brown ore lump with a gold "
         "circular arrow"),
        ("self_cultivation_power", FLAT, "自らを糧に (drain yourself for more golem HP): a dark red drop falling into an open "
         "hand"),
        ("nourish_power", FLAT, "養分 (drain gives extra golem HP): a green sprout growing from a dark red drop"),
        ("symbiosis_power", FLAT, "共生 (block while the golem stands): two interlocked rings, one ochre stone, one silver"),
        ("awakened_vessel_power", FLAT, "番人の目覚め (golem HP becomes damage): a round ochre stone with one glowing gold eye"),
        ("philosophers_blood_power", FLAT, "賢者の血 (golem blocks, enemy is drained): a gold drop with a small red core"),
        ("gate_of_truth_power", FLAT, "真理の扉 (big golem HP hits all enemies): a tall stone door slightly open, white "
         "light from the gap"),
        ("culture_vat_power", FLAT, "自己修復 (golem HP each turn): a round ochre stone with a green plus sign"),
        ("life_drain_power", FLAT, "ドレイン (loses HP that feeds the golem): a dark crimson drop with a thin curl rising from "
         "it"),
        ("homunculus_power", FLAT, "ゴーレム (the golem takes hits): the head of a chunky ochre stone golem, one glowing gold "
         "circle-and-triangle eye"),
        ("death_mark_power", FLAT, "死亡 (dies next turn): a plain cracked grey skull"),
        ("drain_miasma_power", FLAT, "吸精の瘴気 (drain on all enemies): a dark crimson cloud of mist"),
    ],
    # Materials, the material reward, and the four element marks for the phase dial (PhaseDial).
    "materials": [
        ("material_iron", ITEM, "鉄 (iron): one grey iron ore nugget"),
        ("material_herb", ITEM, "薬草 (herb): a small bunch of green leaves"),
        ("material_powder", ITEM, "火薬 (gunpowder): a small heap of black powder with a few red sparks"),
        ("material_ether", ITEM, f"エーテル (ether): a small glass ampoule of glowing {AIR} vapour"),
        ("material_mercury", ITEM, "水銀 (mercury): a round bead of shiny silver liquid metal"),
        ("material_stardust", ITEM, "星砂 (star sand): a small pile of gold sand with star-shaped grains"),
        ("material_void_crystal", ITEM, "虚無結晶 (void crystal): one dark violet-black crystal"),
        ("material_reward", ITEM, "素材 (material reward): an iron nugget, a green leaf and a small vial together"),
        ("phase_earth", FLAT, f"地 (earth): a plain upward triangle, flat {EARTH}"),
        ("phase_water", FLAT, f"水 (water): a plain water drop, flat {WATER}"),
        ("phase_fire", FLAT, f"火 (fire): a plain flame, flat {FIRE}"),
        ("phase_air", FLAT, f"風 (air): a plain spiral swirl, flat {AIR}"),
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
