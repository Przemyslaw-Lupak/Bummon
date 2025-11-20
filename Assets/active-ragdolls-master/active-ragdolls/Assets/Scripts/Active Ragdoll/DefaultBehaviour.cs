using Unity.Netcode;
using UnityEngine;

public class DefaultBehaviour : NetworkBehaviour {
    [Header("Modules")]
    [SerializeField] private ActiveRagdoll _activeRagdoll;
    [SerializeField] private PhysicsModule _physicsModule;
    [SerializeField] private AnimationModule _animationModule;
    [SerializeField] private GripModule _gripModule;
    [SerializeField] private CameraModule _cameraModule;

    [Header("Movement")]
    [SerializeField] private bool _enableMovement = true;
    [SerializeField] private float _movementSpeed = 5f;
    [SerializeField] private float _movementForce = 500f;
    private Vector2 _movement;
    private Vector3 _aimDirection;

    private void OnValidate() {
        if (_activeRagdoll == null) _activeRagdoll = GetComponent<ActiveRagdoll>();
        if (_physicsModule == null) _physicsModule = GetComponent<PhysicsModule>();
        if (_animationModule == null) _animationModule = GetComponent<AnimationModule>();
        if (_gripModule == null) _gripModule = GetComponent<GripModule>();
        if (_cameraModule == null) _cameraModule = GetComponent<CameraModule>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) {
            return;
        }

        _activeRagdoll.Input.OnMoveDelegates += MovementInput;
        _activeRagdoll.Input.OnMoveDelegates += _physicsModule.ManualTorqueInput;
        _activeRagdoll.Input.OnFloorChangedDelegates += ProcessFloorChanged;

        _activeRagdoll.Input.OnLeftArmDelegates += _animationModule.UseLeftArm;
        _activeRagdoll.Input.OnLeftArmDelegates += _gripModule.UseLeftGrip;
        _activeRagdoll.Input.OnRightArmDelegates += _animationModule.UseRightArm;
        _activeRagdoll.Input.OnRightArmDelegates += _gripModule.UseRightGrip;
    }

    private void Update() {
        if (!IsOwner) return;

        _aimDirection = _cameraModule.GetCameraForward();
        _animationModule.AimDirection = _aimDirection;

        UpdateMovement();
    }
    
    private void UpdateMovement() {
        if (_movement == Vector2.zero || !_enableMovement) {
            if (!_enableMovement) {
            }
            SetAnimatorMoving(false, 0f);
            return;
        }

        SetAnimatorMoving(true, _movement.magnitude);

        // Null checks
        if (_physicsModule == null) {
            return;
        }

        if (_activeRagdoll == null || _activeRagdoll.PhysicalTorso == null) {
            return;
        }

        float angleOffset = Vector2.SignedAngle(_movement, Vector2.up);

        Vector3 floorProjection = Auxiliary.GetFloorProjection(_aimDirection);

        Vector3 targetForward = Quaternion.AngleAxis(angleOffset, Vector3.up) * floorProjection;

        _physicsModule.TargetDirection = targetForward;

        // Apply movement force to actually move the character
        Vector3 moveDirection = targetForward.normalized;
        Vector3 currentVelocity = _activeRagdoll.PhysicalTorso.linearVelocity;
        Vector3 targetVelocity = moveDirection * (_movementSpeed * _movement.magnitude);


        // Only apply force to horizontal velocity (preserve vertical for jumping/falling)
        targetVelocity.y = currentVelocity.y;
        Vector3 velocityChange = targetVelocity - currentVelocity;
        velocityChange.y = 0;

        _activeRagdoll.PhysicalTorso.AddForce(velocityChange * (_movementForce * Time.deltaTime), ForceMode.Acceleration);
    }

    private void SetAnimatorMoving(bool isMoving, float speed) {

        // Set locally
        if (_animationModule == null || _animationModule.Animator == null) {
            return;
        }

        _animationModule.Animator.SetBool("moving", isMoving);
        _animationModule.Animator.SetFloat("speed", speed);

        // Sync to network
        if (IsServer) {
            SetAnimatorMovingClientRpc(isMoving, speed);
        } else {
            SetAnimatorMovingServerRpc(isMoving, speed);
        }
    }

    [ServerRpc]
    private void SetAnimatorMovingServerRpc(bool isMoving, float speed) {
        SetAnimatorMovingClientRpc(isMoving, speed);
    }

    [ClientRpc]
    private void SetAnimatorMovingClientRpc(bool isMoving, float speed) {
        if (IsOwner) return; // Owner already set it locally

        if (_animationModule == null || _animationModule.Animator == null) {
            return;
        }

        _animationModule.Animator.SetBool("moving", isMoving);
        _animationModule.Animator.SetFloat("speed", speed);
    }

    private void ProcessFloorChanged(bool onFloor)
    {
        if(!IsServer) return;

        if (onFloor) {
            _physicsModule.SetBalanceMode(PhysicsModule.BALANCE_MODE.STABILIZER_JOINT);
            _enableMovement = true;
            _activeRagdoll.GetBodyPart("Head Neck")?.SetStrengthScale(1);
            _activeRagdoll.GetBodyPart("Right Leg")?.SetStrengthScale(1);
            _activeRagdoll.GetBodyPart("Left Leg")?.SetStrengthScale(1);
            _animationModule.PlayAnimation("Idle");
        }
        else {
            _physicsModule.SetBalanceMode(PhysicsModule.BALANCE_MODE.MANUAL_TORQUE);
            _enableMovement = false;
            _activeRagdoll.GetBodyPart("Head Neck")?.SetStrengthScale(0.1f);
            _activeRagdoll.GetBodyPart("Right Leg")?.SetStrengthScale(0.05f);
            _activeRagdoll.GetBodyPart("Left Leg")?.SetStrengthScale(0.05f);
            _animationModule.PlayAnimation("InTheAir");
        }
    }

    private void MovementInput(Vector2 movement) {
        Debug.Log($"[DefaultBehaviour] MovementInput received: {movement}, IsOwner: {IsOwner}");
        _movement = movement;
    }
}