using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class PlaygroundUIView : MonoBehaviour
{
    [Header("Top Panel")]
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    
    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    
    [Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    
    [Header("Countdown")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    
    private PlaygroundView _playgroundManager;
    private bool _isReady = false;
    private readonly LobbyService _lobbyService;

    [Inject]
    public PlaygroundUIView(LobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }
    void Start()
    {
        _playgroundManager = FindFirstObjectByType<PlaygroundView>();
        
        if (_playgroundManager == null)
        {
            Debug.LogError("[PlaygroundUI] PlaygroundManager not found!");
            return;
        }
        
        // Setup buttons
        readyButton.onClick.AddListener(OnReadyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
        
        // Hide countdown
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
        
        // Initial update
        UpdateLobbyInfo();
        
        // Hide start button if not host
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(_playgroundManager.IsLocalPlayerHost());
            startGameButton.interactable = false;
        }
    }
    
    void Update()
    {
        // Update lobby info periodically
        UpdateLobbyInfo();
    }
    
    private void UpdateLobbyInfo()
    {
        if (_playgroundManager == null) return;
        
        // Update lobby code
        if (lobbyCodeText != null)
        {
            string code = _playgroundManager.GetLobbyCode();
            lobbyCodeText.text = $"Lobby Code: {code}";
        }
        
        // Update player count
        if (playerCountText != null)
        {
            var players = _playgroundManager.GetLobbyPlayers();
            playerCountText.text = $"Players: {players.Count}/8";
        }
    }
    
    public void UpdatePlayerList(NetworkList<PlaygroundView.PlayerLobbyData> players)
    {
        if (playerListContent == null || playerListItemPrefab == null) return;
        
        // Clear existing items
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        
        // Create new items
        foreach (var player in players)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent);
            
            // Setup item
            TextMeshProUGUI nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI statusText = item.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            
            if (nameText != null)
            {
                string displayName = player.PlayerName.ToString();
                if (player.IsHost)
                    displayName += " [HOST]";
                nameText.text = displayName;
            }
            
            if (statusText != null)
            {
                statusText.text = player.IsReady ? "✓ Ready" : "✗ Not Ready";
                statusText.color = player.IsReady ? Color.green : Color.red;
            }
        }
    }
    
    private void OnReadyClicked()
    {
        _isReady = !_isReady;
        
        // Update button text
        if (readyButtonText != null)
        {
            readyButtonText.text = _isReady ? "UNREADY" : "READY";
        }
        
        // Update button color
        ColorBlock colors = readyButton.colors;
        colors.normalColor = _isReady ? Color.green : Color.white;
        readyButton.colors = colors;
        
        // Send to server
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        _playgroundManager.SetPlayerReadyServerRpc(clientId, _isReady);
        
        Debug.Log($"[PlaygroundUI] Ready state: {_isReady}");
    }
    
    private void OnStartGameClicked()
    {
        Debug.Log("[PlaygroundUI] Start game clicked");
        _playgroundManager.StartGameServerRpc();
    }
    
    private void OnLeaveClicked()
    {
        Debug.Log("[PlaygroundUI] Leave clicked");
        
        // Disconnect
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        // Clean up lobby
        if (_lobbyService.CurrentLobby != null)
        {
            if (_lobbyService.IsHost)
            {
                _lobbyService.DeleteLobbyAsync();
            }
            else
            {
                _lobbyService.LeaveLobbyAsync();
            }
        }
        
        // Load main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    public void SetStartButtonEnabled(bool enabled)
    {
        if (startGameButton != null && _playgroundManager.IsLocalPlayerHost())
        {
            startGameButton.interactable = enabled;
        }
    }
    
    public void ShowCountdown()
    {
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
            StartCoroutine(CountdownCoroutine());
        }
    }
    
    private IEnumerator CountdownCoroutine()
    {
        if (countdownText == null) yield break;
        
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = $"Starting in {i}...";
            yield return new WaitForSeconds(1f);
        }
        
        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);
    }
}