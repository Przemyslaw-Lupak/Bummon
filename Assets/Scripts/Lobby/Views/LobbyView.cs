using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using TMPro;

public class LobbyView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI privacyStatusText;  // Optional: Show "Private" or "Public"
    [SerializeField] private Button togglePrivacyButton;  // Optional: Button alternative to "E" key
    
    private bool isPrivate = true;  // Start as private
    private LobbyService lobbyService;
    
    [Inject]
    public void Construct(LobbyService lobbyService)
    {
        Debug.Log("[LobbyView] Construct called - LobbyService injection");
        this.lobbyService = lobbyService;
        
        if (this.lobbyService == null)
        {
            Debug.LogError("[LobbyView] LobbyService is NULL after injection!");
        }
        else
        {
            Debug.Log("[LobbyView] LobbyService injected successfully");
        }
    }
    
    void Start()
    {
        Debug.Log($"[LobbyView] Start called. LobbyService is {(lobbyService == null ? "NULL" : "NOT NULL")}");
        
        // Setup button if it exists
        if (togglePrivacyButton != null)
        {
            togglePrivacyButton.onClick.AddListener(TogglePrivacy);
            Debug.Log("[LobbyView] Toggle privacy button listener added");
        }
        
    }
    
    public void OnInteract(InputValue inputValue)
    {
        Debug.Log($"[LobbyView] OnInteract called. IsServer: {NetworkManager.Singleton.IsServer}");
        
        if (!NetworkManager.Singleton.IsServer) 
        {
            Debug.Log("[LobbyView] Not server - only the host can change lobby privacy");
            return;
        }
        
        TogglePrivacy();
    }
    
    private void TogglePrivacy()
    {
        Debug.Log("[LobbyView] TogglePrivacy START");
        
        if (lobbyService == null)
        {
            Debug.LogError("[LobbyView] LobbyService is NULL! Cannot toggle privacy.");
            return;
        }
        
        isPrivate = !isPrivate;
        
        Debug.Log($"[LobbyView] Toggling lobby privacy to: {(isPrivate ? "Private" : "Public")}");
        Debug.Log($"[LobbyView] About to call lobbyService.SetLobbyPrivacy({isPrivate})");
        
        lobbyService.SetLobbyPrivacyAsync(isPrivate);
        
        Debug.Log("[LobbyView] Called lobbyService.SetLobbyPrivacy");
        
    }
    
    
}