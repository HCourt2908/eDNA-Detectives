"""Render a comparison sheet inside Blender from the three completed views."""
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'seamount-study.blend'))
assert {s.name for s in bpy.data.scenes} == {'Natural', 'Illustrated', 'Bathymetry'}
terrains = [next(o for o in s.objects if o.type == 'MESH' and len(o.data.vertices) > 100000) for s in bpy.data.scenes]
assert len({o.data.name for o in terrains}) == 1
assert all(s.camera.parent is None for s in bpy.data.scenes)
cameras = [tuple(s.camera.location) + tuple(s.camera.rotation_euler) + tuple(s.camera.scale) + (s.camera.data.ortho_scale,) for s in bpy.data.scenes]
assert len(set(cameras)) == 1
print('VERIFIED: three saved scenes, shared terrain mesh and identical cameras', flush=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 8
scene.cycles.use_denoising = False
scene.render.resolution_x = 2400
scene.render.resolution_y = 1008
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.view_settings.exposure = 0
world = bpy.data.worlds.new('Comparison background')
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.006, .015, .025, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.world = world


def emission(name, color=None, image=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    emit = nt.nodes.new('ShaderNodeEmission')
    if image:
        tex = nt.nodes.new('ShaderNodeTexImage')
        tex.image = image
        nt.links.new(tex.outputs['Color'], emit.inputs['Color'])
    else:
        emit.inputs['Color'].default_value = (*color, 1)
    nt.links.new(emit.outputs[0], out.inputs['Surface'])
    return mat


ink = emission('Title ink', (.80, .92, .94))
muted = emission('Caption ink', (.26, .45, .52))


def text(name, content, pos, size, mat=ink, align='LEFT'):
    curve = bpy.data.curves.new(name, 'FONT')
    curve.body = content
    curve.size = size
    curve.align_x = align
    ob = bpy.data.objects.new(name, curve)
    scene.collection.objects.link(ob)
    ob.location = (*pos, .02)
    curve.materials.append(mat)


text('Eyebrow', 'eDNA DETECTIVES  /  BLENDER STUDY 01', (-7.8, 2.92), .17, muted)
text('Title', 'One seamount. Three visual directions.', (-7.8, 2.26), .46)
text('Comparison note', 'SAME GEOMETRY  /  SAME VIEW', (7.8, 2.36), .16, muted, 'RIGHT')
for index, (style, label, caption) in enumerate([
    ('natural', 'A  /  NATURAL', 'Deep-water lighting and rock texture'),
    ('illustrated', 'B  /  ILLUSTRATED', 'Brighter forms for survey comparison'),
    ('bathymetry', 'C  /  BATHYMETRIC', 'Elevation colour and contour study'),
]):
    x = (index - 1) * 5.3
    mesh = bpy.data.meshes.new(style)
    mesh.from_pydata([(-2.5, -1.75, 0), (2.5, -1.75, 0), (2.5, 1.75, 0), (-2.5, 1.75, 0)], [], [(0, 1, 2, 3)])
    uv = mesh.uv_layers.new()
    for loop, coord in zip(uv.data, [(0, 0), (1, 0), (1, 1), (0, 1)]):
        loop.uv = coord
    ob = bpy.data.objects.new(style, mesh)
    scene.collection.objects.link(ob)
    ob.location = (x, -.04, 0)
    image = bpy.data.images.load(str(ROOT / 'renders' / (style + '.png')))
    mesh.materials.append(emission(style + ' render', image=image))
    text(style + ' label', label, (x - 2.5, -2.24), .25)
    text(style + ' caption', caption, (x - 2.5, -2.61), .17, muted)
text('Footer', 'SHAPE + MATERIAL EXPLORATION   /   07 SEP 2026', (-7.8, -3.11), .14, muted)
data = bpy.data.cameras.new('Sheet camera')
data.type = 'ORTHO'
data.ortho_scale = 16.6
camera = bpy.data.objects.new('Sheet camera', data)
scene.collection.objects.link(camera)
camera.location = (0, 0, 10)
scene.camera = camera
scene.render.filepath = str(ROOT / 'comparison.png')
bpy.ops.render.render(write_still=True)
