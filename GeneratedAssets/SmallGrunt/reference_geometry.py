"""Reference-inspired silver alien-mech geometry; executed by create_small_grunt.py."""
import bmesh

silver = material('Armor_Titanium', (.43, .46, .48), .78, .39)
edge = material('Armor_Edge', (.23, .27, .30), .85, .33)
dark = material('Frame_Carbon', (.014, .021, .026), .55, .42)
steel = material('Machined_Steel', (.12, .15, .17), .85, .3)
bronze = material('Connectors_Bronze', (.21, .09, .025), .72, .4)
eye = material('Energy_Orange', (1, .18, .006), .15, .26, 3.5)
parts = []

def raw_mesh(name, vertices, faces, mat, bone):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    return finish(obj, name, mat, bone)

def plate(name, outline, y, depth, ridge, mat, bone):
    # Raised central ridge and individually shaped perimeter create angular armor.
    n = len(outline)
    cx = sum(p[0] for p in outline)/n
    cz = sum(p[1] for p in outline)/n
    vertices = [(x,y,z) for x,z in outline]
    vertices += [(x,y+depth,z) for x,z in outline]
    vertices += [(cx,y-ridge,cz), (cx,y+depth,cz)]
    faces = []
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,2*n), (n+j,n+i,2*n+1), (i,n+i,n+j,j)])
    return raw_mesh(name, vertices, faces, mat, bone)

def cable(name, points, radius, mat, bone, sides=6):
    points = [Vector(p) for p in points]
    vertices, faces = [], []
    for i,p in enumerate(points):
        tangent = (points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
        u = tangent.cross(Vector((0,0,1)))
        if u.length < .01:
            u = tangent.cross(Vector((0,1,0)))
        u.normalize()
        v = tangent.cross(u).normalized()
        for j in range(sides):
            a = math.tau*j/sides
            vertices.append(p+radius*(u*math.cos(a)+v*math.sin(a)))
    for i in range(len(points)-1):
        for j in range(sides):
            a, b = i*sides+j, i*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))), tuple((len(points)-1)*sides+j for j in range(sides))])
    return raw_mesh(name, vertices, faces, mat, bone)

def ring(name, pos, radius, thickness, mat, bone, axis=(0,-1,0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=12, minor_segments=4,
        location=pos, major_radius=radius, minor_radius=thickness)
    obj = bpy.context.object
    obj.rotation_euler = Vector(axis).to_track_quat('Z','Y').to_euler()
    return finish(obj, name, mat, bone)

def rod(name, a, b, radius, mat, bone, tip=None):
    return spike(name,a,b,radius,mat,bone,radius if tip is None else tip)

def bolt(x,y,z,bone):
    rod('Recessed_fastener', (x,y,z),(x,y-.008,z),.009, dark,bone)

bones = [('Root',(0,0,0),(0,0,.2),None),
         ('Body',(0,0,.66),(0,0,1.08),'Root'),
         ('Head',(0,0,1.08),(0,0,1.34),'Body')]

# A narrow exposed mechanical waist beneath swept paired breastplates.
orb('Ribcage_frame',(0,.005,.94),(.185,.107,.205),dark,'Body')
rod('Spine',(0,.06,.66),(0,.06,1.12),.042,steel,'Body')
for z in [.71,.765,.82,.875]:
    plate('Abdominal_segment',[(-.067,z+.026),(.067,z+.026),(.05,z-.02),(0,z-.037),(-.05,z-.02)],
          -.085,.045,.016,steel,'Body')
    box('Abdominal_energy',(0,-.108,z),(.012,.009,.027),eye,'Body',.002)
orb('Pelvis_frame',(0,0,.645),(.125,.07,.068),dark,'Body')
plate('Pelvis_guard',[(-.10,.69),(0,.67),(.10,.69),(.065,.60),(0,.565),(-.065,.60)],
      -.07,.06,.025,silver,'Body')
plate('Sternum',[(-.033,1.015),(0,1.04),(.033,1.015),(.027,.925),(0,.89),(-.027,.925)],
      -.115,.055,.029,edge,'Body')
ring('Chest_reactor_bezel',(0,-.153,.968),.025,.009,bronze,'Body')
rod('Chest_reactor',(0,-.156,.968),(0,-.167,.968),.018,eye,'Body')

for s in [-1,1]:
    plate('Swept_pectoral',[(s*.022,1.04),(s*.125,1.083),(s*.205,1.055),
         (s*.17,.961),(s*.073,.924),(s*.025,.967)],-.11,.085,.028,silver,'Body')
    plate('Lower_rib_plate',[(s*.065,.931),(s*.164,.963),(s*.145,.9),(s*.075,.87)],
          -.079,.05,.012,edge,'Body')
    cable('Chest_energy_seam',[(s*.041,-.133,1.013),(s*.072,-.144,.984),(s*.131,-.137,.985)],
          .005,eye,'Body',4)
    bolt(s*.151,-.143,1.036,'Body')
    # Exposed rib hoses and loops at the waist.
    cable('Rib_hose',[(s*.15,.02,1.03),(s*.182,.032,.96),(s*.176,.035,.88),
          (s*.131,.036,.80),(s*.10,.02,.75)],.013,dark,'Body')
    cable('Hip_hose',[(s*.083,.03,.80),(s*.143,.023,.76),(s*.17,.02,.67),
          (s*.155,.0,.605),(s*.122,-.01,.61)],.009,bronze,'Body')
    for z in [.89,.93,.97]:
        ring('Hose_collar',(s*.178,.033,z),.014,.003,bronze,'Body',axis=(0,0,1))

# Helmet: split, pointed facial plates, a glowing forked slit, and swept side horns.
orb('Helmet_inner',(0,.0,1.213),(.105,.09,.135),dark,'Head')
for s in [-1,1]:
    plate('Helmet_half',[(s*.013,1.338),(s*.068,1.328),(s*.106,1.268),
          (s*.081,1.201),(s*.025,1.13),(s*.012,1.193),(s*.026,1.244)],
          -.087,.112,.018,silver,'Head')
    cable('Visor_energy',[(s*.055,-.113,1.269),(s*.041,-.118,1.226),(0,-.12,1.188),
          (0,-.107,1.145)],.007,eye,'Head',5)
    plate('Cheek_vent',[(s*.105,1.244),(s*.086,1.171),(s*.04,1.135),(s*.06,1.205)],
          -.057,.075,.008,edge,'Head')
    # Curved horn blade with a sharp silver leading edge.
    plate('Temple_blade',[(s*.075,1.254),(s*.123,1.32),(s*.146,1.402),
          (s*.104,1.368),(s*.093,1.314)],.001,.022,.006,edge,'Head')
    cable('Temple_blade_edge',[(s*.075,-.006,1.254),(s*.123,-.006,1.32),(s*.146,-.006,1.402)],
          .004,silver,'Head',4)
    bolt(s*.066,-.104,1.294,'Head')
    cable('Neck_conduit',[(s*.06,.058,1.26),(s*.085,.082,1.18),(s*.064,.08,1.09)],
          .012,dark,'Head')
    ring('Neck_conduit_collar',(s*.065,.08,1.11),.012,.004,bronze,'Head',axis=(0,0,1))

# Rear reactor and tall dorsal blades are readable in silhouette.
ring('Back_reactor_housing',(0,.119,1.003),.061,.014,steel,'Body',axis=(0,1,0))
ring('Back_reactor_light',(0,.136,1.003),.042,.006,eye,'Body',axis=(0,1,0))
rod('Back_core',(0,.13,1.003),(0,.146,1.003),.026,bronze,'Body')
for s in [-1,1]:
    plate('Dorsal_fin',[(s*.07,.97),(s*.146,1.09),(s*.16,1.26),(s*.123,1.32),
          (s*.12,1.16),(s*.084,1.07)],.13,.023,.007,silver,'Body')
    cable('Back_conduit',[(s*.03,.13,.94),(s*.075,.141,.87),(s*.05,.10,.74)],.007,eye,'Body',5)

for s,suffix in [(-1,'L'),(1,'R')]:
    upper,lower='Arm.'+suffix,'Forearm.'+suffix
    thigh,shin='Thigh.'+suffix,'Shin.'+suffix
    shoulder=(s*.225,0,1.025)
    elbow=(s*.30,-.005,.83)
    wrist=(s*.335,-.04,.64)
    hip=(s*.092,0,.635)
    knee=(s*.139,-.014,.377)
    ankle=(s*.157,.026,.105)
    bones.extend([(upper,shoulder,elbow,'Body'),(lower,elbow,wrist,upper),
                  (thigh,hip,knee,'Root'),(shin,knee,ankle,thigh)])
    orb('Shoulder_bearing',shoulder,(.073,.073,.073),dark,upper)
    ring('Shoulder_outer_ring',(s*.225,-.068,1.025),.047,.01,bronze,upper)
    ring('Shoulder_energy',(s*.225,-.078,1.025),.034,.005,eye,upper)
    rod('Shoulder_axle',(s*.225,-.062,1.025),(s*.225,-.081,1.025),.024,steel,upper)
    plate('Shoulder_shell',[(s*.17,1.068),(s*.20,1.125),(s*.277,1.133),
          (s*.329,1.074),(s*.328,1.022),(s*.277,1.054)],-.032,.13,.025,silver,upper)
    plate('Shoulder_blade',[(s*.281,1.087),(s*.32,1.065),(s*.346,.967),(s*.297,1.005)],
          .027,.04,.006,edge,upper)
    bolt(s*.256,-.058,1.105,upper)
    rod('Upper_arm_piston',shoulder,elbow,.027,dark,upper)
    rod('Upper_arm_hydraulic',(s*.253,-.03,.995),(s*.292,-.035,.865),.014,steel,upper)
    plate('Bicep_plate',[(s*.255,1.002),(s*.285,.982),(s*.309,.878),(s*.279,.885)],
          -.046,.056,.014,silver,upper)
    orb('Elbow_joint',elbow,(.042,.043,.039),dark,lower)
    ring('Elbow_ring',(s*.301,-.047,.83),.023,.005,bronze,lower)
    rod('Forearm_frame',elbow,wrist,.032,dark,lower)
    plate('Forearm_shell',[(s*.283,.814),(s*.316,.838),(s*.362,.751),
          (s*.356,.653),(s*.318,.679),(s*.307,.742)],-.052,.07,.025,silver,lower)
    cable('Forearm_energy',[(s*.328,-.08,.792),(s*.343,-.083,.756),(s*.339,-.078,.702)],.005,eye,lower,4)
    bolt(s*.321,-.072,.805,lower)
    box('Wrist',(s*.335,-.04,.623),(.057,.07,.06),steel,lower,.011)
    if s == 1:
        box('Palm',(s*.341,-.04,.577),(.072,.055,.075),dark,lower,.012)
        for i in [-1,0,1]:
            x=s*.341+i*.025
            rod('Finger',(x,-.05,.55),(x,-.065,.504),.009,steel,lower)
            rod('Finger_tip',(x,-.065,.504),(x,-.094,.493),.011,silver,lower,0)
        rod('Thumb',(.304,-.035,.586),(.285,-.076,.553),.012,steel,lower)
    else:
        # Long integrated cannon on the left forearm; angular shroud and twin rails.
        box('Cannon_receiver',(-.354,-.063,.638),(.12,.115,.235),dark,lower,.018)
        plate('Cannon_shroud',[(-.423,.784),(-.336,.753),(-.307,.59),(-.326,.415),
              (-.362,.345),(-.406,.447)],-.133,.13,.023,edge,lower)
        plate('Cannon_silver_rail',[(-.428,.758),(-.407,.733),(-.39,.44),(-.365,.35),
              (-.397,.40),(-.432,.58)],-.153,.021,.008,silver,lower)
        plate('Cannon_inner_rail',[(-.33,.712),(-.318,.68),(-.333,.421),(-.358,.355),
              (-.354,.489)],-.148,.017,.006,silver,lower)
        for x in [-.39,-.358]:
            cable('Cannon_energy_rail',[(x,-.166,.67),(x,-.165,.535),(x+.007,-.16,.44)],
                  .006,eye,lower,4)
        rod('Cannon_barrel',(-.364,-.09,.46),(-.358,-.095,.32),.028,steel,lower)
        rod('Cannon_muzzle',(-.359,-.095,.34),(-.358,-.095,.316),.019,dark,lower)
        ring('Cannon_power_cell',(-.341,-.162,.698),.021,.006,bronze,lower)
    orb('Hip_bearing',hip,(.048,.05,.05),dark,thigh)
    rod('Thigh_frame',hip,knee,.031,dark,thigh)
    plate('Thigh_armor',[(s*.092,.635),(s*.136,.66),(s*.18,.593),(s*.17,.472),
          (s*.139,.41),(s*.107,.50)],-.049,.09,.027,silver,thigh)
    plate('Thigh_inner_plate',[(s*.086,.599),(s*.109,.534),(s*.108,.461),(s*.079,.491)],
          -.03,.04,.009,edge,thigh)
    bolt(s*.143,-.071,.602,thigh)
    cable('Thigh_piston',[(s*.101,.064,.606),(s*.148,.06,.432)],.013,steel,thigh)
    orb('Knee_bearing',knee,(.045,.045,.046),dark,shin)
    ring('Knee_halo',(s*.139,-.061,.383),.027,.005,eye,shin)
    plate('Kneecap',[(s*.118,.426),(s*.149,.441),(s*.168,.397),(s*.139,.346),(s*.113,.39)],
          -.069,.031,.014,edge,shin)
    rod('Shin_hydraulic',knee,ankle,.022,steel,shin)
    plate('Long_shin_guard',[(s*.15,.363),(s*.181,.336),(s*.186,.28),(s*.159,.126),
          (s*.14,.111),(s*.13,.246)],-.043,.068,.022,silver,shin)
    cable('Shin_energy',[(s*.156,-.069,.32),(s*.158,-.067,.265),(s*.15,-.046,.209)],.004,eye,shin,4)
    rod('Calf_piston',(s*.173,.065,.319),(s*.179,.077,.123),.012,dark,shin)
    orb('Ankle',ankle,(.031,.032,.028),dark,shin)
    box('Foot_bridge',(s*.16,-.013,.058),(.10,.14,.067),dark,shin,.012)
    for offset in [-.034,0,.034]:
        x=s*.16+offset
        box('Toe',(x,-.086,.029),(.034,.132,.056),silver,shin,.01)
        box('Toe_joint',(x,-.092,.052),(.027,.027,.012),steel,shin,.003)
    plate('Instep',[(s*.125,.084),(s*.145,.13),(s*.182,.11),(s*.203,.049),(s*.129,.055)],
          -.07,.055,.014,silver,shin)

