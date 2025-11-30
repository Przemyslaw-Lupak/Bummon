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
    private readonly PlayerIdentityService _playerIdentityService;
    
    public bool IsHost => CurrentLobby != null && 
                          CurrentLobby.HostId == _playerIdentityService.PlayerId;
    
    private float _heartbeatTimer;
    private float _lobbyUpdateTimer;
    private const float HEARTBEAT_INTERVAL = 15f;
    private const float LOBBY_UPDATE_INTERVAL = 1.1f;
    
    [Inject]
    public LobbyService(PlayerIdentityService playerIdentityService)
    {
        _playerIdentityService = playerIdentityService;
    }

    public void Tick()
    {
        if (CurrentLobby == null) return;
        
        if (IsHost)
        {
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _heartbeatTimer = 0f;
                SendHeartbeatAsync();
            }
        }
        
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
            
            return CurrentLobby;
        }
        catch (LobbyServiceException e)
        {
            return null;
        }
    }
    
    public async Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode)
    {
        try
        {
            
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            CurrentLobby = await Unity.Services.Lobbies.LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            
            return CurrentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to join lobby: {e.Message}");
            return null;
        }
    }
    
    public async Task<List<Lobby>> QueryLobbiesAsync()
    {
        try
        {
            
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
            
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to query lobbies: {e.Message}");
            return new List<Lobby>();
        }
    }

    public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId)
    {
        try
        {            
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };

            CurrentLobby = await Unity.Services.Lobbies.LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            
            return CurrentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to join lobby: {e.Message}");
            return null;
        }
    }
    
    public async Task LeaveLobbyAsync()
    {
        if (CurrentLobby == null) return;
        
        try
        {
            string lobbyId = CurrentLobby.Id;
            string playerId = _playerIdentityService.PlayerId;
            
            await Unity.Services.Lobbies.LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
            
            Debug.Log("[LobbyService] Left lobby");
            CurrentLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to leave lobby: {e.Message}");
        }
    }
    
    public async Task DeleteLobbyAsync()
    {
        if (CurrentLobby == null || !IsHost) return;
        
        try
        {
            await Unity.Services.Lobbies.LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
            Debug.Log("[LobbyService] Lobby deleted");
            CurrentLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to delete lobby: {e.Message}");
        }
    }
    
    public string GetRelayJoinCode()
    {
        if (CurrentLobby?.Data != null && CurrentLobby.Data.ContainsKey("RelayJoinCode"))
        {
            return CurrentLobby.Data["RelayJoinCode"].Value;
        }
        return null;
    }
    
    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _playerIdentityService.PlayerName) }
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
            Debug.LogError($"[LobbyService] Heartbeat failed: {e.Message}");
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
            Debug.LogError($"[LobbyService] Lobby poll failed: {e.Message}");
        }
    }
    
    public async Task SetLobbyPrivacyAsync(bool privateStatus)
    {
        if (CurrentLobby == null) return;
        if (!IsHost) return;
        
        try
        {            
            var options = new UpdateLobbyOptions
            {
                IsPrivate = privateStatus
            };

            CurrentLobby = await Unity.Services.Lobbies.LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);        
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to update lobby privacy: {e.Message}");
        }
    }
    
    public async Task OnDestroy()
    {
        if (CurrentLobby != null)
        {
            if (IsHost)
                await DeleteLobbyAsync();
            else
                await LeaveLobbyAsync();
        }
    }
}