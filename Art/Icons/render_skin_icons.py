"""Render skin and mastery icon portraits from Art/Blender/AH64.blend.

Writes transparent 512px renders to the output folder; tools/make-skin-icons.py
composites them into the bundle icons. Colours approximate each skin's in-game
paint (see AH64Skins.cs), not the Blender authoring materials.

    blender.exe -b Art/Blender/AH64.blend --python Art/Icons/render_skin_icons.py -- <out_dir>
"""
import math
import os
import sys

import bpy
from mathutils import Vector

# (body, mechanical, markings) in sRGB.
PAINTS = {
    "Default": ((0.33, 0.38, 0.20), (0.12, 0.14, 0.10), (0.50, 0.32, 0.12)),
    "Desert": ((0.66, 0.57, 0.43), (0.24, 0.22, 0.19), (0.10, 0.09, 0.08)),
    "Arctic": ((0.72, 0.75, 0.77), (0.22, 0.25, 0.28), (0.60, 0.28, 0.12)),
    # Slightly above the in-game albedo: at icon size dark green reads as black.
    "Army": ((0.31, 0.345, 0.25), (0.22, 0.235, 0.19), (0.07, 0.07, 0.065)),
    # Mastery. Lifted like Army: true black loses the airframe's shape.
    "Night": ((0.19, 0.20, 0.21), (0.13, 0.14, 0.15), (0.46, 0.10, 0.07)),
}
HIDDEN = {"RotorBlurMain", "RotorBlurTail", "ChinGatling", "ChinGatlingHousing", "ChinCannon"}


def linear(c):
    return tuple(v ** 2.2 for v in c) + (1.0,)


def paint(body, mechanical, markings):
    for m in bpy.data.materials:
        if m.name.startswith("matAH64Body"):
            m.diffuse_color = linear(body)
        elif m.name.startswith("matAH64Dark"):
            m.diffuse_color = linear(mechanical)
        elif m.name.startswith("matAH64Markings"):
            m.diffuse_color = linear(markings)
        elif m.use_nodes:
            bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
            if bsdf:
                c = bsdf.inputs["Base Color"].default_value
                m.diffuse_color = (c[0], c[1], c[2], 1.0)


def setup():
    scene = bpy.context.scene
    for o in scene.objects:
        o.hide_render = o.name in HIDDEN or o.type not in {"MESH", "EMPTY"} or o.name == "Cube"
    scene.render.engine = "BLENDER_WORKBENCH"
    sh = scene.display.shading
    sh.light = "STUDIO"
    sh.color_type = "MATERIAL"
    sh.show_cavity = True
    sh.cavity_type = "BOTH"
    sh.show_object_outline = False
    sh.show_specular_highlight = True
    scene.render.film_transparent = True
    scene.render.resolution_x = scene.render.resolution_y = 512
    scene.view_settings.view_transform = "Standard"

    cam = bpy.data.objects.new("IconCam", bpy.data.cameras.new("IconCam"))
    cam.data.lens = 70
    scene.collection.objects.link(cam)
    scene.camera = cam
    # Baked for Unity, the nose faces Blender -Y. Front three-quarter from slightly above.
    target = Vector((0.0, -0.3, 1.1))
    cam.location = target + Vector((0.95, -1.0, 0.42)).normalized() * 10.4
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    return scene


def main(out):
    os.makedirs(out, exist_ok=True)
    scene = setup()
    for name, colours in PAINTS.items():
        paint(*colours)
        scene.render.filepath = os.path.join(out, "skin_%s.png" % name)
        bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1])
