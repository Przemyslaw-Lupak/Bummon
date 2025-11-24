using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class LobbyService : ITickable
{    
    public Lobby CurrentLobby { get; private set; }
    private readonly ServicesInitializer _servicesInitializer;
    public bool IsHost => CurrentLobby != null && CurrentLobby.HostId == _servicesInitializer.PlayerId;
    
    private float _heartbeatTimer;
    private float _lobbyUpdateTimer;
    private const float HEARTBEAT_INTERVAL = 15f; // Lobby expires without heartbeat every 30 seconds
    private const float LOBBY_UPDATE_INTERVAL = 1.1f; // Poll for lobby updates
    
    [Inject]
    public LobbyService(ServicesInitializer servicesInitializer)
    {
        _servicesInitializer = servicesInitializer;
    }

    public void Tick()
    {
        if (CurrentLobby == null) return;
        
        // Send heartbeat if host
        if (IsHost)
        {
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _heartbeatTimer = 0f;
                SendHeartbeatAsync();
            }
        }
        
        // Poll for lobby updates
        _lobbyUpdateTimer += Time.deltaTime;
        if (_lobbyUpdateTimer >= LOBBY_UPDATE_INTERVAL)
        {
            _lobbyUpdateTimer = 0f;
            PollLobbyAsync();
        }
    }
    
    
    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, string relayJoinCode)
    {
        try
        {
            Debug.Log($"[LobbyManager] Creating lobby: {lobbyName} (Max: {maxPlayers}, Private: {isPrivate})");
            
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
                }
            };

            CurrentLobby = await Unity.Services.Lobbies.LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            
            Debug.Log($"[LobbyManager] Lobby created! Code: {CurrentLobby.LobbyCode}");
            return CurrentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Failed to create lobby: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Joins a lobby by code
    /// </summary>
    public async Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode)
    {
        try
        {
            Debug.Log($"[LobbyManager] Joining lobby with code: {lobbyCode}");
            
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            CurrentLobby = await Unity.Services.Lobbies.LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            
            Debug.Log($"[LobbyManager] Joined lobby: {CurrentLobby.Name}");
            return CurrentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Failed to join lobby: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Queries available public lobbies
    /// </summary>
    public async Task<List<Lobby>> QueryLobbiesAsync()
    {
        try
        {
            Debug.Log("[LobbyManager] Querying lobbies...");
            
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse response = await Unity.Services.Lobbies.LobbyService.Instance.QueryLobbiesAsync(options);
            
            Debug.Log($"[LobbyManager] Found {response.Results.Count} lobbies");
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Failed to query lobbies: {e.Message}");
            return new List<Lobby>();
        }
    }
    
    /// <summary>
    /// Leaves the current lobby
    /// </summary>
    public async Task LeaveLobbyAsync()
    {
        if (CurrentLobby == null) return;
        
        try
        {
            string lobbyId = CurrentLobby.Id;
            string playerId = _servicesInitializer.PlayerId;
            
            await Unity.Services.Lobbies.LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
            
            Debug.Log("[LobbyManager] Left lobby");
            CurrentLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Failed to leave lobby: {e.Message}");
        }
    }
    
    /// <summary>
    /// Deletes the lobby (host only)
    /// </summary>
    public async Task DeleteLobbyAsync()
    {
        if (CurrentLobby == null || !IsHost) return;
        
        try
        {
            await Unity.Services.Lobbies.LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
            Debug.Log("[LobbyManager] Lobby deleted");
            CurrentLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Failed to delete lobby: {e.Message}");
        }
    }
    
    /// <summary>
    /// Gets the Relay join code stored in lobby data
    /// </summary>
    public string GetRelayJoinCode()
    {
        if (CurrentLobby?.Data != null && CurrentLobby.Data.ContainsKey("RelayJoinCode"))
        {
            return CurrentLobby.Data["RelayJoinCode"].Value;
        }
        return null;
    }
    
    // Private helper methods
    
    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _servicesInitializer.PlayerName) }
            }
        };
    }
    
    private async void SendHeartbeatAsync()
    {
        try
        {
            await Unity.Services.Lobbies.LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Heartbeat failed: {e.Message}");
        }
    }
    
    private async void PollLobbyAsync()
    {
        try
        {
            CurrentLobby = await Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Lobby poll failed: {e.Message}");
        }
    }
    
    public async Task OnDestroy()
    {
        // Clean up on quit
        if (CurrentLobby != null)
        {
            if (IsHost)
                await DeleteLobbyAsync();
            else
                await LeaveLobbyAsync();
        }
    }
}