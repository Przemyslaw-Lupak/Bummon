using System;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Manages Unity Relay connections
/// Handles creating and joining Relay allocations
/// </summary>
public class RelayService
{    
    public string JoinCode { get; private set; }
    public bool IsRelayEnabled => Transport != null;
    
    private UnityTransport Transport => NetworkManager.Singleton?.GetComponent<UnityTransport>();
    
    
    /// <summary>
    /// Creates a Relay allocation as host
    /// </summary>
    /// <param name="maxConnections">Maximum number of players (including host)</param>
    /// <returns>Relay join code</returns>
    public async Task<string> CreateRelayAsync(int maxConnections)
    {
        try
        {
            Debug.Log($"[RelayService] Creating Relay allocation for {maxConnections} players...");
            
            // Check NetworkManager exists
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[RelayService] NetworkManager.Singleton is null! Make sure NetworkManager exists in scene.");
                return null;
            }
            
            // Check UnityTransport exists
            if (Transport == null)
            {
                Debug.LogError("[RelayService] UnityTransport not found on NetworkManager! Add UnityTransport component to NetworkManager.");
                return null;
            }
            
            // Create allocation
            Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxConnections - 1);
            
            // Get join code
            JoinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            // Configure transport with host data
            Transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                null  // Host doesn't have HostConnectionData
            );
            
            Debug.Log($"[RelayService] Relay created! Join code: {JoinCode}");
            return JoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayService] Failed to create Relay: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Joins an existing Relay allocation as client
    /// </summary>
    /// <param name="joinCode">6-character Relay join code</param>
    public async Task<bool> JoinRelayAsync(string joinCode)
    {
        try
        {
            Debug.Log($"[RelayService] Joining Relay with code: {joinCode}");
            
            // Check NetworkManager exists
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[RelayService] NetworkManager.Singleton is null! Make sure NetworkManager exists in scene.");
                return false;
            }
            
            // Check UnityTransport exists
            if (Transport == null)
            {
                Debug.LogError("[RelayService] UnityTransport not found on NetworkManager! Add UnityTransport component to NetworkManager.");
                return false;
            }
            
            // Join allocation
            JoinAllocation allocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
            
            // Configure transport with client data
            Transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );
            
            JoinCode = joinCode;
            Debug.Log($"[RelayService] Successfully joined Relay!");
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayService] Failed to join Relay: {e.Message}");
            return false;
        }
    }
}