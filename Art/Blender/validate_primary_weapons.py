"""Historical geometry comparison; see docs/development/WEAPON-MODELS.md for baseline setup."""
import bpy, json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'dist/weapon-review'
CHANGED = {'ChinBarrel', 'ChinGatling'}
ADDED = {'ChinGatlingHousing', 'ChinCannon'}


def capture(path):
    if path.suffix == '.blend':
        bpy.ops.wm.open_mainfile(filepath=str(path))
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(path))
    data = {}
    objects = bpy.data.collections['AH64'].objects if path.suffix == '.blend' else bpy.data.objects
    for o in objects:
        if o.type not in ('MESH', 'EMPTY'): continue
        data[o.name] = dict(matrix=[v for r in o.matrix_world for v in r],
                            parent=o.parent.name if o.parent else None,
                            material=[m.name for m in o.data.materials] if o.type == 'MESH' else [],
                            vertices=[list(o.matrix_world @ v.co) for v in o.data.vertices] if o.type == 'MESH' else [])
    return data


reports = {}
for kind, before, after in [
    ('blend', OUT/'baseline.blend', ROOT/'Art/Blender/AH64.blend'),
    ('fbx', OUT/'baseline.fbx', ROOT/'AH64UnityProject/Assets/AH64/Source/FBX/AH64.fbx')]:
    if not before.is_file():
        raise FileNotFoundError(f'Missing baseline {before}; see docs/development/WEAPON-MODELS.md')
    a, b = capture(before), capture(after)
    assert set(b) - set(a) == ADDED, (kind, 'unexpected added objects', set(b)-set(a))
    assert not set(a) - set(b), (kind, 'removed objects')
    for name, old in a.items():
        new = b[name]
        assert old['parent'] == new['parent'], (kind, name, 'changed parent')
        assert max(abs(x-y) for x,y in zip(old['matrix'], new['matrix'])) < 1e-5, (kind, name, 'changed transform')
        assert old['material'] == new['material'], (kind, name, 'changed material')
        if name not in CHANGED:
            assert len(old['vertices']) == len(new['vertices']), (kind, name, 'changed vertices')
            assert all(abs(x-y) < 1e-5 for p,q in zip(old['vertices'],new['vertices']) for x,y in zip(p,q)), (kind,name,'moved geometry')
    meshes = [o for o in bpy.data.objects if o.name in b and o.type == 'MESH']
    assert len(meshes) == 27
    assert all(len(o.data.materials)==1 and o.data.uv_layers for o in meshes)
    for name in ADDED:
        assert b[name]['parent'] == 'ChinBarrel'
    reports[kind] = dict(status='PASS', preserved_transforms=len(a), meshes=len(meshes), untouched_meshes=23,
                         changed_meshes=sorted(CHANGED), added_meshes=sorted(ADDED))
(OUT/'geometry-validation.json').write_text(json.dumps(reports, indent=2))
print(json.dumps(reports))
