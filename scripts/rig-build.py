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
import json
import math

import numpy as np
from pathlib import Path

from PIL import Image, ImageChops, ImageOps

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
    "fore_f": ("arm_f", 155, 590, -50),
    "hand_f": ("fore_f", None, None, -50),
    "staff": ("hand_f", None, None, 90),
    # The legs hang from the root, not the hip: when the body sinks (death) the shoes stay on the ground.
    "leg_b": ("root", -60, 600, -90),
    "foot_b": ("leg_b", -70, 120, 0),
    "leg_f": ("root", 60, 600, -90),
    "foot_f": ("leg_f", 70, 120, 0),
}

# Slots in draw order (back to front): (slot, bone, part, world centre x, y, extra rotation, flip, tint).
# The torso is drawn turned to the right (three-quarter view), so the near side is the LEFT of the picture: the left
# arm and foot are drawn in front of the body, the right (enemy-side) arm that holds the staff behind it, darkened
# by tint so it reads as farther (user, 2026-10-02: "the arms look the wrong way round").
# Bone names: *_f = the forward (enemy-side, far) limbs, *_b = the back-side (near) limbs.
FAR = "a8a8b0"
SLOTS = [
    ("cape", "cape", "CAPE", -150, 560, 0, False, None),
    ("staff", "staff", "STAFF", None, None, 0, False, None),
    ("hand_f", "hand_f", "HAND_GRIP", None, None, None, False, FAR),
    ("arm_f", "arm_f", "UPPER_ARM", 130, 720, -10, False, FAR),
    ("fore_f", "fore_f", "FOREARM", 245, 505, 40, False, FAR),
    ("foot_f", "foot_f", "BOOT", 95, 108, 0, False, FAR),
    ("foot_b", "foot_b", "BOOT", -40, 112, 0, False, None),
    # The boots stay behind the robe: only the shoes show under the hem.
    ("skirt", "skirt", "SKIRT", 0, 345, 0, False, None),
    # The near upper sleeve stays in front of the torso: the torso picture has an open arm hole there (behind it,
    # the hole showed). Its gold top band is trimmed (TRIM) so the shoulder does not read as a seam.
    ("torso", "torso", "TORSO", 10, 690, 0, False, None),
    ("head", "head", "HOOD", 25, 1030, 0, False, None),
    ("arm_b", "arm_b", "UPPER_ARM", -125, 720, 10, True, None),
    # Not mirrored: this is the character's right hand seen from outside, thumb forward (to the right).
    ("hand_b", "hand_b", "HAND_OPEN", None, None, None, False, None),
    ("fore_b", "fore_b", "FOREARM", -160, 460, 5, True, None),
]

# Hands are not placed by hand: each wrist goes a little inside its sleeve's opening (derive_hands), and the staff
# through the fist's hole. None in a slot or bone is filled in there.
HANDS = {"hand_b": "fore_b", "hand_f": "fore_f"}
WRIST_INSET = 55      # how far the wrist sits inside the sleeve, along the sleeve (34 left the fist outside the cuff)
GRIP_HOLE = (62, 112)  # the gap through the fist in HAND_GRIP, picture pixels from its top left
STAFF_ABOVE_GRIP = 235  # the staff's centre above the fist's hole
LIMB_OVERLAP = 18  # how far an upper sleeve's top reaches up into the shoulder
# The forearm reaches deeper: its top is narrower than the upper sleeve's cut end, which showed at a straightened
# elbow as a gap (rig-probe at 2x and Gemini, 2026-10-02).
ELBOW_OVERLAP = 45
# The sleeve parts were drawn with a gold band at the top; at the shoulder and elbow it read as a seam. Cut off.
TRIM = {"FOREARM": 26, "UPPER_ARM": 22}


def key(t, **v):
    return {"time": round(t, 4), **v} if t else dict(v)


def rot(*frames):
    """Rotation keys: (time, degrees) pairs."""
    return {"rotate": [key(t, value=a) for t, a in frames]}


def move(*frames):
    """Translation keys: (time, x, y) triples."""
    return {"translate": [key(t, x=x, y=y) for t, x, y in frames]}


def bone_anim(**tracks):
    out = {}
    for name, timelines in tracks.items():
        merged = {}
        for tl in timelines if isinstance(timelines, list) else [timelines]:
            merged.update(tl)
        out[name] = merged
    return out


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
    # Draw the staff back, then thrust it forward with a lunge, the sigil head pointing at the enemy.
    # Keys add to the setup angles: arm_f is -80 in the setup pose, the forearm 30 more, the staff 140 more than the
    # hand; so arm 75 puts the arm level (-5), forearm -25 level (0), staff -130 level (pointing at the enemy).
    # Moves across the floor go on the root (the legs hang from it); the hip only rises and sinks.
    "attack": bone_anim(
        root=move((0, 0, 0), (0.15, -40, 0), (0.3, 120, 0), (0.55, 120, 0), (0.85, 0, 0)),
        hip=move((0, 0, 0), (0.3, 0, -15), (0.55, 0, -15), (0.85, 0, 0)),
        torso=rot((0, 0), (0.15, 8), (0.3, -12), (0.55, -12), (0.85, 0)),
        head=rot((0, 0), (0.15, 5), (0.3, -6), (0.85, 0)),
        arm_f=rot((0, 0), (0.15, 30), (0.3, 75), (0.55, 75), (0.85, 0)),
        fore_f=rot((0, 0), (0.15, 40), (0.3, -25), (0.55, -25), (0.85, 0)),
        staff=rot((0, 0), (0.15, -20), (0.3, -130), (0.55, -130), (0.85, 0)),
        arm_b=rot((0, 0), (0.15, -20), (0.3, 30), (0.85, 0)),
        cape=rot((0, 0), (0.3, 18), (0.55, 10), (0.85, 0)),
        skirt=rot((0, 0), (0.3, 6), (0.85, 0)),
    ),
    # Raise the staff high, hold, lower.
    "cast": bone_anim(
        hip=move((0, 0, 0), (0.3, 0, 30), (0.75, 0, 30), (1.0, 0, 0)),
        torso=rot((0, 0), (0.3, 6), (0.75, 6), (1.0, 0)),
        head=rot((0, 0), (0.3, 12), (0.75, 12), (1.0, 0)),
        # Hold the staff up in front: arm level forward (10), forearm a little up (20), staff upright (90).
        # A raised forearm turned the bell sleeve's opening upwards like a cup (2026-10-02), so the arm stays level.
        arm_f=rot((0, 0), (0.3, 90), (0.75, 90), (1.0, 0)),
        fore_f=rot((0, 0), (0.3, -20), (0.75, -20), (1.0, 0)),
        staff=rot((0, 0), (0.3, -70), (0.75, -70), (1.0, 0)),
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
    # Sink to the knees and slump; the staff falls forward. The last pose holds.
    "die": bone_anim(
        hip=move((0, 0, 0), (0.25, -20, 10), (0.8, -30, -330), (1.2, -30, -330)),
        torso=rot((0, 0), (0.25, 10), (0.8, -35), (1.2, -38)),
        head=rot((0, 0), (0.25, 15), (0.8, -30), (1.2, -32)),
        arm_f=rot((0, 0), (0.8, 25), (1.2, 28)),
        fore_f=rot((0, 0), (0.8, -40), (1.2, -40)),
        staff=rot((0, 0), (0.6, -40), (1.0, -95), (1.2, -92)),
        arm_b=rot((0, 0), (0.8, 30), (1.2, 32)),
        cape=rot((0, 0), (0.8, 25), (1.2, 22)),
    ),
}


def world_matrix(name, cache={}):
    if name in cache:
        return cache[name]
    parent, x, y, angle = BONES[name]
    cache[name] = (x, y, angle)
    return cache[name]


def to_local(bone, wx, wy, wangle):
    """World point and angle in the given bone's frame (bones carry no scale below the root)."""
    bx, by, ba = world_matrix(bone)
    a = math.radians(ba)
    dx, dy = wx - bx, wy - by
    return dx * math.cos(a) + dy * math.sin(a), -dx * math.sin(a) + dy * math.cos(a), wangle - ba


def pack(images):
    """Shelf packing on one page; returns page size and each part's box."""
    width, x, y, shelf, boxes = 2048, 2, 2, 0, {}
    for name, im in sorted(images.items(), key=lambda kv: -kv[1].height):
        if x + im.width + 2 > width:
            x, y, shelf = 2, y + shelf + 2, 0
        boxes[name] = (x, y)
        x += im.width + 2
        shelf = max(shelf, im.height)
    height = 1 << math.ceil(math.log2(y + shelf + 2))
    return (width, height), boxes


def turn(dx, dy, degrees):
    """A picture offset (x right, y up) turned by the slot's rotation (counter-clockwise)."""
    a = math.radians(degrees)
    return dx * math.cos(a) - dy * math.sin(a), dx * math.sin(a) + dy * math.cos(a)


def picture_point(slot_index, images, px, py):
    """World position of a pixel (from the top left) of a placed slot's picture."""
    slot, _, _, cx, cy, r, *_ = SLOTS[slot_index]
    im = images[slot]
    dx, dy = turn(px - im.width / 2, im.height / 2 - py, r)
    return cx + dx, cy + dy


def top_centre(im, row=8):
    """The middle of the picture's opaque pixels on a row near its top: where the limb joins its parent."""
    xs = np.nonzero(np.asarray(im.getchannel("A"))[row] > 128)[0]
    return xs.mean(), row


def hang(i, images, pivot, r, overlap):
    """Places slot i so its top centre sits at the pivot, pushed `overlap` up into the parent to hide the joint."""
    slot, bone, part, _, _, _, flip, tint = SLOTS[i]
    im = images[slot]
    tx, ty = top_centre(im)
    ux, uy = turn(0, 1, r)
    jx, jy = pivot[0] + ux * overlap, pivot[1] + uy * overlap
    dx, dy = turn(im.width / 2 - tx, -(im.height / 2 - ty), r)
    SLOTS[i] = (slot, bone, part, jx + dx, jy + dy, r, flip, tint)


def opening(im):
    """The middle of a bell sleeve's opening, in picture pixels: the rim is the lowest opaque pixel of each column in
    the lower part of the picture; the opening's middle is halfway between the rim's two ends, at the rim's mean
    height. (The lowest rows alone are only the rim's lowest corner on a slanted cuff: the hand hung below that
    corner, 2026-10-02.)"""
    alpha = np.asarray(im.getchannel("A")) > 128
    h = alpha.shape[0]
    bottoms = np.array([np.nonzero(col)[0].max() if col.any() else -1 for col in alpha.T])
    rim = np.nonzero(bottoms > h * 0.6)[0]
    return (rim.min() + rim.max()) / 2, bottoms[rim].mean()


def derive(images):
    """Fills in the limbs from the bones: each sleeve hangs from its joint (upper arms turned from shoulder to
    elbow), each wrist sits inside its sleeve opening, the staff goes through the fist."""
    index = {s[0]: i for i, s in enumerate(SLOTS)}
    for upper, lower in (("arm_b", "fore_b"), ("arm_f", "fore_f")):
        sx, sy = BONES[upper][1:3]
        ex, ey = BONES[lower][1:3]
        # The picture's down (0, -1) turned by r points from shoulder to elbow: (sin r, -cos r).
        r = math.degrees(math.atan2(ex - sx, -(ey - sy)))
        hang(index[upper], images, (sx, sy), r, LIMB_OVERLAP)
        hang(index[lower], images, (ex, ey), SLOTS[index[lower]][5], ELBOW_OVERLAP)
    for hand, sleeve in HANDS.items():
        si = index[sleeve]
        im = images[sleeve]
        ox, oy = picture_point(si, images, *opening(im))
        r = SLOTS[si][5]
        ux, uy = turn(0, 1, r)  # up the sleeve
        wx, wy = ox + ux * WRIST_INSET, oy + uy * WRIST_INSET
        hi = index[hand]
        slot, bone, part, _, _, _, flip, tint = SLOTS[hi]
        him = images[hand]
        top = np.nonzero(np.asarray(him.getchannel("A"))[8] > 128)[0]
        dx, dy = turn(him.width / 2 - top.mean(), -(him.height / 2 - 8), r)
        SLOTS[hi] = (slot, bone, part, wx + dx, wy + dy, r, flip, tint)
        parent, _, _, angle = BONES[hand]
        BONES[hand] = (parent, wx, wy, angle)
        if hand == "hand_f":
            gx, gy = picture_point(hi, images, *GRIP_HOLE)
            BONES["staff"] = ("hand_f", gx, gy, 90)
            st = index["staff"]
            SLOTS[st] = SLOTS[st][:3] + (gx, gy + STAFF_ABOVE_GRIP) + SLOTS[st][5:]


def main():
    images = {}
    for slot, _, part, *_rest, flip, tint in SLOTS:
        im = Image.open(PARTS / f"{part}.png").convert("RGBA")
        if part in TRIM:
            im = im.crop((0, TRIM[part], im.width, im.height))
        if flip:
            im = ImageOps.mirror(im)
        if tint:
            rgb = ImageChops.multiply(im.convert("RGB"), Image.new("RGB", im.size, "#" + tint))
            im = Image.merge("RGBA", (*rgb.split(), im.getchannel("A")))
        images[slot] = im
    derive(images)
    size, boxes = pack(images)
    page = Image.new("RGBA", size, (0, 0, 0, 0))
    lines = ["alchemist.png", f"size:{size[0]},{size[1]}", "filter:Linear,Linear"]
    for slot, im in images.items():
        page.alpha_composite(im, boxes[slot])
        lines += [slot, f"bounds:{boxes[slot][0]},{boxes[slot][1]},{im.width},{im.height}"]
    page.save(DIR / "alchemist.png")
    (DIR / "alchemist.atlas").write_text("\n".join(lines) + "\n", encoding="utf-8")

    bones = []
    for name, (parent, x, y, angle) in BONES.items():
        if parent is None:
            bones.append({"name": name, "scaleX": SCALE, "scaleY": SCALE})
            continue
        lx, ly, la = to_local(parent, x, y, angle)
        bones.append({"name": name, "parent": parent, "x": round(lx, 2), "y": round(ly, 2), "rotation": round(la, 2),
                      "length": 60})
    slots, skin = [], {}
    for slot, bone, part, wx, wy, extra, flip, tint in SLOTS:
        slots.append({"name": slot, "bone": bone, "attachment": slot})
        lx, ly, la = to_local(bone, wx, wy, extra)
        im = images[slot]
        skin[slot] = {slot: {"x": round(lx, 2), "y": round(ly, 2), "rotation": round(la, 2), "width": im.width,
                             "height": im.height}}
    skeleton = {
        "skeleton": {"hash": "alchemist", "spine": "4.2.43", "x": -300, "y": 0, "width": 700, "height": 1250,
                     "images": "", "audio": ""},
        "bones": bones,
        "slots": slots,
        "skins": [{"name": "default", "attachments": skin}],
        "animations": {name: {"bones": tracks} for name, tracks in ANIMATIONS.items()},
    }
    (DIR / "alchemist.spine-json").write_text(json.dumps(skeleton, indent=1), encoding="utf-8")
    # The setup pose as placed (no game needed): artifacts/rig-custom/setup.png, with bone pivots as dots.
    from PIL import ImageDraw
    view = Image.new("RGBA", (900, 1350), (58, 58, 68, 255))
    ox, oy = 450, 1300
    for slot, bone, part, wx, wy, extra, flip, tint in SLOTS:
        im = images[slot].rotate(extra, expand=True, resample=Image.BICUBIC)
        view.alpha_composite(im, (int(ox + wx - im.width / 2), int(oy - wy - im.height / 2)))
    d = ImageDraw.Draw(view)
    for name, (parent, x, y, angle) in BONES.items():
        d.ellipse([ox + x - 4, oy - y - 4, ox + x + 4, oy - y + 4], outline=(0, 255, 255, 255))
    (ROOT / "artifacts/rig-custom").mkdir(parents=True, exist_ok=True)
    view.save(ROOT / "artifacts/rig-custom/setup.png")
    print(f"page {size}, {len(slots)} slots, {len(bones)} bones, animations {list(ANIMATIONS)}")


main()
