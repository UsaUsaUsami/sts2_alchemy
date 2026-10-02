"""Expected material supply and demand per act, before and after a change of the furnace yield and box capacity
(2026-10-03, user: "1 per furnace, unlimited box"). Plain arithmetic on stated assumptions, printed as markdown;
change the numbers below and run again. Not a simulation of play: the assumptions are guesses to be checked against
play logs.

Usage: python scripts/material-economy.py
"""

# --- assumptions (per act) -------------------------------------------------------------------------------------
COMBATS = {"normal": 7, "elite": 2, "boss": 1}
FURNACE_USES = 1.7          # of 2 per combat: some fights end first, some are spent in the neutral phase
REWARD_PICKS = {"elite": 1, "boss": 2}   # material choices after elite and boss fights (1 each)
WORKSHOPS = 2               # mid and late band on any route (WorkshopPlanner)
COMBAT_SPEND = 0.7          # materials spent per combat by cards (即席錬成, charged cards, アルカヘスト); grows with the deck

# What one workshop visit can use, by how hard the player leans on it.
VISIT = {
    "light":  {"craft": 2.0, "modify": 1.0, "brew": 0.0},   # a 2-material card, one modification
    "normal": {"craft": 2.8, "modify": 2.0, "brew": 1.0},   # weighted recipe size, half a modification cap
    "heavy":  {"craft": 4.0, "modify": 4.0, "brew": 1.0},   # a big recipe, a full modification
}

SCENARIOS = {
    "now (2 per furnace, box 20)": {"yield": 2, "capacity": 20},
    "proposal (1 per furnace, unlimited)": {"yield": 1, "capacity": None},
}


def supply(y):
    fights = sum(COMBATS.values())
    furnace = fights * FURNACE_USES * y
    rewards = sum(REWARD_PICKS.values())
    return furnace, rewards, furnace + rewards


def main():
    fights = sum(COMBATS.values())
    print(f"Assumptions per act: {fights} combats ({COMBATS}), furnace used {FURNACE_USES}x per combat, "
          f"{WORKSHOPS} workshops, {COMBAT_SPEND} materials spent per combat by cards.\n")
    print("| scenario | furnace | rewards | supply/act | spent in combat | left for workshops | per workshop | "
          "light visit | normal visit | heavy visit |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for name, s in SCENARIOS.items():
        furnace, rewards, total = supply(s["yield"])
        spent = fights * COMBAT_SPEND
        left = total - spent
        per = left / WORKSHOPS
        cells = []
        for kind, v in VISIT.items():
            need = sum(v.values())
            cells.append(f"{need:.1f} ({per / need:.0%})")
        print(f"| {name} | {furnace:.1f} | {rewards} | {total:.1f} | {spent:.1f} | {left:.1f} | {per:.1f} | "
              + " | ".join(cells) + " |")
    print("\n(per visit: materials it uses, and how much of it the supply covers; over 100% means a surplus that, "
          "with a box of 20, piles up against the capacity)")
    furnace_now = supply(2)[2] - fights * COMBAT_SPEND
    print(f"\nWith 2 per furnace the act leaves {furnace_now:.1f} for {WORKSHOPS} workshops; the box of 20 fills within "
          f"{20 / (FURNACE_USES * 2):.1f} combats of saving.")


main()
