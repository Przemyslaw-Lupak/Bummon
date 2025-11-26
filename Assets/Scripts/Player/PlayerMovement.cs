using Brickface.Networking.Physics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private PlayerRagdoll _playerRagdoll;
    [SerializeField] private AnimationModule _animationModule;
    [SerializeField] private PhysicsModule _physicsModule;
    [SerializeField] private float _movementSpeed = 5f;
    [SerializeField] private float _backwardSpeedMultiplier = 0.75f; // Backward is 75% of forward speed
    [SerializeField] private float _movementForce = 1000f;
    [SerializeField] private CameraModule _cameraModule;

    private bool _enableMovement = true;
    private Vector2 _movement;
    private Vector3 _aimDirection;
    
    public override void OnNetworkSpawn()
    {
        // Only the owner (local player) subscribes to input
        if (!IsOwner) return;

        _playerRagdoll.Input.OnMoveDelegates += MovementInput;
        _playerRagdoll.Input.OnFloorChangedDelegates += ProcessFloorChangedClientRpc;
        _playerRagdoll.Input.OnMoveDelegates += _physicsModule.ManualTorqueInput;

        _playerRagdoll.Input.OnLeftArmDelegates += _animationModule.UseLeftArm;
        _playerRagdoll.Input.OnRightArmDelegates += _animationModule.UseRightArm;
        
        Debug.Log($"[PlayerMovement] OnNetworkSpawn - Owner subscribed to input. NetworkObjectId: {NetworkObjectId}");
    }

    private void Update()
    {
        // Only owner updates aim direction and sends movement to server
        if(!IsOwner) return;
        
        _aimDirection = _cameraModule.GetCameraForward();
        
        // Pass A/D horizontal input to camera for rotation
        _cameraModule.UpdateRotationInput(_movement.x);
        
        // ALWAYS send aim direction to server (for body rotation)
        // Even when not moving, body should face camera direction
        HandleMovementInputServerRpc(_movement, _aimDirection);
    }

    [ServerRpc]
    private void HandleMovementInputServerRpc(Vector2 movement, Vector3 aimDirection)
    {
        // Update aim direction on server for IK and movement calculations
        _animationModule.AimDirection = aimDirection;
        
        // ALWAYS update body facing direction (even when not moving)
        Vector3 floorProjection = Auxiliary.GetFloorProjection(aimDirection);
        _physicsModule.TargetDirection = floorProjection;
        
        // Handle movement
        if(movement == Vector2.zero || !_enableMovement) 
        {
            _animationModule.SetMovement(false, 0f, 0f);
            return; // No movement, but body still rotates to face camera
        }

        // Only use forward/backward movement (W/S)
        float forwardInput = movement.y; // W = +1, S = -1
        
        // Apply speed multiplier for backward movement
        float speedMultiplier = forwardInput < 0 ? _backwardSpeedMultiplier : 1f;
        
        // Pass only forward/backward to animator (no strafe)
        _animationModule.SetMovement(true, Mathf.Abs(forwardInput), forwardInput);

        // Movement is only forward/backward relative to camera facing
        Vector3 moveDirection = floorProjection * forwardInput;

        // Apply movement force with speed multiplier
        Vector3 currentVelocity = _playerRagdoll.GetPhysicalTorsoRigidbody().linearVelocity;
        Vector3 targetVelocity = moveDirection * (_movementSpeed * speedMultiplier);

        targetVelocity.y = currentVelocity.y;
        Vector3 velocityChange = targetVelocity - currentVelocity;
        velocityChange.y = 0;
        
        _playerRagdoll.GetPhysicalTorsoRigidbody().AddForce(
            velocityChange * (_movementForce * Time.deltaTime), 
            ForceMode.Acceleration
        );
    }

    [ClientRpc]
    private void ProcessFloorChangedClientRpc(bool onFloor)
    {
        // This gets called on all clients from the server
        if (onFloor) {
            _enableMovement = true;
            
            // Only server modifies physics
            if (IsServer) {
                _physicsModule.SetBalanceMode(PhysicsModule.BALANCE_MODE.STABILIZER_JOINT);
                _playerRagdoll.GetBodyPart("Head Neck")?.SetStrengthScale(1);
                _playerRagdoll.GetBodyPart("Right Leg")?.SetStrengthScale(1);
                _playerRagdoll.GetBodyPart("Left Leg")?.SetStrengthScale(1);
            }
            
            _animationModule.PlayAnimation("Idle");
        }
        else {
            _enableMovement = false;
            
            // Only server modifies physics
            if (IsServer) {
                _physicsModule.SetBalanceMode(PhysicsModule.BALANCE_MODE.MANUAL_TORQUE);
                _playerRagdoll.GetBodyPart("Head Neck")?.SetStrengthScale(0.1f);
                _playerRagdoll.GetBodyPart("Right Leg")?.SetStrengthScale(0.05f);
                _playerRagdoll.GetBodyPart("Left Leg")?.SetStrengthScale(0.05f);
            }
            
            _animationModule.PlayAnimation("InTheAir");
        }
    }

    private void MovementInput(Vector2 movement) {
        // Store input locally on owner
        _movement = movement;
    }
}