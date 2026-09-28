"""Card pool balance report: the alchemist against the base-game characters, plus a phase-transition simulation.

Usage: .\\scripts\\smoke.ps1 -Name pool-dump; python scripts/pool-balance.py artifacts/smoke/pool-dump.log
Reads the ALCHEMIST_POOLDUMP / ALCHEMIST_CARDDUMP lines the smoke test prints and writes docs/pool-balance.md.
Everything here is a rough heuristic, meant to point at outliers, not to settle balance.
"""
import json
import random
import re
import statistics
import sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
log = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "artifacts/smoke/pool-dump.log"
lines = log.read_text(encoding="utf-8", errors="replace").splitlines()
rows = [json.loads(l.split("ALCHEMIST_POOLDUMP ", 1)[1]) for l in lines if "ALCHEMIST_POOLDUMP " in l]
crafted_ids = {json.loads(l.split("ALCHEMIST_CARDDUMP ", 1)[1])["id"] for l in lines
               if "ALCHEMIST_CARDDUMP " in l and json.loads(l.split("ALCHEMIST_CARDDUMP ", 1)[1])["recipe"]}
if not rows:
    sys.exit(f"no ALCHEMIST_POOLDUMP lines in {log}")

POOLS = ["Alchemist", "Ironclad", "Silent", "Defect", "Necrobinder", "Regent"]
POOL_JA = {"Alchemist": "錬金術師", "Ironclad": "アイアンクラッド", "Silent": "サイレント", "Defect": "ディフェクト",
           "Necrobinder": "ネクロバインダー", "Regent": "リージェント"}
RARITIES = ["Common", "Uncommon", "Rare"]
RARITY_JA = {"Common": "コモン", "Uncommon": "アンコモン", "Rare": "レア"}
# Rough exchange rates: one energy is about 6 damage; a card drawn about 3; AoE worth 1.5x single target.
W_DRAW, W_ENERGY, W_AOE = 3, 6, 1.5


def clean(text):
    return re.sub(r"\[/?[a-z]+(=[^\]]*)?\]", "", text or "")


PHASE_LINE = re.compile(r"^(.)相(→(.)相)*$")
SIMPLE_PATTERNS = [
    r"敵全体に\d+ダメージ(を\d+回)?(を)?与える。", r"敵全体に\d+ダメージ。",
    r"\d+ダメージを\d+回与える。", r"\d+ダメージを与える。", r"\d+ダメージ。",
    r"\d+ブロックを得る。", r"カードを\d+枚引く。", r"エナジーを\d+得る。", r"廃棄。", r"保留。",
]


def measure(r, upgraded=False):
    """Damage, block, draw and energy the card gives right away, and whether that is all it does."""
    text = clean(r["upgradedText"] if upgraded else r["text"])
    body = "\n".join(l for l in text.split("\n") if not PHASE_LINE.match(l.strip()))
    v = (r["upgradedVars"] if upgraded else r["vars"]) or {}
    hits = int(m.group(1)) if (m := re.search(r"ダメージを(\d+)回", body)) else 1
    dmg = float(v.get("Damage", 0)) * hits if "ダメージ" in body else 0.0
    aoe = "敵全体" in body and dmg > 0
    block = float(v.get("Block", 0)) if re.search(r"\d+ブロックを得る", body) else 0.0
    draw = int(m.group(1)) if (m := re.search(r"(?<!次のターン、)カードを(\d+)枚引", body)) else 0
    energy = int(m.group(1)) if (m := re.search(r"(?<!次のターン、)エナジーを(\d+)得", body)) else 0
    rest = body.replace("\n", "")
    for p in SIMPLE_PATTERNS:
        rest = re.sub(p, "", rest)
    simple = r["type"] in ("Attack", "Skill") and rest.strip() == "" and (dmg or block or draw or energy)
    value = dmg * (W_AOE if aoe else 1) + block + W_DRAW * draw + W_ENERGY * energy
    return dict(dmg=dmg, aoe=aoe, block=block, draw=draw, energy=energy, simple=bool(simple), value=value)


def per_energy(value, cost):
    return value / max(cost, 0.5)  # 0-cost cards counted as half an energy


def median(xs):
    return statistics.median(xs) if xs else float("nan")


def fmt(x, digits=1):
    return "-" if x != x else f"{x:.{digits}f}"


reward = [r for r in rows if r["rarity"] in RARITIES and r["pool"] in POOLS]
for r in reward:
    r["m"] = measure(r)

# ---------------------------------------------------------------- 1. pool shape
md = ["# カードプールのバランス比較", "",
      "`scripts/pool-balance.py`が生成（手で編集しない）。ゲーム内の定義（数値と日本語の説明文）から機械的に数えた**目安**。"
      "カードの強さの大半はパワーやデバフなど数値化できない部分にあるので、外れ値を見つけるためのもの。", "",
      "## 1. プールの形（報酬に出るカード）", "",
      "| キャラ | 枚数 | C/U/R | アタック/スキル/パワー | 平均コスト | 0コスト | 2コスト以上 | X |", "|---|---|---|---|---|---|---|---|"]
for pool in POOLS:
    items = [r for r in reward if r["pool"] == pool]
    rc = Counter(r["rarity"] for r in items)
    tc = Counter(r["type"] for r in items)
    costs = [r["cost"] for r in items if not r["costsX"]]
    md.append(f"| {POOL_JA[pool]} | {len(items)} | {rc['Common']}/{rc['Uncommon']}/{rc['Rare']} | "
              f"{tc['Attack']}/{tc['Skill']}/{tc['Power']} | {statistics.mean(costs):.2f} | "
              f"{sum(c == 0 for c in costs)} | {sum(c >= 2 for c in costs)} | {sum(r['costsX'] for r in items)} |")

# ---------------------------------------------------------------- 2. efficiency of simple cards
md += ["", "## 2. 単純なカードの効率（1エナジーあたり）", "",
       f"ダメージ・ブロック・ドロー・エナジーだけでできたカードの比較。換算：ドロー1枚＝{W_DRAW}、エナジー1＝{W_ENERGY}、"
       f"全体攻撃は{W_AOE}倍、0コストは0.5エナジーとして割る。（ストライク＝6、ディフェンド＝5、ポンメルストライク＝12）", "",
       "| キャラ | コモン | アンコモン | レア | 対象枚数 |", "|---|---|---|---|---|"]
baseline = defaultdict(list)  # (rarity, type) -> vanilla values
for pool in POOLS:
    cells, n = [], 0
    for rarity in RARITIES:
        vals = [per_energy(r["m"]["value"], r["cost"]) for r in reward
                if r["pool"] == pool and r["rarity"] == rarity and r["m"]["simple"] and not r["costsX"]]
        n += len(vals)
        cells.append(f"{fmt(median(vals))}（{len(vals)}枚）")
        if pool != "Alchemist":
            for r in reward:
                if r["pool"] == pool and r["rarity"] == rarity and r["m"]["simple"] and not r["costsX"]:
                    baseline[(rarity, r["type"])].append(per_energy(r["m"]["value"], r["cost"]))
    md.append(f"| {POOL_JA[pool]} | " + " | ".join(cells) + f" | {n} |")
md += ["", "中央値。「単純」に当たるカードが少ないキャラ（特にレア）は参考程度。", ""]

# Wider baseline: every card with a measurable part, counting only that part (a lower bound for cards with riders).
measured = defaultdict(list)  # (pool, rarity, type) -> per-energy values
for r in reward:
    if r["m"]["value"] > 0 and not r["costsX"]:
        measured[(r["pool"], r["rarity"], r["type"])].append(per_energy(r["m"]["value"], r["cost"]))
vanilla_measured = defaultdict(list)
for (pool, rar, typ), vals in measured.items():
    if pool != "Alchemist":
        vanilla_measured[(rar, typ)] += vals
md += ["### 数えられる部分だけの比較（付加効果のあるカードも含む）", "",
       "弱体・毒・パワーなど数えられない効果は0として、ダメージ・ブロック・ドロー・エナジーの部分だけを1エナジーあたりで比べた中央値。"
       "付加効果の多いキャラほど低く出る。（ ）は対象枚数。", "",
       "| キャラ | コモン攻撃 | コモンスキル | アンコモン攻撃 | アンコモンスキル | レア攻撃 | レアスキル |", "|---|---|---|---|---|---|---|"]
for pool in POOLS:
    cells = []
    for rar in RARITIES:
        for typ in ("Attack", "Skill"):
            vals = measured[(pool, rar, typ)]
            cells.append(f"{fmt(median(vals))}（{len(vals)}）")
    md.append(f"| {POOL_JA[pool]} | " + " | ".join(cells) + " |")
cells = []
for rar in RARITIES:
    for typ in ("Attack", "Skill"):
        vals = vanilla_measured[(rar, typ)]
        cells.append(f"{fmt(median(vals))}（{len(vals)}）")
md += ["| **バニラ5キャラ** | " + " | ".join(cells) + " |", ""]

md += ["### 錬金術師の単純なカード（バニラ5キャラの同レアリティ・同種類の中央値との比）", "",
       "| 名前 | レア度 | 種類 | コスト | 1エナジーあたり | バニラ中央値 | 比 | 判定 |", "|---|---|---|---|---|---|---|---|"]
for r in sorted([r for r in reward if r["pool"] == "Alchemist" and r["m"]["simple"]],
                key=lambda r: (RARITIES.index(r["rarity"]), r["type"], r["title"])):
    pe = per_energy(r["m"]["value"], r["cost"])
    base = median(baseline[(r["rarity"], r["type"])])
    ratio = pe / base if base == base else float("nan")
    verdict = "強め" if ratio > 1.25 else "弱め" if ratio < 0.8 else ""
    md.append(f"| {r['title']} | {RARITY_JA[r['rarity']]} | {r['type']} | {r['cost']} | {pe:.1f} | {fmt(base)} | {fmt(ratio, 2)} | {verdict} |")

# ---------------------------------------------------------------- 3. transition simulation
alch = {r["id"]: r for r in rows if r["pool"] == "Alchemist"}
PH = {"地": "E", "水": "W", "火": "F", "風": "A"}
EL = {"Earth": "E", "Water": "W", "Fire": "F", "Air": "A"}
NEXT = {"E": "W", "W": "F", "F": "A", "A": "E"}
SPECIAL = {"素材投入": "any", "万象流転": "any", "即席錬成": "any", "逆相": "prev"}
MATERIAL_CARDS = {"素材投入", "即席錬成", "触媒反応"}  # unplayable without a material
MATERIALS_PER_COMBAT = 6  # two furnace uses (4) plus a little from the box


def sim_card(r):
    text = clean(r["text"])
    phases = []
    for l in text.split("\n"):
        if PHASE_LINE.match(l.strip()):
            phases = [PH[c] for c in re.findall(r"(.)相", l.strip())]
    if not phases and r.get("element") in EL:
        phases = [EL[r["element"]]]
    m = measure(r)
    return dict(id=r["id"], title=r["title"], cost=r["cost"] if not r["costsX"] else 1, phases=phases,
                move=SPECIAL.get(r["title"]), mat=r["title"] in MATERIAL_CARDS, draw=m["draw"], energy=m["energy"], type=r["type"],
                exhaust="Exhaust" in r["keywords"] or r["type"] == "Power", wheel=r["title"] == "四相輪転")


STRIKE = dict(id="STRIKE", title="ストライク", cost=1, phases=[], move=None, mat=False, draw=0, energy=0, type="Attack", exhaust=False, wheel=False)
DEFEND = dict(STRIKE, id="DEFEND", title="ディフェンド", type="Skill")
starter = [STRIKE] * 3 + [DEFEND] * 3 + [sim_card(alch[i]) for i in alch if alch[i]["rarity"] == "Basic"]
pool_cards = {rar: [sim_card(r) for r in alch.values() if r["rarity"] == rar] for rar in RARITIES}
crafted_cards = [sim_card(alch[i]) for i in crafted_ids if i in alch]
PICK_ODDS = [("Common", 0.55), ("Uncommon", 0.37), ("Rare", 0.08)]  # regular 60/37/3, elites and bosses lift rares


def roll_card(rng):
    x, acc = rng.random(), 0
    for rar, p in PICK_ODDS:
        acc += p
        if x < acc:
            return rng.choice(pool_cards[rar])
    return rng.choice(pool_cards["Rare"])


def transition_score(c):
    """How much a transition-minded player wants this card for transitions."""
    if c["move"] or c["wheel"]:
        return 3
    if not c["phases"]:
        return 0
    return len(set(c["phases"])) + (1 if c["cost"] == 0 else 0) - (1 if c["cost"] >= 2 else 0)


def build_deck(rng, picks, focused):
    deck = list(starter)
    for _ in range(picks):
        offer = [roll_card(rng) for _ in range(3)]
        deck.append(max(offer, key=transition_score) if focused else rng.choice(offer))
    workshop = picks // 6  # about two workshops per act, one crafted card each
    for _ in range(workshop):
        options = [c for c in crafted_cards if c["type"] != "Power"] if focused else crafted_cards
        deck.append(max(rng.sample(options, 3), key=transition_score) if focused else rng.choice(options))
    return deck


def play_combat(rng, deck, turns):
    draw, discard, hand = rng.sample(deck, len(deck)), [], []
    phase, prev, wheel = None, None, False
    mats = MATERIALS_PER_COMBAT
    total = air = 0
    per_turn, played_per_turn = [], []

    def draw_n(n):
        nonlocal draw, discard
        for _ in range(n):
            if len(hand) >= 10:
                return
            if not draw:
                draw, discard = rng.sample(discard, len(discard)), []
                if not draw:
                    return
            hand.append(draw.pop())

    def enter(p):
        nonlocal phase, prev, total, air, turn_t
        if p == phase:
            return False
        moved = phase is not None
        prev, phase = phase, p
        if moved:
            total += 1
            turn_t += 1
            if p == "A":
                air += 1
                draw_n(1)
        return moved

    def would_transition(c):
        if c["move"] == "any":
            return 1 if phase else 0
        if c["move"] == "prev":
            return 1 if prev and prev != phase else 0
        n, cur = 0, phase
        for p in c["phases"]:
            if cur is not None and p != cur:
                n += 1
            cur = p
        return n

    for _ in range(turns):
        turn_t, energy, plays = 0, 3, 0
        draw_n(5)
        while plays < 40:
            playable = [c for c in hand if c["cost"] <= energy and (mats > 0 or not c["mat"])]
            if not playable:
                break
            c = max(playable, key=lambda c: (would_transition(c), c["draw"] + c["energy"], -c["cost"]))
            hand.remove(c)
            energy -= c["cost"]
            mats -= 1 if c["mat"] else 0
            plays += 1
            before = turn_t
            if c["move"] == "any":
                enter(NEXT[phase] if phase else rng.choice("EWFA"))
            elif c["move"] == "prev" and prev:
                enter(prev)
            for p in c["phases"]:
                enter(p)
            draw_n(c["draw"])
            energy += c["energy"]
            if wheel and turn_t == before and phase:
                enter(NEXT[phase])
            if c["wheel"]:
                wheel = True
            if not c["exhaust"]:
                discard.append(c)
        discard.extend(hand)
        hand.clear()
        per_turn.append(turn_t)
        played_per_turn.append(plays)
    return per_turn, total, air, played_per_turn


TURNS, COMBATS = 6, 4000
results = {}
rng = random.Random(20260928)
for focused in (False, True):
    for picks in (4, 8, 14, 20):
        turn_counts, totals3, totals5, air5, plays = [], [], [], [], []
        for _ in range(COMBATS):
            deck = build_deck(rng, picks, focused)
            per_turn, _, _, played = play_combat(rng, deck, TURNS)
            turn_counts += per_turn
            totals3.append(sum(per_turn[:3]))
            totals5.append(sum(per_turn[:5]))
            plays += played
            # air transitions by turn 5: rerun is costly, so approximate with the air share of this pool
        results[(focused, picks)] = dict(
            mean=statistics.mean(turn_counts), p4=sum(t >= 4 for t in turn_counts) / len(turn_counts),
            p0=sum(t == 0 for t in turn_counts) / len(turn_counts),
            after2=statistics.mean(max(0, t - 2) for t in turn_counts),
            t3=statistics.mean(totals3), t5=statistics.mean(totals5), plays=statistics.mean(plays))

# Air share among transitions, measured once on a mid-size focused deck.
air_share_samples = []
for _ in range(2000):
    per_turn, total, air, _ = play_combat(rng, build_deck(rng, 14, True), TURNS)
    if total:
        air_share_samples.append(air / total)
air_share = statistics.mean(air_share_samples)

md += ["", "## 3. 相転移のシミュレーション", "",
       f"スターター9枚＋報酬N枚（毎回3枚から1枚。レアリティはコモン55%・アンコモン37%・レア8%）＋工房の錬成（6枚ごとに1枚）で"
       f"{COMBATS}戦ずつ、各{TURNS}ターン。毎ターン5枚引き・3エナジー。相転移が起きるカードから優先して使い、ドロー・エナジーも処理する"
       f"（風への転移の1ドローを含む）。素材を使うカード（素材投入・即席錬成・触媒反応）は1戦闘{MATERIALS_PER_COMBAT}個まで。敵・HP・カードの効果（ダメージなど）は扱わない。", "",
       "- **ランダム**：3枚から無作為に取る（相転移を意識しない）。",
       "- **相転移寄り**：3枚から、相を持つ軽いカードや相を動かすカードを優先して取る。", "",
       "| デッキ | 報酬N枚 | 1ターンの平均 | 0回のターン | 4回以上のターン | 3ターン目までの累計 | 5ターン目までの累計 | 1ターンに使う枚数 |",
       "|---|---|---|---|---|---|---|---|"]
for (focused, picks), r in results.items():
    md.append(f"| {'相転移寄り' if focused else 'ランダム'} | {picks} | {r['mean']:.2f} | {r['p0']:.0%} | {r['p4']:.0%} | "
              f"{r['t3']:.1f} | {r['t5']:.1f} | {r['plays']:.1f} |")
md += ["", f"相転移のうち風へのものは約{air_share:.0%}（相転移寄り・14枚）。", ""]

# ---------------------------------------------------------------- 4. transition payoffs at the simulated rates
def payoff_table(label, focused, picks):
    r = results[(focused, picks)]
    tt, t3, t5 = r["mean"], r["t3"], r["t5"]
    a5 = t5 * air_share
    # (title, cost, value at this rate, comparison rarity/type, note)
    items = [
        ("連鎖反応", 1, 20 * r["after2"], "Rare", "Attack", "20×このターンの3回目以降の回数（v0.23.1）"),
        ("業火", 2, 6 + 2 * t3, "Rare", "Attack", "6＋2×累計（3ターン目に使用）"),
        ("業火", 2, 6 + 2 * t5, "Rare", "Attack", "6＋2×累計（5ターン目に使用）"),
        ("嵐刃", 1, 6 * max(1, a5), "Rare", "Attack", "6×風への累計（5ターン目。v0.23.1）"),
        ("大地の記憶", 2, 2 * t3, "Rare", "Skill", "2×累計のブロック（3ターン目）"),
        ("大地の記憶", 2, 2 * t5, "Rare", "Skill", "2×累計のブロック（5ターン目）"),
        ("大地の加護", 1, 1 * tt * 2.5, "Uncommon", "Skill", "プレート（回数）×残り約2.5ターン分"),
        ("風の残像", 1, 3 * tt * 3, "Uncommon", "Power", "毎ターン3×回数、残り3ターン分の合計"),
        ("火花", 0, 4 + 3 * (tt > 0), "Common", "Attack", "4＋転移時3（転移率は平均で近似）"),
    ]
    out = [f"### {label}", "", "| カード | コスト | 想定値 | 1エナジーあたり | バニラ中央値（同レア・同種類、数えられる部分） | 計算 |", "|---|---|---|---|---|---|"]
    for title, cost, value, rar, typ, note in items:
        base = median(vanilla_measured[(rar, typ)])
        out.append(f"| {title} | {cost} | {value:.1f} | {per_energy(value, cost):.1f} | {fmt(base)} | {note} |")
    return out


md += ["## 4. 相転移で伸びるカードの想定値", "",
       "3の平均回数を当てはめた値。バニラ側は2の「数えられる部分だけ」の中央値で、条件つきの伸び（「〜1枚につき」など）を0とした**下限**。"
       "条件を満たしたバニラのレア攻撃は1エナジー15〜25が普通（鬼火・ヘヴンリードリル・セブンスターズ・不時着など）なので、レアはそちらと比べる。"
       "パワーは効果の総量で比べているので、数ターン分の合計になる。", ""]
md += payoff_table("相転移寄り・報酬8枚（Act1終盤）", True, 8) + [""]
md += payoff_table("相転移寄り・報酬14枚（Act2終盤）", True, 14) + [""]
md += payoff_table("ランダム・報酬14枚", False, 14) + [""]
天空 = results[(True, 14)]["p4"]
md += [f"《天空》（4回以上で次のターンにエナジー2）が発動するターンは、相転移寄り14枚で約{天空:.0%}。", ""]

(ROOT / "docs/pool-balance.md").write_text("\n".join(md), encoding="utf-8")
print("docs/pool-balance.md written;", {k: round(v["mean"], 2) for k, v in results.items()})


# ---------------------------------------------------------------- 5. poison against drain
def poison_ticks(n):
    out = []
    while n > 0:
        out.append(n)
        n -= 1
    return out


def drain_ticks(n):
    out = []
    while n > 0:
        out.append(n)
        n //= 2
    return out


def per_turn_power(add, turns, decay):
    """Damage per enemy by the end of each enemy turn from a power adding `add` at each of your turn starts."""
    s, total, out = 0, 0, []
    for _ in range(turns):
        s += add
        total += s
        s = s - 1 if decay == "poison" else s // 2
        out.append(total)
    return out


HP_WEIGHTS = [0, 0.5, 1]  # homunculus HP counted as 0, half or all of the damage it comes with


def row(name, rarity, cost, stacks, extra_dmg=0, ticks=drain_ticks, note=""):
    t = ticks(stacks)
    dmg3 = sum(t[:3])
    total = sum(t)
    is_drain = ticks is drain_ticks
    cells = []
    for w in HP_WEIGHTS:
        v = total + extra_dmg + (w * total if is_drain else 0)
        cells.append(f"{v:.0f}（{per_energy(v, cost):.1f}）")
    return (f"| {name} | {rarity} | {cost} | {stacks} | {total}（{len(t)}ターン） | {dmg3} | "
            + " | ".join(cells) + f" | {note} |")


md += ["## 5. サイレントの毒とドレイン", "",
       "毒は発動のたびに1減り、ドレインは半分（切り捨て）になる。ドレインは失わせたHPと同じだけホムンクルスHPも得る。",
       "ホムンクルスHPを何点と見るかで結論が変わるので、ダメージだけ（×0）・半分（×0.5）・ダメージと同じ（×1）の3通りで出す。"
       "敵が最後まで生きている前提の1体あたりの値。（ ）は1エナジーあたり。", "",
       "### 5.1 スタック量ごとの総量", "",
       "| スタック | 毒の総ダメージ | ドレインの総ダメージ | ドレイン＋HP×0.5 | ドレイン＋HP×1 | 3ターンで入る分（毒／ドレイン） |",
       "|---|---|---|---|---|---|"]
for n in (2, 3, 4, 5, 7, 9, 12, 20):
    p, d = poison_ticks(n), drain_ticks(n)
    md.append(f"| {n} | {sum(p)}（{len(p)}ターン） | {sum(d)}（{len(d)}ターン） | {sum(d) * 1.5:.0f} | {sum(d) * 2} | {sum(p[:3])}／{sum(d[:3])} |")
md += ["", "### 5.2 すでに積まれたスタックに足したときの増え方（毒・ドレインを4足す）", "",
       "| もとのスタック | 毒の増加 | ドレインの増加 | ドレイン＋HP×1 |", "|---|---|---|---|"]
for s in (0, 5, 10, 20):
    dp = sum(poison_ticks(s + 4)) - sum(poison_ticks(s))
    dd = sum(drain_ticks(s + 4)) - sum(drain_ticks(s))
    md.append(f"| {s} | +{dp} | +{dd} | +{dd * 2} |")
md += ["", "毒は積むほど1枚の価値が上がる（2乗で伸びる）。ドレインはほぼ一定（2倍前後）で、積み上げても伸びない。", "",
       "### 5.3 カードごとの比較（未強化、1体あたり）", "",
       "| カード | レア度 | コスト | 付与 | 総ダメージ | 3ターンで入る分 | ダメージだけ | ＋HP×0.5 | ＋HP×1 | 備考 |",
       "|---|---|---|---|---|---|---|---|---|---|"]
P = poison_ticks
md += [
    row("致死毒（サ）", "コモン", 1, 5, ticks=P),
    row("毒の一刺し（サ）", "コモン", 1, 3, 6, ticks=P, note="6ダメージ込み"),
    row("蛇の噛みつき（サ）", "コモン", 2, 7, ticks=P, note="保留"),
    row("吸血の刃（錬）", "コモン", 1, 2, 6, note="6ダメージ込み"),
    row("薬毒の滴（錬）", "コモン", 1, 1, note="素材なし"),
    row("薬毒の滴（錬）", "コモン", 1, 4, note="薬草1個を消費"),
    row("バブル・バブル（サ）", "アンコモン", 1, 9, ticks=P, note="毒がある敵だけ"),
    row("毒の薄霧（サ）", "アンコモン", 2, 4, ticks=P, note="敵全体・脱力1"),
    row("バウンドフラスコ（サ）", "アンコモン", 2, 9, ticks=P, note="3×3をランダムに。1体に集まった場合"),
    row("毒霧雨（錬・錬成）", "錬成", 1, 4, note="1ドロー・2回相転移"),
    row("感染爆発（サ）", "レア", 3, 9, ticks=P, note="敵全体・即座に1回発動"),
    row("清流（錬）", "レア", 1, 2 + 8, note="3ターン目ごろ（相転移累計8回）で2＋8"),
]
md += ["", "### 5.4 毎ターン付与するパワー（1体あたりの累計ダメージ）", "",
       "| パワー | コスト | 1ターン目 | 3ターン目 | 5ターン目 | 8ターン目 | 備考 |", "|---|---|---|---|---|---|---|"]
for name, cost, add, decay, note in [("有毒ガス（サ）", 1, 2, "poison", "敵全体"), ("有毒ガス＋（サ）", 1, 3, "poison", "敵全体"),
                                     ("吸精の瘴気（錬）", 1, 1, "drain", "敵全体。同量のHPも得る"), ("吸精の瘴気＋（錬）", 1, 2, "drain", "敵全体。同量のHPも得る")]:
    c = per_turn_power(add, 8, decay)
    md.append(f"| {name} | {cost} | {c[0]} | {c[2]} | {c[4]} | {c[7]} | {note} |")
md += ["", "### 5.5 まとめて発動させるカード（スタック10の敵1体に使った場合）", "",
       "| カード | コスト | その場のダメージ | 備考 |", "|---|---|---|---|",
       f"| 感染爆発（サ） | 3 | 9を足して1回発動＝19 | 毒は19→18で残り、その後も171入る |",
       f"| 生命の収穫（錬） | 1 | {sum(drain_ticks(10)[:3])}（10＋5＋2） | 残り1。同量のHPも得る |",
       f"| 生命の収穫＋（錬） | 1 | {sum(drain_ticks(10)[:4])} | 残り0 |", ""]

(ROOT / "docs/pool-balance.md").write_text("\n".join(md), encoding="utf-8")
