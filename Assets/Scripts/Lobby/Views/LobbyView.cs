using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class LobbyView : MonoBehaviour{

    private bool isPrivate = false;
    private LobbyService lobbyService;
    
    [Inject]
    public void Construct(LobbyService lobbyService)
    {
        this.lobbyService = lobbyService;
    }
    public void OnInteract(InputValue inputValue)
    {
        if(!NetworkManager.Singleton.IsServer) return;
        Debug.Log("LobbyView OnInteract called");
        isPrivate = !isPrivate;
        lobbyService.SetLobbyPrivacy(isPrivate);
    }
}
