"""What to draw for each card: a prompt sheet for an image generator (ChatGPT etc.).

Usage: .\\scripts\\smoke.ps1 -Name card-dump; python scripts/art-prompts.py artifacts/smoke/card-dump.log [--all]
Reads the ALCHEMIST_CARDDUMP lines and writes assets/art/prompts.json and docs/art-prompts.md. Cards that already
have art in assets/art/cards are left out unless --all is given. Save each generated image as <file> (the slug) in
assets/art/incoming, then run scripts/import-art.py. See docs/art-pipeline.md.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
args = [a for a in sys.argv[1:] if not a.startswith("--")]
include_done = "--all" in sys.argv
log = Path(args[0]) if args else ROOT / "artifacts/smoke/card-dump.log"
rows = [json.loads(l.split("ALCHEMIST_CARDDUMP ", 1)[1])
        for l in log.read_text(encoding="utf-8", errors="replace").splitlines() if "ALCHEMIST_CARDDUMP " in l]
if not rows:
    sys.exit(f"no ALCHEMIST_CARDDUMP lines in {log}")
subjects_path = ROOT / "assets/art/subjects.json"
subjects = json.loads(subjects_path.read_text(encoding="utf-8")) if subjects_path.exists() else {}

# The base game's portrait sizes (smoke: ALCHEMIST_STAT portraitSize). import-art.py crops and scales to these.
SIZE = {"normal": (250, 190), "ancient": (250, 351)}

# Shared style for every card. Change it here; the sheet is regenerated from it.
STYLE = ("Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital "
         "painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple "
         "background, centered subject that still reads at small size. No text, no letters, no card frame, no border, "
         "no UI. The character is a travelling alchemist who fights by shifting between four elemental phases "
         "(earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in "
         "assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with "
         "one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, "
         "orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.")
PALETTE = {
    "Earth": "earth phase: ochre, moss green and stone grey, heavy and solid",
    "Water": "water phase: deep teal and aqua, flowing, misty",
    "Fire": "fire phase: crimson, orange and ember gold, explosive",
    "Air": "air phase: pale cyan and white, swirling wind, light and fast",
    "None": "neutral alchemy: brass, glass flasks, violet arcane light",
}
TYPE_HINT = {
    "Attack": "an attack in motion, aimed at an unseen enemy",
    "Skill": "a technique or alchemical action, defensive or utility",
    "Power": "a lasting aura, sigil or transformation, emblem-like",
}
PHASE_JA = {"Earth": "地", "Water": "水", "Fire": "火", "Air": "風", "None": "無相"}


def clean(text):
    text = re.sub(r"\[/?[a-z]+(=[^\]]*)?\]", "", text or "")
    return re.sub(r"\{InCombat:.*?\|\}", "", text).replace("\n", " ").strip()


def slug(card_id):
    return card_id.split("-", 1)[-1].lower()


def kind(r):
    if r["cls"].endswith("MaterialCard"):
        return "素材（選択画面の表示用）"
    if r["recipe"]:
        return "工房の錬成"
    return {"Basic": "初期デッキ", "Common": "コモン", "Uncommon": "アンコモン", "Rare": "レア",
            "Ancient": "エンシェント", "Token": "トークン"}.get(r["rarity"], r["rarity"])


done = {p.stem for p in (ROOT / "assets/art/cards").glob("*.png")}
entries = []
for r in sorted(rows, key=lambda r: slug(r["id"])):
    s = slug(r["id"])
    if s in done and not include_done:
        continue
    element = r["element"] or "None"
    w, h = SIZE["ancient" if r["rarity"] == "Ancient" else "normal"]
    subject = subjects.get(s, "")
    effect = clean(r["text"])
    prompt = (f"{STYLE}\n\nCard: 「{r['title']}」 ({r['type']}). {TYPE_HINT.get(r['type'], '')}. "
              f"Colours: {PALETTE.get(element, PALETTE['None'])}.\n"
              f"What the card does (Japanese, for mood only, do not write it): {effect}\n"
              + (f"Subject: {subject}\n" if subject else "Subject: illustrate the card's name and effect as one clear scene or object.\n")
              + f"Aspect ratio {w}:{h} (landscape), keep the subject inside the centre.")
    entries.append(dict(file=f"{s}.png", slug=s, title=r["title"], kind=kind(r), type=r["type"], element=element,
                        size=[w, h], effect=effect, subject=subject, prompt=prompt, has_art=s in done))

out = ROOT / "assets/art/prompts.json"
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(entries, ensure_ascii=False, indent=1), encoding="utf-8")

md = ["# カード絵のプロンプト一覧", "",
      "`scripts/art-prompts.py`が生成（手で編集しない。画風は同スクリプトの`STYLE`、題材は`assets/art/subjects.json`で変える）。"
      "生成した画像は`assets/art/incoming/`に**ファイル名どおり**保存し、`python scripts/import-art.py`で取り込む。手順は`docs/art-pipeline.md`。", "",
      f"- 対象：**{len(entries)}枚**（絵がまだないカード{'。--allで全カード' if not include_done else '・全カード'}）",
      f"- サイズ：通常{SIZE['normal'][0]}×{SIZE['normal'][1]}、エンシェント{SIZE['ancient'][0]}×{SIZE['ancient'][1]}（横長。大きめに作ってよい。取り込み時に中央を切り抜いて縮小する）", "",
      "## 共通の画風（すべてのプロンプトの先頭に入っている）", "", "```", STYLE, "```", ""]
for e in entries:
    md += [f"## {e['title']} → `{e['file']}`", "",
           f"- {e['kind']}・{e['type']}・{PHASE_JA.get(e['element'], e['element'])}　効果：{e['effect']}",
           *( [f"- 題材：{e['subject']}"] if e["subject"] else [] ), "",
           "```", e["prompt"], "```", ""]
(ROOT / "docs/art-prompts.md").write_text("\n".join(md), encoding="utf-8")
print(f"{len(entries)} prompts -> assets/art/prompts.json, docs/art-prompts.md")
