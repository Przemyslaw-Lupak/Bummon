using UnityEngine;
public class PlayerIdentityService
{
    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    
    public void Initialize(string playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        Debug.Log($"[PlayerIdentity] Initialized - ID: {playerId}, Name: {playerName}");
    }
}