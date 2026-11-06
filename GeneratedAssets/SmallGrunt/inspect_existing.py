import bpy, json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
source=next((root/'Assets/Models/Enemy').glob('Meshy*/*.fbx'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(source))
scene=bpy.context.scene
meshes=[o for o in scene.objects if o.type=='MESH']
rigs=[o for o in scene.objects if o.type=='ARMATURE']
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
print('EXISTING_INFO',json.dumps({'bounds':[list(lo),list(hi)],'meshes':[(o.name,len(o.data.polygons)) for o in meshes], 'rigs':[(o.name,len(o.data.bones)) for o in rigs], 'actions':list(bpy.data.actions.keys())}))
for m in bpy.data.materials:
    if not m.use_nodes: continue
    nt=m.node_tree
    bs=next((n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if not bs: continue
    base=source.with_suffix('')
    for suffix,socket in [('', 'Base Color'),('_metallic','Metallic'),('_roughness','Roughness'),('_normal','Normal'),('_emission','Emission Color')]:
        path=Path(str(base)+suffix+'.png')
        if not path.exists(): continue
        tex=nt.nodes.new('ShaderNodeTexImage')
        tex.image=bpy.data.images.load(str(path),check_existing=True)
        if suffix in ['_normal','_metallic','_roughness']:
            tex.image.colorspace_settings.name='Non-Color'
        if suffix=='_normal':
            normal=nt.nodes.new('ShaderNodeNormalMap')
            nt.links.new(tex.outputs['Color'],normal.inputs['Color'])
            nt.links.new(normal.outputs['Normal'],bs.inputs[socket])
        else: nt.links.new(tex.outputs['Color'],bs.inputs[socket])
    bs.inputs['Emission Strength'].default_value=1.5
center=(lo+hi)/2
height=hi.z-lo.z
def aim(o,p): o.rotation_euler=(p-o.location).to_track_quat('-Z','Y').to_euler()
for loc,energy in [((2,-3,4),500),((-3,-1,2),350),((1,3,4),600)]:
    bpy.ops.object.light_add(type='AREA',location=center+Vector(loc)*height/1.7)
    o=bpy.context.object
    o.data.energy=energy*(height/1.7)**2
    o.data.shape='DISK';o.data.size=height*1.5
    aim(o,center)
bpy.ops.object.camera_add()
cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=height*1.3
scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.07,.07,.07)
for name,offset in [('front',(.8,-2,.4)),('back',(-.8,2,.4))]:
    cam.location=center+Vector(offset)*height
    aim(cam,center)
    scene.render.filepath=str(root/'GeneratedAssets/SmallGrunt'/('existing_'+name+'.png'))
    bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'GeneratedAssets/SmallGrunt/existing_inspection.blend'))
