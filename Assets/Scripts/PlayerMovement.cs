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
    [SerializeField] private float _movementForce = 1000f;
    [SerializeField] private CameraModule _cameraModule;

    private bool _enableMovement = true;
    private Vector2 _movement;
    private Vector3 _aimDirection;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        _playerRagdoll.Input.OnMoveDelegates += MovementInput;
        _playerRagdoll.Input.OnFloorChangedDelegates += ProcessFloorChanged;
        _playerRagdoll.Input.OnMoveDelegates += _physicsModule.ManualTorqueInput;

        _playerRagdoll.Input.OnLeftArmDelegates += _animationModule.UseLeftArm;
        _playerRagdoll.Input.OnRightArmDelegates += _animationModule.UseRightArm;
    }
    private void Update()
    {
        _aimDirection = _cameraModule.GetCameraForward();
        _animationModule.AimDirection = _aimDirection;
        if(IsOwner)
            HandleMovementInputServerRpc(_movement);
    }

    [ServerRpc]
    private void HandleMovementInputServerRpc(Vector2 movement)
    {
        if(movement == Vector2.zero || !_enableMovement) 
        {
            _animationModule.SetMovment(false, 0f);
            return;
        }


        _animationModule.SetMovment(true, movement.magnitude);

        float angleOffset = Vector2.SignedAngle(movement, Vector2.up);
        Vector3 floorProjection = Auxiliary.GetFloorProjection(_animationModule.AimDirection);
        Vector3 targetForward = Quaternion.AngleAxis(angleOffset, Vector3.up) * floorProjection;
        _physicsModule.TargetDirection = targetForward;

        Vector3 moveDirection = targetForward.normalized;
        Vector3 currentVelocity = _playerRagdoll.GetPhysicalTorsoRigidbody().linearVelocity;
        Vector3 targetVelocity = moveDirection*(_movementSpeed * movement.magnitude);

        targetVelocity.y = currentVelocity.y;
        Vector3 velocityChange = targetVelocity - currentVelocity;
        velocityChange.y = 0;
        _playerRagdoll.GetPhysicalTorsoRigidbody().AddForce(velocityChange * (_movementForce*Time.deltaTime),ForceMode.Acceleration);
    }

    private void ProcessFloorChanged(bool onFloor)
    {
        if(!IsServer) return;

        if (onFloor) {
            _physicsModule.SetBalanceMode(PhysicsModule.BALANCE_MODE.STABILIZER_JOINT);
            _enableMovement = true;
            _playerRagdoll.GetBodyPart("Head Neck")?.SetStrengthScale(1);
            _playerRagdoll.GetBodyPart("Right Leg")?.SetStrengthScale(1);
            _playerRagdoll.GetBodyPart("Left Leg")?.SetStrengthScale(1);
            _animationModule.PlayAnimation("Idle");
        }
        else {
            _physicsModule.SetBalanceMode(PhysicsModule.BALANCE_MODE.MANUAL_TORQUE);
            _enableMovement = false;
            _playerRagdoll.GetBodyPart("Head Neck")?.SetStrengthScale(0.1f);
            _playerRagdoll.GetBodyPart("Right Leg")?.SetStrengthScale(0.05f);
            _playerRagdoll.GetBodyPart("Left Leg")?.SetStrengthScale(0.05f);
            _animationModule.PlayAnimation("InTheAir");
        }
    }
     private void MovementInput(Vector2 movement) {
        _movement = movement;
    }
}
