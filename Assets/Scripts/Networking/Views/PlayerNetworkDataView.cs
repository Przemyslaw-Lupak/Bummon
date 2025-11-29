using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkDataView : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    
    public event System.Action OnDataChanged;
    
    public override void OnNetworkSpawn()
    {
        PlayerName.OnValueChanged += OnPlayerNameChanged;
        IsReady.OnValueChanged += OnReadyChanged;
        
        if (IsOwner)
        {
            string playerName = Unity.Services.Authentication.AuthenticationService.Instance.PlayerName ?? "Player";
            InitializePlayerDataServerRpc(playerName);
        }
        
        NetworkEventBus.PublishPlayerSpawned(this);
    }
    
    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnPlayerNameChanged;
        IsReady.OnValueChanged -= OnReadyChanged;
        
        NetworkEventBus.PublishPlayerDespawned(this);
    }
    
    [ServerRpc]
    private void InitializePlayerDataServerRpc(string playerName)
    {
        PlayerName.Value = playerName;
        Debug.Log($"[PlayerNetworkData] Initialized player name: {playerName}");
    }
    
    [ServerRpc]
    public void SetReadyServerRpc(bool ready)
    {
        IsReady.Value = ready;
    }
    
    private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        OnDataChanged?.Invoke();
    }
    
    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        OnDataChanged?.Invoke();
    }
}