"""Render the paint schemes for the README's "Choose your gunship" lineup.

Writes one transparent render per skin to the output folder; tools/make-readme-lineup.py
composites them onto a sky gradient as docs/images/skin-lineup.png.

    blender.exe -b Art/Blender/AH64.blend --python Art/Icons/render_readme_lineup.py -- <out_dir>
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_skin_icons as icons  # noqa: E402


def main(out):
    os.makedirs(out, exist_ok=True)
    scene = icons.setup()
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 760
    scene.render.resolution_percentage = 100
    scene.display.render_aa = "32"
    #studio lighting renders the airframe too dark to read against the README's daylight sky
    scene.view_settings.exposure = 0.7

    cam = scene.camera
    cam.data.lens = 60
    target = Vector((0.0, -0.4, 1.0))
    cam.location = target + Vector((0.95, -1.0, 0.38)).normalized() * 11.5
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()

    for name, colours in icons.PAINTS.items():
        icons.paint(*colours)
        scene.render.filepath = os.path.join(out, "lineup_%s.png" % name)
        bpy.ops.render.render(write_still=True)


main(sys.argv[sys.argv.index("--") + 1])
