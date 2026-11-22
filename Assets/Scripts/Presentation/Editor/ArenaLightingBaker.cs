using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ArenaLightingBaker
{
    [MenuItem("Tools/Arena/Bake Lighting")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Nature Square Arena") return;

        var sun = GameObject.Find("Directional Light").GetComponent<Light>();
        sun.transform.rotation = Quaternion.Euler(55f, -55f, 0f);
        sun.color = new Color(1f, 0.96f, 0.89f);
        sun.intensity = 1.25f;
        sun.lightmapBakeType = LightmapBakeType.Mixed;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.9f;
        RenderSettings.sun = sun;

        const string path = "Assets/Maps/NatureSquareArena/Arena Lighting.lighting";
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
        if (settings == null)
        {
            settings = new LightingSettings();
            AssetDatabase.CreateAsset(settings, path);
        }
        settings.name = "Arena Lighting";
        settings.bakedGI = true;
        settings.realtimeGI = false;
        settings.mixedBakeMode = MixedLightingMode.Shadowmask;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
        settings.lightmapResolution = 12f;
        settings.lightmapMaxSize = 2048;
        settings.lightmapPadding = 4;
        settings.directSampleCount = 64;
        settings.indirectSampleCount = 256;
        settings.environmentSampleCount = 128;
        settings.maxBounces = 3;
        settings.ao = true;
        settings.aoMaxDistance = 1f;
        settings.aoExponentIndirect = 0.6f;
        Lightmapping.lightingSettings = settings;

        var models = new HashSet<string>();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.name.StartsWith("Nature Square Arena -")) continue;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                    GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) | StaticEditorFlags.ContributeGI);
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    models.Add(AssetDatabase.GetAssetPath(filter.sharedMesh));
                renderer.receiveGI = ReceiveGI.Lightmaps;
                renderer.scaleInLightmap = 1f;
            }
            foreach (var terrain in root.GetComponentsInChildren<Terrain>())
                GameObjectUtility.SetStaticEditorFlags(terrain.gameObject,
                    GameObjectUtility.GetStaticEditorFlags(terrain.gameObject) | StaticEditorFlags.ContributeGI);
        }
        foreach (string modelPath in models)
        {
            if (AssetImporter.GetAtPath(modelPath) is ModelImporter importer && !importer.generateSecondaryUV)
            {
                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
            }
        }
        var probeObject = GameObject.Find("Arena Light Probes");
        if (probeObject == null) probeObject = new GameObject("Arena Light Probes");
        var probes = probeObject.GetComponent<LightProbeGroup>();
        if (probes == null) probes = probeObject.AddComponent<LightProbeGroup>();
        var positions = new List<Vector3>();
        for (int x = -28; x <= 28; x += 7)
            for (int z = -28; z <= 28; z += 7)
                foreach (float height in new[] { 0.5f, 2f, 4f })
                    positions.Add(new Vector3(x, height, z));
        probes.probePositions = positions.ToArray();
        EditorUtility.SetDirty(settings);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Lightmapping.bakeCompleted -= OnBakeCompleted;
        Lightmapping.bakeCompleted += OnBakeCompleted;
        Lightmapping.BakeAsync();
    }

    private static void OnBakeCompleted()
    {
        Lightmapping.bakeCompleted -= OnBakeCompleted;
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Arena lighting bake completed. Lightmaps: " + LightmapSettings.lightmaps.Length);
    }
}
