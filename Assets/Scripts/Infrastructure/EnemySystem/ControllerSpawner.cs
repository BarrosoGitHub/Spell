using System;
using System.Collections.Generic;
using UnityEngine;

public class ControllerSpawner : MonoBehaviour
{
    public Action<Controller> OnControllerSpawned;
    public Action<Controller> OnControllerDespawned;

    public List<Controller> SpawnedControllers { get; private set; } = new List<Controller>();
    public Controller SpawnController(string tag, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        GameObject gameObject = Pooler.SpawnFromPool(tag, spawnPosition, spawnRotation);
        Controller controller = gameObject.GetComponentInChildren<Controller>();
        if (controller != null)
        {
            SpawnedControllers.Add(controller);
            OnControllerSpawned?.Invoke(controller);
            // controller.OnControllerSpawn();
        }

        return controller;
    }

    public void DespawnController(string tag, Controller controller)
    {
        if (controller != null)
        {
            SpawnedControllers.Remove(controller);
            Pooler.DespawnToPool(tag, controller.transform.root.gameObject);

            OnControllerDespawned?.Invoke(controller);
        }
    }
}
