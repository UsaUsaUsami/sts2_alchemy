"""Extract StS2 character art from the local game pck as style references for image generation (not shipped).

Usage: python scripts/extract-sts2-refs.py [path/to/SlayTheSpire2.pck]
Writes .research/sts2-ref/ (git-ignored): the five character-select portraits (lineup_small.png) and the
character-select art (characterselect_<name>.png; only the Silent's is one whole picture, the others are animation
part sheets). These are the base game's assets: use them only as references handed to a generator, never put them in
the mod or the repository (AGENTS.md 3).

Format notes (Godot 4.5 pack v3, unencrypted): header "GDPC", then the file table at the offset stored after the
header. Imported textures are .ctex ("GST2"): small ones embed WebP/PNG, large ones are raw DXT5 (format 19).
"""
import io
import json
import re
import struct
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
pck = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / ".research/runtime/SlayTheSpire2.pck"
out = ROOT / ".research/sts2-ref"
out.mkdir(parents=True, exist_ok=True)
NAMES = ["ironclad", "silent", "defect", "necrobinder", "regent"]

f = open(pck, "rb")
if f.read(4) != b"GDPC":
    sys.exit("pckではありません。")
version = struct.unpack("<I", f.read(4))[0]
f.seek(4 + 16)
flags, base = struct.unpack("<IQ", f.read(12))
if version < 3 or flags & 1:
    sys.exit(f"未対応のpck（version {version}, flags {flags}）。")
f.seek(struct.unpack("<Q", f.read(8))[0])
files = {}
for _ in range(struct.unpack("<I", f.read(4))[0]):
    path = f.read(struct.unpack("<I", f.read(4))[0]).rstrip(b"\0").decode("utf-8", "replace")
    offset, size = struct.unpack("<QQ", f.read(16))
    f.read(16 + 4)
    files[path] = (base + offset, size)


def texture(pattern):
    path = next((p for p in files if re.search(pattern, p)), None)
    if path is None:
        return None
    f.seek(files[path][0])
    data = f.read(files[path][1])
    for sig in (b"RIFF", b"\x89PNG"):
        i = data.find(sig)
        if 0 <= i < 128:
            return Image.open(io.BytesIO(data[i:])).convert("RGBA")
    _, _, width, height, *_ = struct.unpack("<4sIIIIi3I", data[:36])
    data_format, w, h, _, fmt = struct.unpack("<IHHII", data[36:52])
    if data_format != 0 or fmt != 19:
        return None
    return Image.frombytes("RGBA", (w, h), data[52:52 + (w // 4) * (h // 4) * 16], "bcn", (3,)).crop((0, 0, width, height))


portraits = []
for name in NAMES:
    small = texture(rf"imported/char_select_{name}\.png-.*\.ctex$")
    if small:
        portraits.append(small)
    big = texture(rf"imported/characterselect_{name}\.png-.*\.ctex$")
    if big:
        big.save(out / f"characterselect_{name}.png")
if portraits:
    sheet = Image.new("RGBA", (sum(p.width * 2 for p in portraits), max(p.height for p in portraits) * 2), (40, 40, 48, 255))
    x = 0
    for p in portraits:
        big = p.resize((p.width * 2, p.height * 2))
        sheet.paste(big, (x, 0), big)
        x += big.width
    sheet.save(out / "lineup_small.png")
print(f"{out}: " + ", ".join(sorted(p.name for p in out.glob("*.png"))))
