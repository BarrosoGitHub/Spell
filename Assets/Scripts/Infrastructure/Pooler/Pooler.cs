using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Pooler : MonoBehaviour
{
    [Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public int maxSize;
    }

    public static Pooler Instance;


    private void Awake()
    {
        Instance = this;

        poolDictionary = new();

        foreach (Pool pool in pools)
        {
            ObjectPool<GameObject> objectPool = new(() => 
                {
                    GameObject instance = Instantiate(pool.prefab);
                    return instance;
                }, obj => 
                {
                    obj.SetActive(true);
                }, obj => 
                {
                    obj.SetActive(false);
                }, obj => 
                {
                    DestroyPooledObject(obj);
                }, false, pool.size, pool.maxSize
            );

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public List<Pool> pools;
    public static Dictionary<string, ObjectPool<GameObject>> poolDictionary;

    static void DestroyPooledObject(GameObject pooledObject)
    {
        if (!pooledObject)
            return;

        // ObjectPool can trim its inactive collection while Unity is changing
        // from Play mode back to Edit mode. Destroy is illegal at that point.
        if (Application.isPlaying)
            Destroy(pooledObject);
        else
            DestroyImmediate(pooledObject);
    }

    public static GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        GameObject gameObject = poolDictionary[tag].Get();
        gameObject.transform.SetPositionAndRotation(position, rotation);

        return gameObject;
    }

    public static void DespawnToPool(string tag, GameObject objectToDespawn)
    {
        poolDictionary[tag].Release(objectToDespawn);
    }
}
