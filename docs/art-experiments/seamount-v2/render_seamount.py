"""Blender look-development study. Does not write to the Unity project.

Run: Blender -b -t 8 -P render_seamount.py -- --preview --styles natural
Final: Blender -b -t 8 -P render_seamount.py -- --styles natural illustrated bathymetry
All three scenes use the same geometry and camera. The asset is fictional.
"""
import argparse
import math
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
SEED = 20260907
N = 601
EXTENT = 12.0


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def noise(size, seed, octaves=5, base=4, persistence=0.5):
    rng = np.random.default_rng(seed)
    total = np.zeros((size, size))
    weight = 1.0
    norm = 0.0
    for k in range(octaves):
        res = base * 2 ** k
        values = rng.random((res + 1, res + 1))
        coord = np.linspace(0, res, size)
        lo = np.minimum(coord.astype(int), res - 1)
        t = coord - lo
        t = t * t * (3 - 2 * t)
        rows = values[lo] * (1 - t[:, None]) + values[lo + 1] * t[:, None]
        result = rows[:, lo] * (1 - t[None, :]) + rows[:, lo + 1] * t[None, :]
        total += result * weight
        norm += weight
        weight *= persistence
    return total / norm - 0.5


def terrain():
    lin = np.linspace(-EXTENT, EXTENT, N)
    # Expand only the flat outer seabed so no rectangular boundary enters frame.
    lin = np.sign(lin) * (np.abs(lin) + np.maximum(np.abs(lin) - 7.0, 0.0) ** 2 * 2.0)
    x, y = np.meshgrid(lin, lin)
    macro = noise(N, SEED, 4, 5)
    fine = noise(N, SEED + 1, 6, 18, 0.58)
    # An offset, oblong summit with a broad sediment apron.
    u = (x + 0.48 + macro * 0.42) / 1.12
    v = (y - 0.38 + noise(N, SEED + 2, 3, 7) * 0.38) / 0.91
    r = np.sqrt(u * u + v * v)
    theta = np.arctan2(v, u)
    rim = r + 0.085 * np.sin(theta * 3 + r) + 0.045 * np.sin(theta * 7)
    for angle, extension, width in [(-155, .37, .34), (-67, .68, .31), (14, .40, .39), (114, .24, .27)]:
        diff = np.arctan2(np.sin(theta - math.radians(angle)), np.cos(theta - math.radians(angle)))
        rim -= extension * np.exp(-(diff / width) ** 2) * smooth(.55, 1.35, r)
    # Broad descending volcanic flanks keep the summit from reading as a tower.
    h = 3.22 * (1 - smooth(.60, 3.35, rim)) ** 1.52
    h += .18 * np.exp(-(r / 4.0) ** 3)
    # Long descending spurs are connected to the summit, not separate cones.
    for end_x, end_y, width in [(-4.1, -2.6, .48), (2.8, -3.3, .54), (3.4, 2.2, .60)]:
        start_x, start_y = -.48, .38
        vx, vy = end_x - start_x, end_y - start_y
        t = np.clip(((x - start_x) * vx + (y - start_y) * vy) / (vx * vx + vy * vy), 0, 1)
        perpendicular = np.sqrt((x - start_x - t * vx) ** 2 + (y - start_y - t * vy) ** 2)
        ridge = 3.15 * (1 - t) ** 1.20 * np.exp(-(perpendicular / (width + .20 * t)) ** 2)
        h = np.maximum(h, ridge)
    # Winding channels divide the unequal ridge arms.
    for angle, width, cut in [(-111, .16, .57), (-27, .13, .44), (61, .18, .35), (172, .12, .28)]:
        a = math.radians(angle) + .075 * np.sin(r * 2.4)
        diff = np.arctan2(np.sin(theta - a), np.cos(theta - a))
        h -= cut * .58 * np.exp(-(diff / width) ** 2) * np.exp(-((r - 1.9) / .92) ** 2)
    # Lower side platform: a distinct potential survey landmark.
    shoulder = np.sqrt(((x - 2.0) / .93) ** 2 + ((y - .65) / .72) ** 2)
    side = 1.20 / (1 + np.exp(np.clip((shoulder - .90) * 5, -50, 50)))
    h = np.maximum(h, side + .22 * np.exp(-(shoulder / 1.6) ** 2))
    h -= .10 * np.exp(-((u + .13) ** 2 + (v - .03) ** 2) / .42)
    h += macro * .32 + fine * (.025 + .42 * smooth(.22, 1.5, h))
    # Broken outcrops rather than evenly spaced contour terraces.
    h += .018 * np.sin(h * 18 + macro * 7) * smooth(.6, 1.2, r) * (1 - smooth(2.3, 3.5, r))
    h = np.maximum(h, .002 + fine * .003)
    h *= 1 - smooth(5.9, 6.8, np.sqrt(x * x + y * y))
    vertices = np.column_stack((x.ravel(), y.ravel(), h.ravel())).tolist()
    faces = [(j * N + i, j * N + i + 1, (j + 1) * N + i + 1, (j + 1) * N + i) for j in range(N - 1) for i in range(N - 1)]
    mesh = bpy.data.meshes.new('Seamount | asymmetric summit, gullies and eastern ledge')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    for p in mesh.polygons:
        p.use_smooth = True
    return mesh, h


def node(nt, kind, **properties):
    n = nt.nodes.new(kind)
    for key, value in properties.items():
        setattr(n, key, value)
    return n


def ramp(nt, entries):
    n = node(nt, 'ShaderNodeValToRGB')
    for e in list(n.color_ramp.elements)[2:]:
        n.color_ramp.elements.remove(e)
    for index, (position, color) in enumerate(entries):
        e = n.color_ramp.elements[index] if index < 2 else n.color_ramp.elements.new(position)
        e.position = position
        e.color = (*color, 1)
    return n


def rock_material(style):
    m = bpy.data.materials.new(style.title() + ' | rock and sediment')
    m.diffuse_color = (.08, .22, .25, 1)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial')
    bsdf = node(nt, 'ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = .92
    bsdf.inputs['Specular IOR Level'].default_value = .22
    nt.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    coord = node(nt, 'ShaderNodeTexCoord')
    xyz = node(nt, 'ShaderNodeSeparateXYZ')
    nt.links.new(coord.outputs['Object'], xyz.inputs[0])
    if style == 'bathymetry':
        geometry = node(nt, 'ShaderNodeNewGeometry')
        nt.links.new(geometry.outputs['Position'], xyz.inputs[0])
        normalize = node(nt, 'ShaderNodeMath', operation='DIVIDE')
        normalize.inputs[1].default_value = 3.35
        nt.links.new(xyz.outputs['Z'], normalize.inputs[0])
        colors = ramp(nt, [(0, (.012, .047, .15)), (.18, (.015, .20, .37)), (.39, (.025, .48, .53)), (.62, (.21, .64, .42)), (.82, (.85, .68, .20)), (1, (.91, .26, .095))])
        nt.links.new(normalize.outputs[0], colors.inputs['Fac'])
        freq = node(nt, 'ShaderNodeMath', operation='MULTIPLY')
        freq.inputs[1].default_value = 7
        nt.links.new(xyz.outputs['Z'], freq.inputs[0])
        fract = node(nt, 'ShaderNodeMath', operation='FRACT')
        nt.links.new(freq.outputs[0], fract.inputs[0])
        line = node(nt, 'ShaderNodeMath', operation='LESS_THAN')
        line.inputs[1].default_value = .055
        nt.links.new(fract.outputs[0], line.inputs[0])
        above_floor = node(nt, 'ShaderNodeMath', operation='GREATER_THAN')
        above_floor.inputs[1].default_value = .14
        nt.links.new(xyz.outputs['Z'], above_floor.inputs[0])
        visible_line = node(nt, 'ShaderNodeMath', operation='MULTIPLY')
        nt.links.new(line.outputs[0], visible_line.inputs[0])
        nt.links.new(above_floor.outputs[0], visible_line.inputs[1])
        mix = node(nt, 'ShaderNodeMixRGB', blend_type='MIX')
        mix.inputs[2].default_value = (.008, .025, .05, 1)
        nt.links.new(visible_line.outputs[0], mix.inputs[0])
        nt.links.new(colors.outputs['Color'], mix.inputs[1])
        nt.links.new(mix.outputs[0], bsdf.inputs['Base Color'])
        nt.links.new(mix.outputs[0], bsdf.inputs['Emission Color'])
        bsdf.inputs['Emission Strength'].default_value = .18
        return m
    texture = node(nt, 'ShaderNodeTexNoise')
    texture.inputs['Scale'].default_value = 2.3
    texture.inputs['Detail'].default_value = 5
    texture.inputs['Roughness'].default_value = .72
    nt.links.new(coord.outputs['Object'], texture.inputs['Vector'])
    colors = ramp(nt, [(.23, (.027, .067, .071)), (.54, (.13, .22, .22)), (.79, (.29, .34, .32))] if style == 'natural' else [(.23, (.026, .11, .14)), (.54, (.065, .28, .30)), (.79, (.22, .46, .43))])
    nt.links.new(texture.outputs['Fac'], colors.inputs['Fac'])
    geom = node(nt, 'ShaderNodeNewGeometry')
    normal = node(nt, 'ShaderNodeSeparateXYZ')
    nt.links.new(geom.outputs['Normal'], normal.inputs[0])
    sediment = node(nt, 'ShaderNodeMapRange')
    sediment.inputs['From Min'].default_value = .62
    sediment.inputs['From Max'].default_value = .98
    sediment.inputs['To Max'].default_value = .60 if style == 'natural' else .42
    nt.links.new(normal.outputs['Z'], sediment.inputs['Value'])
    mix = node(nt, 'ShaderNodeMixRGB', blend_type='MIX')
    mix.inputs[2].default_value = (.13, .18, .17, 1) if style == 'natural' else (.24, .38, .32, 1)
    nt.links.new(sediment.outputs['Result'], mix.inputs[0])
    nt.links.new(colors.outputs['Color'], mix.inputs[1])
    nt.links.new(mix.outputs[0], bsdf.inputs['Base Color'])
    grain = node(nt, 'ShaderNodeTexNoise')
    grain.inputs['Scale'].default_value = 13
    grain.inputs['Detail'].default_value = 3
    grain.inputs['Roughness'].default_value = .7
    nt.links.new(coord.outputs['Object'], grain.inputs['Vector'])
    bump = node(nt, 'ShaderNodeBump')
    bump.inputs['Strength'].default_value = .38 if style == 'natural' else .14
    bump.inputs['Distance'].default_value = .075
    nt.links.new(grain.outputs['Fac'], bump.inputs['Height'])
    nt.links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    return m


def add_obj(scene, name, data):
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    return ob


def aim(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()


def light(scene, name, location, energy, color, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.color, data.shape, data.size = energy, color, 'DISK', size
    ob = add_obj(scene, name, data)
    ob.location = location
    aim(ob, (0, 0, 1))


def make_scene(style, mesh, height, material, preview):
    scene = bpy.data.scenes.new(style.title())
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 24 if preview else 72
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 5
    scene.cycles.volume_bounces = 0
    scene.cycles.seed = SEED
    scene.render.resolution_x = 1000 if preview else 1800
    scene.render.resolution_y = 700 if preview else 1260
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.exposure = -.25 if style == 'natural' else .2
    world = bpy.data.worlds.new(style + ' | deep water')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.027, .060, .090, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .12
    scene.world = world
    ob = add_obj(scene, 'Seamount', mesh)
    if not mesh.materials:
        mesh.materials.append(material)
    ob.material_slots[0].link = 'OBJECT'
    ob.material_slots[0].material = material
    # Shared camera framing across looks, suited to survey comparison.
    camera = bpy.data.cameras.new(style + ' camera')
    camera.type = 'ORTHO'
    camera.ortho_scale = 10.6
    cam = add_obj(scene, 'Camera | shared survey angle', camera)
    cam.location = (8.2, -12.8, 8.4)
    aim(cam, (0, 0, 1.2))
    scene.camera = cam
    light(scene, 'Key | upper left', (-5.5, -5.5, 9), 620, (.72, .88, 1), 3.2)
    light(scene, 'Fill | camera right', (6, -4, 4), 80, (.35, .64, .82), 7)
    light(scene, 'Rim | distant water', (-.5, 5, 7), 760, (.25, .73, .91), 5)
    if style == 'illustrated':
        light(scene, 'Soft illustration fill', (0, -5, 8), 300, (.65, .91, .93), 8)
    if style == 'bathymetry':
        scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .4
    # Small irregular rocks make the apron and ledges read as terrain.
    rng = np.random.default_rng(SEED + 11)
    rock_meshes = []
    for k in range(5):
        bm = __import__('bmesh').new()
        __import__('bmesh').ops.create_icosphere(bm, subdivisions=1, radius=1)
        rm = bpy.data.meshes.new(style + ' rock variant ' + str(k))
        for vertex in bm.verts:
            vertex.co *= float(rng.uniform(.72, 1.25))
        bm.to_mesh(rm)
        bm.free()
        rm.materials.append(material)
        rock_meshes.append(rm)
    for k in range(115):
        px, py = rng.uniform(-3.8, 3.8), rng.uniform(-3.4, 3.3)
        ix, iy = int((px + EXTENT) / (2 * EXTENT) * (N - 1)), int((py + EXTENT) / (2 * EXTENT) * (N - 1))
        z = height[iy, ix]
        if z < .08 or z > 3.4:
            continue
        step = 2 * EXTENT / (N - 1)
        dx = (height[iy, ix + 1] - height[iy, ix - 1]) / (2 * step)
        dy = (height[iy + 1, ix] - height[iy - 1, ix]) / (2 * step)
        if math.hypot(dx, dy) > .80:
            continue
        size = float(rng.uniform(.045, .14))
        rock = add_obj(scene, 'Talus fragment', rock_meshes[k % 5])
        rock.location = (px, py, z + size * .12)
        rock.scale = (size * rng.uniform(1, 1.7), size, size * rng.uniform(.5, .9))
        rock.rotation_euler = tuple(rng.uniform(-.25, .25, 2)) + (float(rng.uniform(0, 6.28)),)
    # The heightfield continues as the seafloor beyond the camera's view.
    # A second coplanar floor would introduce a visible self-shadow seam.
    # World volume provides distance attenuation without a visible volume boundary.
    if style != 'bathymetry':
        nt = world.node_tree
        scatter = node(nt, 'ShaderNodeVolumeScatter')
        scatter.inputs['Color'].default_value = (.24, .50, .65, 1)
        scatter.inputs['Density'].default_value = .012 if style == 'natural' else .006
        scatter.inputs['Anisotropy'].default_value = .12
        nt.links.new(scatter.outputs[0], nt.nodes['World Output'].inputs['Volume'])
    return scene


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview', action='store_true')
    parser.add_argument('--styles', nargs='+', default=['natural', 'illustrated', 'bathymetry'])
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mesh, height = terrain()
    styles = ['natural', 'illustrated', 'bathymetry']
    scenes = {style: make_scene(style, mesh, height, rock_material(style), args.preview) for style in styles}
    bpy.context.window.scene = scenes['natural']
    for unused in list(bpy.data.scenes):
        if unused.name not in {scene.name for scene in scenes.values()}:
            bpy.data.scenes.remove(unused)
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                area.spaces.active.region_3d.view_perspective = 'CAMERA'
    ROOT.mkdir(parents=True, exist_ok=True)
    out = ROOT / ('preview' if args.preview else 'renders')
    out.mkdir(exist_ok=True)
    for style, scene in scenes.items():
        scene.render.filepath = str(out / (style + '.png'))
    if not args.preview:
        bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'seamount-study.blend'))
    for style in args.styles:
        scene = scenes[style]
        scene.render.filepath = str(out / (style + '.png'))
        print('RENDERING', style, flush=True)
        bpy.ops.render.render(write_still=True, scene=scene.name)
        print('SAVED', scene.render.filepath, flush=True)


if __name__ == '__main__':
    main()
