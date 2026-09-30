"""Build texAH64BodyPanelAtlasNeutral.png from the olive panel atlas.

Alternate paints tint the body material's _Color. Tinting the olive atlas always
reads green, so skins need a neutral copy that keeps the panel lines, rivets,
vents and grime but carries no hue. The median panel value maps to PANEL_VALUE,
which makes the skin's _Color close to the paint colour players actually see.

Usage: python tools/make-neutral-atlas.py
"""
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
TEXTURES = ROOT / "AH64UnityProject/Assets/AH64/Bundle/AH64Surface/Textures"
SOURCE = TEXTURES / "texAH64BodyPanelAtlas_0116a.png"
TARGET = TEXTURES / "texAH64BodyPanelAtlasNeutral.png"

PANEL_VALUE = 0.80
KNEE = 0.88


def main():
    rgb = np.asarray(Image.open(SOURCE).convert("RGB"), dtype=np.float64) / 255.0
    luma = rgb @ np.array([0.299, 0.587, 0.114])
    value = luma * (PANEL_VALUE / np.median(luma))
    # Soft shoulder keeps edge highlights distinct instead of clipping them flat.
    over = value > KNEE
    value[over] = KNEE + (1.0 - KNEE) * np.tanh((value[over] - KNEE) / (1.0 - KNEE))
    out = np.clip(value * 255.0 + 0.5, 0, 255).astype(np.uint8)
    Image.fromarray(np.stack([out] * 3, axis=-1), "RGB").save(TARGET, optimize=True)
    print("median luma %.3f -> %s" % (np.median(luma), TARGET.name))


if __name__ == "__main__":
    main()
