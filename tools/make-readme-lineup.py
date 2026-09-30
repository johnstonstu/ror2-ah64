"""Composite the skin renders into docs/images/skin-lineup.png for the README.

Renders come from Art/Icons/render_readme_lineup.py:

    python tools/make-readme-lineup.py <render_dir>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "docs" / "images" / "skin-lineup.png"

SKINS = [("Default", "Olive"), ("Desert", "Desert Tan"), ("Arctic", "Arctic"), ("Army", "Army Green"),
         ("Night", "Night Stalker")]
CELL_W, CELL_H = 620, 440
LABEL_H = 56
MARGIN = 30
SKY_TOP = (118, 158, 196)
SKY_BOTTOM = (214, 226, 234)


def sky(width, height):
    image = Image.new("RGB", (width, height))
    draw = ImageDraw.Draw(image)
    for y in range(height):
        t = y / (height - 1)
        draw.line([(0, y), (width, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip(SKY_TOP, SKY_BOTTOM)))
    return image.convert("RGBA")


def fit(render, width, height):
    render = render.crop(render.getbbox())
    scale = min(width / render.width, height / render.height)
    return render.resize((round(render.width * scale), round(render.height * scale)), Image.LANCZOS)


def font(size):
    for name in ("segoeuib.ttf", "arialbd.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def main(render_dir):
    width = MARGIN * 2 + CELL_W * len(SKINS)
    height = MARGIN * 2 + CELL_H + LABEL_H
    canvas = sky(width, height)
    label_font = font(34)

    for i, (key, label) in enumerate(SKINS):
        render = fit(Image.open(Path(render_dir) / ("lineup_%s.png" % key)).convert("RGBA"), CELL_W - 30, CELL_H - 20)
        x = MARGIN + i * CELL_W + (CELL_W - render.width) // 2
        y = MARGIN + (CELL_H - render.height) // 2

        shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        shadow_alpha = render.getchannel("A").point(lambda a: a * 0.28)
        shadow.paste((20, 30, 40, 255), (x + 10, y + 24), shadow_alpha)
        canvas = Image.alpha_composite(canvas, shadow.filter(ImageFilter.GaussianBlur(14)))
        canvas.alpha_composite(render, (x, y))

        draw = ImageDraw.Draw(canvas)
        text_w = draw.textlength(label, font=label_font)
        draw.text((MARGIN + i * CELL_W + (CELL_W - text_w) / 2, MARGIN + CELL_H), label,
                  font=label_font, fill=(28, 36, 44))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(OUT, optimize=True)
    print("wrote", OUT.relative_to(ROOT))


main(sys.argv[1])
