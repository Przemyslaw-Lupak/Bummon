using Unity.Netcode;
using UnityEngine;

// Base Network Module
[RequireComponent(typeof(ActiveRagdoll))]
public class NetworkModule : NetworkBehaviour {
    [SerializeField] protected ActiveRagdoll _activeRagdoll;
    public ActiveRagdoll ActiveRagdoll { get { return _activeRagdoll; } }

    private void OnValidate() {
        if (_activeRagdoll == null) {
            if (!TryGetComponent<ActiveRagdoll>(out _activeRagdoll))
                Debug.LogWarning("No ActiveRagdoll could be found for this module.");
        }
    }
}