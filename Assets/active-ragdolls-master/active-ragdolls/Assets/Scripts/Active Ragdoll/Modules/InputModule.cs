using System;
using Brickface.Networking.Physics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputModule : NetworkBehaviour {
    [SerializeField] protected PlayerRagdoll _playerRagdoll;
    
    public delegate void onMoveDelegate(Vector2 movement);
    public onMoveDelegate OnMoveDelegates { get; set; }
    
    public delegate void onLeftArmDelegate(float armWeight);
    public onLeftArmDelegate OnLeftArmDelegates { get; set; }
    
    public delegate void onRightArmDelegate(float armWeight);
    public onRightArmDelegate OnRightArmDelegates { get; set; }
    
    public delegate void onFloorChangedDelegate(bool onFloor);
    public onFloorChangedDelegate OnFloorChangedDelegates { get; set; }

    [Header("--- FLOOR ---")]
    public float floorDetectionDistance = 0.3f;
    public float maxFloorSlope = 60;

    private bool _isOnFloor = true;
    public bool IsOnFloor { get { return _isOnFloor; } }

    Rigidbody _rightFoot, _leftFoot;
    
    public override void OnNetworkSpawn() {
        _rightFoot = _playerRagdoll.GetPhysicalBone(HumanBodyBones.RightFoot).GetComponent<Rigidbody>();
        _leftFoot = _playerRagdoll.GetPhysicalBone(HumanBodyBones.LeftFoot).GetComponent<Rigidbody>();

        // ONLY enable input for the owner (the player controlling this character)
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) {
            if (IsOwner) {
                playerInput.enabled = true;
                playerInput.ActivateInput();
                Debug.Log($"[InputModule] Input ENABLED for owner. NetworkObjectId: {NetworkObjectId}");
            } else {
                playerInput.enabled = false;
                playerInput.DeactivateInput();
                Debug.Log($"[InputModule] Input DISABLED for non-owner. NetworkObjectId: {NetworkObjectId}");
            }
        } else {
            Debug.LogError("[InputModule] PlayerInput component NOT FOUND!");
        }
    }

    void Update() {
        // Floor detection should ONLY run on server (server authority for physics)
        if(!IsServer) return;
        UpdateOnFloor();
    }

    private void UpdateOnFloor() {
        bool lastIsOnFloor = _isOnFloor;
        _isOnFloor = CheckRigidbodyOnFloor(_rightFoot, out Vector3 foo)
                        || CheckRigidbodyOnFloor(_leftFoot, out foo);

        if (_isOnFloor != lastIsOnFloor)
            OnFloorChangedDelegates?.Invoke(_isOnFloor);
    }

    public bool CheckRigidbodyOnFloor(Rigidbody bodyPart, out Vector3 normal) {
        Ray ray = new Ray(bodyPart.position, Vector3.down);
        bool onFloor = Physics.Raycast(ray, out RaycastHit info, floorDetectionDistance, ~(1 << bodyPart.gameObject.layer));

        onFloor = onFloor && Vector3.Angle(info.normal, Vector3.up) <= maxFloorSlope;

        if (onFloor && info.collider.gameObject.TryGetComponent<Floor>(out Floor floor))
                onFloor = floor.isFloor;

        normal = info.normal;
        return onFloor;
    }

    // These input callbacks only fire on the owner (due to PlayerInput being disabled on non-owners)
    public void OnMove(InputValue value) {
        if (!IsOwner) return; // Safety check
        
        Vector2 input = value.Get<Vector2>();
        Debug.Log($"[InputModule] OnMove called: {input}, IsOwner: {IsOwner}, IsServer: {IsServer}");
        OnMoveDelegates?.Invoke(input);
    }

    public void OnLeftArm(InputValue value) {
        if (!IsOwner) return;
        OnLeftArmDelegates?.Invoke(value.Get<float>());
    }
    
    public void OnRightArm(InputValue value) {
        if (!IsOwner) return;
        OnRightArmDelegates?.Invoke(value.Get<float>());
    }
}