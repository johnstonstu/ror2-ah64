"""Primary assemblies, authored in the airframe's +Y forward frame.
One mesh per material/motion; shared turret and pitch pivots remain unchanged.
"""
import math


def build(g, col, materials):
    cube, cyl, duct, join = (g[k] for k in ('add_cube', 'add_cyl', 'add_duct', 'join_as'))
    y, z, rx = g['PITCH_Y'], g['PITCH_Z'], g['RX90']
    gun, rotary = materials['matAH64DarkGun'], materials['matAH64DarkGatling']

    def box(name, dims, offset, mat=gun):
        obj = cube(col, name, dims, (offset[0], y + offset[1], z + offset[2]), m=mat)
        # Small single-segment bevels echo the illustrated machined facets.
        bevel = obj.modifiers.new('Machined edges', 'BEVEL')
        bevel.width = 0.012
        bevel.segments = 1
        import bpy
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=bevel.name)
        return obj

    def tube(name, back, front, radius, wall, mat=gun, x=0, dz=0):
        return duct(col, name, x, z + dz, y + back, y + front, radius, radius, wall, mat)

    def drum(name, center, length, radius, mat=gun):
        return cyl(col, name, radius, length, (0, y + center, z), rx, 12, m=mat)

    # M230: angular receiver, narrow exposed tube and open slotted flash cage.
    m230 = [box('M230Receiver', (.20, .36, .18), (0, .10, 0)),
            box('M230Drive', (.07, .25, .11), (.12, .07, -.01)),
            drum('M230Trunnion', .28, .13, .079),
            tube('M230Tube', .30, 1.28, .043, .014),
            tube('M230MuzzleRear', 1.06, 1.10, .071, .029),
            tube('M230MuzzleRim', 1.26, 1.30, .071, .025)]
    for i in range(4):
        a = i * math.tau / 4
        m230.append(box('M230CageRail%d' % i, (.025, .19, .025),
                         (.058 * math.cos(a), 1.18, .058 * math.sin(a))))
    # Joining into the axial cylinder preserves the original RX90 pitch-local frame.
    join('ChinBarrel', [m230[2]] + m230[:2] + m230[3:], origin=(0, y, z))

    # Stationary motor/receiver must NOT rotate with the barrel pack.
    housing = [box('GatlingReceiver', (.29, .34, .25), (0, .06, 0), rotary),
               box('GatlingDrive', (.08, .27, .16), (.17, .07, -.02), rotary),
               drum('GatlingBearing', .235, .11, .158, rotary)]
    join('ChinGatlingHousing', [housing[2]] + housing[:2], origin=(0, y, z))
    cluster = [drum('GatlingRotor', .29, .10, .131, rotary),
               drum('GatlingRearCollar', .42, .055, .140, rotary),
               drum('GatlingFrontCollar', 1.13, .055, .140, rotary)]
    # Six separated, hollow bores. Collars stop behind the mouths so each reads.
    for i in range(6):
        a = i * math.tau / 6
        x, dz = .096 * math.cos(a), .096 * math.sin(a)
        cluster.append(tube('GatlingTube%d' % i, .30, 1.30, .031, .010, rotary, x, dz))
    join('ChinGatling', cluster, origin=(0, y, z))

    # HE cannon: short reinforced sleeve, recoil rails and broad recessed muzzle.
    cannon = [box('CannonBreech', (.28, .37, .25), (0, .08, 0)),
              drum('CannonLock', .29, .11, .155),
              tube('CannonSleeve', .31, .96, .112, .045),
              tube('CannonMuzzleRear', .65, .73, .165, .070),
              tube('CannonMuzzleLip', .88, .98, .175, .060)]
    for side in (-1, 1):
        cannon.append(box('CannonRecoilRail', (.044, .42, .048), (side * .124, .48, .073)))
    for i in range(6):
        a = i * math.tau / 6
        cannon.append(box('CannonBrakeBridge%d' % i, (.056, .21, .056),
                          (.136 * math.cos(a), .79, .136 * math.sin(a))))
    join('ChinCannon', [cannon[1], cannon[0]] + cannon[2:], origin=(0, y, z))
