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
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject browserPanel;

    [SerializeField] private Button joinButton;
    [SerializeField] private Button lobbyListButton;
    [SerializeField] private StatusView statusView;
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
        
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        joinButton.onClick.AddListener(ShowJoinPanel);
        lobbyListButton.onClick.AddListener(ShowBrowserPanel);
        refreshButton.onClick.AddListener(OnRefreshLobbiesClicked);
        
        ShowMainPanel();
    }
    
    private void CheckServicesReady()
    {
        if (!_servicesInitializer.IsInitialized)
        {
            Invoke(nameof(CheckServicesReady), 0.5f);
        }
    }
    
    
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
        
        OnRefreshLobbiesClicked();
    }
    
    
    private async void OnCreateLobbyClicked()
    {
        string lobbyName = "mega lobby";
        
        bool isPrivate = true;
        
        statusView.SetStatus("Creating Relay...");
        
        try
        {
            string relayJoinCode = await _relayService.CreateRelayAsync(maxPlayers);
            if (relayJoinCode == null)
            {
                statusView.SetStatus("Failed to create Relay");
                return;
            }
            
            statusView.SetStatus("Creating lobby...");
            
            var lobby = await _lobbyService.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, relayJoinCode);
            if (lobby == null)
            {
                statusView.SetStatus("Failed to create lobby");
                return;
            }
            
            statusView.SetStatus($"Lobby created! Code: {lobby.LobbyCode}");
            
            NetworkManager.Singleton.StartHost();
            
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            statusView.SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MainMenuUI] Create lobby failed: {e}");
        }
    }
    
    private async void OnRefreshLobbiesClicked()
    {
        statusView.SetStatus("Loading lobbies...");
        refreshButton.interactable = false;
        
        foreach (var item in _lobbyListItems)
        {
            Destroy(item);
        }
        _lobbyListItems.Clear();
        
        try
        {
            var lobbies = await _lobbyService.QueryLobbiesAsync();
            
            if (lobbies.Count == 0)
            {
                statusView.SetStatus("No lobbies found");
            }
            else
            {
                statusView.SetStatus($"Found {lobbies.Count} lobbies");
                
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
            var joinedLobby = await _lobbyService.JoinLobbyByCodeAsync(lobby.LobbyCode);

            if (joinedLobby == null)
            {
                statusView.SetStatus("Failed to join lobby");
                return;
            }
            
            string relayJoinCode = _lobbyService.GetRelayJoinCode();
            bool joined = await _relayService.JoinRelayAsync(relayJoinCode);

            if (!joined)
            {
                statusView.SetStatus("Failed to connect to Relay");
                return;
            }
            
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            statusView.SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MainMenuUI] Join from browser failed: {e}");
        }
    }    
    public async void JoinPublicLobby(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        statusView.SetStatus($"Joining {lobby.Name}...");
        
        try
        {
            var joinedLobby = await _lobbyService.JoinLobbyByIdAsync(lobby.Id);

            if (joinedLobby == null)
            {
                statusView.SetStatus("Failed to join lobby");
                return;
            }
            
            string relayJoinCode = _lobbyService.GetRelayJoinCode();
            bool joined = await _relayService.JoinRelayAsync(relayJoinCode);

            if (!joined)
            {
                statusView.SetStatus("Failed to connect to Relay");
                return;
            }
            
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            statusView.SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MainMenuUI] Join from browser failed: {e}");
        }
    }   
    
    public void OnQuitClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}