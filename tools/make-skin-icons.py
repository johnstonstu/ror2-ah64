"""Draw the character-select skin swatches, and optionally the mastery icon.

    python tools/make-skin-icons.py              # skin swatches only
    python tools/make-skin-icons.py <renders>    # also the mastery icon, from
        blender.exe -b Art/Blender/AH64.blend --python Art/Icons/render_skin_icons.py -- <renders>

Skin icons are four-colour swatches in the vanilla style: body paint on top,
mechanical parts right, markings bottom, canopy glass left. Colours approximate
each skin's in-game look (see AH64Skins.cs), in sRGB.

Files are overwritten in place so their .meta GUIDs and sprite import settings
are unchanged.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
ICONS = ROOT / "AH64UnityProject/Assets/AH64/Bundle/Icons"
SIZE = 256
SUPERSAMPLE = 4
CORNER = 44
GLASS = (0.10, 0.24, 0.31)

# (body, mechanical, markings)
SWATCHES = {
    "texMainSkin.png": ((0.36, 0.42, 0.22), (0.20, 0.22, 0.17), (0.52, 0.33, 0.13)),
    "texAH64SkinDesert.png": ((0.70, 0.60, 0.45), (0.33, 0.31, 0.26), (0.12, 0.11, 0.10)),
    "texAH64SkinArctic.png": ((0.84, 0.87, 0.89), (0.30, 0.34, 0.37), (0.67, 0.30, 0.13)),
    "texAH64SkinArmy.png": ((0.34, 0.38, 0.27), (0.24, 0.26, 0.21), (0.10, 0.10, 0.09)),
    "texAH64SkinNight.png": ((0.17, 0.18, 0.19), (0.11, 0.12, 0.13), (0.50, 0.11, 0.08)),
}


def rgb(c):
    return tuple(int(round(v * 255)) for v in c) + (255,)


def swatch(body, mechanical, markings):
    s = SIZE * SUPERSAMPLE
    c = s // 2
    art = Image.new("RGBA", (s, s))
    draw = ImageDraw.Draw(art)
    draw.polygon([(0, 0), (s, 0), (c, c)], fill=rgb(body))
    draw.polygon([(s, 0), (s, s), (c, c)], fill=rgb(mechanical))
    draw.polygon([(0, s), (s, s), (c, c)], fill=rgb(markings))
    draw.polygon([(0, 0), (0, s), (c, c)], fill=rgb(GLASS))
    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, s - 1, s - 1), CORNER * SUPERSAMPLE, fill=255)
    art.putalpha(mask)
    return art.resize((SIZE, SIZE), Image.LANCZOS)


def gradient(top, bottom):
    t = np.linspace(0.0, 1.0, SIZE)[:, None, None]
    colour = (1 - t) * np.array(top) + t * np.array(bottom)
    colour = np.broadcast_to(colour, (SIZE, SIZE, 3))
    return Image.fromarray((colour * 255).astype(np.uint8), "RGB").convert("RGBA")


def mastery(render):
    icon = gradient((0.80, 0.58, 0.34), (0.30, 0.20, 0.16))
    icon.alpha_composite(Image.open(render).convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS))
    draw = ImageDraw.Draw(icon)
    for i in range(10):
        draw.rectangle((i, i, SIZE - 1 - i, SIZE - 1 - i), outline=(176, 138, 64))
    return icon.convert("RGB")


def main():
    written = []
    for name, colours in SWATCHES.items():
        swatch(*colours).save(ICONS / name)
        written.append(name)
    if len(sys.argv) > 1:
        #The Mastery achievement unlocks Night Stalker, so its icon shows that paint.
        mastery(Path(sys.argv[1]) / "skin_Night.png").save(ICONS / "texMasteryAchievement.png")
        written.append("texMasteryAchievement.png")
    print("wrote", ", ".join(written))


if __name__ == "__main__":
    main()
