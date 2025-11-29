using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using VContainer;

public class LobbyView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private PlayerListView playerListView;
    [SerializeField] private GameObject readyButton;
    [SerializeField] private GameObject startButton;
    
    private LobbyService _lobbyService;
    private NetworkPlayerService _networkPlayerService; // ✅ Z MainScope
    
    [Inject]
    public void Construct(LobbyService lobbyService, NetworkPlayerService networkPlayerService)
    {
        _lobbyService = lobbyService;
        _networkPlayerService = networkPlayerService;
    }
    
    void Start()
    {
        _networkPlayerService.OnPlayerJoined += OnPlayerJoined;
        _networkPlayerService.OnPlayerLeft += OnPlayerLeft;
        _networkPlayerService.OnPlayersChanged += OnPlayersChanged;
                
        RefreshUI();
    }
    
    void OnDestroy()
    {
        if (_networkPlayerService != null)
        {
            _networkPlayerService.OnPlayerJoined -= OnPlayerJoined;
            _networkPlayerService.OnPlayerLeft -= OnPlayerLeft;
            _networkPlayerService.OnPlayersChanged -= OnPlayersChanged;
        }
    }
    
    private void OnPlayerJoined(PlayerNetworkDataView player)
    {
        Debug.Log($"[LobbyView] Player joined: {player.PlayerName.Value}");
        RefreshUI();
    }
    
    private void OnPlayerLeft(PlayerNetworkDataView player)
    {
        RefreshUI();
    }
    
    private void OnPlayersChanged()
    {
        RefreshUI();
    }
    
    private void RefreshUI()
    {
        if (_networkPlayerService == null)
        {
            return;
        }
        
        // Get current players
        var players = _networkPlayerService.GetAllPlayers();
        
        // Update player list UI
        if (playerListView != null)
        {
            playerListView.UpdatePlayerList(players);
        }
        
        // Update buttons
        UpdateButtons();
    }
    
    private void UpdateButtons()
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        bool allReady = _networkPlayerService.AreAllPlayersReady();
        int playerCount = _networkPlayerService.GetPlayerCount();
        
        if (readyButton != null)
        {
            readyButton.SetActive(!isHost);
        }
        
        if (startButton != null)
        {
            startButton.SetActive(isHost && allReady && playerCount > 0);
        }
    }
    
    public void OnReadyButtonClicked()
    {
        var localPlayer = _networkPlayerService.GetLocalPlayer();
        if (localPlayer != null)
        {
            bool newReadyState = !localPlayer.IsReady.Value;
            localPlayer.SetReadyServerRpc(newReadyState);
        }
    }
    
    public void OnStartButtonClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    
    public async void OnLeaveButtonClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            
            if (_lobbyService != null)
            {
                await _lobbyService.LeaveLobbyAsync();
            }
            
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}