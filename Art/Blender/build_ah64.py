"""
AH-64 airframe generator - the source of truth for Art/Blender/AH64.blend.

The model is fully procedural: every dimension lives in the CONFIG block below,
and running this script rebuilds the whole airframe from scratch. Edit a number,
re-run, done. Do NOT hand-edit geometry in the .blend and expect it to survive -
the next run of this script discards everything in the AH64 collection.

    Inside Blender (Scripting workspace, or via the MCP bridge):
        exec(compile(open(PATH).read(), PATH, "exec"))

    Headless:
        "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" ^
            --background --python Art\\Blender\\build_ah64.py

Conventions, fixed - see AH64_HANDOFF.md before changing any of them:
  * Authoring is nose +Y, up +Z. Every CONFIG table, comment and validate()
    check reads that way, so keep authoring in it.
  * BUT Blender's FBX export maps Blender (x,y,z) onto Unity (-x, z, -y) - a
    180 deg yaw on top of the Z-up -> Y-up swap - so a nose-+Y model arrives in
    Unity facing -Z, i.e. BACKWARDS. This was measured, and no combination of
    axis_forward / axis_up / bake_space_transform changes it; all four were
    tested against a live import. An earlier version of this file claimed the
    export "needs no fixup" - that was wrong.
    bake_unity_orientation() therefore yaws the airframe 180 deg and bakes it
    into mesh data, so the exported FBX faces Unity +Z for real.
  * Origin (0,0,0) sits at ground contact under the main gear. The RoR2 hover
    probe and modelBasePosition both measure from the model's feet.
  * Every object ends with scale 1,1,1. Non-unit scale on an FBX is a lasting
    source of wrong-sized colliders and skewed child transforms.

Anything that must stay attached OVERLAPS its neighbour rather than abutting it.
Three separate parts (front wheels, chin turret, canopy) shipped detached because
they were placed flush against a hull surface that was then retapered. validate()
below asserts those clearances so the failure is loud instead of visual.

ONE RENDERER = ONE MATERIAL. This is not a style preference, it is forced by
RoR2. CharacterModel.RendererInfo carries a single `defaultMaterial`, and
CharacterModel.UpdateRendererMaterials does:

    renderer.sharedMaterials = array;   // [defaultMaterial, ...up to 6 overlays]

every time materials go dirty. Slot 0 is the base material and slots 1+ are
overlays (on fire, elite, cloaked). A renderer with several submeshes therefore
never receives a second base material - its extra submeshes get handed overlay
materials or nothing. So an object carrying two materials in Blender renders
wrong in game, and nothing in the log explains it.

That is why the static airframe is merged into exactly TWO objects rather than
one: `Airframe` (body) and `AirframeDark` (dark). `Canopy` is a third only
because it is glass. check_single_material() asserts the invariant on every
build, because it is invisible in Blender - the viewport shows the two-tone
version you intended.

Object count is also renderer count, and every renderer needs a matching
ChildLocator entry plus a customRendererInfos entry on the Unity side, so these
names are expensive to change once the assetbundle exists. Current set, 17:

    Airframe  AirframeDark  Canopy
    AirframeMarkings
    NoseOptics
    MainRotor  TailRotor  ChinTurret  ChinBarrel  ChinGatling
    PodRocketL  PodRocketR  PodMissileL  PodMissileR
    RadarDome
    RotorBlurMain  RotorBlurTail

ChinBarrel is a separate mesh so yaw (housing) and pitch (barrel) articulate
independently. It is parented under ChinTurret *after* bake_unity_orientation
(that bake forbids any parenting).

ChinGatling is the alternate primary's spinning barrel cluster, parented one level
deeper under ChinBarrel so it inherits pitch. It ships hidden; the loadout turns
the ChinBarrel *renderer* off and the ChinGatling renderer on. Toggle renderers,
never SetActive on ChinBarrel - that would hide the gatling with it, and take the
Muzzle anchor down too.

RotorBlurMain/RotorBlurTail are FX geometry, not airframe: they get ChildLocator
entries but deliberately NO customRendererInfos entry on the mod side, so
CharacterModel never manages their (transparent) material - no hopoo conversion,
no elite overlays. AH64Phase4Builder ships them deactivated in the prefab and
AH64FlightVisuals activates and fades them with rotor effort at runtime.
"""

import bpy
import bmesh
import math
import os
from mathutils import Matrix, Vector

# ---------------------------------------------------------------- CONFIG ----

BLEND_PATH = r"C:\Users\stuwj\Documents\Coding\ror2-chopper\Art\Blender\AH64.blend"
FBX_PATH = r"C:\Users\stuwj\Documents\Coding\ror2-chopper\AH64UnityProject\Assets\AH64\Source\FBX\AH64.fbx"
COLLECTION = "AH64"
SAVE_ON_RUN = True
EXPORT_FBX_ON_RUN = True   # headless __main__ runs export_fbx() after build()
UNWRAP_UVS = True          # RoR2's hopoo shaders sample textures; meshes need UVs

# Cross-sections are (y, half_width, z_bottom, z_top) and run front to back.
# The nose taper is what makes this read as an Apache rather than a bus.
FUSELAGE = [
    (2.14, 0.11, 0.82, 0.93),   # needle cap — strong Apache nose at icon distance
    (2.00, 0.18, 0.71, 0.99),
    (1.60, 0.27, 0.64, 1.10),
    (1.20, 0.36, 0.57, 1.20),
    (0.60, 0.43, 0.50, 1.32),
    (0.00, 0.45, 0.46, 1.44),
    (-0.30, 0.45, 0.45, 1.56),
    (-1.20, 0.33, 0.62, 1.44),
]

# The fuselage top is deliberately LOW across the cockpit and the canopy supplies
# that height back. It is not a box sitting on the hull - that version detached
# every time the nose was retapered.
CANOPY = [
    (1.95, 0.17, 0.96, 1.02),   # buried under the nose deck
    (1.55, 0.24, 1.06, 1.30),   # steeper windscreen facet (icon read)
    (1.15, 0.32, 1.16, 1.46),   # gunner
    (0.75, 0.38, 1.24, 1.54),
    (0.55, 0.40, 1.28, 1.70),   # step up to the pilot
    (0.05, 0.41, 1.40, 1.72),
    (-0.25, 0.42, 1.47, 1.55),  # fairs back under the hull skin
]

MAST_HOUSING = [
    (0.00, 0.23, 1.50, 1.86),
    (-0.40, 0.26, 1.44, 1.92),
    (-0.85, 0.21, 1.46, 1.82),
]

# Slimmed 2026-08-03. The boom carried too much depth too far aft - 0.56 deep at
# the root falling only to 0.20 at the tail - which read as a fat tube rather than
# the hard taper an Apache boom has. Depth now falls 0.50 -> 0.16 and the section
# narrows with it, so the tail rotor sits on something that looks like it tapered
# to get there. Kept the same station positions so nothing hung off the boom moves.
TAIL_BOOM = [
    (-1.10, 0.260, 0.860, 1.360),
    (-1.70, 0.193, 0.995, 1.315),
    (-2.35, 0.148, 1.045, 1.265),
    (-2.95, 0.120, 1.080, 1.240),
]

# Vertical fin: (z, half_width, y_leading, y_trailing). Leading edge sweeps aft
# with height, which is most of what stops the tail reading as a slab.
# Root thickened 0.075 -> 0.115 and the tip thinned 0.045 -> 0.034 (2026-08-03).
# At the old near-constant thickness the fin read as a flat slab of cardboard
# planted on the boom; a real fin is a wing section, fat where it carries load and
# thin at the tip. The taper is what stops it looking like a cutout.
TAIL_FIN = [
    (1.05, 0.115, -2.30, -3.10),
    (1.60, 0.092, -2.50, -3.14),
    (2.10, 0.062, -2.72, -3.16),
    (2.35, 0.034, -2.86, -3.14),
]

# Spanwise surfaces: (x, y_leading, y_trailing, z_centre, half_thickness)
STABILATOR = [
    (-0.88, -2.56, -2.88, 1.15, 0.035),
    (-0.55, -2.49, -2.92, 1.15, 0.050),
    (0.00, -2.45, -2.95, 1.15, 0.055),
    (0.55, -2.49, -2.92, 1.15, 0.050),
    (0.88, -2.56, -2.88, 1.15, 0.035),
]
# Stub wings. Anhedral (droop) is the single strongest Apache silhouette cue and
# the wing read as a flat plank without it - every section used to sit at z=1.00.
#
# EVERYTHING HUNG ON THE WING MUST USE wing_z(). Pylons, pods, braces, station
# plates and marking bands are all placed at absolute coordinates, NOT parented to
# the wing surface, so a droop applied here alone leaves them floating in mid-air.
# wing_z() is the single place that relationship is expressed; validate() asserts it.
WING_ROOT_Z = 1.00
WING_ANHEDRAL = 0.110      # z drop per unit |x|, about 6.3 deg over the half-span
WING_HALF_SPAN = 1.45


def _wing_sec(x, y_lead, y_trail, hz):
    """One wing section, with the anhedral applied so the table cannot drift."""
    return (x, y_lead, y_trail, WING_ROOT_Z - WING_ANHEDRAL * abs(x), hz)


# Taper is stronger than it was (tip chord 0.48 against a 0.82 root, was 0.58/0.78)
# and the leading edge now sweeps aft, so the planform reads as a wing rather than
# a slab. Root is also slightly thicker to sell it carrying the stores.
WING = [
    _wing_sec(-1.45, 0.14, -0.34, 0.048),
    _wing_sec(-0.90, 0.24, -0.44, 0.082),
    _wing_sec(0.00, 0.30, -0.52, 0.100),
    _wing_sec(0.90, 0.24, -0.44, 0.082),
    _wing_sec(1.45, 0.14, -0.34, 0.048),
]


def wing_z(x, z):
    """Height z carried down onto the drooped wing surface at spanwise position x."""
    return z - WING_ANHEDRAL * abs(x)

# Blade sections: (span, chord_offset_x, half_chord, half_thickness).
# The swept, thinned tip is what stops blades reading as flat sticks.
MAIN_BLADE = [
    (0.34, 0.00, 0.060, 0.032),
    (0.62, 0.00, 0.130, 0.034),
    (2.30, 0.00, 0.130, 0.030),
    (2.72, -0.04, 0.118, 0.024),
    (3.00, -0.12, 0.078, 0.015),
]
TAIL_BLADE = [
    (0.10, 0.00, 0.035, 0.022),
    (0.18, 0.00, 0.062, 0.022),
    (0.48, 0.00, 0.058, 0.018),
    (0.56, -0.02, 0.035, 0.012),
]

MAIN_ROTOR_CENTRE = (0.0, 0.0, 2.15)
# Longbow FCR radome — a shallow mast-mounted lenticular dome, not a ball. These
# dimensions preserve the tuned live-scene silhouette at third-person distance.
# Own renderer/material so CharacterModel never hands it overlays from another part.
RADAR_DOME_SIZE = (0.70, 0.71, 0.35)
RADAR_DOME_CENTRE = (0.0, 0.0, 2.35)
# Low dark collar overlaps the radome underside and rotor hub. It is folded into
# AirframeDark, so the extra shape does not create another runtime renderer.
RADAR_COLLAR_R = 0.20
RADAR_COLLAR_DEPTH = 0.12
RADAR_COLLAR_CENTRE = (0.0, 0.0, 2.18)
# Hub sits just outside the fin face (fin half-width ~0.07 + hub radius 0.09).
# 0.28 left a visible air gap that read as "floating off the tail".
TAIL_ROTOR_CENTRE = (0.16, -2.88, 1.75)

# Rotor blur discs - thin cylinders the runtime fades in with rotor effort.
# Radii match the blade tip spans so the blur reads as the blades' own disc.
ROTOR_BLUR_MAIN_R = 3.00   # MAIN_BLADE tip span (~1.35x prior 2.20 for real-chopper overhang)
ROTOR_BLUR_MAIN_Z = 2.16   # a hair above the blade mid-plane (blades are z 2.15 +/- 0.03)
ROTOR_BLUR_TAIL_R = 0.58   # TAIL_BLADE tip span
ROTOR_BLUR_TAIL_X = 0.17   # match hub face just outboard of the fin

ENGINE_X = 0.58
ENGINE_Y = -0.60
ENGINE_Z = 1.45

TURRET_Y = 1.70
TURRET_PIVOT_Z = 0.66      # yaw pivot; hull underside here is ~0.66
HOUSING_Z = 0.52           # turret ball centre height
HOUSING_DEPTH = 0.40
# Pitch pivot sits just forward of the ball so the long barrel reads as articulated.
PITCH_Y = TURRET_Y + 0.20
PITCH_Z = HOUSING_Z
BARREL_LEN = 1.30          # long enough to read at third-person camera distance

# Gatling primary variant (0.2.0). A separate barrel assembly, NOT a reskin of the
# M230 - the whole point is that it visibly spins, so it needs its own renderer with
# an origin sitting on the bore axis. Deliberately shorter and fatter than the M230
# tube so the two read as different weapons in the loadout preview.
GATLING_BARRELS = 6
GATLING_RING_R = 0.058     # bore-axis offset of each tube; sets the visible spin radius
GATLING_TUBE_R = 0.021
GATLING_LEN = 1.00
GATLING_ROOT_Y = PITCH_Y + 0.20   # where the cluster starts, clear of the breech

# Gear. The strut is ANGLED: its foot is outboard at the wheel and its head is
# inboard and buried in the hull. A vertical strut at the wheel's track sits
# outside the hull half-width entirely and touches nothing.
GEAR_WHEEL_X = 0.50
GEAR_Y = -0.30
GEAR_HEAD = (0.26, 0.72)   # (x, z) inboard/high - must land inside the hull
GEAR_FOOT = (0.50, 0.10)   # (x, z) outboard/low  - inside the wheel
GEAR_WHEEL_R = 0.19
GEAR_WHEEL_W = 0.20
GEAR_STRUT_W = 0.09        # narrower than the wheel, so head-on the tyre reads

TAILGEAR_Y = -2.42
TAILGEAR_TOP = 1.10        # boom underside here is ~1.02
TAILGEAR_BOTTOM = 0.72
TAILGEAR_WHEEL_R = 0.12

POD_ROCKET_X = 1.28
POD_MISSILE_X = 0.78

# ChildLocator anchors. The ChildLocator component itself is Unity-side, but the
# transforms it points at are authored here as empties so their positions live
# with the geometry and survive a re-export.
#   name: (position, parent_object_or_None)
ANCHORS = {
    "Chest":         ((0.00, 0.00, 1.10), None),   # all 184 inherited item rules use this
    "Head":          ((0.00, 1.10, 1.50), None),
    "NoseTip":       ((0.00, 2.14, 0.87), None),
    # Wing anchors and the store muzzles ride the drooped surface — leaving them at
    # the old flat heights fires rockets and missiles out of empty air above the pods.
    "WingL":         ((-1.45, -0.10, wing_z(1.45, 1.00)), None),
    "WingR":         ((1.45, -0.10, wing_z(1.45, 1.00)), None),
    "TailTip":       ((0.00, -2.95, 1.16), None),
    "MainHurtbox":   ((0.00, 0.00, 1.05), None),
    "HeadHurtbox":   ((0.00, 1.10, 1.40), None),
    "AimOrigin":     ((0.00, 0.80, 1.45), None),
    # parented to the barrel so muzzle follows yaw + pitch
    "Muzzle":        ((0.00, PITCH_Y + BARREL_LEN, PITCH_Z), "ChinBarrel"),
    "MuzzleRocketL": ((-1.28, 0.37, wing_z(POD_ROCKET_X, 0.55)), None),
    "MuzzleRocketR": ((1.28, 0.37, wing_z(POD_ROCKET_X, 0.55)), None),
    "MuzzleMissileL": ((-0.78, 0.42, wing_z(POD_MISSILE_X, 0.61)), None),
    "MuzzleMissileR": ((0.78, 0.42, wing_z(POD_MISSILE_X, 0.61)), None),
    # RadarDome is a MESH renderer (see build_radar_dome), not an empty — ChildLocator
    # resolves the mesh transform for the scan-pulse origin.
}

# One material slot per renderer, but materials may be shared by symmetric parts.
# Category names retain the four runtime-polish prefixes
# (matAH64Body/Dark/Glass/Radar), so existing HGStandard setup keeps working.
# Never request bare "matAH64": CreateHopooMaterialFromBundle uses Contains.
MATERIALS = {
    # Readable olive-drab, weathered charcoal mechanisms, and cool smoked glass.
    # Keep the contrast restrained: RoR2 stage lighting needs a legible silhouette.
    "matAH64BodySurface":   ((0.280, 0.340, 0.190), 0.58, 0.00),
    "matAH64DarkStatic":    ((0.090, 0.110, 0.075), 0.56, 0.00),
    "matAH64DarkRotor":     ((0.050, 0.060, 0.045), 0.74, 0.00),
    "matAH64DarkGun":       ((0.120, 0.135, 0.100), 0.30, 0.28),
    "matAH64DarkGatling":   ((0.070, 0.078, 0.062), 0.34, 0.24),
    "matAH64DarkRocketPod": ((0.110, 0.140, 0.065), 0.46, 0.05),
    "matAH64DarkHellfire":  ((0.075, 0.085, 0.060), 0.46, 0.10),
    #Declutter pass: was (0.055, 0.140, 0.185) - a bright cyan that read as a
    #cartoon windscreen. Real Apache glass is near-black olive with a cool sky
    #reflection, so this is darker and pulled well off pure cyan.
    "matAH64GlassCanopy":   ((0.030, 0.070, 0.090), 0.10, 0.00),
    # Fictional low-visibility ochre markings: no unit insignia or real serials.
    #Declutter pass: was (0.480, 0.300, 0.070) - safety orange. Muted toward a dull
    #rust so the one remaining band reads as a stencil rather than a hazard stripe.
    "matAH64Markings":      ((0.300, 0.185, 0.070), 0.48, 0.00),
    "matAH64Optics":        ((0.012, 0.070, 0.110), 0.12, 0.08),
    # Dark grey-teal radome - muted; runtime polish owns any residual glow.
    "matAH64RadarDome":     ((0.055, 0.075, 0.085), 0.35, 0.05),
    # Placeholder only: Unity swaps the blur discs onto a transparent material.
    "matAH64RotorBlur":     ((0.060, 0.065, 0.070), 0.90, 0.00),
}

# Material roles are a build contract, just like renderer names. Keeping these
# exact means a future rebuild cannot silently collapse the texture categories.
EXPECTED_RENDERER_MATERIALS = {
    "Airframe": "matAH64BodySurface",
    "AirframeDark": "matAH64DarkStatic",
    "Canopy": "matAH64GlassCanopy",
    "AirframeMarkings": "matAH64Markings",
    "NoseOptics": "matAH64Optics",
    "MainRotor": "matAH64DarkRotor",
    "TailRotor": "matAH64DarkRotor",
    "ChinTurret": "matAH64DarkGun",
    "ChinBarrel": "matAH64DarkGun",
    #Own material, not matAH64DarkGun: the cluster is a much larger unbroken metal mass
    #than the M230's thin tube, so the same albedo reads noticeably lighter on it.
    "ChinGatling": "matAH64DarkGatling",
    "PodRocketL": "matAH64DarkRocketPod",
    "PodRocketR": "matAH64DarkRocketPod",
    "PodMissileL": "matAH64DarkHellfire",
    "PodMissileR": "matAH64DarkHellfire",
    "MissileL0": "matAH64DarkHellfire",
    "MissileL1": "matAH64DarkHellfire",
    "MissileL2": "matAH64DarkHellfire",
    "MissileL3": "matAH64DarkHellfire",
    "MissileR0": "matAH64DarkHellfire",
    "MissileR1": "matAH64DarkHellfire",
    "MissileR2": "matAH64DarkHellfire",
    "MissileR3": "matAH64DarkHellfire",
    "RadarDome": "matAH64RadarDome",
    "RotorBlurMain": "matAH64RotorBlur",
    "RotorBlurTail": "matAH64RotorBlur",
}

RX90 = (math.radians(90), 0, 0)
RY90 = (0, math.radians(90), 0)
RXN90 = (math.radians(-90), 0, 0)


# --------------------------------------------------------------- HELPERS ----

def scene():
    return bpy.context.scene


def get_collection():
    col = bpy.data.collections.get(COLLECTION)
    if col is None:
        col = bpy.data.collections.new(COLLECTION)
        scene().collection.children.link(col)
    for lc in bpy.context.view_layer.layer_collection.children:
        if lc.collection == col:
            bpy.context.view_layer.active_layer_collection = lc
    return col


def ensure_materials():
    out = {}
    for name, (rgb, rough, metal) in MATERIALS.items():
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
            bsdf.inputs["Roughness"].default_value = rough
            bsdf.inputs["Metallic"].default_value = metal
        out[name] = m
    return out


def deselect():
    bpy.ops.object.select_all(action="DESELECT")


def put_mat(o, m):
    o.data.materials.clear()
    o.data.materials.append(m)


def add_cube(col, name, dims, loc=(0, 0, 0), rot=(0, 0, 0), m=None):
    bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, 0))
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    o.dimensions = dims                 # set while rotation is still zero
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.rotation_euler = rot
    o.location = loc
    if m:
        put_mat(o, m)
    return o


def add_cyl(col, name, r, d, loc=(0, 0, 0), rot=(0, 0, 0), verts=12, m=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=d,
                                        location=(0, 0, 0))
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    o.rotation_euler = rot              # built at true radius/depth, so scale stays 1
    o.location = loc
    if m:
        put_mat(o, m)
    return o


def add_cone(col, name, r, d, loc=(0, 0, 0), rot=(0, 0, 0), verts=8, m=None):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=0.0,
                                    depth=d, location=(0, 0, 0))
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    o.rotation_euler = rot
    o.location = loc
    if m:
        put_mat(o, m)
    return o


def add_sphere(col, name, r, loc=(0, 0, 0), segments=16, rings=10, m=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings,
                                        radius=r, location=(0, 0, 0))
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    o.location = loc
    if m:
        put_mat(o, m)
    return o


def strut_between(col, name, p0, p1, w, m=None):
    """Box spanning two points. Derives its own angle, so moving an endpoint in
    CONFIG cannot leave a stale rotation behind."""
    p0, p1 = Vector(p0), Vector(p1)
    d = p1 - p0
    o = add_cube(col, name, (w, w, d.length))
    o.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    o.location = (p0 + p1) / 2.0
    if m:
        put_mat(o, m)
    return o


def join_as(name, objs, origin=None):
    deselect()
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    j = bpy.context.active_object
    j.name = name
    j.data.name = name
    if origin is not None:
        scene().cursor.location = Vector(origin)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        scene().cursor.location = (0.0, 0.0, 0.0)
    return j


def loft(col, name, rings, m=None):
    """Bridge a run of 4-point cross-sections. Every tapered part uses this - a
    swept fin or a tapering boom is not expressible as a scaled cube."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    vr = [[bm.verts.new(p) for p in r] for r in rings]
    for a, b in zip(vr, vr[1:]):
        for j in range(4):
            k = (j + 1) % 4
            bm.faces.new((a[j], a[k], b[k], b[j]))
    bm.faces.new(vr[0])
    bm.faces.new(tuple(reversed(vr[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    col.objects.link(o)
    if m:
        put_mat(o, m)
    return o


def sec_y(y, hw, z0, z1):
    return [(-hw, y, z0), (hw, y, z0), (hw, y, z1), (-hw, y, z1)]


def side_sec_y(sx, y, x_inner, x_outer, z0, z1):
    """One four-point cross-section for an Apache-style side sponson."""
    inner, outer = sx * x_inner, sx * x_outer
    if sx < 0:
        inner, outer = outer, inner
    return [(inner, y, z0), (outer, y, z0),
            (outer, y, z1), (inner, y, z1)]


def sec_z(z, hw, y_lead, y_trail):
    return [(-hw, y_trail, z), (hw, y_trail, z), (hw, y_lead, z), (-hw, y_lead, z)]


def sec_x(x, y_lead, y_trail, zc, hz):
    return [(x, y_trail, zc - hz), (x, y_lead, zc - hz),
            (x, y_lead, zc + hz), (x, y_trail, zc + hz)]


def blade_rings(sections):
    out = []
    for (y, xc, ch, th) in sections:
        out.append([(xc - ch, y, -th), (xc + ch, y, -th),
                    (xc + ch, y, th), (xc - ch, y, th)])
    return out


def hull_top(y):
    """Top of the fuselage at station y - used to guarantee the canopy is seated."""
    for (ya, _, _, za), (yb, _, _, zb) in zip(FUSELAGE, FUSELAGE[1:]):
        if yb <= y <= ya:
            t = (y - yb) / (ya - yb)
            return zb + t * (za - zb)
    return FUSELAGE[-1][3]


def hull_half_width(y):
    for (ya, wa, _, _), (yb, wb, _, _) in zip(FUSELAGE, FUSELAGE[1:]):
        if yb <= y <= ya:
            t = (y - yb) / (ya - yb)
            return wb + t * (wa - wb)
    return FUSELAGE[-1][1]


# ----------------------------------------------------------------- BUILD ----

def clear(col):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    for o in list(col.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)


def build_airframe(col, M):
    """Static airframe. Returns the loose dark-material parts rather than joining
    them, so build() can merge them with the landing gear - which is also dark,
    also static, and so belongs in the same renderer.

    Nothing here moves at runtime, so the only reason to split it at all is
    material. See the one-renderer-one-material note at the top of this file.
    """
    body = M["matAH64BodySurface"]
    dark = M["matAH64DarkStatic"]
    gun = M["matAH64DarkGun"]
    gatling_mat = M["matAH64DarkGatling"]
    glass = M["matAH64GlassCanopy"]

    main = [
        loft(col, "Fuselage", [sec_y(*s) for s in FUSELAGE], m=body),
        loft(col, "MastHousing", [sec_y(*s) for s in MAST_HOUSING], m=body),
        loft(col, "TailBoom", [sec_y(*s) for s in TAIL_BOOM], m=body),
        loft(col, "TailFin", [sec_z(*s) for s in TAIL_FIN], m=body),
        loft(col, "Stabilator", [sec_x(*s) for s in STABILATOR], m=body),
        loft(col, "Wing", [sec_x(*s) for s in WING], m=body),
    ]
    join_as("Airframe", main, origin=(0, 0, 0))

    # Glass, so it cannot share a renderer with the hull however static it is.
    # Merging it into Airframe renders the canopy as opaque olive.
    loft(col, "Canopy", [sec_y(*s) for s in CANOPY], m=glass)

    # The cockpit needs a strong Apache read even at survivor-camera distance.
    # These frames are folded into AirframeDark below, rather than becoming
    # separate renderers, so the existing RoR2 renderer contract stays intact.
    # Declutter pass 2026-08-03: the canopy was carrying two full-length side rails
    # and two bow frames. At survivor-camera distance that reads as black strips
    # laid across the glass rather than as structure, and it was the single busiest
    # area on the aircraft.
    #
    # Down to ONE divider, on the step between the gunner's front seat and the
    # pilot's raised rear seat. That is both the cleanest read and the honest one:
    # the tandem step is the Apache's most recognisable profile cue, so the one
    # frame worth keeping is the one that marks it. Side rails are gone entirely -
    # the canopy silhouette already has hard facet edges doing that job.
    trim = [
        add_cube(col, "CanopySeatDivider", (0.395 * 2.0, 0.042, 0.045),
                 (0.0, 0.52, 1.67), m=dark),
    ]

    # Faceted M-TADS/PNVS-inspired nose sensor.  The low-profile optical ball
    # and guard read as an Apache cue without adding a new texture/render role.
    trim += [
        add_sphere(col, "NoseSensorBall", 0.155, (0.0, 1.68, 0.74),
                   segments=10, rings=6, m=dark),
        add_cyl(col, "NoseSensorGuard", 0.185, 0.085, (0.0, 1.80, 0.74),
                RX90, 10, dark),
        add_cube(col, "NoseSensorMount", (0.20, 0.16, 0.13),
                 (0.0, 1.54, 0.77), m=dark),
    ]

    # Angular side sponsons give the otherwise clean fuselage its characteristic
    # Apache shoulder line and provide a stronger visual root for the stub wings.
    for sx in (-1, 1):
        s = "L" if sx < 0 else "R"
        trim.append(loft(col, "Sponson" + s, [
            side_sec_y(sx, 0.92, 0.35, 0.50, 0.69, 1.03),
            side_sec_y(sx, 0.30, 0.43, 0.62, 0.62, 1.10),
            side_sec_y(sx, -0.42, 0.40, 0.57, 0.68, 1.05),
            side_sec_y(sx, -0.82, 0.33, 0.44, 0.78, 0.98),
        ], m=dark))
        # Declutter pass 2026-08-03: AvionicsHatch, AccessPanel and TailAccess were
        # removed. Three small dark rectangles per side, each 0.02 deep, they were
        # intended as panel hierarchy but at survivor-camera distance they only ever
        # resolved as speckle on the flanks - the "random bits" of the busy read.
        #
        # Panel hierarchy on a smooth hull wants either a texture or nothing at this
        # scale; scattered proxy geometry lands in between and reads as neither.
        # The structural dark parts that define silhouette (engine cowls, intake
        # splitters, wing-root fairings, pylons, nose sensor) all stay.
        pass

    # Chin turret: housing yaws, barrel pitches — two renderers, same dark material.
    # ChinBarrel stays unparented until after bake_unity_orientation (that bake
    # forbids parenting). parent_chin_barrel() wires the hierarchy later.
    join_as("ChinTurret", [
        add_cyl(col, "TurretBall", 0.22, HOUSING_DEPTH, (0.0, TURRET_Y, HOUSING_Z),
                verts=14, m=gun),
        add_cyl(col, "TurretCollar", 0.14, 0.16, (0.0, TURRET_Y + 0.18, HOUSING_Z),
                RX90, 10, m=gun),
    ], origin=(0.0, TURRET_Y, TURRET_PIVOT_Z))

    join_as("ChinBarrel", [
        add_cyl(col, "BarrelBreech", 0.085, 0.22,
                (0.0, PITCH_Y + 0.11, PITCH_Z), RX90, 10, m=gun),
        add_cyl(col, "BarrelTube", 0.05, BARREL_LEN,
                (0.0, PITCH_Y + BARREL_LEN * 0.5, PITCH_Z), RX90, 10, m=gun),
        add_cyl(col, "BarrelMuzzle", 0.065, 0.08,
                (0.0, PITCH_Y + BARREL_LEN - 0.04, PITCH_Z), RX90, 8, m=gun),
    ], origin=(0.0, PITCH_Y, PITCH_Z))

    # Gatling barrel cluster — alternate primary, hidden by default in the prefab.
    # Origin is put on the bore axis at the pitch pivot, the SAME origin ChinBarrel
    # uses, so AH64GatlingSpin can rotate this transform about the bore without the
    # cluster wobbling off-centre. Do not let join_as pick the origin here.
    gatling = [
        add_cyl(col, "GatlingRotorHousing", 0.105, 0.28,
                (0.0, PITCH_Y + 0.12, PITCH_Z), RX90, 12, m=gatling_mat),
    ]
    for i in range(GATLING_BARRELS):
        a = math.radians(i * (360.0 / GATLING_BARRELS))
        ox = GATLING_RING_R * math.cos(a)
        oz = GATLING_RING_R * math.sin(a)
        gatling.append(add_cyl(col, "GatlingTube%d" % i, GATLING_TUBE_R, GATLING_LEN,
                               (ox, GATLING_ROOT_Y + GATLING_LEN * 0.5, PITCH_Z + oz),
                               RX90, 6, m=gatling_mat))
    # Front clamp ring ties the tubes together — without it the cluster reads as
    # loose sticks once it is spinning.
    gatling.append(add_cyl(col, "GatlingClamp", 0.082, 0.055,
                           (0.0, GATLING_ROOT_Y + GATLING_LEN - 0.10, PITCH_Z),
                           RX90, 12, m=gatling_mat))
    join_as("ChinGatling", gatling, origin=(0.0, PITCH_Y, PITCH_Z))

    for sx in (-1, 1):
        s = "L" if sx < 0 else "R"
        X = sx * ENGINE_X
        trim += [
            add_cyl(col, "Nacelle" + s, 0.26, 1.00, (X, ENGINE_Y, ENGINE_Z), RX90, 14, dark),
            # Root fairing blending the nacelle into the fuselage shoulder
            # (2026-08-03). The nacelle centre sits outboard of the hull half-width
            # and near its top, so without this it read as a tin can bolted to the
            # side. The wedge fills the inboard gap and gives the engine a root, the
            # same problem the fin had before it got one.
            add_cube(col, "EngineFairing" + s, (0.30, 0.86, 0.30),
                     (sx * (ENGINE_X - 0.145), ENGINE_Y + 0.02, ENGINE_Z - 0.105),
                     m=dark),
            add_cyl(col, "Intake" + s, 0.21, 0.14, (X, -0.04, ENGINE_Z), RX90, 12, dark),
            add_cyl(col, "Nozzle" + s, 0.19, 0.22, (X, -1.18, ENGINE_Z), RX90, 12, dark),
            # Small lips make the nacelles read as turbines instead of plain tubes.
            add_cyl(col, "IntakeLip" + s, 0.235, 0.045, (X, 0.045, ENGINE_Z), RX90, 12, dark),
            add_cyl(col, "NozzleLip" + s, 0.215, 0.055, (X, -1.305, ENGINE_Z), RX90, 12, dark),
            # Apache nacelles are not just round tubes: a raised cowl, an intake
            # splitter and a heat-dark exhaust shroud give them a deliberate
            # turbine silhouette from the normal third-person camera.
            add_cube(col, "EngineCowl" + s, (0.26, 0.48, 0.105),
                     (X, -0.50, ENGINE_Z + 0.225), m=dark),
            add_cyl(col, "EngineVent" + s, 0.055, 0.055,
                    (X, -0.48, ENGINE_Z + 0.335), verts=10, m=dark),
            add_cube(col, "IntakeSplitter" + s, (0.038, 0.060, 0.165),
                     (X, 0.125, ENGINE_Z), m=dark),
            add_cyl(col, "ExhaustShroud" + s, 0.232, 0.115,
                    (X, -1.36, ENGINE_Z), RX90, 12, dark),
            # A broad dark wing-root fairing separates the stores station from
            # the olive fuselage without introducing another renderer/material.
            add_cube(col, "WingRootFairing" + s, (0.30, 0.62, 0.075),
                     (sx * 0.78, -0.03, wing_z(0.78, 1.105)), m=dark),
            add_cube(col, "OuterStationPlate" + s, (0.25, 0.48, 0.055),
                     (sx * POD_ROCKET_X, -0.05, wing_z(POD_ROCKET_X, 1.025)), m=dark),
            add_cube(col, "PylonIn" + s, (0.16, 0.42, 0.28),
                     (sx * POD_MISSILE_X, -0.08, wing_z(POD_MISSILE_X, 0.86)), m=dark),
            add_cube(col, "PylonOut" + s, (0.16, 0.42, 0.26),
                     (sx * POD_ROCKET_X, -0.08, wing_z(POD_ROCKET_X, 0.85)), m=dark),
            # Braces and narrow bands give the stores useful scale/readability.
            strut_between(col, "InboardBrace" + s,
                          (sx * 0.62, 0.18, wing_z(0.62, 0.95)),
                          (sx * POD_MISSILE_X, -0.08, wing_z(POD_MISSILE_X, 0.78)),
                          0.045, dark),
            strut_between(col, "OutboardBrace" + s,
                          (sx * 0.93, 0.15, wing_z(0.93, 0.93)),
                          (sx * POD_ROCKET_X, -0.08, wing_z(POD_ROCKET_X, 0.72)),
                          0.045, dark),
            add_cyl(col, "RocketPodBand" + s, 0.222, 0.040,
                    (sx * POD_ROCKET_X, -0.05, wing_z(POD_ROCKET_X, 0.55)),
                    RX90, 14, dark),
        ]
    # Short axle stub from fin face to tail-rotor hub so the hub doesn't float in air.
    # Static dark trim (does not spin with TailRotor).
    trim.append(add_cyl(col, "TailRotorAxle", 0.048, 0.14,
                        (0.09, TAIL_ROTOR_CENTRE[1], TAIL_ROTOR_CENTRE[2]),
                        RY90, 8, dark))

    # Tail-rotor gearbox fairing (2026-08-03). The axle previously emerged from bare
    # fin surface; a real tail rotor sits on a visible gearbox housing, and without
    # one the rotor looked pinned on rather than driven.
    trim.append(add_cube(col, "TailRotorGearbox", (0.135, 0.30, 0.26),
                         (0.035, TAIL_ROTOR_CENTRE[1] - 0.02, TAIL_ROTOR_CENTRE[2]),
                         m=dark))

    # Fin root fairing. The fin used to plant on the boom at a hard right angle,
    # which is the single thing that made the tail read as an assembly of flat
    # plates. A tapered wedge blends the leading edge into the spine.
    trim.append(add_cube(col, "FinRootFairing", (0.115, 0.62, 0.20),
                         (0.0, -2.44, 1.09), m=dark))
    return trim


def build_rotors(col, M):
    dark = M["matAH64DarkRotor"]

    parts = [
        add_cyl(col, "Mast", 0.085, 0.55, (0, 0, -0.28), verts=10, m=dark),
        add_cyl(col, "Swashplate", 0.255, 0.06, (0, 0, -0.19), verts=14, m=dark),
        add_cyl(col, "Hub", 0.190, 0.16, (0, 0, 0.00), verts=14, m=dark),
        add_cyl(col, "HubCap", 0.105, 0.09, (0, 0, 0.11), verts=10, m=dark),
    ]
    for i in range(4):
        a = math.radians(i * 90)
        b = loft(col, "Blade%d" % i, blade_rings(MAIN_BLADE), m=dark)
        b.rotation_euler = (0, 0, a)
        parts.append(b)
        parts.append(add_cube(col, "Grip%d" % i, (0.095, 0.22, 0.085),
                              (-0.26 * math.sin(a), 0.26 * math.cos(a), 0.0),
                              (0, 0, a), m=dark))
    # origin AT the hub - a rotor whose origin is off-centre wobbles, not spins,
    # and that is not visible in the viewport
    join_as("MainRotor", parts, origin=(0, 0, 0)).location = MAIN_ROTOR_CENTRE

    tparts = [add_cyl(col, "TRHub", 0.09, 0.13, (0, 0, 0), verts=10, m=dark)]
    for i in range(4):
        b = loft(col, "TRBlade%d" % i, blade_rings(TAIL_BLADE), m=dark)
        b.rotation_euler = (0, 0, math.radians(i * 90))
        tparts.append(b)
    tr = join_as("TailRotor", tparts, origin=(0, 0, 0))
    tr.rotation_euler = RY90            # laid on its side; still spins about local Z
    tr.location = TAIL_ROTOR_CENTRE


def build_ordnance(col, M):
    """Separate objects so loadout variants can swap or hide them later without
    touching the airframe mesh.

    Every pod is entirely dark, which is what keeps each one a single renderer.
    The tube and the missile bodies used to be body-coloured, giving each pod two
    materials - so the mouths and missiles would have rendered wrong in game
    anyway. Real M261 launchers and Hellfires are dark olive, and the tube mouths
    still read at game distance because they are recessed geometry, not just a
    colour change.
    """
    rocket = M["matAH64DarkRocketPod"]
    missile = M["matAH64DarkHellfire"]

    for sx in (-1, 1):
        s = "L" if sx < 0 else "R"

        X = sx * POD_ROCKET_X
        pod_z = wing_z(POD_ROCKET_X, 0.55)
        pod = [add_cyl(col, "PodTube", 0.20, 0.88, (X, -0.05, pod_z), RX90, 14, rocket)]
        # seven modelled tube mouths stand in for nineteen - reads the same at
        # game distance for a fraction of the geometry
        mouths = [(0.0, 0.0)] + [(0.112 * math.cos(math.radians(k * 60)),
                                  0.112 * math.sin(math.radians(k * 60)))
                                 for k in range(6)]
        # Tube mouths stand PROUD of the face as individual stubs (2026-08-03).
        #
        # They cannot be holes: one renderer = one material means the pod is a single
        # solid, so there is nothing to boolean against. A recessed muzzle collar was
        # tried and is worse - add_cyl produces a CAPPED cylinder, so the collar just
        # became a larger flat plate covering the mouths entirely. There is no ring
        # primitive in this file.
        #
        # Protruding stubs get the read for free instead: the gaps between them shade
        # as crevices, which is what makes the cluster legible. It also matches the
        # real M261, whose tube ends do stand slightly out of the fairing. The old
        # flush mouths were the failure - same colour, same plane, so they resolved as
        # faint embossed hexagons on a brightly lit disc.
        for (ox, oz) in mouths:
            pod.append(add_cyl(col, "Mouth", 0.052, 0.16,
                               (X + ox, 0.43, pod_z + oz), RX90, 6, rocket))
        join_as("PodRocket" + s, pod, origin=(X, -0.05, pod_z))

        X = sx * POD_MISSILE_X
        rack_z = wing_z(POD_MISSILE_X, 0.70)

        # The rail alone keeps the PodMissile{L,R} name, so the existing ChildLocator
        # entry and item-display rules that reference it stay valid.
        join_as("PodMissile" + s,
                [add_cube(col, "Rail", (0.26, 0.46, 0.07), (X, -0.05, rack_z), m=missile)],
                origin=(X, -0.05, rack_z))

        # Each Hellfire is its own renderer so AH64PylonMissiles can hide them one at
        # a time as the special is spent. They were previously joined into the rack,
        # which is what made depletion impossible.
        #
        # All eight stay on matAH64DarkHellfire, so one-renderer-one-material holds.
        # They are deliberately NOT parented to the rail: a two-level hierarchy is
        # what the FBX exporter mis-rotates (see the ChinGatling note at the top of
        # this file), and these need no articulation, so top-level siblings avoid the
        # whole problem.
        for i, (ox, oz) in enumerate([(-0.08, -0.09), (0.08, -0.09),
                                      (-0.08, -0.20), (0.08, -0.20)]):
            join_as("Missile%s%d" % (s, i), [
                add_cyl(col, "MslBody", 0.048, 0.62,
                        (X + ox, -0.02, rack_z + oz), RX90, 8, missile),
                add_cone(col, "MslNose", 0.048, 0.13,
                         (X + ox, 0.355, rack_z + oz), RXN90, 8, missile),
            ], origin=(X + ox, -0.02, rack_z + oz))


def build_markings(col, M):
    """Small, fictional low-visibility identifiers and store bands.

    This is intentionally one static renderer.  It gives the aircraft a clear
    authored surface hierarchy while staying compatible with RoR2's material
    overlay pipeline and avoiding real unit marks or copied decals.

    Declutter pass 2026-08-03: cut from seven marked features per side to one.
    The set had grown into scattered high-contrast confetti - sponson stripe, tail
    bar, rail datum, wing station plate, engine and exhaust marks - which at
    survivor-camera distance read as noise rather than as hierarchy. A real Apache
    is low-visibility: a handful of small dark stencils, no bright banding.

    Only the rocket-pod band survives, because it does structural work the others
    did not: it breaks up an otherwise featureless cylinder. Keep at least one
    object here - AirframeMarkings is a renderer with a ChildLocator entry and a
    customRendererInfos entry, and an empty one would break both contracts.
    """
    marking = M["matAH64Markings"]
    parts = []
    for sx in (-1, 1):
        s = "L" if sx < 0 else "R"
        parts += [
            # Non-branded pod identification band. The one mark that earns its place:
            # without it the rocket pod is a bare tube at any distance.
            add_cyl(col, "RocketMarkBand" + s, 0.229, 0.028,
                    (sx * POD_ROCKET_X, -0.11, wing_z(POD_ROCKET_X, 0.55)),
                    RX90, 14, marking),
        ]
    return join_as("AirframeMarkings", parts, origin=(0, 0, 0))


def build_optics(col, M):
    """The two forward-facing glass elements of the M-TADS/PNVS-style unit.

    A dedicated glass-like renderer lets the optical cue remain crisp and blue
    while the surrounding protective structure remains part of AirframeDark.
    """
    optics = M["matAH64Optics"]
    parts = [
        add_cyl(col, "TadsLens", 0.115, 0.032, (0.0, 1.915, 0.80), RX90, 14, optics),
        add_cyl(col, "PnvsLens", 0.082, 0.032, (0.0, 1.948, 0.61), RX90, 12, optics),
    ]
    return join_as("NoseOptics", parts, origin=(0, 0, 0))


def build_rotor_blur(col, M):
    """Blur discs for the spinning rotors. Separate objects (= renderers) because
    their material is transparent FX and one renderer = one material. They export
    active so the FBX carries them; AH64Phase4Builder deactivates them in the
    prefab (lobby/portrait stay clean, and the hopoo pass skips inactive
    renderers) and AH64FlightVisuals re-activates and fades them at runtime."""
    blur = M["matAH64RotorBlur"]
    add_cyl(col, "RotorBlurMain", ROTOR_BLUR_MAIN_R, 0.02,
            (MAIN_ROTOR_CENTRE[0], MAIN_ROTOR_CENTRE[1], ROTOR_BLUR_MAIN_Z),
            verts=36, m=blur)
    # Laid on its side like the tail rotor itself: disc normal points +/-X.
    add_cyl(col, "RotorBlurTail", ROTOR_BLUR_TAIL_R, 0.02,
            (ROTOR_BLUR_TAIL_X, TAIL_ROTOR_CENTRE[1], TAIL_ROTOR_CENTRE[2]),
            RY90, verts=24, m=blur)


def build_radar_dome(col, M):
    """Shallow Fire Control Radar radome on the mast.

    RadarDome remains its own renderer with matAH64RadarDome. The restrained collar
    returns to the caller for merging into AirframeDark. Both stay stationary,
    rather than parenting to MainRotor, so the scan origin never spins with the
    blades.
    """
    dome = add_sphere(col, "RadarDome", 0.5, RADAR_DOME_CENTRE,
                      segments=18, rings=12, m=M["matAH64RadarDome"])
    # A radius-0.5 sphere starts at one unit on every axis, so these scale values
    # are the desired final dimensions. Bake them to preserve the 1,1,1 FBX rule.
    dome.scale = RADAR_DOME_SIZE
    deselect()
    bpy.context.view_layer.objects.active = dome
    dome.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    collar = add_cyl(
        col,
        "RadarCollar",
        RADAR_COLLAR_R,
        RADAR_COLLAR_DEPTH,
        RADAR_COLLAR_CENTRE,
        verts=12,
        m=M["matAH64DarkStatic"])
    return [collar]


def build_gear(col, M):
    """Returns its parts unjoined. The AH-64's gear is fixed - it does not
    retract, and the chopper never lands - so there is no runtime transform to
    preserve and no reason for it to be its own renderer. It is dark and static,
    so build() folds it into AirframeDark."""
    dark = M["matAH64DarkStatic"]
    g = []
    for sx in (-1, 1):
        s = "L" if sx < 0 else "R"
        head = (sx * GEAR_HEAD[0], GEAR_Y, GEAR_HEAD[1])
        foot = (sx * GEAR_FOOT[0], GEAR_Y, GEAR_FOOT[1])
        # Two-stage leg instead of one slab (2026-08-03). A single constant-width
        # strut read as a plank; splitting it into a fat upper leg and a thin lower
        # oleo gives the stepped silhouette that says "shock absorber" at a glance.
        mid = (sx * (GEAR_HEAD[0] + GEAR_FOOT[0]) * 0.5,
               GEAR_Y,
               (GEAR_HEAD[1] + GEAR_FOOT[1]) * 0.5)
        g.append(strut_between(col, "GearLegUpper" + s, mid, head,
                               GEAR_STRUT_W * 1.35, m=dark))
        g.append(strut_between(col, "GearOleo" + s, foot, mid,
                               GEAR_STRUT_W * 0.78, m=dark))
        # Drag brace back to the hull — cheap, and it stops the leg looking like it
        # is floating unattached under a smooth belly.
        g.append(strut_between(col, "GearBrace" + s,
                               (sx * (GEAR_FOOT[0] - 0.04), GEAR_Y + 0.34, GEAR_FOOT[1] + 0.10),
                               (sx * GEAR_HEAD[0] * 0.7, GEAR_Y + 0.02, GEAR_HEAD[1] - 0.02),
                               0.038, m=dark))
        g.append(add_cyl(col, "GearWheel" + s, GEAR_WHEEL_R, GEAR_WHEEL_W,
                         (sx * GEAR_WHEEL_X, GEAR_Y, GEAR_WHEEL_R), RY90, 12, dark))
        # Hub, proud of the tyre on the outboard face. One extra cylinder turns a
        # bare drum into a wheel; without it the tyre has no readable centre.
        g.append(add_cyl(col, "GearHub" + s, GEAR_WHEEL_R * 0.46, GEAR_WHEEL_W * 1.18,
                         (sx * GEAR_WHEEL_X, GEAR_Y, GEAR_WHEEL_R), RY90, 8, dark))
    g.append(strut_between(col, "TailStrut",
                           (0.0, TAILGEAR_Y, TAILGEAR_BOTTOM),
                           (0.0, TAILGEAR_Y, TAILGEAR_TOP), 0.09, m=dark))
    g.append(add_cyl(col, "TailWheel", TAILGEAR_WHEEL_R, 0.14,
                     (0.0, TAILGEAR_Y, 0.70), RY90, 10, dark))
    return g


def parent_chin_barrel():
    """Nest the chin weapons after the orientation bake.

    ChinTurret (yaw) -> ChinBarrel (pitch) -> ChinGatling (spin).

    Must run after bake_unity_orientation (which rejects any parenting) and
    before build_anchors (so Muzzle can parent onto the already-nested barrel).

    ChinGatling hangs off ChinBarrel rather than ChinTurret on purpose: it then
    inherits pitch from the existing AH64ChinTurret aimer with no change to that
    component, and AH64GatlingSpin only has to drive its own local bore rotation.
    It also means the shared Muzzle anchor already tracks both weapons.
    """
    barrel = bpy.data.objects.get("ChinBarrel")
    turret = bpy.data.objects.get("ChinTurret")
    if barrel is None or turret is None:
        raise RuntimeError("parent_chin_barrel needs ChinTurret and ChinBarrel")
    gatling = bpy.data.objects.get("ChinGatling")
    if gatling is None:
        raise RuntimeError("parent_chin_barrel needs ChinGatling")

    reparent_keep_world(barrel, turret)
    reparent_keep_world(gatling, barrel)


def reparent_keep_world(child, parent):
    """Parent `child` to `parent` without moving it, in a way that survives FBX export.

    Do NOT use matrix_parent_inverse for this. It is a Blender-only concept: the FBX
    exporter writes matrix_basis as the node's local transform and silently drops the
    parent inverse. A child whose rotation was 'cancelled' by a parent inverse therefore
    arrives in Unity with that rotation still applied, composed on top of its parent's.

    That is exactly what shipped the gatling pointing straight down (playtest
    2026-08-03): ChinBarrel carries the 90 deg X that faces a Y-authored part along
    Unity +Z, ChinGatling kept its own 90 deg, and the two composed to 180. It was
    invisible in Blender, where the parent inverse works fine.

    Baking the correction into matrix_basis instead means what Blender shows and what
    the FBX contains are the same thing.
    """
    bpy.context.view_layer.update()
    world = child.matrix_world.copy()
    child.parent = parent
    child.matrix_parent_inverse = Matrix.Identity(4)
    child.matrix_world = world
    bpy.context.view_layer.update()


def build_anchors(col):
    """ChildLocator targets. Blender empties export to FBX as null nodes and
    Unity imports them as plain GameObjects, so authoring them here keeps their
    positions with the geometry instead of hand-placing them in the editor."""
    made = []
    for name, (pos, parent_name) in ANCHORS.items():
        e = bpy.data.objects.new(name, None)
        e.empty_display_type = "PLAIN_AXES"
        e.empty_display_size = 0.15
        e.location = yaw_anchor(pos)    # geometry is already yawed; follow it
        col.objects.link(e)
        made.append(e)
    for name, (pos, parent_name) in ANCHORS.items():
        if parent_name:
            child = bpy.data.objects[name]
            parent = bpy.data.objects[parent_name]
            child.parent = parent
            child.matrix_parent_inverse = parent.matrix_world.inverted()
    return made


def bake_unity_orientation(col):
    """Yaw the airframe 180 deg about Z and bake it into mesh data.

    Why this exists: see the conventions note at the top of this file. Blender's
    FBX export maps Blender (x,y,z) onto Unity (-x, z, -y), so a nose-+Y model
    lands in Unity facing -Z. Baking the yaw here means the FBX genuinely faces
    Unity +Z and the import needs no fixup rotation - the same principle the
    project already applies to scale, where a non-unit scale left on an FBX is a
    lasting source of wrong-sized colliders.

    Each object's own rotation_euler is deliberately PRESERVED. The yaw is pushed
    into mesh data as (Rot^-1 . R . Rot) instead of being added to the object's
    rotation, so per-part local axes keep the meaning the rest of the pipeline
    assumes. That identity only holds for unit scale, which build() guarantees.

    Runs before build_anchors() on purpose: with no empties created yet there are
    no parent/child relationships to compensate, which is where transform-baking
    normally goes wrong. Anchors are placed in already-yawed coordinates.
    """
    R = Matrix.Rotation(math.radians(180.0), 4, "Z")
    for o in col.objects:
        if o.type != "MESH":
            continue
        if o.parent is not None:
            raise RuntimeError("bake_unity_orientation must run before any "
                               "parenting; %s already has a parent" % o.name)
        rot = o.rotation_euler.to_matrix().to_4x4()
        o.data.transform(rot.inverted() @ R @ rot)
        o.data.update()
        o.location = R @ o.location
    bpy.context.view_layer.update()
    return R


def yaw_anchor(pos):
    """ANCHORS are authored nose-+Y; bake_unity_orientation has already yawed the
    geometry 180 deg about Z, so anchors must follow. (x,y,z) -> (-x,-y,z)."""
    return (-pos[0], -pos[1], pos[2])


def check_unity_orientation(col):
    """Assert the yaw actually happened and in the right direction.

    After the bake the nose sits at negative Blender Y (so it exports to Unity
    +Z) and the tail at positive Y. Getting this backwards produces a helicopter
    that flies tail-first, which is only obvious once it is in game.
    """
    nose = col.objects.get("NoseTip")
    tail = col.objects.get("TailTip")
    if nose is None or tail is None:
        raise RuntimeError("orientation check needs the NoseTip and TailTip anchors")
    problems = []
    if nose.location.y >= 0:
        problems.append("NoseTip is at Blender y=%.3f, expected negative - the "
                        "model will export facing Unity -Z (backwards)"
                        % nose.location.y)
    if tail.location.y <= 0:
        problems.append("TailTip is at Blender y=%.3f, expected positive"
                        % tail.location.y)
    if nose.location.y >= tail.location.y:
        problems.append("NoseTip is not forward of TailTip in the baked frame")
    if problems:
        raise RuntimeError("AH-64 orientation validation failed:\n  "
                           + "\n  ".join(problems))
    return True


def planar_disc_uv(o):
    """Stable top-down UVs for the two blur discs.

    Smart Project gives every cylinder cap a different island orientation, which
    makes a directional rotor-streak texture visibly discontinuous. Project the
    two widest local axes instead; this stays deterministic across rebuilds.
    """
    mesh = o.data
    uv_layer = mesh.uv_layers.get("UVMap") or mesh.uv_layers.new(name="UVMap")
    spans = []
    for axis in range(3):
        values = [v.co[axis] for v in mesh.vertices]
        spans.append((max(values) - min(values), axis))
    axes = [axis for _, axis in sorted(spans, reverse=True)[:2]]
    bounds = []
    for axis in axes:
        values = [v.co[axis] for v in mesh.vertices]
        bounds.append((min(values), max(values)))
    for loop in mesh.loops:
        coords = []
        for axis, (low, high) in zip(axes, bounds):
            width = high - low
            coords.append((mesh.vertices[loop.vertex_index].co[axis] - low) / width
                          if width > 1e-6 else 0.5)
        uv_layer.data[loop.index].uv = coords


def unwrap(col):
    for o in col.objects:
        if o.type != "MESH":
            continue
        if o.name in ("RotorBlurMain", "RotorBlurTail"):
            planar_disc_uv(o)
            continue
        deselect()
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
        bpy.ops.object.mode_set(mode="OBJECT")
    deselect()


# -------------------------------------------------------------- VALIDATE ----

def validate():
    """Assert the clearances that have silently broken before. Loud beats visual."""
    problems = []

    for (y, hw, z0, z1) in CANOPY:
        if z0 > hull_top(y):
            problems.append("Canopy floats at y=%.2f: base %.3f > hull top %.3f"
                            % (y, z0, hull_top(y)))
        if hw > hull_half_width(y):
            problems.append("Canopy wider than hull at y=%.2f: %.3f > %.3f"
                            % (y, hw, hull_half_width(y)))

    hx, hz = GEAR_HEAD
    if hx + GEAR_STRUT_W / 2 > hull_half_width(GEAR_Y):
        problems.append("Gear strut head outboard of the hull: %.3f > %.3f"
                        % (hx + GEAR_STRUT_W / 2, hull_half_width(GEAR_Y)))
    fuse_bottom = next(z0 for (y, _, z0, _) in FUSELAGE if abs(y - GEAR_Y) < 0.001)
    if hz < fuse_bottom:
        problems.append("Gear strut head below the hull underside: %.3f < %.3f"
                        % (hz, fuse_bottom))

    if GEAR_STRUT_W >= GEAR_WHEEL_W:
        problems.append("Gear strut is not narrower than the wheel; head-on they "
                        "merge into one bar")

    dome_bottom = RADAR_DOME_CENTRE[2] - RADAR_DOME_SIZE[2] / 2
    collar_top = RADAR_COLLAR_CENTRE[2] + RADAR_COLLAR_DEPTH / 2
    if collar_top < dome_bottom:
        problems.append("Radar dome floats above its collar: %.3f > %.3f"
                        % (dome_bottom, collar_top))

    turret_top = HOUSING_Z + HOUSING_DEPTH / 2
    hull_bottom_at_turret = next(
        z0 + (TURRET_Y - yb) / (ya - yb) * (za - z0)
        for (ya, _, za, _), (yb, _, z0, _) in zip(FUSELAGE, FUSELAGE[1:])
        if yb <= TURRET_Y <= ya)
    if turret_top < hull_bottom_at_turret:
        problems.append("Chin turret floats: top %.3f < hull underside %.3f"
                        % (turret_top, hull_bottom_at_turret))

    # Stores hang off the wing at absolute coordinates rather than being parented to
    # it, so a change to WING_ANHEDRAL silently detaches them. Assert that each pylon
    # still reaches its wing station instead of floating under it.
    for (station_x, pylon_top_z, label) in (
            (POD_MISSILE_X, wing_z(POD_MISSILE_X, 0.86) + 0.28 / 2, "PylonIn"),
            (POD_ROCKET_X, wing_z(POD_ROCKET_X, 0.85) + 0.26 / 2, "PylonOut")):
        wing_lower = wing_surface_z(station_x) - wing_half_thickness(station_x)
        if pylon_top_z < wing_lower - 0.001:
            problems.append("%s floats below the wing at x=%.2f: top %.3f < underside %.3f"
                            % (label, station_x, pylon_top_z, wing_lower))

    if problems:
        raise RuntimeError("AH-64 build validation failed:\n  " + "\n  ".join(problems))
    return True


def wing_surface_z(x):
    """Centre height of the wing at spanwise |x|, interpolating the WING sections."""
    ax = abs(x)
    right = [s for s in WING if s[0] >= 0]
    for (a, b) in zip(right, right[1:]):
        if a[0] <= ax <= b[0]:
            t = (ax - a[0]) / (b[0] - a[0])
            return a[3] + t * (b[3] - a[3])
    return right[-1][3]


def wing_half_thickness(x):
    """Half thickness of the wing at spanwise |x|, interpolating the WING sections."""
    ax = abs(x)
    right = [s for s in WING if s[0] >= 0]
    for (a, b) in zip(right, right[1:]):
        if a[0] <= ax <= b[0]:
            t = (ax - a[0]) / (b[0] - a[0])
            return a[4] + t * (b[4] - a[4])
    return right[-1][4]


def check_single_material(col):
    """Assert one renderer = one material - see the note at the top of this file.

    A second material slot is invisible in Blender: the viewport cheerfully shows
    the two-tone version you intended. It only surfaces in game as a part
    rendering in the wrong colour, with nothing in the log to explain it, because
    CharacterModel overwrites the whole sharedMaterials array from a single
    RendererInfo.defaultMaterial. Fail the build instead.
    """
    problems = []
    renderer_names = {o.name for o in col.objects if o.type == "MESH"}
    expected_names = set(EXPECTED_RENDERER_MATERIALS)
    for name in sorted(expected_names - renderer_names):
        problems.append("expected renderer %s is missing" % name)
    for name in sorted(renderer_names - expected_names):
        problems.append("unexpected mesh renderer %s is not assigned a visual role" % name)

    for o in col.objects:
        if o.type != "MESH":
            continue
        mats = [m for m in o.data.materials if m is not None]
        if len(mats) != 1:
            problems.append(
                "%s has %d material slots (%s) - a RoR2 RendererInfo carries one"
                % (o.name, len(mats), ", ".join(m.name for m in mats) or "none"))
        else:
            expected = EXPECTED_RENDERER_MATERIALS.get(o.name)
            if expected and mats[0].name != expected:
                problems.append("%s uses %s, expected visual-role material %s"
                                % (o.name, mats[0].name, expected))
        if UNWRAP_UVS and not o.data.uv_layers:
            problems.append("%s has no UV map after unwrap" % o.name)
    if problems:
        raise RuntimeError("AH-64 renderer validation failed:\n  "
                           + "\n  ".join(problems))
    return True


# ------------------------------------------------------------------ MAIN ----

def build():
    validate()
    col = get_collection()
    M = ensure_materials()
    clear(col)
    dark_static = build_airframe(col, M)
    build_rotors(col, M)
    build_rotor_blur(col, M)
    dark_static += build_radar_dome(col, M)
    build_ordnance(col, M)
    build_markings(col, M)
    build_optics(col, M)
    dark_static += build_gear(col, M)
    # Engines, pylons and landing gear: all dark, all static, nothing moves at
    # runtime. One material and one transform means one renderer.
    airframe_dark = join_as("AirframeDark", dark_static, origin=(0, 0, 0))
    # The first loose trim shape can be rotated (for example, a canopy rail).
    # Joining inherits that active object's transform, which is harmless in the
    # Blender viewport but leaves a surprising FBX child transform. AirframeDark
    # is entirely static, so bake that rotation into its mesh before export.
    deselect()
    airframe_dark.select_set(True)
    bpy.context.view_layer.objects.active = airframe_dark
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    # Must precede parenting: baking a transform through a parent/child pair is
    # where this normally goes wrong, so do it while nothing is parented yet.
    bake_unity_orientation(col)
    parent_chin_barrel()
    build_anchors(col)
    if UNWRAP_UVS:
        unwrap(col)
    deselect()
    bpy.context.view_layer.update()
    check_single_material(col)
    check_unity_orientation(col)

    meshes = [o for o in col.objects if o.type == "MESH"]
    for o in meshes:
        o.data.calc_loop_triangles()

    # Measure from real vertices, NOT object.bound_box. bound_box is the
    # LOCAL-space AABB, so for any rotated object (LandingGear inherits the
    # strut's 21 deg tilt, TailRotor is laid on its side) transforming its eight
    # corners over-estimates the world extent - it once reported the gear
    # hanging 0.4 units below ground when it was sitting exactly on it.
    dg = bpy.context.evaluated_depsgraph_get()
    mins = [1e9] * 3
    maxs = [-1e9] * 3
    for o in meshes:
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        mw = o.matrix_world
        for v in me.vertices:
            w = mw @ v.co
            for i in range(3):
                mins[i] = min(mins[i], w[i])
                maxs[i] = max(maxs[i], w[i])
        ev.to_mesh_clear()

    summary = {
        # one mesh = one renderer = one customRendererInfos entry, so this count
        # is the number the Unity side has to mirror
        "renderers": sorted(o.name for o in meshes),
        "meshes": len(meshes),
        "anchors": len([o for o in col.objects if o.type == "EMPTY"]),
        "tris": sum(len(o.data.loop_triangles) for o in meshes),
        "size_XYZ": [round(maxs[i] - mins[i], 3) for i in range(3)],
        "bounds_min": [round(v, 3) for v in mins],
    }

    if SAVE_ON_RUN:
        os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
        summary["saved"] = BLEND_PATH
    return summary


def export_fbx():
    """Export the AH64 collection with the settings recorded in AH64_HANDOFF.md.

    Do not re-derive these; they were found by measurement against a live Unity
    import. The two non-defaults each fix a real defect: FBX_SCALE_ALL stops
    every mesh child importing at localScale 100, and bake_space_transform stops
    20 children carrying a 270 deg X fixup rotation. Select only the collection -
    the scene may also hold a default Camera and Light that must not ship.
    """
    col = bpy.data.collections.get(COLLECTION)
    if col is None:
        raise RuntimeError("export_fbx: collection %s does not exist" % COLLECTION)
    deselect()
    for o in col.objects:
        o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y",
        bake_space_transform=True,
        apply_unit_scale=True, global_scale=1.0,
        mesh_smooth_type="FACE", add_leaf_bones=False)
    deselect()
    return FBX_PATH


if __name__ == "__main__":
    print(build())
    if EXPORT_FBX_ON_RUN:
        print("FBX exported to %s" % export_fbx())
