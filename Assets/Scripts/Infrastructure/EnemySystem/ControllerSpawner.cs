using System;
using UnityEngine;

public class ControllerSpawner : MonoBehaviour
{
    public Action<Controller> OnControllerSpawned;
    public Action<Controller> OnControllerDespawned;

    void Awake()
    {

    }

    public Controller SpawnController(string tag, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        GameObject gameObject = Pooler.SpawnFromPool(tag, spawnPosition, spawnRotation);
        Controller controller = gameObject.GetComponentInChildren<Controller>();
        if (controller != null)
        {
            OnControllerSpawned?.Invoke(controller);
            // controller.OnControllerSpawn();
        }

        return controller;
    }

    public void DespawnController(string tag, Controller controller)
    {
        if (controller != null)
        {
            Pooler.DespawnToPool(tag, controller.transform.root.gameObject);

            OnControllerDespawned?.Invoke(controller);
        }
    }
}
