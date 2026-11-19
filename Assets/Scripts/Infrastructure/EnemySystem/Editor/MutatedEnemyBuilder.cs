using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MutatedEnemyBuilder
{
    private const string Output = "Assets/Prefab/Enemies/Enemy_mutated.prefab";
    private const string Model = "Assets/Models/Enemy/Mutated/Meshy_AI_Crimson_Revenant_20261118_animated_mixamo_RunFast.fbx";

    [MenuItem("Tools/Enemies/Create Mutated Enemy")]
    public static void Create()
    {
        if (Application.isPlaying || AssetDatabase.LoadAssetAtPath<GameObject>(Output) != null) return;
        GameObject enemy = PrefabUtility.LoadPrefabContents("Assets/Prefab/Enemies/Enemy_normal.prefab");
        GameObject model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
        try
        {
            enemy.name = "Enemy_mutated";
            enemy.GetComponentInChildren<EnemyController>(true).enemyType = EnemyType.Mutated;
            SkinnedMeshRenderer source = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(renderer => renderer.bones.Count(bone => bone.name.Contains("mixamorig"))).First();
            SkinnedMeshRenderer destination = enemy.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Transform[] hierarchy = enemy.GetComponentsInChildren<Transform>(true);
            Transform[] bones = new Transform[source.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                string name = source.bones[i].name;
                bones[i] = FindOrCreateBone(source.bones[i], enemy.transform);
                bones[i].localPosition = source.bones[i].localPosition;
                bones[i].localRotation = source.bones[i].localRotation;
                bones[i].localScale = source.bones[i].localScale;
            }
            destination.sharedMesh = source.sharedMesh;
            destination.bones = bones;
            destination.rootBone = hierarchy.First(bone => bone.name == source.rootBone.name);
            destination.localBounds = source.localBounds;
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Mutated";
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Models/Enemy/Mutated/Meshy_AI_Crimson_Revenant_20261118_animated_mixamo_RunFast.png"));
            material.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(material, "Assets/Materials/Characters/Materials/Mutated.mat");
            destination.sharedMaterials = Enumerable.Repeat(material, source.sharedMaterials.Length).ToArray();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, Output);
            foreach (Pooler pooler in UnityEngine.Object.FindObjectsByType<Pooler>(FindObjectsInactive.Include))
            {
                if (pooler.pools.Exists(pool => pool.tag == "Mutated")) continue;
                pooler.pools.Add(new Pooler.Pool { tag = "Mutated", prefab = prefab, size = 5, maxSize = 100 });
                PrefabUtility.RecordPrefabInstancePropertyModifications(pooler);
                EditorUtility.SetDirty(pooler);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pooler.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(pooler.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Created Mutated enemy prefab with normal enemy behavior and animations.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(enemy);
            UnityEngine.Object.DestroyImmediate(model);
        }
    }
    private static Transform FindOrCreateBone(Transform source, Transform enemy)
    {
        Transform existing = enemy.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(bone => bone.name == source.name);
        if (existing != null) return existing;
        Transform parent = source.parent != null ? FindOrCreateBone(source.parent, enemy) : enemy;
        Transform boneTransform = new GameObject(source.name).transform;
        boneTransform.SetParent(parent, false);
        boneTransform.localPosition = source.localPosition;
        boneTransform.localRotation = source.localRotation;
        boneTransform.localScale = source.localScale;
        return boneTransform;
    }

}
