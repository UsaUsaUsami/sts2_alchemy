"""Builds the alchemist's rest-site rig (2026-10-02, user: the rest site too): the alchemist sitting by the fire
with a steaming cup, the golem resting beside. Parts: rig-parts.py --set rest (REST_BODY, REST_ARMS, GOLEM_REST)
and the standing rig's HOOD and UPPER_ARM. Writes assets/art/character/rig/rest/rest.{spine-json,atlas,png}.

The rest site plays one loop per act (overgrowth_loop, hive_loop, glory_loop) and, when the fire goes out,
"_tracks/light_off" on track 1 (NRestSiteCharacter); the rig has all of them (the same breathing loop; an empty
light_off).
Placement in sheet pixels (y up, ground at 0); each part has its own scale (the generated sheets differ).
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import numpy as np  # noqa: E402
from PIL import Image  # noqa: E402
from riglib import bone_anim, move, prepare, rot, scale, shadow, write_rig  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
PARTS = ROOT / "assets/art/character/rig/alchemist/parts"
OUT = ROOT / "assets/art/character/rig/rest"
FAR = "a8a8b0"

BONES = {
    "root": (None, 0, 0, 0),
    "body": ("root", -20, 60, 90),
    "head": ("body", -80, 545, 90),
    "golem": ("root", -560, 10, 90),
}

# (slot, bone, part, scale, world centre x, y, turn, flip, tint, trim), back to front. The seated figure is drawn
# whole (rig-parts.py --set rest2; the first body had shoulder sockets no arm covered), only the hood separate.
ART = [
    ("golem", "golem", "GOLEM_REST", 0.62, -560, 165, 0, False, None, 0),
    # The hood goes behind the body so the scarf wraps round its lower edge.
    ("head", "head", "HOOD", 0.66, -66, 598, 0, False, None, 0),
    ("body", "body", "REST_SEATED", 0.62, 0, 268, 0, False, None, 0),
]
SOCKETS = {}

T = 3.6
LOOP = bone_anim(
    body=[scale((0, 1, 1), (T / 2, 1, 1.012), (T, 1, 1))],
    head=[rot((0, 0), (T * 0.35, -2), (T * 0.5, -7), (T * 0.65, -7), (T * 0.85, 1), (T, 0)),
          move((0, 0, 0), (T / 2, 0, 5), (T, 0, 0))],
    # The hood dips towards the cup now and then.
    golem=[scale((0, 1, 1), (T * 0.55, 1.015, 0.99), (T, 1, 1))],
)
ANIMATIONS = {"overgrowth_loop": LOOP, "hive_loop": LOOP, "glory_loop": LOOP, "_tracks/light_off": {}}


def repaint_sockets(part):
    """The part's picture with its sockets in the robe's charcoal (SOCKETS), saved beside it as <part>_fixed.png."""
    im = Image.open(PARTS / f"{part}.png").convert("RGBA")
    a = np.asarray(im).astype(np.float32)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    region = np.zeros(al.shape, bool)
    for x0, y0, x1, y1 in SOCKETS.get(part, []):
        region[y0:y1, x0:x1] = True
    socket = region & (al > 100) & (r > 70) & (r - b > 18) & (r - b < 70) & (g < r)
    shade = (r + g + b) / 3
    for c, base in zip(range(3), (50, 47, 48)):
        a[..., c] = np.where(socket, base * (0.55 + 0.45 * shade / max(shade[socket].mean(), 1)) if socket.any() else a[..., c], a[..., c])
    path = PARTS / f"{part}_fixed.png"
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA").save(path)
    return path.stem


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    images = {"shadow": shadow(900, 70)}
    slots = [("shadow", "root", -220, 15, 0)]
    for slot, bone, part, s, x, y, r, flip, tint, trim in ART:
        name = repaint_sockets(part) if part in SOCKETS else part
        images[slot] = prepare(PARTS / f"{name}.png", flip=flip, tint=tint, trim=trim, scale_by=s)
        slots.append((slot, bone, x, y, r))
    size, n_slots, n_bones = write_rig(OUT, "rest", BONES, slots, images, ANIMATIONS, 1.0,
                                       preview=ROOT / "artifacts/rig-custom/rest-setup.png",
                                       preview_size=(1300, 800), preview_origin=(800, 760))
    print(f"page {size}, {n_slots} slots, {n_bones} bones")


main()
