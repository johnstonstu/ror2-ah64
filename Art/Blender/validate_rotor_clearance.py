"""Read-only mesh clearance audit; run Blender with --background FILE --python-exit-code 1
--python validate_rotor_clearance.py [-- --expect-fail]. Never saves or edits the scene.

Continuous cylinder envelopes prove rotor/rotor clearance, including both blur discs.
Triangle AABB lower bounds conservatively screen static geometry. BVH sampling is
an additional intersection check, not a proof between samples. Units are Blender
authoring units. Main/tail rotor spin axes are their saved local +Z axes.
"""
import argparse
import json
import math
import sys

import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

MARGIN = 0.05
SAMPLES = 72  # Five degrees, with all relative main/tail phase combinations.
NAMES = ("MainRotor", "TailRotor", "RotorBlurMain", "RotorBlurTail")
# Exclude central rotor attachments only when auditing blades against static parts.
# Rotor/rotor envelopes and BVHs include the complete hubs.
HUB_RADII = {"MainRotor": 0.50, "TailRotor": 0.20,
             "RotorBlurMain": 0.50, "RotorBlurTail": 0.20}


def read_mesh(obj, depsgraph):
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        mesh.calc_loop_triangles()
        return ([v.co.copy() for v in mesh.vertices],
                [tuple(t.vertices) for t in mesh.loop_triangles],
                evaluated.matrix_world.copy())
    finally:
        evaluated.to_mesh_clear()


def radial(point):
    return math.hypot(point.x, point.y)


def bounds(points):
    return ([min(p[i] for p in points) for i in range(3)],
            [max(p[i] for p in points) for i in range(3)])


def interval_gap(a0, a1, b0, b1):
    return max(b0 - a1, a0 - b1, 0.0)


def rectangle_radius(lo, hi):
    return math.hypot(interval_gap(lo[0], hi[0], 0, 0),
                      interval_gap(lo[1], hi[1], 0, 0))


def cylinder(mesh, hub=0):
    vertices, faces, matrix = mesh
    # Include complete crossing triangles, not just their outermost vertices.
    indices = {i for f in faces if any(radial(vertices[j]) > hub for j in f) for i in f}
    points = [vertices[i] for i in indices]
    return max(map(radial, points)), min(p.z for p in points), max(p.z for p in points)


def check_rigid(matrix, name):
    basis = matrix.to_3x3()
    for i in range(3):
        if abs(basis.col[i].length - 1) > 1e-4:
            raise ValueError(name + ": apply scale before validating authoring-unit margins")
        for j in range(i):
            if abs(basis.col[i].dot(basis.col[j])) > 1e-4:
                raise ValueError(name + ": sheared transform is unsupported")


def envelope_gap(main, tail):
    radius, zmin, zmax = cylinder(main)
    tail_radius, tmin, tmax = cylinder(tail)
    relative = main[2].inverted() @ tail[2]
    axis = relative.to_3x3() @ Vector((0, 0, 1))
    center = relative.translation
    lo, hi = [], []
    for i in range(3):
        extent = tail_radius * math.sqrt(max(0, 1 - axis[i] ** 2))
        ends = (center[i] + axis[i] * tmin, center[i] + axis[i] * tmax)
        lo.append(min(ends) - extent)
        hi.append(max(ends) + extent)
    radial_gap = max(0, rectangle_radius(lo, hi) - radius)
    axial_gap = interval_gap(lo[2], hi[2], zmin, zmax)
    return math.hypot(radial_gap, axial_gap)


def clip_z(points, plane, above):
    result = []
    for index, end in enumerate(points):
        start = points[index - 1]
        start_in = start.z >= plane if above else start.z <= plane
        end_in = end.z >= plane if above else end.z <= plane
        if start_in != end_in:
            result.append(start.lerp(end, (plane - start.z) / (end.z - start.z)))
        if end_in:
            result.append(end)
    return result


def polygon_radius(points):
    # Polygon is convex because it is a triangle clipped by two planes.
    crosses = []
    distance = float("inf")
    for index, b in enumerate(points):
        a = points[index - 1]
        crosses.append(a.x * b.y - a.y * b.x)
        dx, dy = b.x - a.x, b.y - a.y
        length = dx * dx + dy * dy
        t = max(0, min(1, -(a.x * dx + a.y * dy) / length)) if length else 0
        distance = min(distance, math.hypot(a.x + t * dx, a.y + t * dy))
    if abs(sum(crosses)) > 1e-10 and (min(crosses) >= 0 or max(crosses) <= 0):
        return 0
    return distance


def static_envelope(mesh, static):
    name, rotor = mesh
    radius, zmin, zmax = cylinder(rotor, HUB_RADII[name])
    inverse = rotor[2].inverted()
    closest = (float("inf"), None)
    uncertain = []
    for object_name, other in static.items():
        relative = inverse @ other[2]
        vertices = [relative @ v for v in other[0]]
        for face in other[1]:
            points = [vertices[i] for i in face]
            # Completely central static triangles are intended hub/mast/axle attachments.
            if max(map(radial, points)) <= HUB_RADII[name]:
                continue
            lo, hi = bounds(points)
            gap = math.hypot(max(0, rectangle_radius(lo, hi) - radius),
                             interval_gap(lo[2], hi[2], zmin, zmax))
            if gap < closest[0]:
                closest = (gap, object_name)
            if gap < MARGIN:
                # An axially and radially inflated cylinder contains the true margin
                # offset. Exact polygon clipping avoids AABB false alarms near fin tips.
                clipped = clip_z(clip_z(points, zmin - MARGIN, True), zmax + MARGIN, False)
                if clipped and max(map(radial, clipped)) > HUB_RADII[name]:
                    if polygon_radius(clipped) < radius + MARGIN:
                        uncertain.append((object_name, face))
    return closest, len(uncertain)


def bvh(mesh, angle=0, hub=0):
    vertices, faces, matrix = mesh
    transform = matrix @ Matrix.Rotation(angle, 4, "Z")
    faces = [f for f in faces if any(radial(vertices[i]) > hub for i in f)]
    return BVHTree.FromPolygons([transform @ v for v in vertices], faces, all_triangles=True)


def sample_collisions(rotors, static):
    static_trees = {name: bvh(mesh) for name, mesh in static.items()}
    collisions = set()
    full_trees = {}
    for name in ("MainRotor", "TailRotor"):
        full_trees[name] = []
        for step in range(SAMPLES):
            angle = math.tau * step / SAMPLES
            full_trees[name].append(bvh(rotors[name], angle))
            blade = bvh(rotors[name], angle, HUB_RADII[name])
            for other, tree in static_trees.items():
                if blade.overlap(tree):
                    collisions.add((name, other))
    for main in full_trees["MainRotor"]:
        if any(main.overlap(tail) for tail in full_trees["TailRotor"]):
            collisions.add(("MainRotor", "TailRotor"))
            break
    return sorted(collisions)


def validate_rotor_clearance(raise_on_failure=True):
    """Return the audit report; default failure raises before a generator can save/export."""
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    meshes = {obj.name: read_mesh(obj, depsgraph) for obj in bpy.context.scene.objects
              if obj.type == "MESH"}
    missing = set(NAMES) - set(meshes)
    if missing:
        raise ValueError("Required geometry missing: " + ", ".join(sorted(missing)))
    for name in NAMES:
        check_rigid(meshes[name][2], name)
    rotors = {name: meshes[name] for name in NAMES}
    static = {name: mesh for name, mesh in meshes.items() if name not in NAMES}
    gaps = {a + "/" + b: envelope_gap(rotors[a], rotors[b])
            for a in ("MainRotor", "RotorBlurMain")
            for b in ("TailRotor", "RotorBlurTail")}
    static_gaps = {name: static_envelope((name, rotors[name]), static) for name in NAMES}
    collisions = sample_collisions(rotors, static)
    failures = [pair + ": continuous envelope clearance below margin"
                for pair, gap in gaps.items() if gap < MARGIN]
    failures += ["/".join(pair) + ": sampled mesh intersection" for pair in collisions]
    failures += [name + ": static geometry lacks continuous margin proof"
                 for name in ("MainRotor", "RotorBlurMain") if static_gaps[name][1]]
    # The broad static envelope is deliberately conservative around attached hardware;
    # report inconclusive triangles rather than claim their lower bound is a collision.
    report = {
        "blend": bpy.data.filepath, "margin": MARGIN, "samples_per_rotor": SAMPLES,
        "rotor_envelopes_local": {name: cylinder(mesh) for name, mesh in rotors.items()},
        "continuous_pair_clearance_lower_bounds": gaps,
        "static_envelope_nearest_and_unproven_triangles": static_gaps,
        "sampled_intersections": collisions, "failures": failures,
        "status": "FAIL" if failures else "PASS",
        "limitations": "Static envelope warnings are not continuous clearance proofs. "
                       "BVH tests surface intersections at 5-degree intervals only. "
                       "Central hub attachments excluded only from rotor/static checks.",
    }
    if failures and raise_on_failure:
        raise RuntimeError("Rotor clearance validation failed: " + json.dumps(report, sort_keys=True))
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--expect-fail", action="store_true",
                        help="Succeed only when the model fails clearance (baseline regression check).")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    report = validate_rotor_clearance(raise_on_failure=False)
    print("ROTOR_CLEARANCE_REPORT=" + json.dumps(report, sort_keys=True))
    if bool(report["failures"]) != args.expect_fail:
        raise RuntimeError("Rotor clearance validation failed" if report["failures"]
                           else "Expected a failing baseline, but clearance passed")


if __name__ == "__main__":
    main()
