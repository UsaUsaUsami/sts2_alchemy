"""Shared pieces for the scripts that write our own Spine 4.2 rigs as JSON (rig-build.py: the alchemist,
golem-build.py: the golem, rest-build.py: the rest site). The game's spine-godot loads them from plain files
(".spine-json"; ".json" is read as binary and refused).

A rig is described in the assembled pose in sheet pixels (y up): bones by world position and angle, parts by the
world position of their centre and their turn. write_rig turns that into Spine's local values.
"""
import json
import math

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps


def key(t, **v):
    return {"time": round(t, 4), **v} if t else dict(v)


def rot(*frames):
    """Rotation keys: (time, degrees) pairs, added to the setup angle."""
    return {"rotate": [key(t, value=a) for t, a in frames]}


def move(*frames):
    """Translation keys: (time, x, y) triples, added to the setup position."""
    return {"translate": [key(t, x=x, y=y) for t, x, y in frames]}


def scale(*frames):
    """Scale keys: (time, x, y) triples, multiplying the setup scale."""
    return {"scale": [key(t, x=x, y=y) for t, x, y in frames]}


def bone_anim(**tracks):
    out = {}
    for name, timelines in tracks.items():
        merged = {}
        for tl in timelines if isinstance(timelines, list) else [timelines]:
            merged.update(tl)
        out[name] = merged
    return out


def turn(dx, dy, degrees):
    """An offset (x right, y up) turned counter-clockwise."""
    a = math.radians(degrees)
    return dx * math.cos(a) - dy * math.sin(a), dx * math.sin(a) + dy * math.cos(a)


def top_centre(im, row=8):
    """The middle of the picture's opaque pixels on a row near its top: where a limb joins its parent."""
    xs = np.nonzero(np.asarray(im.getchannel("A"))[row] > 8)[0]  # > 8: a faded top still counts
    return xs.mean(), row


def place(im, pivot, r, overlap):
    """Where a picture's centre goes so its top centre sits at the pivot, `overlap` up into the part above."""
    tx, ty = top_centre(im)
    ux, uy = turn(0, 1, r)
    jx, jy = pivot[0] + ux * overlap, pivot[1] + uy * overlap
    dx, dy = turn(im.width / 2 - tx, -(im.height / 2 - ty), r)
    return jx + dx, jy + dy


def picture_point(centre, r, im, px, py):
    """World position of a pixel (from the top left) of a picture placed at `centre`, turned by r."""
    dx, dy = turn(px - im.width / 2, im.height / 2 - py, r)
    return centre[0] + dx, centre[1] + dy


def prepare(path, flip=False, tint=None, degrees_cw=0, trim=0, width=1.0, feather=0, scale_by=1.0):
    """A part picture made ready: turned (degrees clockwise, cropped to what is left), its top cut off (trim),
    narrowed (width), its top faded in over `feather` pixels, mirrored, darkened by multiplying with `tint`."""
    im = Image.open(path).convert("RGBA")
    if scale_by != 1:
        im = im.resize((round(im.width * scale_by), round(im.height * scale_by)), Image.LANCZOS)
    if degrees_cw:
        im = im.rotate(-degrees_cw, expand=True, resample=Image.BICUBIC)
        im = im.crop(im.getbbox())
    if trim:
        im = im.crop((0, trim, im.width, im.height))
    if width != 1:
        im = im.resize((round(im.width * width), im.height), Image.LANCZOS)
    if feather:
        a = np.asarray(im).astype(np.float32)
        a[..., 3] *= np.clip(np.arange(im.height) / feather, 0, 1)[:, None]
        im = Image.fromarray(a.astype(np.uint8), "RGBA")
    if flip:
        im = ImageOps.mirror(im)
    if tint:
        rgb = ImageChops.multiply(im.convert("RGB"), Image.new("RGB", im.size, "#" + tint))
        im = Image.merge("RGBA", (*rgb.split(), im.getchannel("A")))
    return im


def shadow(width, height, alpha=110):
    """A soft dark ellipse for under the feet (the base game's rigs carry one too)."""
    pad = height
    im = Image.new("RGBA", (width + 2 * pad, height + 2 * pad), (0, 0, 0, 0))
    ImageDraw.Draw(im).ellipse([pad, pad, pad + width, pad + height], fill=(0, 0, 0, alpha))
    return im.filter(ImageFilter.GaussianBlur(height / 4))


def pack(images):
    """Shelf packing on one page; returns page size and each picture's box."""
    width, x, y, shelf, boxes = 2048, 2, 2, 0, {}
    for name, im in sorted(images.items(), key=lambda kv: -kv[1].height):
        if x + im.width + 2 > width:
            x, y, shelf = 2, y + shelf + 2, 0
        boxes[name] = (x, y)
        x += im.width + 2
        shelf = max(shelf, im.height)
    height = 1 << math.ceil(math.log2(y + shelf + 2))
    return (width, height), boxes


def write_rig(out_dir, name, bones, slots, images, animations, root_scale=1.0, extra_attachments=None,
              slot_keys=None, preview=None, preview_size=(900, 1350), preview_origin=(450, 1300), hidden=(),
              slot_colors=None):
    """Writes <name>.png (the atlas page), <name>.atlas and <name>.spine-json into out_dir.

    bones: {name: (parent, world x, world y, world angle)}, parents first; the root (parent None) carries
    root_scale. slots: [(slot, bone, world centre x, y, turn)] in draw order; images[slot] is its picture.
    extra_attachments: {slot: [(attachment, world x, y, turn)]} other pictures the slot can switch to (images[name]).
    slot_keys: {animation: {slot: [(time, attachment)]}}; every slot with extra pictures also gets a key back to
    its setup picture at the start of each animation, so an animation cut short does not leave the extra on.
    preview: a path for the setup pose drawn without the game, with the bone pivots as dots.
    hidden: slots that show nothing in the setup pose (shown by slot_keys, e.g. flying debris).
    slot_colors: {animation: {slot: [(time, "rrggbbaa")]}}, a tint multiplied over the slot's picture.
    """
    extra_attachments = extra_attachments or {}
    slot_keys = slot_keys or {}
    slot_colors = slot_colors or {}
    size, boxes = pack(images)
    page = Image.new("RGBA", size, (0, 0, 0, 0))
    lines = [f"{name}.png", f"size:{size[0]},{size[1]}", "filter:Linear,Linear"]
    for region, im in images.items():
        page.alpha_composite(im, boxes[region])
        lines += [region, f"bounds:{boxes[region][0]},{boxes[region][1]},{im.width},{im.height}"]
    page.save(out_dir / f"{name}.png")
    (out_dir / f"{name}.atlas").write_text("\n".join(lines) + "\n", encoding="utf-8")

    def local(bone, wx, wy, wangle):
        _, bx, by, ba = bones[bone]
        a = math.radians(ba)
        dx, dy = wx - bx, wy - by
        return dx * math.cos(a) + dy * math.sin(a), -dx * math.sin(a) + dy * math.cos(a), wangle - ba

    out_bones = []
    for bone, (parent, x, y, angle) in bones.items():
        if parent is None:
            out_bones.append({"name": bone, "scaleX": root_scale, "scaleY": root_scale})
            continue
        lx, ly, la = local(parent, x, y, angle)
        out_bones.append({"name": bone, "parent": parent, "x": round(lx, 2), "y": round(ly, 2),
                          "rotation": round(la, 2), "length": 60})
    out_slots, skin = [], {}
    for slot, bone, wx, wy, r in slots:
        out_slots.append({"name": slot, "bone": bone, **({} if slot in hidden else {"attachment": slot})})
        skin[slot] = {}
        for att, ax, ay, ar in [(slot, wx, wy, r)] + extra_attachments.get(slot, []):
            lx, ly, la = local(bone, ax, ay, ar)
            skin[slot][att] = {"x": round(lx, 2), "y": round(ly, 2), "rotation": round(la, 2),
                               "width": images[att].width, "height": images[att].height}

    def timelines(animation):
        keys = {slot: [(0, slot)] for slot in extra_attachments}
        keys.update({slot: [(0, None)] for slot in hidden})
        keys.update(slot_keys.get(animation, {}))
        out = {slot: {"attachment": [key(t, name=att) for t, att in frames]} for slot, frames in keys.items()}
        for slot, frames in slot_colors.get(animation, {}).items():
            out.setdefault(slot, {})["rgba"] = [key(t, color=c) for t, c in frames]
        # Slots tinted in some animation are put back to white at the start of the others.
        for slot in {s for anim in slot_colors.values() for s in anim} - set(slot_colors.get(animation, {})):
            out.setdefault(slot, {})["rgba"] = [key(0, color="ffffffff")]
        return out

    skeleton = {
        "skeleton": {"hash": name, "spine": "4.2.43", "x": -preview_origin[0], "y": 0, "width": preview_size[0],
                     "height": preview_size[1], "images": "", "audio": ""},
        "bones": out_bones,
        "slots": out_slots,
        "skins": [{"name": "default", "attachments": skin}],
        "animations": {anim: {"bones": tracks, **({"slots": t} if (t := timelines(anim)) else {})}
                       for anim, tracks in animations.items()},
    }
    (out_dir / f"{name}.spine-json").write_text(json.dumps(skeleton, indent=1), encoding="utf-8")
    if preview:
        view = Image.new("RGBA", preview_size, (58, 58, 68, 255))
        ox, oy = preview_origin
        for slot, bone, wx, wy, r in slots:
            if slot in hidden:
                continue
            im = images[slot].rotate(r, expand=True, resample=Image.BICUBIC)
            view.alpha_composite(im, (int(ox + wx - im.width / 2), int(oy - wy - im.height / 2)))
        d = ImageDraw.Draw(view)
        for bone, (parent, x, y, angle) in bones.items():
            d.ellipse([ox + x - 4, oy - y - 4, ox + x + 4, oy - y + 4], outline=(0, 255, 255, 255))
        preview.parent.mkdir(parents=True, exist_ok=True)
        view.save(preview)
    return size, len(out_slots), len(out_bones)
