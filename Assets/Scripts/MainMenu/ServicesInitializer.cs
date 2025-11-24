using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using VContainer.Unity;

public class ServicesInitializer : IInitializable
{
    public string PlayerName { get; private set; }
    public string PlayerId { get; private set; }
    public bool IsInitialized { get; private set; }
    
    public async void Initialize()
    {   
        await InitializeServices();
    }
    
    private async Task InitializeServices()
    {
        try
        {
            Debug.Log("[ServicesManager] Initializing Unity Services...");
            await UnityServices.InitializeAsync();
            
            Debug.Log("[ServicesManager] Authenticating...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            PlayerId = AuthenticationService.Instance.PlayerId;
            
            PlayerName = PlayerPrefs.GetString("PlayerName", "");
            if (string.IsNullOrEmpty(PlayerName))
            {
                PlayerName = GenerateRandomName();
                PlayerPrefs.SetString("PlayerName", PlayerName);
                PlayerPrefs.Save();
            }
            
            IsInitialized = true;
            Debug.Log($"[ServicesManager] Services initialized! Player: {PlayerName} (ID: {PlayerId})");
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
    
    public void SetPlayerName(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            PlayerName = newName;
            PlayerPrefs.SetString("PlayerName", newName);
            PlayerPrefs.Save();
            Debug.Log($"[ServicesManager] Player name changed to: {PlayerName}");
        }
    }
}