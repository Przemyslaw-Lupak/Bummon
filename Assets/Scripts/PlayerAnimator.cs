using UnityEngine;
using Unity.Netcode;
public class PlayerAnimator : NetworkBehaviour
{
    ConfigurableJoint[] _joints;
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        
    }

    // void FixedUpdate()
    // {
    //     if(!IsServer) return;

    //     UpdateJointTargets();
    //     UpdateIK();
    // }

    // private void UpdateJointTargets() {
    //     for (int i = 0; i < _joints.Length; i++) {
    //         ConfigurableJointExtensions.SetTargetRotationLocal(_joints[i], 
    //             _animatedBones[i + 1].localRotation, _initialJointsRotation[i]);
    //     }
    // }
}
