using UnityEngine;

namespace Brickface.Networking.Physics    
{
    [System.Serializable]
    public class RagdollBody
    {
        public Transform Head;
        public Transform Neck;
        public Transform Torso;
        public Transform Chest;
        public Transform LeftArm;
        public Transform LeftForearm;
        public Transform LeftHand;
        public Transform RightArm;
        public Transform RightForearm;
        public Transform RightHand;
        public Transform LeftThigh;
        public Transform LeftCalve;
        public Transform LeftFoot;
        public Transform RightThigh;
        public Transform RightCalve;
        public Transform RightFoot;

        public Transform[] GetBones()
        {
            return new Transform[] {
                Head,
                Neck,
                Torso,
                Chest,
                LeftArm,
                LeftForearm,
                LeftHand,
                RightArm,
                RightForearm,
                RightHand,
                LeftThigh,
                LeftCalve,
                LeftFoot,
                RightThigh,
                RightCalve,
                RightFoot
            };
        }
        public Transform GetTorso()
        {
            return Torso;
        }
    }
}