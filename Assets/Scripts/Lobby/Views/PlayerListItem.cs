using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostIcon;
    [SerializeField] private GameObject readyIcon;
    [SerializeField] private Image backgroundImage;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color localPlayerColor = new Color(0.8f, 0.9f, 1f);
    
    public void Setup(string playerName, bool isHost, bool isReady, bool isLocalPlayer)
    {
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
            
            if (isHost)
            {
                playerNameText.text += " (Host)";
            }
        }
        
        // Show host icon
        if (hostIcon != null)
        {
            hostIcon.SetActive(isHost);
        }
        
        // Show ready icon
        if (readyIcon != null)
        {
            readyIcon.SetActive(isReady);
        }
        
        // Highlight local player
        if (backgroundImage != null)
        {
            backgroundImage.color = isLocalPlayer ? localPlayerColor : normalColor;
        }
    }
}