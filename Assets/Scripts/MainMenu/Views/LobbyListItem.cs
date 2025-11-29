using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for a single lobby in the browser list
/// Shows lobby info and join button
/// </summary>
public class LobbyListItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI hostNameText;
    [SerializeField] private Button joinButton;
    
    private Lobby _lobby;
    private MainMenuView _mainMenuUI;
    
    public void Setup(Lobby lobby, MainMenuView mainMenuUI)
    {
        _lobby = lobby;
        _mainMenuUI = mainMenuUI;
        
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        
        string hostName = "Unknown";
        foreach (var player in lobby.Players)
        {
            if (player.Id == lobby.HostId)
            {
                if (player.Data.ContainsKey("PlayerName"))
                {
                    hostName = player.Data["PlayerName"].Value;
                }
                break;
            }
        }
        hostNameText.text = $"Host: {hostName}";
        
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(OnJoinClicked);
        
        if (lobby.Players.Count >= lobby.MaxPlayers)
        {
            joinButton.interactable = false;
            playerCountText.text += " (FULL)";
        }
    }
    
    private void OnJoinClicked()
    {
        if (_lobby != null && _mainMenuUI != null)
        {
            _mainMenuUI.JoinPublicLobby(_lobby);
        }
    }
}