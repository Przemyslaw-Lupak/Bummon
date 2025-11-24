using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class PlaygroundView : NetworkBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject playerPrefab;
    
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";
    
    private NetworkList<PlayerLobbyData> _lobbyPlayers;
    private int _nextSpawnIndex = 0;

    private readonly LobbyService _lobbyService;
    private readonly ServicesInitializer _servicesInitializer;

    [Inject]
    PlaygroundView(LobbyService lobbyService, ServicesInitializer servicesInitializer)
    {
        _lobbyService = lobbyService;
        _servicesInitializer = servicesInitializer;
    }
    
    public struct PlayerLobbyData : INetworkSerializable, System.IEquatable<PlayerLobbyData>
    {
        public ulong ClientId;
        public FixedString64Bytes PlayerName;
        public bool IsReady;
        public bool IsHost;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref IsHost);
        }
        
        public bool Equals(PlayerLobbyData other)
        {
            return ClientId == other.ClientId &&
                   PlayerName.Equals(other.PlayerName) &&
                   IsReady == other.IsReady &&
                   IsHost == other.IsHost;
        }
    }
    
    void Awake()
    {
        _lobbyPlayers = new NetworkList<PlayerLobbyData>();
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Subscribe to connection events
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // Add host to lobby
            AddPlayerToLobby(NetworkManager.ServerClientId, true);
        }
        
        // Subscribe to lobby changes
        _lobbyPlayers.OnListChanged += OnLobbyPlayersChanged;
        
        // Update UI immediately
        UpdateLobbyUI();
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        _lobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[PlaygroundManager] Client {clientId} connected");
        
        // Add player to lobby
        AddPlayerToLobby(clientId, false);
        
        // Spawn player at next spawn point
        SpawnPlayer(clientId);
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[PlaygroundManager] Client {clientId} disconnected");
        
        // Remove from lobby
        RemovePlayerFromLobby(clientId);
    }
    
    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        
        // Get spawn point
        Transform spawnPoint = GetNextSpawnPoint();
        
        // Spawn player
        GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
        
        // Spawn as player object for this client
        netObj.SpawnAsPlayerObject(clientId);
        
        Debug.Log($"[PlaygroundManager] Spawned player for client {clientId} at {spawnPoint.position}");
    }
    
    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[PlaygroundManager] No spawn points! Using world origin.");
            return null;
        }
        
        Transform spawnPoint = spawnPoints[_nextSpawnIndex];
        _nextSpawnIndex = (_nextSpawnIndex + 1) % spawnPoints.Length;
        return spawnPoint;
    }
    
    private void AddPlayerToLobby(ulong clientId, bool isHost)
    {
        if (!IsServer) return;
        
        string playerName = _servicesInitializer.PlayerName;
        
        var playerData = new PlayerLobbyData
        {
            ClientId = clientId,
            PlayerName = playerName,
            IsReady = false,
            IsHost = isHost
        };
        
        _lobbyPlayers.Add(playerData);
        Debug.Log($"[PlaygroundManager] Added {playerName} to lobby (Host: {isHost})");
    }
    
    private void RemovePlayerFromLobby(ulong clientId)
    {
        if (!IsServer) return;
        
        for (int i = 0; i < _lobbyPlayers.Count; i++)
        {
            if (_lobbyPlayers[i].ClientId == clientId)
            {
                _lobbyPlayers.RemoveAt(i);
                Debug.Log($"[PlaygroundManager] Removed client {clientId} from lobby");
                return;
            }
        }
    }
    
    private void OnLobbyPlayersChanged(NetworkListEvent<PlayerLobbyData> changeEvent)
    {
        UpdateLobbyUI();
    }
    
    private void UpdateLobbyUI()
    {
        // Notify UI to update
        PlaygroundUI playgroundUI = FindObjectOfType<PlaygroundUI>();
        if (playgroundUI != null)
        {
            playgroundUI.UpdatePlayerList(_lobbyPlayers);
        }
    }
    
    // Called from UI when player clicks Ready
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientId, bool isReady, ServerRpcParams serverRpcParams = default)
    {
        // Verify the RPC came from the correct client
        if (serverRpcParams.Receive.SenderClientId != clientId)
        {
            Debug.LogWarning($"[PlaygroundManager] Client {serverRpcParams.Receive.SenderClientId} tried to set ready for client {clientId}");
            return;
        }
        
        for (int i = 0; i < _lobbyPlayers.Count; i++)
        {
            if (_lobbyPlayers[i].ClientId == clientId)
            {
                var player = _lobbyPlayers[i];
                player.IsReady = isReady;
                _lobbyPlayers[i] = player;
                
                Debug.Log($"[PlaygroundManager] Client {clientId} ready: {isReady}");
                
                // Check if all ready
                CheckAllPlayersReady();
                return;
            }
        }
    }
    
    private void CheckAllPlayersReady()
    {
        if (!IsServer) return;
        
        bool allReady = true;
        foreach (var player in _lobbyPlayers)
        {
            if (!player.IsReady)
            {
                allReady = false;
                break;
            }
        }
        
        // Notify UI about ready state
        UpdateStartButtonStateClientRpc(allReady);
    }
    
    [ClientRpc]
    private void UpdateStartButtonStateClientRpc(bool canStart)
    {
        PlaygroundUI playgroundUI = FindObjectOfType<PlaygroundUI>();
        if (playgroundUI != null)
        {
            playgroundUI.SetStartButtonEnabled(canStart);
        }
    }
    
    // Called from UI when host clicks Start Game
    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Verify caller is host
        if (serverRpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
        {
            Debug.LogWarning($"[PlaygroundManager] Non-host tried to start game!");
            return;
        }
        
        // Check all players ready
        foreach (var player in _lobbyPlayers)
        {
            if (!player.IsReady)
            {
                Debug.LogWarning($"[PlaygroundManager] Not all players ready!");
                return;
            }
        }
        
        Debug.Log($"[PlaygroundManager] Starting game! Loading {gameSceneName}");
        
        // Show countdown
        ShowCountdownClientRpc();
        
        // Load game scene after delay
        StartCoroutine(LoadGameSceneAfterDelay(3f));
    }
    
    [ClientRpc]
    private void ShowCountdownClientRpc()
    {
        PlaygroundUI playgroundUI = FindObjectOfType<PlaygroundUI>();
        if (playgroundUI != null)
        {
            playgroundUI.ShowCountdown();
        }
    }
    
    private IEnumerator LoadGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Load game scene
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
    
    // Public getters for UI
    public NetworkList<PlayerLobbyData> GetLobbyPlayers() => _lobbyPlayers;
    
    public bool IsLocalPlayerHost()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        return localClientId == NetworkManager.ServerClientId;
    }
    
    public string GetLobbyCode()
    {
        if (_lobbyService.CurrentLobby != null)
        {
            return _lobbyService.CurrentLobby.LobbyCode;
        }
        return "N/A";
    }
}