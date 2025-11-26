using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    [SerializeField] private Toggle privacyToggle;

    private LobbyService lobbyService;

    public void Construct(LobbyService lobbyService)
    {
        this.lobbyService = lobbyService;
    }

    void Awake()
    {
        privacyToggle.onValueChanged.AddListener((bool isOn)=>
        {
            lobbyService.SetLobbyPrivacy(isOn);
        });
    }
}
