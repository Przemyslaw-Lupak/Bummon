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

        // Check if PlayerInput component exists
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput == null) {
            Debug.LogError("[InputModule] PlayerInput component NOT FOUND! This is required for input to work!");
        } else {
            if (IsOwner) {
                playerInput.enabled = true;
                playerInput.ActivateInput();

                var moveAction = playerInput.actions?.FindAction("Move");
            } else {
                playerInput.enabled = false;
                playerInput.DeactivateInput();
            }
        }
    }

    void Update() {
        if(!IsServer) return;
        UpdateOnFloor();
    }

   
    private void UpdateOnFloor() {
        bool lastIsOnFloor = _isOnFloor;
        _isOnFloor = CheckRigidbodyOnFloor(_rightFoot, out Vector3 foo)
                        || CheckRigidbodyOnFloor(_leftFoot, out foo);

        if (_isOnFloor != lastIsOnFloor)
            OnFloorChangedDelegates(_isOnFloor);
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

    public void OnMove(InputValue value) {
        Vector2 input = value.Get<Vector2>();
        
        OnMoveDelegates?.Invoke(input);
    }

    public void OnLeftArm(InputValue value) {
        OnLeftArmDelegates?.Invoke(value.Get<float>());
    }
    public void OnRightArm(InputValue value) {
        OnRightArmDelegates?.Invoke(value.Get<float>());
    }
}
