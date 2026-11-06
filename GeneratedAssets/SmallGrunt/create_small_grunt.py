"""Run with Blender 4.4 in background mode. Builds the SmallGrunt test asset."""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
OUT = ROOT / 'Assets/Models/Enemy/SmallGrunt'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.fps = 30

def material(name, color, metallic=0, roughness=.6, emission=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = emission
    return m

red = material('Armor_Crimson', (.38, .027, .037), .25)
edge = material('Armor_Scarlet', (.68, .07, .045), .25)
dark = material('Joints_Charcoal', (.035, .047, .057), .35)
steel = material('Claws_IvorySteel', (.58, .57, .46), .65)
eye = material('Eyes_Amber', (1, .43, .025), .1, .3, 2)
parts = []

def finish(obj, name, mat, bone):
    obj.name = name
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    parts.append(obj)
    return obj

def box(name, location, scale, mat, bone, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new('Machined edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 1
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return finish(obj, name, mat, bone)

def orb(name, location, scale, mat, bone):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=location)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, name, mat, bone)

def spike(name, start, end, radius, mat, bone, tip=0):
    start, end = Vector(start), Vector(end)
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=radius, radius2=tip,
        depth=(end-start).length, location=(start+end)/2)
    obj = bpy.context.object
    obj.rotation_euler = (end-start).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, mat, bone)

# Build the reference-inspired alien mech using the shared geometry helpers.
exec(compile((SOURCE/'reference_geometry.py').read_text(), str(SOURCE/'reference_geometry.py'), 'exec'))

# One skinned mesh; rigid weights suit the mechanical joints.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
mesh = bpy.context.object
mesh.name = 'SmallGrunt_Mesh'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = mesh.modifiers.new('Triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
bpy.ops.object.select_all(action='DESELECT')
data = bpy.data.armatures.new('SmallGrunt_Skeleton')
rig = bpy.data.objects.new('SmallGrunt_Rig', data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for name, head, tail, parent in bones:
    b = data.edit_bones.new(name)
    b.head, b.tail = head, tail
    if parent:
        b.parent = data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
mesh.parent = rig
mod = mesh.modifiers.new('Skeleton', 'ARMATURE')
mod.object = rig
rig.animation_data_create()

for clip, frames in [('Idle', 60), ('Walk', 30), ('Attack', 24)]:
    action = bpy.data.actions.new(clip)
    rig.animation_data.action = action
    for frame in range(1, frames+2):
        t = (frame-1)/frames
        for p in rig.pose.bones:
            p.rotation_mode = 'XYZ'
            p.rotation_euler = (0,0,0)
            p.location = (0,0,0)
        if clip == 'Idle':
            rig.pose.bones['Body'].location.y = .012*math.sin(t*math.tau)
            rig.pose.bones['Head'].rotation_euler.z = .05*math.sin(t*math.tau)
        elif clip == 'Walk':
            for suffix, phase in [('L',0), ('R',math.pi)]:
                swing = math.sin(t*math.tau+phase)
                rig.pose.bones['Thigh.'+suffix].rotation_euler.x = .48*swing
                rig.pose.bones['Shin.'+suffix].rotation_euler.x = -.45*max(0,-swing)
                rig.pose.bones['Arm.'+suffix].rotation_euler.x = -.4*swing
            rig.pose.bones['Body'].location.y = .015*(1-math.cos(t*math.tau*2))
        else:
            # Wind up, strike at frame 11, then recover; no root translation.
            strike = math.sin(math.pi*min(1,t/.42))*.55 if t < .42 else -math.sin(math.pi*(t-.42)/.58)*1.1
            for suffix in ['L','R']:
                rig.pose.bones['Arm.'+suffix].rotation_euler.x = strike
                rig.pose.bones['Forearm.'+suffix].rotation_euler.x = min(0,strike)*.4
            rig.pose.bones['Body'].rotation_euler.x = -.12*math.sin(t*math.pi)
        for p in rig.pose.bones:
            p.keyframe_insert('rotation_euler', frame=frame, group=p.name)
            p.keyframe_insert('location', frame=frame, group=p.name)
    action.use_fake_user = True

rig.animation_data.action = bpy.data.actions['Idle']
scene.frame_set(1)
scene.frame_end = 60
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'SmallGrunt.fbx'), use_selection=True,
    object_types={'MESH','ARMATURE'}, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0,
    path_mode='AUTO')

# Studio preview is kept outside Unity's Assets directory.
floor = material('PreviewFloor', (.035,.045,.055))
bpy.ops.mesh.primitive_plane_add(size=200)
bpy.context.object.name = 'Preview_Ground'
bpy.context.object.data.materials.append(floor)
def point_at(obj, pos):
    obj.rotation_euler = (Vector(pos)-obj.location).to_track_quat('-Z','Y').to_euler()
for name, loc, energy, size in [('Key', (2,-3,4),450,3), ('Fill',(-3,-1,2),250,3), ('Rim',(0,3,3),550,2)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    obj = bpy.context.object
    obj.name = 'Preview_'+name
    obj.data.energy, obj.data.shape, obj.data.size = energy, 'DISK', size
    point_at(obj,(0,0,.6))
bpy.ops.object.camera_add(location=(1.7,-2.8,1.65))
camera = bpy.context.object
point_at(camera,(0,0,.71))
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.82
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 1100, 1100
scene.render.resolution_percentage = 100
scene.world.color = (.15,.15,.15)
scene.render.filepath = str(SOURCE/'SmallGrunt_preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'SmallGrunt.blend'))
bpy.ops.render.render(write_still=True)

# Round-trip the actual FBX, checking export contents and scale.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for action in list(bpy.data.actions):
    bpy.data.actions.remove(action)
bpy.ops.import_scene.fbx(filepath=str(OUT/'SmallGrunt.fbx'))
meshes = [obj for obj in scene.objects if obj.type == 'MESH']
rigs = [obj for obj in scene.objects if obj.type == 'ARMATURE']
assert len(meshes) == 1 and len(rigs) == 1
assert .9 < meshes[0].dimensions.z < 1.6, tuple(meshes[0].dimensions)
assert len(bpy.data.actions) == 3, list(bpy.data.actions.keys())
assert all(len(v.groups) > 0 for v in meshes[0].data.vertices)
report = dict(meshes=len(meshes), bones=len(rigs[0].data.bones),
    triangles=len(meshes[0].data.polygons), dimensions_m=list(meshes[0].dimensions),
    actions=list(bpy.data.actions.keys()), materials=[m.name for m in meshes[0].data.materials])
(SOURCE/'validation.json').write_text(json.dumps(report, indent=2))
print('SMALL_GRUNT_VALIDATED', json.dumps(report))
