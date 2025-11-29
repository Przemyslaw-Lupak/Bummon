using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using VContainer.Unity;

public class ServicesInitializer : IInitializable
{
    public bool IsInitialized { get; private set; }
    
    private readonly PlayerIdentityService _playerIdentityService;
    
    public ServicesInitializer(PlayerIdentityService playerIdentityService)
    {
        _playerIdentityService = playerIdentityService;
    }

    public async void Initialize()
    {   
        await InitializeServices();
    }
    
    private async Task InitializeServices()
    {
       try
        {
            Debug.Log("[ServicesInitializer] Initializing Unity Services...");
            
            await UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            
            string playerId = AuthenticationService.Instance.PlayerId;
            string playerName = GenerateRandomName();
            
            // Initialize player identity
            _playerIdentityService.Initialize(playerId, playerName);
            
            Debug.Log($"[ServicesInitializer] ✓ Services initialized! PlayerId: {playerId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ServicesManager] Failed to initialize: {e.Message}");
        }
    }
    
    private string GenerateRandomName()
    {
        string[] adjectives = { "Swift", "Bold", "Silent", "Mighty", "Wild", "Brave", "Quick", "Strong" };
        string[] nouns = { "Warrior", "Hunter", "Fighter", "Champion", "Hero", "Raider", "Knight", "Slayer" };
        
        return $"{adjectives[Random.Range(0, adjectives.Length)]} {nouns[Random.Range(0, nouns.Length)]}";
    }
}