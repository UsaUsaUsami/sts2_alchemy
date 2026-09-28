"""Builds docs/card-list.md from the card lines a smoke run prints (ALCHEMIST_CARDDUMP).

Usage: .\\scripts\\smoke.ps1 -Name card-dump; python scripts/card-list.py artifacts/smoke/card-dump.log
The text and numbers are the game's own (descriptions rendered outside combat), so the list matches the mod.
"""
import json
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
log = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "artifacts/smoke/card-dump.log"
rows = [json.loads(line.split("ALCHEMIST_CARDDUMP ", 1)[1])
        for line in log.read_text(encoding="utf-8", errors="replace").splitlines() if "ALCHEMIST_CARDDUMP " in line]
if not rows:
    sys.exit(f"no ALCHEMIST_CARDDUMP lines in {log}")

version = re.search(r'"version":\s*"([^"]+)"', (ROOT / "src/Alchemist/Alchemist.json").read_text(encoding="utf-8-sig")).group(1)
STARTER = {"EarthenGuard", "SoothingMist", "InstantAlchemy"}
TYPE = {"Attack": "アタック", "Skill": "スキル", "Power": "パワー"}
RARITY = {"Basic": "初期", "Common": "コモン", "Uncommon": "アンコモン", "Rare": "レア", "Ancient": "エンシェント", "Token": "トークン", "Event": "報酬外"}
TARGET = {"AnyEnemy": "敵1体", "AllEnemies": "敵全体", "Self": "自身", "None": "-", "RandomEnemy": "ランダムな敵"}
ELEMENT = {"None": "無相", "Earth": "地", "Water": "水", "Fire": "火", "Air": "風"}
ELEMENT_ORDER = {"地": 0, "水": 1, "火": 2, "風": 3, "無相": 4}
PHASE_LINE = re.compile(r"^(.相)(→.相)*$")


def clean(text):
    """Game BBCode to Markdown: upgrade changes ([green]) become bold, other tags go."""
    text = re.sub(r"\[green\](.*?)\[/green\]", r"**\1**", text)
    text = re.sub(r"\[/?[a-z]+(=[^\]]*)?\]", "", text)
    return text


def split_phase(row, text):
    """Most descriptions end with the element line; it gets its own column instead."""
    lines = clean(text).split("\n")
    phases = [l.strip() for l in lines if PHASE_LINE.match(l.strip())]
    if phases:
        return "\n".join(l for l in lines if not PHASE_LINE.match(l.strip())), phases[-1].replace("相", "")
    return "\n".join(lines), ELEMENT.get(row["element"] or "None", "-")


def cell(text):
    return text.strip().replace("|", "\\|").replace("\n", "<br>")


def cost(value, costs_x):
    return "X" if costs_x else str(value)


def table(items, show_rarity=False, show_recipe=False):
    head = ["名前"] + (["レア度"] if show_rarity else []) + ["種類", "コスト", "相"] + (["素材"] if show_recipe else []) + ["効果", "強化後"]
    out = ["| " + " | ".join(head) + " |", "|" + "---|" * len(head)]
    for r in items:
        text, phase = split_phase(r, r["text"])
        up = ""
        if r["upgradedText"] is not None:
            up_text, _ = split_phase(r, r["upgradedText"])
            parts = []
            if r["upgradedCost"] is not None and r["upgradedCost"] != r["cost"] and not r["costsX"]:
                parts.append(f"コスト{r['upgradedCost']}")
            if clean(r["upgradedText"]) != clean(r["text"]):
                parts.append(up_text)
            up = "<br>".join(cell(p) for p in parts) if parts else "（変化なし）"
        else:
            up = "（強化不可）"
        c = cost(r["cost"], r["costsX"])
        kind = f"{TYPE.get(r['type'], r['type'])}（{TARGET.get(r['target'], r['target'])}）"
        row = [f"**{r['title']}**"] + ([RARITY.get(r["rarity"], r["rarity"])] if show_rarity else []) + [kind, c, phase]
        if show_recipe:
            row.append("＋".join(r["recipe"] or []))
        row += [cell(text), up]
        out.append("| " + " | ".join(row) + " |")
    return "\n".join(out)


def order(r):
    _, phase = split_phase(r, r["text"])
    return (ELEMENT_ORDER.get(phase[:1] if phase else "無相", 5), list(TYPE).index(r["type"]) if r["type"] in TYPE else 9, r["cost"], r["title"])


pool = [r for r in rows if r["rarity"] in ("Common", "Uncommon", "Rare")]
crafted = [r for r in rows if r["recipe"]]
ancient = [r for r in rows if r["rarity"] == "Ancient"]
tokens = [r for r in rows if r["rarity"] == "Token"]
old = [r for r in rows if r["rarity"] == "Event" and not r["recipe"] and not r["cls"].endswith("MaterialCard")]

md = [f"# 錬金術師 カード一覧（v{version}）", "",
      "バランス調整の検討用。`scripts/card-list.py`がゲーム内の定義（戦闘外で表示される説明文）から生成したもの。手で編集せず、カードを変えたら作り直す。",
      "", "- 「相」は属性（地・水・火・風・無相）。`地→火`は使うと複数の相へ続けて入る複相カード。",
      "- 「強化後」の**太字**は強化で変わる数値。休憩所のアップグレードの結果で、工房の改造・希少素材の付与は含まない。",
      "- 可変ダメージのカードは、戦闘中は本文の下に「（○ダメージ）」も出る（この一覧には出ない）。", ""]

counts = Counter(r["rarity"] for r in pool)
types = Counter(r["type"] for r in pool)
phases = Counter(split_phase(r, r["text"])[1][:1] or "無相" for r in pool)
md += ["## 概要", "",
       f"- 報酬・商人に出るカード：**{len(pool)}枚**（コモン{counts['Common']}・アンコモン{counts['Uncommon']}・レア{counts['Rare']}／"
       f"アタック{types['Attack']}・スキル{types['Skill']}・パワー{types['Power']}／"
       + "・".join(f"{p if p != '無' else '無相'}{phases[p]}" for p in ["地", "水", "火", "風", "無"] if phases[p]) + "）",
       f"- 工房の錬成：**{len(crafted)}種**", f"- エンシェント：{len(ancient)}枚、トークン・一時カード：{len(tokens)}枚",
       f"- 報酬から外した旧カード：{len(old)}枚（旧セーブ読込用。新しく手に入らない）", "",
       "## 初期デッキ", "",
       "ストライク×3、ディフェンド×3（アイアンクラッドの基本カードを流用）と、以下の3枚。", "",
       table(sorted([r for r in rows if r["cls"] in STARTER], key=order), show_rarity=True), ""]
for rarity in ["Common", "Uncommon", "Rare"]:
    items = sorted([r for r in pool if r["rarity"] == rarity], key=order)
    md += [f"## 報酬プール：{RARITY[rarity]}（{len(items)}枚）", "", table(items), ""]
md += ["## 工房の錬成（{}種）".format(len(crafted)), "",
       "素材の組み合わせ（順不同）で決まる。報酬・商人には出ない。", "",
       table(sorted(crafted, key=lambda r: (len(r["recipe"]), order(r))), show_recipe=True), "",
       "## エンシェント", "", table(ancient), "",
       "## トークン・一時カード", "", table(sorted(tokens, key=order)), "",
       "## 報酬から外した旧カード（{}枚）".format(len(old)), "",
       "過去の版で報酬・商人から外したカード。旧セーブのデッキにあれば使える。報酬に戻す候補の検討用。", "",
       table(sorted(old, key=order)), ""]

(ROOT / "docs/card-list.md").write_text("\n".join(md), encoding="utf-8")
print(f"docs/card-list.md: {len(rows)} cards (pool {len(pool)}, crafted {len(crafted)}, old {len(old)})")
