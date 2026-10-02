"""Builds the alchemist's own Spine rig from the cut parts (scripts/rig-cut.py): one atlas page, a skeleton and its
animations, written as Spine 4.2 JSON. The game's spine-godot loads it from plain files (".spine-json"; ".json" is
read as binary and refused), the way HelloSpire loads its rigs (2026-10-02).

Usage: python scripts/rig-build.py
Writes assets/art/character/rig/alchemist/alchemist.{spine-json,atlas,png}.

Everything is placed in the assembled pose in sheet pixels (y up, feet at 0): bones by world position and angle,
parts by the world position of their centre (drawn upright). The script turns that into Spine's local values, so a
placement can be changed by its numbers on screen. The root bone scales the whole rig to the game's size.
Animations key rotation (degrees, added to the setup pose) and translation (sheet pixels) of named bones.
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from riglib import bone_anim, move, picture_point as point_on, place, prepare as prepare_file, rot, shadow, turn, write_rig  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
DIR = ROOT / "assets/art/character/rig/alchemist"
PARTS = DIR / "parts"
# Measured with rig-probe: one unit is about 0.26 px of a 700 px frame at the probe's 1.25 zoom, and the Necrobinder
# stands about 330 px there; ours is about 1200 sheet pixels tall.
SCALE = 1.0

# Bones: name -> (parent, world x, world y, world angle). Angle 90 = pointing up, -90 = down.
BONES = {
    "root": (None, 0, 0, 0),
    "hip": ("root", 0, 600, 90),
    "skirt": ("hip", 0, 610, -90),
    "torso": ("hip", 0, 600, 90),
    "neck": ("torso", 8, 900, 90),
    "head": ("neck", 8, 900, 90),
    "cape": ("torso", -55, 905, -90),
    "arm_b": ("torso", -100, 860, -100),
    "fore_b": ("arm_b", -150, 590, -95),
    "hand_b": ("fore_b", None, None, -95),
    "arm_f": ("torso", 105, 860, -80),
    # No staff (2026-10-02, user: it never looked held; the key visual has none): the far arm hangs at the side.
    "fore_f": ("arm_f", 155, 590, -85),
    "hand_f": ("fore_f", None, None, -85),
    # The legs hang from the root, not the hip: when the body sinks (death) the shoes stay on the ground.
    "leg_b": ("root", -60, 600, -90),
    "foot_b": ("leg_b", -70, 120, 0),
    "leg_f": ("root", 60, 600, -90),
    "foot_f": ("leg_f", 70, 120, 0),
}

# Slots in draw order (back to front): (slot, bone, part, world centre x, y, extra rotation, flip, tint).
# The torso is drawn turned to the right (three-quarter view), so the near side is the LEFT of the picture: the left
# arm and foot are drawn in front of the body, the right (enemy-side) arm behind it, darkened
# by tint so it reads as farther (user, 2026-10-02: "the arms look the wrong way round").
# Bone names: *_f = the forward (enemy-side, far) limbs, *_b = the back-side (near) limbs.
FAR = "a8a8b0"
SLOTS = [
    ("cape", "cape", "CAPE", -150, 560, 0, False, None),
    ("arm_f", "arm_f", "UPPER_ARM", None, None, None, False, FAR),
    # The forearms carry their hands (rig-parts.py --set forearms / --set left): the left hand from the palm side.
    ("fore_f", "fore_f", "FOREARM_OPEN_L", None, None, None, False, FAR),
    ("foot_f", "foot_f", "BOOT", 95, 108, 0, False, FAR),
    ("foot_b", "foot_b", "BOOT", -40, 112, 0, False, None),
    # The boots stay behind the robe: only the shoes show under the hem.
    ("skirt", "skirt", "SKIRT", 0, 345, 0, False, None),
    # The near upper sleeve stays in front of the torso: the torso picture has an open arm hole there (behind it,
    # the hole showed). Its gold top band is trimmed (TRIM) so the shoulder does not read as a seam.
    ("torso", "torso", "TORSO", 10, 690, 0, False, None),
    ("head", "head", "HOOD", 25, 1030, 0, False, None),
    ("arm_b", "arm_b", "UPPER_ARM", None, None, None, True, None),
    # The right hand seen from outside, thumb forward (to the right), coming out of the cuff.
    ("fore_b", "fore_b", "FOREARM_OPEN", None, None, None, False, None),
]

# The limbs are not placed by hand: each sleeve hangs from its joint (derive), turned along its bone. None in a slot
# is filled in there.
# 2026-10-02: hands used to be separate parts fitted into the slanted cuffs by calculation; they kept looking stuck on
# from outside the sleeve (user, Codex), so the forearms are now drawn with the hand coming out of the cuff.
LIMB_OVERLAP = 18  # how far an upper sleeve's top reaches up into the shoulder
# The forearm reaches deeper: its top is narrower than the upper sleeve's cut end, which showed at a straightened
# elbow as a gap (rig-probe at 2x and Gemini, 2026-10-02).
ELBOW_OVERLAP = 45
# The sleeve parts were drawn with a gold band at the top; at the shoulder and elbow it read as a seam. Cut off.
TRIM = {"UPPER_ARM": 22}
# The upper sleeve is narrowed to the forearms' top (drawn separately, about 87 px wide against its 110): at a
# straightened elbow the joint pinched in (Codex, 2026-10-02).
WIDTH = {"UPPER_ARM": 0.8}
# The forearms' top fades in over these many pixels, so their outline does not draw a line across the upper sleeve
# at the elbow (it lies inside ELBOW_OVERLAP, over the upper sleeve; Codex, 2026-10-02).
FEATHER = {"FOREARM_OPEN": 28, "FOREARM_GRIP": 28, "FOREARM_OPEN_L": 28, "FOREARM_PUSH_L": 28, "FOREARM_RAISE_L": 28}
# Extra pictures a slot can switch to during an animation: slot -> [(attachment, part, degrees turned clockwise)].
# They are drawn as seen in the pose; the turn puts the elbow at the top like the other forearms. The palm push
# (attack) lies flat; the raised hand (cast) rises at about 38 degrees (measured from the sleeve's pixels). Their
# elbow ends have a gold band: cut. (2026-10-02, user)
ALTS = {"fore_f": [("fore_f_push", "FOREARM_PUSH_L", 90), ("fore_f_raise", "FOREARM_RAISE_L", 128)]}
TRIM["FOREARM_PUSH_L"] = 40
TRIM["FOREARM_RAISE_L"] = 40
# Which picture each slot shows, keyed in time per animation (the others start from the setup picture).
SLOT_KEYS = {"attack": {"fore_f": [(0, "fore_f"), (0.22, "fore_f_push"), (0.7, "fore_f")]},
             "cast": {"fore_f": [(0, "fore_f"), (0.15, "fore_f_raise"), (0.85, "fore_f")]}}


T = 2.4  # idle period
ANIMATIONS = {
    "idle_loop": bone_anim(
        hip=move((0, 0, 0), (T / 2, 0, -10), (T, 0, 0)),
        torso=rot((0, 0), (T / 2, -1.5), (T, 0)),
        head=rot((0, 0), (T / 2, 2.5), (T, 0)),
        cape=rot((0, 0), (T / 2, 4), (T, 0)),
        skirt=rot((0, 0), (T / 2, 1.2), (T, 0)),
        arm_b=rot((0, 0), (T / 2, 3), (T, 0)),
        arm_f=rot((0, 0), (T / 2, -2), (T, 0)),
        fore_b=rot((0, 0), (T / 2, 4), (T, 0)),
    ),
    # The merchant room plays this (NMerchantCharacter); the idle, slower and softer.
    "relaxed_loop": bone_anim(
        hip=move((0, 0, 0), (T * 0.75, 0, -7), (T * 1.5, 0, 0)),
        torso=rot((0, 0), (T * 0.75, -1), (T * 1.5, 0)),
        head=rot((0, 0), (T * 0.75, 2), (T * 1.5, 0)),
        cape=rot((0, 0), (T * 0.75, 3), (T * 1.5, 0)),
        arm_b=rot((0, 0), (T * 0.75, 2), (T * 1.5, 0)),
        arm_f=rot((0, 0), (T * 0.75, -1.5), (T * 1.5, 0)),
    ),
    # Draw the hand back to the chest, then lunge and thrust it at the enemy, a cast thrown from the open hand.
    # Keys add to the setup angles: arm_f is -80, the forearm -85; arm 75 and forearm 10 put both level (forward).
    # Moves across the floor go on the root (the legs hang from it); the hip only rises and sinks.
    "attack": bone_anim(
        root=move((0, 0, 0), (0.15, -40, 0), (0.3, 120, 0), (0.55, 120, 0), (0.85, 0, 0)),
        hip=move((0, 0, 0), (0.3, 0, -15), (0.55, 0, -15), (0.85, 0, 0)),
        torso=rot((0, 0), (0.15, 8), (0.3, -12), (0.55, -12), (0.85, 0)),
        head=rot((0, 0), (0.15, 5), (0.3, -6), (0.85, 0)),
        arm_f=rot((0, 0), (0.15, -15), (0.3, 75), (0.55, 75), (0.85, 0)),
        fore_f=rot((0, 0), (0.15, 60), (0.3, 10), (0.55, 10), (0.85, 0)),
        arm_b=rot((0, 0), (0.15, -20), (0.3, 30), (0.85, 0)),
        cape=rot((0, 0), (0.3, 18), (0.55, 10), (0.85, 0)),
        skirt=rot((0, 0), (0.3, 6), (0.85, 0)),
    ),
    "cast": bone_anim(
        hip=move((0, 0, 0), (0.3, 0, 30), (0.75, 0, 30), (1.0, 0, 0)),
        torso=rot((0, 0), (0.3, 6), (0.75, 6), (1.0, 0)),
        head=rot((0, 0), (0.3, 12), (0.75, 12), (1.0, 0)),
        # Arm level (10), forearm up at 45 (setup -85). Straight up turned the bell sleeve's opening upwards like
        # a cup (2026-10-02).
        arm_f=rot((0, 0), (0.3, 90), (0.75, 90), (1.0, 0)),
        fore_f=rot((0, 0), (0.3, 40), (0.75, 40), (1.0, 0)),
        # The near arm opens out and down; held level, the hand read as turned the wrong way (Gemini, 2026-10-02).
        arm_b=rot((0, 0), (0.3, -40), (0.75, -40), (1.0, 0)),
        fore_b=rot((0, 0), (0.3, -10), (0.75, -10), (1.0, 0)),
        cape=rot((0, 0), (0.3, 10), (1.0, 0)),
    ),
    "hurt": bone_anim(
        root=move((0, 0, 0), (0.1, -70, 0), (0.45, 0, 0)),
        # The torso leans little (its hem would slide off the skirt); the head takes the rest.
        torso=rot((0, 0), (0.1, 5), (0.45, 0)),
        head=rot((0, 0), (0.1, 20), (0.45, 0)),
        arm_f=rot((0, 0), (0.1, 20), (0.45, 0)),
        arm_b=rot((0, 0), (0.1, 25), (0.45, 0)),
        cape=rot((0, 0), (0.1, -12), (0.45, 0)),
    ),
    # Sink to the knees and slump. The last pose holds.
    "die": bone_anim(
        hip=move((0, 0, 0), (0.25, -20, 10), (0.8, -30, -330), (1.2, -30, -330)),
        torso=rot((0, 0), (0.25, 10), (0.8, -35), (1.2, -38)),
        head=rot((0, 0), (0.25, 15), (0.8, -30), (1.2, -32)),
        arm_f=rot((0, 0), (0.8, 25), (1.2, 28)),
        fore_f=rot((0, 0), (0.8, -40), (1.2, -40)),
        arm_b=rot((0, 0), (0.8, 30), (1.2, 32)),
        cape=rot((0, 0), (0.8, 25), (1.2, 22)),
    ),
}


def picture_point(slot_index, images, px, py):
    """World position of a pixel (from the top left) of a placed slot's picture."""
    slot, _, _, cx, cy, r, *_ = SLOTS[slot_index]
    return point_on((cx, cy), r, images[slot], px, py)


def hang(i, images, pivot, r, overlap):
    """Places slot i so its top centre sits at the pivot, pushed `overlap` up into the parent to hide the joint."""
    slot, bone, part, _, _, _, flip, tint = SLOTS[i]
    SLOTS[i] = (slot, bone, part, *place(images[slot], pivot, r, overlap), r, flip, tint)


def derive(images):
    """Fills in the arms from the bones: each sleeve hangs from its joint, turned along its bone (upper arms from
    shoulder to elbow)."""
    index = {s[0]: i for i, s in enumerate(SLOTS)}
    for upper, lower in (("arm_b", "fore_b"), ("arm_f", "fore_f")):
        sx, sy = BONES[upper][1:3]
        ex, ey = BONES[lower][1:3]
        # The picture's down (0, -1) turned by r points from shoulder to elbow: (sin r, -cos r).
        r = math.degrees(math.atan2(ex - sx, -(ey - sy)))
        hang(index[upper], images, (sx, sy), r, LIMB_OVERLAP)
        hang(index[lower], images, (ex, ey), BONES[lower][3] + 90, ELBOW_OVERLAP)
    for hand, sleeve in (("hand_b", "fore_b"), ("hand_f", "fore_f")):
        # The hand bones sit at the wrist, about where the sleeve's lower third starts (nothing hangs from them now;
        # kept for later props).
        im = images[sleeve]
        wx, wy = picture_point(index[sleeve], images, im.width / 2, im.height * 0.6)
        parent, _, _, angle = BONES[hand]
        BONES[hand] = (parent, wx, wy, angle)


def prepare(part, flip, tint, degrees_cw=0):
    return prepare_file(PARTS / f"{part}.png", flip, tint, degrees_cw, TRIM.get(part, 0), WIDTH.get(part, 1.0),
                        FEATHER.get(part, 0))


def main():
    images = {slot: prepare(part, flip, tint) for slot, _, part, *_rest, flip, tint in SLOTS}
    index = {s[0]: i for i, s in enumerate(SLOTS)}
    for slot, alts in ALTS.items():
        _, _, _, *_r, flip, tint = SLOTS[index[slot]]
        for name, part, turns in alts:
            images[name] = prepare(part, flip, tint, turns)
    derive(images)
    extra = {}
    for slot, alts in ALTS.items():
        bone = SLOTS[index[slot]][1]
        r = SLOTS[index[slot]][5]
        # Deeper than the other forearms: the upper sleeve's open bottom showed as a dark oval through the
        # push forearm's faded top (Codex, 2026-10-02).
        extra[slot] = [(name, *place(images[name], BONES[bone][1:3], r, ELBOW_OVERLAP + 30), r) for name, _, _ in alts]
    slots = [(slot, bone, wx, wy, r) for slot, bone, part, wx, wy, r, flip, tint in SLOTS]
    # A soft shadow under the feet, on the root so it follows the lunges (the base game's rigs carry one; the
    # merchant room showed ours without, 2026-10-02).
    images["shadow"] = shadow(440, 64)
    slots.insert(0, ("shadow", "root", 20, 8, 0))
    size, n_slots, n_bones = write_rig(DIR, "alchemist", BONES, slots, images, ANIMATIONS, SCALE, extra, SLOT_KEYS,
                                       ROOT / "artifacts/rig-custom/setup.png")
    print(f"page {size}, {n_slots} slots, {n_bones} bones, animations {list(ANIMATIONS)}")


main()
