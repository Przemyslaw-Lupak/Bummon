using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class MainMenuView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject browserPanel;
    
    [Header("Host Panel")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private Button createLobbyButton;
    
    [Header("Join Panel")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private Button browseLobbiesButton;
    
    [Header("Browser Panel")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Transform lobbyListContent;
    [SerializeField] private GameObject lobbyListItemPrefab;
    
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Settings")]
    [SerializeField] private string lobbySceneName = "Playground";
    [SerializeField] private int maxPlayers = 4;
    private List<GameObject> _lobbyListItems = new List<GameObject>();
    private readonly ServicesInitializer _servicesInitializer;
    private readonly RelayService _relayService;
    private readonly LobbyService _lobbyService;
    
    [Inject]
    public MainMenuView(ServicesInitializer servicesInitializer, RelayService relayService, LobbyService lobbyService)
    {
        _servicesInitializer = servicesInitializer;
        _relayService = relayService;
        _lobbyService = lobbyService;
    }

    void Start()
    {
        // Wait for services to initialize
        if (!_servicesInitializer.IsInitialized)
        {
            SetStatus("Initializing services...");
            Invoke(nameof(CheckServicesReady), 0.5f);
        }
        else
        {
            OnServicesReady();
        }
        
        // Setup button listeners
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
        browseLobbiesButton.onClick.AddListener(OnBrowseLobbiesClicked);
        refreshButton.onClick.AddListener(OnRefreshLobbiesClicked);
        
        // Default lobby name
        lobbyNameInput.text = $"{_servicesInitializer.PlayerName}'s Game";
        
        // Show main panel
        ShowMainPanel();
    }
    
    private void CheckServicesReady()
    {
        if (_servicesInitializer.IsInitialized)
        {
            OnServicesReady();
        }
        else
        {
            Invoke(nameof(CheckServicesReady), 0.5f);
        }
    }
    
    private void OnServicesReady()
    {
        SetStatus($"Welcome, {_servicesInitializer.PlayerName}!");
    }
    
    // Panel navigation
    
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        browserPanel.SetActive(false);
    }
    
    public void ShowHostPanel()
    {
        mainPanel.SetActive(false);
        hostPanel.SetActive(true);
        joinPanel.SetActive(false);
        browserPanel.SetActive(false);
    }
    
    public void ShowJoinPanel()
    {
        mainPanel.SetActive(false);
        hostPanel.SetActive(false);
        joinPanel.SetActive(true);
        browserPanel.SetActive(false);
    }
    
    public void ShowBrowserPanel()
    {
        mainPanel.SetActive(false);
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        browserPanel.SetActive(true);
        
        // Auto-refresh when opening
        OnRefreshLobbiesClicked();
    }
    
    // Host Game Flow
    
    private async void OnCreateLobbyClicked()
    {
        string lobbyName = "mega lobby";
        
        bool isPrivate = true;
        
        SetStatus("Creating Relay...");
        createLobbyButton.interactable = false;
        
        try
        {
            // Create Relay
            string relayJoinCode = await _relayService.CreateRelayAsync(maxPlayers);
            if (relayJoinCode == null)
            {
                SetStatus("Failed to create Relay");
                createLobbyButton.interactable = true;
                return;
            }
            
            SetStatus("Creating lobby...");
            
            // Create Lobby
            var lobby = await _lobbyService.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, relayJoinCode);
            if (lobby == null)
            {
                SetStatus("Failed to create lobby");
                createLobbyButton.interactable = true;
                return;
            }
            
            SetStatus($"Lobby created! Code: {lobby.LobbyCode}");
            
            // Start as host
            NetworkManager.Singleton.StartHost();
            
            // Load lobby scene
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}");
            createLobbyButton.interactable = true;
            Debug.LogError($"[MainMenuUI] Create lobby failed: {e}");
        }
    }
    
    // Join Game Flow
    
    private async void OnJoinByCodeClicked()
    {
        string code = joinCodeInput.text.ToUpper().Trim();
        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            SetStatus("Please enter a valid 6-character code");
            return;
        }
        
        SetStatus($"Joining lobby {code}...");
        joinByCodeButton.interactable = false;
        
        try
        {
            // Join lobby
            var lobby = await _lobbyService.JoinLobbyByCodeAsync(code);
            if (lobby == null)
            {
                SetStatus("Failed to join lobby. Check the code.");
                joinByCodeButton.interactable = true;
                return;
            }
            
            SetStatus("Connecting to Relay...");
            
            // Get Relay code from lobby
            string relayJoinCode = _lobbyService.GetRelayJoinCode();
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                SetStatus("Lobby has no Relay code");
                joinByCodeButton.interactable = true;
                return;
            }
            
            // Join Relay
            bool joined = await _relayService.JoinRelayAsync(relayJoinCode);
            if (!joined)
            {
                SetStatus("Failed to connect to Relay");
                joinByCodeButton.interactable = true;
                return;
            }
            
            SetStatus("Joining game...");
            
            // Start as client
            NetworkManager.Singleton.StartClient();
            
            // Lobby scene will load automatically via NetworkManager
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}");
            joinByCodeButton.interactable = true;
            Debug.LogError($"[MainMenuUI] Join lobby failed: {e}");
        }
    }
    
    // Browse Lobbies
    
    private void OnBrowseLobbiesClicked()
    {
        ShowBrowserPanel();
    }
    
    private async void OnRefreshLobbiesClicked()
    {
        SetStatus("Loading lobbies...");
        refreshButton.interactable = false;
        
        // Clear old list
        foreach (var item in _lobbyListItems)
        {
            Destroy(item);
        }
        _lobbyListItems.Clear();
        
        try
        {
            // Query lobbies
            var lobbies = await _lobbyService.QueryLobbiesAsync();
            
            if (lobbies.Count == 0)
            {
                SetStatus("No lobbies found");
            }
            else
            {
                SetStatus($"Found {lobbies.Count} lobbies");
                
                // Create list items
                foreach (var lobby in lobbies)
                {
                    CreateLobbyListItem(lobby);
                }
            }
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MainMenuUI] Query lobbies failed: {e}");
        }
        
        refreshButton.interactable = true;
    }
    
    private void CreateLobbyListItem(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (lobbyListItemPrefab == null)
        {
            Debug.LogWarning("[MainMenuUI] Lobby list item prefab not assigned!");
            return;
        }
        
        GameObject item = Instantiate(lobbyListItemPrefab, lobbyListContent);
        _lobbyListItems.Add(item);
        
        var lobbyItem = item.GetComponent<LobbyListItem>();
        if (lobbyItem != null)
        {
            lobbyItem.Setup(lobby, this);
        }
    }
    
    public async void JoinLobby(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        SetStatus($"Joining {lobby.Name}...");
        
        try
        {
            // Join lobby
            var joinedLobby = await _lobbyService.JoinLobbyByCodeAsync(lobby.LobbyCode);
            if (joinedLobby == null)
            {
                SetStatus("Failed to join lobby");
                return;
            }
            
            // Get Relay code
            string relayJoinCode = _lobbyService.GetRelayJoinCode();
            
            // Join Relay
            bool joined = await _relayService.JoinRelayAsync(relayJoinCode);
            if (!joined)
            {
                SetStatus("Failed to connect to Relay");
                return;
            }
            
            // Start as client
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MainMenuUI] Join from browser failed: {e}");
        }
    }
    
    // UI Helpers
    
    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[MainMenuUI] {message}");
    }
    
    public void OnQuitClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}