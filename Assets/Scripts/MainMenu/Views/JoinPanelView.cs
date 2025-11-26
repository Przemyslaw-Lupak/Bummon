using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class JoinPanelView : MonoBehaviour
{
    
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private StatusView statusView;

    private  RelayService _relayService;
    private  LobbyService _lobbyService;
    
    [Inject]
    public void Construct(RelayService relayService, LobbyService lobbyService)
    {
        _relayService = relayService;
        _lobbyService = lobbyService;
    }
    void Awake()
    {
        joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
    }
     private async void OnJoinByCodeClicked()
    {
        string code = joinCodeInput.text.ToUpper().Trim();
        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            statusView.SetStatus("Please enter a valid 6-character code");
            return;
        }
        
        statusView.SetStatus($"Joining lobby {code}...");
        joinByCodeButton.interactable = false;
        
        try
        {
            // Join lobby
            var lobby = await _lobbyService.JoinLobbyByCodeAsync(code);
            if (lobby == null)
            {
                statusView.SetStatus("Failed to join lobby. Check the code.");
                joinByCodeButton.interactable = true;
                return;
            }
            
            statusView.SetStatus("Connecting to Relay...");
            
            // Get Relay code from lobby
            string relayJoinCode = _lobbyService.GetRelayJoinCode();
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                statusView.SetStatus("Lobby has no Relay code");
                joinByCodeButton.interactable = true;
                return;
            }
            
            bool joined = await _relayService.JoinRelayAsync(relayJoinCode);
            if (!joined)
            {
                statusView.SetStatus("Failed to connect to Relay");
                joinByCodeButton.interactable = true;
                return;
            }
            
            statusView.SetStatus("Joining game...");
            
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            statusView.SetStatus($"Error: {e.Message}");
            joinByCodeButton.interactable = true;
            Debug.LogError($"[MainMenuUI] Join lobby failed: {e}");
        }
    }
}
