using Unity.Netcode;
using UnityEngine;

public class ConnectionView : NetworkBehaviour
{
    [SerializeField] private GameObject playerNetworkDataPrefab;

    public override void OnNetworkSpawn()
    {
        if(!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        foreach(var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerData(clientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        if(!IsServer) return;

        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        SpawnPlayerData(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("Disconnected");
    }

    private void SpawnPlayerData(ulong clientId)
    {
        if(playerNetworkDataPrefab == null)
        {
            Debug.LogError("[ConnectionManager] Player prefab not assigned");
            return;
        }

        if(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) != null)
        {
            Debug.LogError("[ConnectionManager] Player Data already spawned");
            return;
        }

        GameObject playerDataObj = Instantiate(playerNetworkDataPrefab);
        NetworkObject netObj = playerDataObj.GetComponent<NetworkObject>();

        netObj.SpawnAsPlayerObject(clientId, true);
        Debug.Log("[ConncetionManager] Spawn PlayerNetworkData");
    }
}
