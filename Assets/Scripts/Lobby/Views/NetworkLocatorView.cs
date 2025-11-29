using UnityEngine;

public static class NetworkServiceLocator
{
    private static NetworkPlayerService _playerService;
    
    public static NetworkPlayerService PlayerService
    {
        get
        {
            if (_playerService == null)
            {
                Debug.LogError("[NetworkServiceLocator] PlayerService not initialized!");
            }
            return _playerService;
        }
    }
    
    public static void Initialize(NetworkPlayerService playerService)
    {
        _playerService = playerService;
        Debug.Log("[NetworkServiceLocator] PlayerService initialized");
    }
    
    public static void Clear()
    {
        _playerService = null;
    }
}