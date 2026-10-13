"""Monte Carlo of the alchemist's material economy over whole runs (2026-10-03, user: "run test runs a great many
times and add them up"). Not the game: a model of the draw and of a simple player, to see how often the furnace
can be used and how many materials reach the workshops. Enemies, damage and HP are not modelled; a combat is a
number of turns drawn from a range. The rules that are modelled follow the code:

- starter deck (Character.StartingDeck), 5 cards a turn, 3 energy, hand limit 10;
- MaterialBox deals the furnace tokens (0 cost, retain) into the hand on turn 1; a token needs a phase and another
  card in hand, exhausts that card and gives 1 material of the current phase (YieldPerEvent);
- combats start in the neutral phase (the refined box: a random phase); a phase card moves into its phase and the
  phase stays across turns;
- 即席錬成 (1 energy, exhaust) spends 1 normal material for a 0-cost card of its phase;
- elite: 1 material choice, boss: 2 (one of them rare, not counted as normal);
- 2 workshops per act (mid and late band, WorkshopPlanner); each visit can craft once (a recipe of
  AlchemyState.Recipes), modify once (1-4 materials, InfusionRules.Limit) and brew once (1 material).

Player policy (stated, change below): in the neutral phase, play a phase card first, or 即席錬成 if no phase card;
use the furnace tokens as soon as there is a phase, exhausting a Strike/Defend first; take a card reward most of the
time, from the pool's phase mix; at a workshop craft the biggest recipe the box can pay for, then modify with what
is left down to a reserve, then brew.

Usage: python scripts/run-sim.py [runs]      (default 4000 runs per scenario; prints markdown)
"""
import random
import sys
from collections import Counter

PHASES = ["earth", "water", "fire", "air"]
MAT = {"earth": "iron", "water": "herb", "fire": "powder", "air": "ether"}
MATS = ["iron", "herb", "powder", "ether"]

# --- recipes (AlchemyState.Recipes, 2026-10-03) -------------------------------------------------------------------
R = lambda *m: Counter(m)
RECIPES = [
    R("iron", "iron"), R("herb", "herb"), R("powder", "powder"), R("ether", "ether"),
    R("iron", "herb"), R("iron", "powder"), R("iron", "ether"), R("herb", "powder"), R("herb", "ether"),
    R("powder", "ether"),
    R("iron", "iron", "iron"), R("powder", "powder", "powder"), R("ether", "ether", "ether"),
    R("herb", "herb", "iron"), R("herb", "herb", "ether"), R("iron", "iron", "herb"),
    R("powder", "powder", "powder", "herb"), R("iron", "herb", "powder", "ether"),
    R("herb", "herb", "herb", "iron", "powder"), R("iron", "iron", "powder", "powder", "ether"),
]
# Phase a crafted card leaves the player in (the last phase of a multi-phase card); good enough for the draw.
RECIPE_PHASE = ["earth", "water", "fire", "air", "water", "fire", "air", "fire", "air", "air",
                "earth", "fire", "air", "water", "water", "earth", "fire", "air", "fire", "fire"]

# --- reward pool (docs/card-list.md v0.24: earth 12, water 12, fire 15, air 14, neutral 11 of 64) -----------------
POOL_PHASES = ["earth"] * 12 + ["water"] * 12 + ["fire"] * 15 + ["air"] * 14 + [None] * 11
SPENDERS = 4          # 素材投入, 素材爆弾, アルカヘスト, コペルニクスシフト: cards that spend materials in combat
TAKE_REWARD = 0.7     # share of card rewards taken (the rest skipped)

# --- act shape ----------------------------------------------------------------------------------------------------
# N normal, E elite, B boss, W workshop. 8 normal, 2 elite, 1 boss, 2 workshops (mid and late band).
ACT = ["N", "N", "N", "N", "W", "N", "E", "N", "N", "W", "E", "N", "B"]
TURNS = {"N": (3, 5), "E": (4, 7), "B": (6, 9)}
SHOP_REMOVE = 0.5     # chance per act of removing a Strike/Defend at a merchant


class Card:
    __slots__ = ("name", "phase", "cost", "spends")

    def __init__(self, name, phase=None, cost=1, spends=0):
        self.name, self.phase, self.cost, self.spends = name, phase, cost, spends


def starter(variant):
    deck = [Card("strike") for _ in range(variant.get("strikes", 5))]
    deck += [Card("defend") for _ in range(variant.get("defends", 5))]
    deck += [Card("earthen_guard", "earth"), Card("soothing_mist", "water"), Card("instant", None, 1)]
    deck += [Card("extra_" + p, p) for p in variant.get("extra_phase", [])]
    return deck


class Run:
    def __init__(self, rng, sc):
        self.rng, self.sc = rng, sc
        self.deck = starter(sc)
        self.box = Counter()
        self.stats = Counter()
        self.first_workshop_held = None
        self.workshop_held = []
        self.craft_sizes = []
        self.no_craft = 0
        self.mods = []

    # --- one combat ---------------------------------------------------------------------------------------------
    def combat(self, kind, act):
        rng, sc = self.rng, self.sc
        draw = self.deck[:]
        rng.shuffle(draw)
        discard, hand = [], []
        phase = rng.choice(PHASES) if sc.get("random_start") else None
        tokens = sc.get("furnace", 2)
        used = 0
        lo, hi = TURNS[kind]
        turns = rng.randint(lo + act, hi + act)   # later acts: longer fights
        if sc.get("short"):
            turns = rng.randint(2, 3)
        first_turn_phase = phase is not None
        for turn in range(turns):
            for _ in range(5):
                if len(hand) >= 10:
                    break
                if not draw:
                    draw, discard = discard, []
                    rng.shuffle(draw)
                    if not draw:
                        break
                hand.append(draw.pop())
            energy = 3
            # enter a phase
            if phase is None:
                cards = [c for c in hand if c.phase and c.cost <= energy]
                if cards:
                    c = min(cards, key=lambda c: c.cost)
                    hand.remove(c); discard.append(c); energy -= c.cost; phase = c.phase
                else:
                    ia = next((c for c in hand if c.name == "instant"), None)
                    if ia and sc.get("ia_for_phase", True) and energy >= 1 and sum(self.box.values()) > 0:
                        m = max(MATS, key=lambda m: self.box[m])
                        self.box[m] -= 1; self.stats["spent_combat"] += 1; self.stats["instant_used"] += 1
                        hand.remove(ia); energy -= 1     # exhausted
                        phase = [p for p in PHASES if MAT[p] == m][0]
                if turn == 0 and phase is not None:
                    first_turn_phase = True
                if phase is not None:
                    self.stats["phase_turn"] += turn + 1
            # furnace, as soon as there is a phase
            while phase is not None and used < tokens and len(hand) >= 1:
                fodder = next((c for c in hand if c.name in ("strike", "defend")), None) or \
                    min(hand, key=lambda c: (c.phase is not None, c.spends))
                hand.remove(fodder)
                used += 1
                n = 1 + (1 if used == 1 and sc.get("bag") else 0)
                self.box[MAT[phase]] += n * sc.get("yield", 1)
                self.stats["furnace_mats"] += n * sc.get("yield", 1)
            # play the rest (phase cards change the phase; spenders spend)
            rng.shuffle(hand)
            for c in hand[:]:
                if c.cost > energy or c.name == "instant":
                    continue
                energy -= c.cost
                hand.remove(c); discard.append(c)
                if c.phase:
                    phase = c.phase
                if c.spends and sum(self.box.values()) > sc.get("reserve_combat", 0):
                    for _ in range(c.spends):
                        if sum(self.box.values()) == 0:
                            break
                        m = max(MATS, key=lambda m: self.box[m])
                        self.box[m] -= 1; self.stats["spent_combat"] += 1
            # end of turn: discard the rest, the furnace tokens are retained (they are not in hand here)
            discard += hand
            hand = []
        if phase is None:
            self.stats["phase_turn"] += turns + 1   # never: counted as one past the end
        elif sc.get("random_start"):
            self.stats["phase_turn"] += 1
        self.stats["combats"] += 1
        self.stats["furnace_uses"] += used
        self.stats["furnace_zero"] += used == 0
        self.stats["no_phase_turn1"] += not first_turn_phase
        if act == 0:
            self.stats["a1_combats"] += 1; self.stats["a1_uses"] += used
        if act == 0 and self.stats["combats"] <= 4:      # the four fights before the first workshop
            self.stats["early"] += 1
            self.stats["early_no_t1"] += not first_turn_phase
            self.stats["early_uses"] += used
        # cap the box (scenarios with a capacity)
        cap = sc.get("capacity")
        if cap is not None:
            over = sum(self.box.values()) - cap
            if over > 0:
                self.stats["lost_cap"] += over
                for _ in range(over):
                    m = max(MATS, key=lambda m: self.box[m]); self.box[m] -= 1

    def reward(self, kind):
        rng = self.rng
        if kind in ("E", "B"):
            # pick the material that the box holds least of (helps recipes)
            m = min(MATS, key=lambda m: (self.box[m], rng.random()))
            self.box[m] += 1; self.stats["reward_mats"] += 1
        if kind == "B":
            self.stats["rares"] += 1
        if rng.random() < TAKE_REWARD:
            i = rng.randrange(len(POOL_PHASES) + 0)
            phase = POOL_PHASES[i]
            spends = 1 if rng.random() < SPENDERS / 64 else 0
            cost = rng.choice([0, 1, 1, 1, 2, 2]) if phase else rng.choice([0, 1, 1, 2])
            self.deck.append(Card("reward", phase, cost, spends))

    def workshop(self, act):
        held = sum(self.box.values())
        self.workshop_held.append(held)
        if self.first_workshop_held is None:
            self.first_workshop_held = held
        # craft: the biggest affordable recipe
        best = None
        for i, r in enumerate(RECIPES):
            if all(self.box[m] >= n for m, n in r.items()):
                size = sum(r.values())
                if best is None or size > best[0]:
                    best = (size, i)
        if best:
            size, i = best
            self.box -= RECIPES[i]
            self.craft_sizes.append(size)
            self.deck.append(Card("crafted", RECIPE_PHASE[i], 1))
        else:
            self.no_craft += 1
        # modify: up to 4, keeping a reserve
        reserve = self.sc.get("reserve", 1)
        k = max(0, min(4, sum(self.box.values()) - reserve - 1))
        for _ in range(k):
            m = max(MATS, key=lambda m: self.box[m]); self.box[m] -= 1
        self.mods.append(k)
        # brew
        if sum(self.box.values()) > reserve:
            m = max(MATS, key=lambda m: self.box[m]); self.box[m] -= 1
            self.stats["brews"] += 1

    def play(self):
        for act in range(3):
            if self.rng.random() < SHOP_REMOVE:
                basic = [c for c in self.deck if c.name in ("strike", "defend")]
                if basic:
                    self.deck.remove(self.rng.choice(basic))
            for room in ACT:
                if room == "W":
                    self.workshop(act)
                else:
                    self.combat(room, act)
                    self.reward(room)
        self.stats["left_at_end"] = sum(self.box.values())
        return self


SCENARIOS = {
    "旧：炉2個・容量20・初期9枚": {"strikes": 3, "defends": 3, "yield": 2, "capacity": 20},
    "現行：炉1個・無制限・初期13枚": {},
    "A：初期のStrike1枚を火相カードに": {"strikes": 4, "extra_phase": ["fire"]},
    "B：Strike・Defend各1枚を火・風相カードに": {"strikes": 4, "defends": 4, "extra_phase": ["fire", "air"]},
    "C：戦闘開始時にランダムな相": {"random_start": True},
    "D：初期9枚に戻す（炉1個）": {"strikes": 3, "defends": 3},
    "E：現行で、即席錬成を相に入るために使わない": {"ia_for_phase": False},
    "F：現行で、戦闘が短い（2〜3ターン）": {"short": True},
}


def summarize(name, runs):
    n = len(runs)
    tot = Counter()
    for r in runs:
        tot.update(r.stats)
    combats = tot["combats"]
    ws = [h for r in runs for h in r.workshop_held]
    first = [r.first_workshop_held for r in runs]
    crafts = [s for r in runs for s in r.craft_sizes]
    visits = len(ws)
    size_mix = Counter(crafts)
    return {
        "name": name,
        "uses": tot["furnace_uses"] / combats,
        "a1_uses": tot["a1_uses"] / tot["a1_combats"],
        "zero": tot["furnace_zero"] / combats,
        "no_t1": tot["no_phase_turn1"] / combats,
        "phase_turn": tot["phase_turn"] / combats,
        "early_no_t1": tot["early_no_t1"] / tot["early"],
        "early_uses": tot["early_uses"] / tot["early"],
        "ia": tot["instant_used"] / n,
        "supply": (tot["furnace_mats"] + tot["reward_mats"]) / n,
        "combat_spent": tot["spent_combat"] / n,
        "first": sum(first) / n,
        "first_le1": sum(1 for f in first if f <= 1) / n,
        "held": sum(ws) / visits,
        "no_craft": sum(r.no_craft for r in runs) / visits,
        "craft5": size_mix[5] / visits, "craft34": (size_mix[3] + size_mix[4]) / visits, "craft2": size_mix[2] / visits,
        "mods": sum(m for r in runs for m in r.mods) / visits,
        "left": tot["left_at_end"] / n,
        "lost": tot["lost_cap"] / n,
    }


def main():
    runs_n = int(sys.argv[1]) if len(sys.argv) > 1 else 4000
    rows = []
    for name, sc in SCENARIOS.items():
        rng = random.Random(20261003)
        rows.append(summarize(name, [Run(rng, sc).play() for _ in range(runs_n)]))
    print(f"{runs_n} runs per scenario, 3 acts of {ACT}, seed 20261003.\n")
    print("### 炉の使用（戦闘あたり）\n")
    print("| 案 | 炉の使用回数 | うちAct1 | 1回も使えない戦闘 | 1ターン目に相へ入れない戦闘 | 相に入ったターン（平均） | 相に入るための即席錬成（ランあたり） | 最初の工房までの4戦：1ターン目に相へ入れない | 同：炉の使用回数 |")
    print("|---|---|---|---|---|---|---|---|---|")
    for r in rows:
        print(f"| {r['name']} | {r['uses']:.2f} | {r['a1_uses']:.2f} | {r['zero']:.0%} | {r['no_t1']:.0%} | {r['phase_turn']:.2f} | {r['ia']:.1f} | {r['early_no_t1']:.0%} | {r['early_uses']:.2f} |")
    print("\n### ラン全体の素材（ランあたり）\n")
    print("| 案 | 入手（炉＋報酬） | 戦闘で消費 | 最初の工房に着いた時の所持 | 最初の工房で1個以下 | 工房到着時の平均所持 | "
          "錬成できない訪問 | 5素材 / 3〜4素材 / 2素材の錬成 | 改造に使えた数 | ラン終了時の残り | 容量で失った数 |")
    print("|---|---|---|---|---|---|---|---|---|---|---|")
    for r in rows:
        print(f"| {r['name']} | {r['supply']:.1f} | {r['combat_spent']:.1f} | {r['first']:.1f} | {r['first_le1']:.0%} | "
              f"{r['held']:.1f} | {r['no_craft']:.0%} | {r['craft5']:.0%} / {r['craft34']:.0%} / {r['craft2']:.0%} | "
              f"{r['mods']:.1f} | {r['left']:.1f} | {r['lost']:.1f} |")


if __name__ == "__main__":
    main()
