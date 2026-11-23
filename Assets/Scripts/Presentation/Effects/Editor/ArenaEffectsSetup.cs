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
                GameplayEffect.Shot => "SoundFX/IceCubes1",
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
            if (clip)
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
