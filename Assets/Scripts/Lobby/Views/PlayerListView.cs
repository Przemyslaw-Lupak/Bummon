using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pure UI component for displaying player list.
/// Receives data from LobbyView, no business logic.
/// </summary>
public class PlayerListView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    
    private List<GameObject> _playerListItems = new List<GameObject>();
    
    public void UpdatePlayerList(List<PlayerNetworkDataView> players)
    {
        // Clear old items
        foreach (var item in _playerListItems)
        {
            if (item != null) Destroy(item);
        }
        _playerListItems.Clear();
        
        // Create new items
        foreach (var player in players)
        {
            if (player != null)
            {
                CreatePlayerListItem(player);
            }
        }
        
        Debug.Log($"[PlayerListView] Updated list with {players.Count} players");
    }
    
    private void CreatePlayerListItem(PlayerNetworkDataView player)
    {
        if (playerListItemPrefab == null || playerListContent == null)
        {
            Debug.LogError("[PlayerListView] Missing prefab or content references!");
            return;
        }
        
        GameObject item = Instantiate(playerListItemPrefab, playerListContent);
        _playerListItems.Add(item);
        
        var playerItem = item.GetComponent<PlayerListItem>();
        if (playerItem != null)
        {
            string playerName = player.PlayerName.Value.ToString();
            bool isHost = player.OwnerClientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost;
            bool isReady = player.IsReady.Value;
            bool isLocalPlayer = player.IsOwner;
            
            playerItem.Setup(playerName, isHost, isReady, isLocalPlayer);
        }
    }
}