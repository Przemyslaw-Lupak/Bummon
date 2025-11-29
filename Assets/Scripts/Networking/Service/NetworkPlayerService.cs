using System;
using System.Collections.Generic;
using Unity.Netcode;

public class NetworkPlayerService : IDisposable
{    
    private Dictionary<ulong, PlayerNetworkDataView> _players = new Dictionary<ulong, PlayerNetworkDataView>();

    public event Action<PlayerNetworkDataView> OnPlayerJoined;
    public event Action<PlayerNetworkDataView> OnPlayerLeft;
    public event Action OnPlayersChanged;
    
    public NetworkPlayerService()
    {
        NetworkEventBus.OnPlayerSpawned += RegisterPlayer;
        NetworkEventBus.OnPlayerDespawned += UnregisterPlayer;
    }
    
    public void Dispose()
    {
        NetworkEventBus.OnPlayerSpawned -= RegisterPlayer;
        NetworkEventBus.OnPlayerDespawned -= UnregisterPlayer;
        
        // Clear all players
        foreach (var player in _players.Values)
        {
            if (player != null)
            {
                player.OnDataChanged -= HandlePlayerDataChanged;
            }
        }
        _players.Clear();
    }

    public void RegisterPlayer(PlayerNetworkDataView player)
    {
        if(player == null) return;

        ulong clientId = player.OwnerClientId;

        if (!_players.ContainsKey(clientId))
        {
            _players[clientId] = player;
            player.OnDataChanged += HandlePlayerDataChanged;

            OnPlayerJoined?.Invoke(player);
            OnPlayersChanged?.Invoke();
        }
    }

    public void UnregisterPlayer(PlayerNetworkDataView player)
    {
        if(player == null) return;

        ulong clientId = player.OwnerClientId;

        if (_players.Remove(clientId))
        {
            player.OnDataChanged -= HandlePlayerDataChanged;

            OnPlayerLeft?.Invoke(player);
            OnPlayersChanged?.Invoke();
        }
    }

    private void HandlePlayerDataChanged()
    {
        OnPlayersChanged?.Invoke();
    }

    public List<PlayerNetworkDataView> GetAllPlayers()
    {
        return new List<PlayerNetworkDataView>(_players.Values);
    }

    public PlayerNetworkDataView GetPlayer(ulong clientId)
    {
        return _players.TryGetValue(clientId, out var player) ? player : null;
    }

    public PlayerNetworkDataView GetLocalPlayer()
    {
        if(NetworkManager.Singleton == null) return null;
        return GetPlayer(NetworkManager.Singleton.LocalClientId);
    }

    public int GetPlayerCount()
    {
        return _players.Count;
    }
    
    public bool AreAllPlayersReady()
    {
        if(_players.Count == 0) return false;

        foreach(var player in _players.Values)
        {
            if(!player.IsReady.Value) return false;
        } 
        return true;
    }
}