using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class MainMenuView : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    // [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject browserPanel;

    [SerializeField] private Button joinButton;
    [SerializeField] private Button lobbyListButton;
    [SerializeField] private StatusView statusView;
    // [Header("Host Panel")]
    // [SerializeField] private TMP_InputField lobbyNameInput;
    // [SerializeField] private Toggle privateToggle;
    [SerializeField] private Button createLobbyButton;
        
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
    private  ServicesInitializer _servicesInitializer;
    private  RelayService _relayService;
    private  LobbyService _lobbyService;
    
    [Inject]
    public void Construct(ServicesInitializer servicesInitializer, RelayService relayService, LobbyService lobbyService)
    {
        _servicesInitializer = servicesInitializer;
        _relayService = relayService;
        _lobbyService = lobbyService;
    }

    void Awake()
    {
         if (_servicesInitializer == null)
        {
            statusView.SetStatus("Waiting for dependency injection...");
            return;
        }

        if (!_servicesInitializer.IsInitialized)
        {
            statusView.SetStatus("Initializing services...");
            Invoke(nameof(CheckServicesReady), 0.5f);
        }
        else
        {
            OnServicesReady();
        }
        
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        joinButton.onClick.AddListener(ShowJoinPanel);
        lobbyListButton.onClick.AddListener(ShowBrowserPanel);
        refreshButton.onClick.AddListener(OnRefreshLobbiesClicked);
        
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
        // statusView.SetStatus($"Welcome, {_servicesInitializer.PlayerName}!");
    }
    
    // Panel navigation
    
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        joinPanel.SetActive(false);
        browserPanel.SetActive(false);
    }
    
    public void ShowHostPanel()
    {
        mainPanel.SetActive(false);
        joinPanel.SetActive(false);
        browserPanel.SetActive(false);
    }
    
    public void ShowJoinPanel()
    {
        mainPanel.SetActive(false);
        joinPanel.SetActive(true);
        browserPanel.SetActive(false);
    }
    
    public void ShowBrowserPanel()
    {
        mainPanel.SetActive(false);
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
        
        statusView.SetStatus("Creating Relay...");
        // createLobbyButton.interactable = false;
        
        try
        {
            // Create Relay
            string relayJoinCode = await _relayService.CreateRelayAsync(maxPlayers);
            if (relayJoinCode == null)
            {
                statusView.SetStatus("Failed to create Relay");
                // createLobbyButton.interactable = true;
                return;
            }
            
            statusView.SetStatus("Creating lobby...");
            
            // Create Lobby
            var lobby = await _lobbyService.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, relayJoinCode);
            if (lobby == null)
            {
                statusView.SetStatus("Failed to create lobby");
                // createLobbyButton.interactable = true;
                return;
            }
            
            statusView.SetStatus($"Lobby created! Code: {lobby.LobbyCode}");
            
            // Start as host
            NetworkManager.Singleton.StartHost();
            
            // Load lobby scene
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            statusView.SetStatus($"Error: {e.Message}");
            // createLobbyButton.interactable = true;
            Debug.LogError($"[MainMenuUI] Create lobby failed: {e}");
        }
    }
    
    private async void OnRefreshLobbiesClicked()
    {
        statusView.SetStatus("Loading lobbies...");
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
                statusView.SetStatus("No lobbies found");
            }
            else
            {
                statusView.SetStatus($"Found {lobbies.Count} lobbies");
                
                // Create list items
                foreach (var lobby in lobbies)
                {
                    CreateLobbyListItem(lobby);
                }
            }
        }
        catch (System.Exception e)
        {
            statusView.SetStatus($"Error: {e.Message}");
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
        statusView.SetStatus($"Joining {lobby.Name}...");
        
        try
        {
            // Join lobby
            var joinedLobby = await _lobbyService.JoinLobbyByCodeAsync(lobby.LobbyCode);
            if (joinedLobby == null)
            {
                statusView.SetStatus("Failed to join lobby");
                return;
            }
            
            // Get Relay code
            string relayJoinCode = _lobbyService.GetRelayJoinCode();
            
            // Join Relay
            bool joined = await _relayService.JoinRelayAsync(relayJoinCode);
            if (!joined)
            {
                statusView.SetStatus("Failed to connect to Relay");
                return;
            }
            
            // Start as client
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            statusView.SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MainMenuUI] Join from browser failed: {e}");
        }
    }
    
    // UI Helpers
    
   
    
    public void OnQuitClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}