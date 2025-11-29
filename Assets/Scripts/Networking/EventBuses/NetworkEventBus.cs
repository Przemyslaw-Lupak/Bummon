using System;
using UnityEngine;

public static class NetworkEventBus
{
    public static event Action<PlayerNetworkDataView> OnPlayerSpawned;
    public static event Action<PlayerNetworkDataView> OnPlayerDespawned;

    public static void PublishPlayerSpawned(PlayerNetworkDataView player)
    {
        OnPlayerSpawned?.Invoke(player);
    }
    
    public static void PublishPlayerDespawned(PlayerNetworkDataView player)
    {
        OnPlayerDespawned?.Invoke(player);
    }
    
    public static void Clear()
    {
        OnPlayerSpawned = null;
        OnPlayerDespawned = null;
    }
    
}
