"""Render a shared silhouette mask so the terrain blends into Unity's water."""
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'seamount-study.blend'))
scene = bpy.data.scenes['Natural']
bpy.context.window.scene = scene
scene.cycles.samples = 32
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.view_settings.exposure = 0
scene.world.node_tree.nodes.clear()
world_out = scene.world.node_tree.nodes.new('ShaderNodeOutputWorld')
background = scene.world.node_tree.nodes.new('ShaderNodeBackground')
background.inputs['Color'].default_value = (0, 0, 0, 1)
scene.world.node_tree.links.new(background.outputs[0], world_out.inputs['Surface'])
material = bpy.data.materials.new('Terrain mask | authored height feather')
material.use_nodes = True
nt = material.node_tree
nt.nodes.clear()
geometry = nt.nodes.new('ShaderNodeNewGeometry')
xyz = nt.nodes.new('ShaderNodeSeparateXYZ')
nt.links.new(geometry.outputs['Position'], xyz.inputs[0])
height = nt.nodes.new('ShaderNodeMapRange')
height.interpolation_type = 'SMOOTHSTEP'
height.inputs['From Min'].default_value = .015
height.inputs['From Max'].default_value = .23
height.inputs['To Min'].default_value = 0
height.inputs['To Max'].default_value = 1
nt.links.new(xyz.outputs['Z'], height.inputs['Value'])
emission = nt.nodes.new('ShaderNodeEmission')
nt.links.new(height.outputs['Result'], emission.inputs['Color'])
output = nt.nodes.new('ShaderNodeOutputMaterial')
nt.links.new(emission.outputs[0], output.inputs['Surface'])
for ob in scene.objects:
    if ob.type != 'MESH':
        continue
    ob.material_slots[0].link = 'OBJECT'
    ob.material_slots[0].material = material
scene.render.filepath = str(ROOT / 'renders/surface-mask.png')
bpy.ops.render.render(write_still=True)
