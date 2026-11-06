using UnityEngine;
using Unity.Netcode;

public class Client : NetworkBehaviour
{
    [SerializeField] private Controller controller;
    
    private void Awake()
    {
        controller.enabled = false;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            controller.enabled = true;
        }
    }
}
