using System;
using UnityEditor;
using Unity.Netcode;
using UnityEngine;
using Brickface.Networking.Physics;

public class AnimationModule : NetworkBehaviour {
    [SerializeField] private PlayerRagdoll _playerRagdoll;
    [SerializeField] private Animator _fixedAnimator;
    [Header("--- BODY ---")]
    private Quaternion[] _initialJointsRotation;
    private ConfigurableJoint[] _joints;
    private Transform[] _animatedBones;
    private AnimatorHelper _animatorHelper;
    public Animator Animator { get; private set; }

    [Header("--- INVERSE KINEMATICS ---")]
    public bool _enableIK = true;
    public float minTargetDirAngle = -30, maxTargetDirAngle = 60;
    public float minArmsAngle = -70, maxArmsAngle = 100;
    public float minLookAngle = -50, maxLookAngle = 60;
    public float lookAngleOffset, armsAngleOffset;
    public float handsRotationOffset = 0;
    public float armsHorizontalSeparation = 0.75f;
    public AnimationCurve armsDistance;
    
    public Vector3 AimDirection { get; set; }
    private Vector3 _armsDir, _lookDir, _targetDir2D;
    private Transform _animTorso, _chest;
    private float _targetDirVerticalPercent;

    public override void OnNetworkSpawn() {

        Animator = _fixedAnimator;
        if (!IsOwner) return; 
        _joints = _playerRagdoll.Joints;
        _animatedBones = _playerRagdoll.AnimatedBones;
        _animatorHelper = _playerRagdoll.AnimatorHelper;

        _initialJointsRotation = new Quaternion[_joints.Length];
        for (int i = 0; i < _joints.Length; i++) {
            _initialJointsRotation[i] = _joints[i].transform.localRotation;
        }
    }

    void FixedUpdate() {
        if (!IsOwner || !IsServer) return;
        
        UpdateJointTargets();
        UpdateIK();
    }

    private void UpdateJointTargets() {
        for (int i = 0; i < _joints.Length; i++) {
            ConfigurableJointExtensions.SetTargetRotationLocal(_joints[i], 
                _animatedBones[i + 1].localRotation, _initialJointsRotation[i]);
        }
    }

    private void UpdateIK() {
        if (_animatorHelper == null) return; // Safety check

        if (!_enableIK) {
            _animatorHelper.LeftArmIKWeight = 0;
            _animatorHelper.RightArmIKWeight = 0;
            _animatorHelper.LookIKWeight = 0;
            return;
        }
        _animatorHelper.LookIKWeight = 1;

        _animTorso = _playerRagdoll.FixedTorso;
        _chest = _playerRagdoll.GetAnimatedBone(HumanBodyBones.Spine);
        ReflectBackwards();
        _targetDir2D = Auxiliary.GetFloorProjection(AimDirection);
        CalculateVerticalPercent();

        UpdateLookIK();
        UpdateArmsIK();
    }

    private void ReflectBackwards() {
        bool lookingBackwards = Vector3.Angle(AimDirection, _animTorso.forward) > 90;
        if (lookingBackwards) AimDirection = Vector3.Reflect(AimDirection, _animTorso.forward);
    }

    private void CalculateVerticalPercent() {
        float directionAngle = Vector3.Angle(AimDirection, Vector3.up);
        directionAngle -= 90;
        _targetDirVerticalPercent = 1 - Mathf.Clamp01((directionAngle - minTargetDirAngle) / 
            Mathf.Abs(maxTargetDirAngle - minTargetDirAngle));
    }

    private void UpdateLookIK() {
        // Use the aim direction directly for looking
        Vector3 lookPoint = _playerRagdoll.GetAnimatedBone(HumanBodyBones.Head).position + AimDirection * 5f;
        _animatorHelper.LookAtPoint(lookPoint);
    }

    private void UpdateArmsIK() {
        if (_animatorHelper.LeftHandTarget == null || _animatorHelper.RightHandTarget == null) {
            Debug.LogWarning("[AnimationModule] IK targets not initialized yet");
            return;
        }

        float armsVerticalAngle = _targetDirVerticalPercent * Mathf.Abs(maxArmsAngle - minArmsAngle) + minArmsAngle;
        armsVerticalAngle += armsAngleOffset;
        _armsDir = Quaternion.AngleAxis(-armsVerticalAngle, _animTorso.right) * _targetDir2D;

        float currentArmsDistance = armsDistance.Evaluate(_targetDirVerticalPercent);

        Vector3 armsMiddleTarget = _chest.position + _armsDir * currentArmsDistance;
        Vector3 upRef = Vector3.Cross(_armsDir, _animTorso.right).normalized;
        Vector3 armsHorizontalVec = Vector3.Cross(_armsDir, upRef).normalized;
        Quaternion handsRot = _armsDir != Vector3.zero ? Quaternion.LookRotation(_armsDir, upRef)
            : Quaternion.identity;

        _animatorHelper.LeftHandTarget.position = armsMiddleTarget + armsHorizontalVec * armsHorizontalSeparation / 2;
        _animatorHelper.RightHandTarget.position = armsMiddleTarget - armsHorizontalVec * armsHorizontalSeparation / 2;
        _animatorHelper.LeftHandTarget.rotation = handsRot * Quaternion.Euler(0, 0, 90 - handsRotationOffset);
        _animatorHelper.RightHandTarget.rotation = handsRot * Quaternion.Euler(0, 0, -90 + handsRotationOffset);

        var armsUpVec = Vector3.Cross(_armsDir, _animTorso.right).normalized;
        _animatorHelper.LeftHandHint.position = armsMiddleTarget + armsHorizontalVec * armsHorizontalSeparation - armsUpVec;
        _animatorHelper.RightHandHint.position = armsMiddleTarget - armsHorizontalVec * armsHorizontalSeparation - armsUpVec;
    }

    public void PlayAnimation(string animation, float speed = 1) {
        if (!IsOwner) return;
        
        Animator.Play(animation);
        Animator.SetFloat("speed", speed);
        
        // Sync animation state to other clients
        PlayAnimationServerRpc(animation, speed);
    }
    
    [ServerRpc]
    private void PlayAnimationServerRpc(string animation, float speed) {
        PlayAnimationClientRpc(animation, speed);
    }
    
    [ClientRpc]
    private void PlayAnimationClientRpc(string animation, float speed) {
        if (IsOwner) return; // Owner already set it locally
        
        Animator.Play(animation);
        Animator.SetFloat("speed", speed);
    }
    
    public void UseLeftArm(float weight) {
        if (!_enableIK || !IsOwner) return;
        _animatorHelper.LeftArmIKWeight = weight;
    }

    public void UseRightArm(float weight) {
        if (!_enableIK || !IsOwner) return;
        _animatorHelper.RightArmIKWeight = weight;
    }

    public void SetMovment(bool isMoving, float speed) {
        if (!IsServer) return;
        Animator.SetBool("moving", isMoving);
        Animator.SetFloat("speed", speed);
    }
}