"""Builds the golem's own Spine rig (2026-10-02, user: the golem too, with a reaction to damage) from the parts cut
by rig-cut.py (rig-parts.py --set golem): assets/art/pets/rig/golem.{spine-json,atlas,png}.

The golem uses the Necrobinder's Osty state machine (HomunculusPet), so the rig has Osty's animations: idle_loop,
cast, attack, attack_poke, hurt, die, dead_loop (the fallen pose, held) and revive (back up from it).
Taking damage: knocked back, the body shakes, the stones flush red and three chips fly off. Death: the body topples
and the arms fall slack; dead_loop holds that pose and revive rebuilds the golem from it.
Placement in the assembled pose in sheet pixels (y up, feet at 0), as rig-build.py does for the alchemist.
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from riglib import bone_anim, move, place, prepare, rot, scale, shadow, write_rig  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
PARTS = ROOT / "assets/art/character/rig/alchemist/parts"
OUT = ROOT / "assets/art/pets/rig"
# The golem stands about 190 px on screen beside the alchemist's ~265 (PetArt.GolemHeight); its parts are about
# 610 sheet px tall against the alchemist's 1250 at scale 1.
SCALE = 1.2
FAR = "a8a8b0"

BONES = {
    "root": (None, 0, 0, 0),
    "body": ("root", 0, 386, 90),
    # The near arm hangs from the socket drawn on the body's left (the golem faces right), the far one behind.
    "arm_b": ("body", -112, 426, -92),
    "arm_f": ("body", 110, 430, -88),
    "leg_b": ("root", -45, 226, -90),
    "leg_f": ("root", 55, 226, -90),
    # Chips knocked off when hit (hidden otherwise).
    "chip1": ("body", 70, 470, 0),
    "chip2": ("body", -40, 520, 0),
    "chip3": ("body", 120, 380, 0),
}

SHOULDER_OVERLAP = 55  # the arm's round shoulder stone sits over the body's socket
HIP_OVERLAP = 40


def arm_slot(slot, bone, part, flip, tint, images):
    images[slot] = prepare(PARTS / f"{part}.png", flip=flip, tint=tint)
    r = BONES[bone][3] + 90
    return (slot, bone, *place(images[slot], BONES[bone][1:3], r, SHOULDER_OVERLAP if "arm" in bone else HIP_OVERLAP), r)


T = 2.6
DIE_END = {  # the fallen pose: rotations and moves at the end of die, held by dead_loop, undone by revive
    "body": {"rotate": -80, "translate": (-50, -215)},
    "arm_b": {"rotate": 55},
    "arm_f": {"rotate": -35},
    "leg_b": {"rotate": 25},
    "leg_f": {"rotate": -20},
}
RED = "ff7060ff"
WHITE = "ffffffff"


def held(pose, t=0):
    """Keys that hold a pose (DIE_END-shaped) from time t."""
    out = {}
    for bone, parts in pose.items():
        tracks = []
        if "rotate" in parts:
            tracks.append(rot((t, parts["rotate"])))
        if "translate" in parts:
            tracks.append(move((t, *parts["translate"])))
        out[bone] = tracks
    return bone_anim(**out)


def revive(duration=0.8):
    out = {}
    for bone, parts in DIE_END.items():
        tracks = []
        if "rotate" in parts:
            tracks.append(rot((0, parts["rotate"]), (duration * 0.7, 0)))
        if "translate" in parts:
            x, y = parts["translate"]
            tracks.append(move((0, x, y), (duration * 0.55, 0, 25), (duration, 0, 0)))
        out[bone] = tracks
    return bone_anim(**out)


def punch(reach, lunge):
    """The near arm swings forward into a punch with a lunge (attack, and a smaller attack_poke)."""
    return bone_anim(
        root=move((0, 0, 0), (0.12, -30, 0), (0.25, lunge, 0), (0.45, lunge, 0), (0.7, 0, 0)),
        body=rot((0, 0), (0.12, 8), (0.25, -12), (0.45, -12), (0.7, 0)),
        arm_b=rot((0, 0), (0.12, -40), (0.25, reach), (0.45, reach), (0.7, 0)),
        arm_f=rot((0, 0), (0.12, 15), (0.25, -20), (0.7, 0)),
    )


ANIMATIONS = {
    "idle_loop": bone_anim(
        body=[move((0, 0, 0), (T / 2, 0, -10), (T, 0, 0)), rot((0, 0), (T / 2, 1.5), (T, 0))],
        arm_b=rot((0, 0), (T / 2, 3), (T, 0)),
        arm_f=rot((0, 0), (T / 2, -3), (T, 0)),
    ),
    "attack": punch(100, 90),
    "attack_poke": punch(70, 50),
    # Both arms up, then slammed down with the body squashing.
    "cast": bone_anim(
        body=[move((0, 0, 0), (0.3, 0, 25), (0.55, 0, 25), (0.65, 0, -15), (0.9, 0, 0)),
              scale((0, 1, 1), (0.55, 0.97, 1.04), (0.65, 1.08, 0.92), (0.9, 1, 1))],
        arm_b=rot((0, 0), (0.3, 150), (0.55, 150), (0.65, -10), (0.9, 0)),
        arm_f=rot((0, 0), (0.3, 150), (0.55, 150), (0.65, 10), (0.9, 0)),
    ),
    # Knocked back, the body shakes, the arms flail.
    "hurt": bone_anim(
        root=move((0, 0, 0), (0.08, -45, 0), (0.5, 0, 0)),
        body=[rot((0, 0), (0.08, 12), (0.5, 0)),
              move((0, 0, 0), (0.08, -8, 0), (0.14, 8, 0), (0.2, -6, 0), (0.26, 4, 0), (0.32, 0, 0))],
        arm_b=rot((0, 0), (0.08, 18), (0.5, 0)),
        arm_f=rot((0, 0), (0.08, 12), (0.5, 0)),
        chip1=move((0, 0, 0), (0.2, 70, 90), (0.45, 120, -40)),
        chip2=move((0, 0, 0), (0.2, -60, 110), (0.45, -110, -20)),
        chip3=move((0, 0, 0), (0.2, 110, 40), (0.45, 170, -90)),
    ),
    "die": bone_anim(
        body=[rot((0, 0), (0.3, 15), (0.75, DIE_END["body"]["rotate"] - 5), (1.0, DIE_END["body"]["rotate"])),
              move((0, 0, 0), (0.3, 0, 15), (0.75, *DIE_END["body"]["translate"]), (0.85, DIE_END["body"]["translate"][0], DIE_END["body"]["translate"][1] + 15), (1.0, *DIE_END["body"]["translate"]))],
        arm_b=rot((0, 0), (0.4, -20), (0.8, DIE_END["arm_b"]["rotate"]), (1.0, DIE_END["arm_b"]["rotate"])),
        arm_f=rot((0, 0), (0.4, 20), (0.8, DIE_END["arm_f"]["rotate"]), (1.0, DIE_END["arm_f"]["rotate"])),
        leg_b=rot((0, 0), (0.75, DIE_END["leg_b"]["rotate"]), (1.0, DIE_END["leg_b"]["rotate"])),
        leg_f=rot((0, 0), (0.75, DIE_END["leg_f"]["rotate"]), (1.0, DIE_END["leg_f"]["rotate"])),
    ),
    "dead_loop": held(DIE_END),
    "revive": revive(),
}
SLOT_KEYS = {"hurt": {chip: [(0, chip), (0.45, None)] for chip in ("chip1", "chip2", "chip3")}}
STONES = ("body", "arm_b", "arm_f", "leg_b", "leg_f")
SLOT_COLORS = {"hurt": {s: [(0, WHITE), (0.04, RED), (0.3, WHITE)] for s in STONES}}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    images = {"shadow": shadow(360, 56)}
    slots = [("shadow", "root", 10, 8, 0)]
    slots.append(arm_slot("arm_f", "arm_f", "GOLEM_ARM", False, FAR, images))
    slots.append(arm_slot("leg_f", "leg_f", "GOLEM_LEG", False, FAR, images))
    slots.append(arm_slot("leg_b", "leg_b", "GOLEM_LEG", False, None, images))
    images["body"] = prepare(PARTS / "GOLEM_BODY.png")
    slots.append(("body", "body", 0, 386, 0))
    slots.append(arm_slot("arm_b", "arm_b", "GOLEM_ARM", False, None, images))
    for i, chip in enumerate(("chip1", "chip2", "chip3")):
        images[chip] = prepare(PARTS / f"PEBBLE_{i + 1}.png")
        slots.append((chip, chip, *BONES[chip][1:3], 0))
    size, n_slots, n_bones = write_rig(OUT, "golem", BONES, slots, images, ANIMATIONS, SCALE, None, SLOT_KEYS,
                                       ROOT / "artifacts/rig-custom/golem-setup.png", (700, 800), (350, 760),
                                       hidden=("chip1", "chip2", "chip3"), slot_colors=SLOT_COLORS)
    print(f"page {size}, {n_slots} slots, {n_bones} bones, animations {list(ANIMATIONS)}")


main()
