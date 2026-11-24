using System;
using System.IO;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ArenaEffectsSetup
{
    [MenuItem("Tools/Effects/Set Up Ice Cross Hit")]
    public static void SetUpIceCross()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing effects.");
        AssetDatabase.ImportAsset("Assets/Shaders/IceCrossImpact.shader", ImportAssetOptions.ForceSynchronousImport);
        string path = AssetFolder + "/IceCrossImpact.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find("Spell/Ice Cross Impact"));
            AssetDatabase.CreateAsset(material, path);
        }
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (GameplayEffect trigger in new[] { GameplayEffect.Hit, GameplayEffect.CriticalHit })
            {
                MMF_Player feedback = root.transform.Find(trigger.ToString()).GetComponent<MMF_Player>();
                if (feedback.transform.Find("Ice cross impact")) continue;
                bool critical = trigger == GameplayEffect.CriticalHit;
                ParticleSystem cross = AddLayer(feedback, "Ice cross impact", material, Color.white,
                    1, 0.16f, 0.26f, critical ? 0.85f : 0.6f, critical ? 1.35f : 1f, 0f, 0f);
                var main = cross.main;
                main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
                var shape = cross.shape;
                shape.enabled = false;
                var size = cross.sizeOverLifetime;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.45f), new Keyframe(0.12f, 1f), new Keyframe(1f, 1.15f)));
                var fade = cross.colorOverLifetime;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0f, 1f) });
                fade.color = gradient;
                EditorUtility.SetDirty(feedback);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private const string AssetFolder = "Assets/Prefab/Effects";
    private const string MaterialPath = AssetFolder + "/FeedbackParticles.mat";
    private const string PrefabPath = AssetFolder + "/ArenaEffects.prefab";

    [MenuItem("Tools/Effects/Set Up Arena Feel")]
    public static void SetUp()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before setting up feedback.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Nature Square Arena")
            throw new InvalidOperationException("Open Nature Square Arena before setting up feedback.");
        if (UnityEngine.Object.FindFirstObjectByType<EffectsManager>())
        {
            Debug.Log("Arena already has an EffectsManager. Edit its feedback players to tune effects.");
            return;
        }

        Directory.CreateDirectory(AssetFolder);
        AssetDatabase.Refresh();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        var root = new GameObject("EffectsManager");
        EffectsManager manager = root.AddComponent<EffectsManager>();
        var serialized = new SerializedObject(manager);
        SerializedProperty bindings = serialized.FindProperty("effects");
        bindings.arraySize = Enum.GetValues(typeof(GameplayEffect)).Length;
        Color ice = new Color(0.3f, 0.85f, 1f, 1f);
        Color gold = new Color(1f, 0.72f, 0.2f, 1f);
        Color damage = new Color(1f, 0.22f, 0.16f, 1f);
        foreach (GameplayEffect trigger in Enum.GetValues(typeof(GameplayEffect)))
        {
            var child = new GameObject(trigger.ToString());
            child.transform.SetParent(root.transform, false);
            var feedback = child.AddComponent<MMF_Player>();
            feedback.InitializationMode = MMFeedbacks.InitializationModes.Script;
            feedback.AutoPlayOnStart = false;
            feedback.AutoPlayOnEnable = false;
            feedback.CanPlayWhileAlreadyPlaying = true;
            bool celebration = trigger == GameplayEffect.LevelUp || trigger == GameplayEffect.RewardSelected
                || trigger == GameplayEffect.MissionComplete;
            bool wave = trigger == GameplayEffect.Wave || trigger == GameplayEffect.BossWave;
            bool strong = celebration || trigger == GameplayEffect.CriticalHit || trigger == GameplayEffect.Kill;
            Color color = celebration ? gold : trigger == GameplayEffect.PlayerDamage || trigger == GameplayEffect.BossWave ? damage : ice;
            int count = trigger == GameplayEffect.Shot ? 5 : strong ? 24 : wave ? 16 : 12;
            float lifetime = celebration ? 0.8f : wave ? 0.65f : 0.35f;
            ParticleSystem particles = CreateParticles(child.transform, material, color, lifetime, wave || celebration);
            feedback.AddFeedback(new MMF_Particles
            {
                Label = "World-space sparks", BoundParticleSystem = particles,
                Mode = MMF_Particles.Modes.Emit, MoveToPosition = true, EmitCount = count,
                DeclaredDuration = lifetime
            });

            string sound = trigger switch
            {
                GameplayEffect.Shot => null,
                GameplayEffect.Hit => "Objects/Hammer1",
                GameplayEffect.CriticalHit => "Objects/Hammer2",
                GameplayEffect.Kill => "SoundFX/Crumbling1",
                GameplayEffect.PlayerDamage => "Objects/Hammer3",
                GameplayEffect.Dodge => "SoundFX/Boost1",
                GameplayEffect.Landing => "Footsteps/FootstepGravel1",
                GameplayEffect.Wave => "ApplicationUX/Beep1",
                GameplayEffect.BossWave => "ApplicationUX/Alert1",
                GameplayEffect.LevelUp => "ApplicationUX/Award1",
                GameplayEffect.RewardSelected => "Objects/Coins2",
                _ => "ApplicationUX/Award1"
            };
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/" + sound + ".wav");
            if (clip && trigger != GameplayEffect.Shot)
                feedback.AddFeedback(new MMF_Sound
                {
                    Label = "Feedback sound", Sfx = clip, PlayMethod = MMF_Sound.PlayMethods.Pool, PoolSize = 3,
                    MinVolume = trigger == GameplayEffect.Shot ? 0.06f : 0.12f,
                    MaxVolume = trigger == GameplayEffect.Shot ? 0.09f : 0.18f,
                    MinPitch = 0.95f, MaxPitch = 1.08f, SpatialBlend = 0f
                });

            float shake = trigger == GameplayEffect.PlayerDamage ? 0.14f
                : trigger == GameplayEffect.BossWave ? 0.18f
                : trigger == GameplayEffect.CriticalHit ? 0.08f
                : trigger == GameplayEffect.Kill ? 0.045f
                : trigger == GameplayEffect.Dodge ? 0.035f : 0f;
            if (shake > 0f)
                feedback.AddFeedback(new MMF_CinemachineImpulse
                {
                    Label = "Restrained impact shake", Velocity = Vector3.down * shake,
                    m_ImpulseDefinition = new CinemachineImpulseDefinition
                    {
                        ImpulseChannel = 1, ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                        ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump, ImpulseDuration = 0.18f
                    }
                }, copy: true);
            if (celebration || trigger == GameplayEffect.CriticalHit || trigger == GameplayEffect.BossWave)
                feedback.AddFeedback(new MMF_Bloom_URP
                {
                    Label = "Brief bloom pulse", ShakeDuration = 0.3f, RelativeValues = true,
                    ModifyIntensity = true, ModifyThreshold = false, RemapIntensityZero = 0f,
                    RemapIntensityOne = celebration ? 0.45f : 0.2f,
                    ResetTargetValuesAfterShake = true, ResetShakerValuesAfterShake = true
                });
            EditorUtility.SetDirty(feedback);
            SerializedProperty binding = bindings.GetArrayElementAtIndex((int)trigger);
            binding.FindPropertyRelative("trigger").enumValueIndex = (int)trigger;
            binding.FindPropertyRelative("enabled").boolValue = true;
            binding.FindPropertyRelative("feedback").objectReferenceValue = feedback;
            binding.FindPropertyRelative("cooldown").floatValue = wave || celebration ? 0.5f : 0.08f;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        UpgradeVisuals();
        root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        serialized = new SerializedObject(root.GetComponent<EffectsManager>());
        serialized.FindProperty("player").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        serialized.FindProperty("entityManager").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<EntityManager>();
        serialized.FindProperty("missionManager").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<MissionManager>();
        serialized.FindProperty("rewardManager").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<RewardManager>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var camera = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
        if (camera && !camera.GetComponent<CinemachineImpulseListener>())
        {
            var listener = Undo.AddComponent<CinemachineImpulseListener>(camera.gameObject);
            listener.ChannelMask = 1;
            listener.Gain = 1f;
        }
        foreach (Volume volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (!volume.isGlobal || !volume.sharedProfile || !volume.sharedProfile.TryGet<Bloom>(out _)) continue;
            if (!volume.GetComponent<MMBloomShaker_URP>()) Undo.AddComponent<MMBloomShaker_URP>(volume.gameObject);
            break;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("Arena Feel setup complete: 12 event-driven feedback players, Cinemachine impulse and bloom receiver.");
    }

    [MenuItem("Tools/Effects/Upgrade Arena Visual Feedback")]
    public static void UpgradeVisuals()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing feedback assets.");
        const string pack = "Assets/UnityTechnologies/ParticlePack/EffectExamples/";
        Material ice = CreateSurface("IceFragments", "Universal Render Pipeline/Particles/Lit",
            pack + "Magic Effects/Textures/IceGrey.png");
        ice.SetFloat("_Smoothness", 0.88f);
        ice.SetColor("_BaseColor", new Color(0.78f, 0.88f, 0.92f, 1f));
        ice.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(pack + "Magic Effects/Textures/IceNormals.tif"));
        ice.EnableKeyword("_NORMALMAP");
        ice.SetFloat("_BumpScale", 0.35f);
        Material mist = CreateSurface("ColdMist", "Universal Render Pipeline/Particles/Unlit",
            pack + "Smoke & Steam Effects/Textures/SmokePuff.png");
        Material frost = CreateSurface("GroundFrost", "Universal Render Pipeline/Particles/Unlit", "Assets/VFX/FrostPatch.png");
        Mesh shard = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(pack + "Magic Effects/Models/IceShard.FBX"))
            if (asset is Mesh mesh) { shard = mesh; break; }
        if (!shard) throw new InvalidOperationException("ParticlePack IceShard mesh is missing.");
        float meshSize = Mathf.Max(shard.bounds.size.x, shard.bounds.size.y, shard.bounds.size.z);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (MMF_Player feedback in root.GetComponentsInChildren<MMF_Player>(true))
            {
                if (!Enum.TryParse(feedback.name, out GameplayEffect trigger)) continue;
                // Rebuild only visual layers; keep authored sound and camera feedback.
                feedback.FeedbacksList.RemoveAll(effect => effect is MMF_Particles);
                foreach (ParticleSystem old in feedback.GetComponentsInChildren<ParticleSystem>(true))
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (MMF_Feedback effect in feedback.FeedbacksList)
                    if (effect is MMF_Bloom_URP) effect.Active = false;
                bool shot = trigger == GameplayEffect.Shot;
                bool ground = trigger == GameplayEffect.Dodge || trigger == GameplayEffect.Landing;
                bool impact = trigger == GameplayEffect.Hit || trigger == GameplayEffect.CriticalHit || trigger == GameplayEffect.Kill;
                bool strong = trigger == GameplayEffect.CriticalHit || trigger == GameplayEffect.Kill;
                bool celebration = trigger == GameplayEffect.LevelUp || trigger == GameplayEffect.RewardSelected || trigger == GameplayEffect.MissionComplete;
                Color tint = celebration ? new Color(0.75f, 0.65f, 0.42f) : new Color(0.7f, 0.82f, 0.88f);
                if (trigger == GameplayEffect.PlayerDamage || trigger == GameplayEffect.BossWave)
                    tint = new Color(0.46f, 0.36f, 0.32f);
                if (shot || impact || ground)
                {
                    ParticleSystem fragments = AddLayer(feedback, "Ice fragments", ice, Color.white,
                        strong ? 22 : ground ? 14 : shot ? 4 : 12, 0.45f, strong ? 1.05f : 0.8f,
                        0.12f / meshSize, (strong ? 0.5f : 0.3f) / meshSize, shot ? 0.4f : 1.2f, strong ? 4.5f : 2.8f);
                    var renderer = fragments.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.mesh = shard;
                    var main = fragments.main;
                    main.gravityModifier = 0.75f;
                    main.startRotation3D = true;
                    main.startRotationX = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
                    main.startRotationY = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
                    var rotation = fragments.rotationOverLifetime;
                    rotation.enabled = true;
                    rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
                    var limit = fragments.limitVelocityOverLifetime;
                    limit.enabled = true;
                    limit.limit = 2.5f;
                    limit.dampen = 0.3f;
                    if (ground)
                    {
                        var shape = fragments.shape;
                        shape.shapeType = ParticleSystemShapeType.Hemisphere;
                        shape.rotation = new Vector3(-90f, 0f, 0f);
                    }
                }
                ParticleSystem vapor = AddLayer(feedback, "Cold vapor", mist, new Color(tint.r, tint.g, tint.b, shot ? 0.15f : 0.38f),
                    shot ? 2 : strong ? 9 : 6, 0.35f, shot ? 0.5f : 1.25f, shot ? 0.2f : 0.5f,
                    strong ? 1.6f : 0.9f, 0.12f, ground ? 1.1f : 0.65f);
                var vaporMain = vapor.main;
                vaporMain.gravityModifier = -0.015f;
                var vaporSize = vapor.sizeOverLifetime;
                vaporSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.4f));
                var drift = vapor.noise;
                drift.enabled = true;
                drift.strength = 0.18f;
                drift.frequency = 0.7f;
                drift.scrollSpeed = 0.25f;
                if (impact && frost.GetTexture("_BaseMap"))
                {
                    AddLayer(feedback, "Crystal fracture", frost, new Color(0.82f, 0.92f, 1f, 0.55f),
                        1, 0.16f, 0.22f, strong ? 1.3f : 0.8f, strong ? 1.6f : 1f, 0f, 0f);
                }
                if (ground && frost.GetTexture("_BaseMap"))
                {
                    ParticleSystem patch = AddLayer(feedback, "Settled frost", frost, new Color(0.8f, 0.88f, 0.9f, 0.65f),
                        1, 1.6f, 2f, 1.3f, 1.8f, 0f, 0f);
                    var shape = patch.shape;
                    shape.enabled = false;
                    patch.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                }
                EditorUtility.SetDirty(feedback);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EditorUtility.SetDirty(ice);
        AssetDatabase.SaveAssets();
        Debug.Log("Arena visuals rebuilt with lit ice fragments, cold vapor and settled frost; arcade rings removed.");
    }

    private static Material CreateSurface(string name, string shaderName, string texturePath)
    {
        string path = AssetFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static ParticleSystem AddLayer(MMF_Player feedback, string name, Material material, Color color,
        int count, float minLife, float maxLife, float minSize, float maxSize, float minSpeed, float maxSpeed)
    {
        ParticleSystem system = CreateParticles(feedback.transform, material, color, maxLife, false);
        system.name = name;
        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.gravityModifier = 0f;
        main.maxParticles = 128;
        var fade = system.colorOverLifetime;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.06f), new GradientAlphaKey(0.7f, 0.45f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        feedback.AddFeedback(new MMF_Particles
        {
            Label = name, BoundParticleSystem = system, Mode = MMF_Particles.Modes.Emit,
            MoveToPosition = true, EmitCount = count, DeclaredDuration = maxLife
        });
        return system;
    }

    private static ParticleSystem CreateParticles(Transform parent, Material material, Color color, float lifetime, bool ring)
    {
        var particlesObject = new GameObject("Particles");
        particlesObject.transform.SetParent(parent, false);
        var particles = particlesObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(ring ? 2f : 0.6f, ring ? 4f : 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, ring ? 0.12f : 0.085f);
        main.startColor = color;
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = ring ? 0f : 0.25f;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ring ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Sphere;
        shape.radius = ring ? 0.4f : 0.08f;
        shape.rotation = new Vector3(90f, 0f, 0f);
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }
}
