"""Reproducible geometry previews; never saves preview overrides into source blend."""
import bpy
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'dist/weapon-review'
OUT.mkdir(parents=True, exist_ok=True)
col = bpy.data.collections['AH64']
weapons = {'m230': {'ChinBarrel'}, 'gatling': {'ChinGatling', 'ChinGatlingHousing'}, 'cannon': {'ChinCannon'}}
all_guns = set.union(*weapons.values())
for m in bpy.data.materials:
    bsdf = m.node_tree.nodes.get('Principled BSDF') if m.node_tree else None
    if bsdf: m.diffuse_color = bsdf.inputs['Base Color'].default_value
s = bpy.context.scene
s.render.engine = 'BLENDER_WORKBENCH'
s.display.shading.light = 'STUDIO'
s.display.shading.studiolight_rotate_z = .5
s.display.shading.color_type = 'MATERIAL'
s.display.shading.show_shadows = True
s.display.shading.show_cavity = True
s.display.shading.cavity_type = 'BOTH'
s.display.shading.background_type = 'WORLD'
s.world.color = (.10, .12, .15)
s.render.resolution_x = 1200
s.render.resolution_y = 900
s.render.resolution_percentage = 100
camdata = bpy.data.cameras.new('WeaponReviewCamera')
cam = bpy.data.objects.new('WeaponReviewCamera', camdata)
s.collection.objects.link(cam)
s.camera = cam
camdata.type = 'ORTHO'
for weapon, names in weapons.items():
    for o in bpy.data.objects:
        o.hide_render = o.name not in col.objects or o.name.startswith('RotorBlur') or (o.name in all_guns and o.name not in names)
    for angle, pos, target, scale in [
        ('close', (2.8, -5.6, 1.4), (0, -2.25, .56), 2.35),
        ('aircraft', (5, -8, 4), (0, 0, 1.2), 7.1),
        ('side', (5, -2.7, .9), (0, -2.35, .60), 2.2)]:
        cam.location = pos
        cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        camdata.ortho_scale = scale
        s.render.filepath = str(OUT / f'{weapon}-{angle}.png')
        bpy.ops.render.render(write_still=True)
