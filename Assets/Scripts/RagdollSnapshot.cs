using UnityEngine;

namespace Brickface.Networking.Physics    
{
    public class RagdollSnapshot : MonoBehaviour
    {
        public Vector3[] BonePositions;
        public Quaternion[] BoneRotations;
        public float Timestamp;

        public RagdollSnapshot(int boneCount)
        {
            BonePositions = new Vector3[boneCount];
            BoneRotations = new Quaternion[boneCount];
            Timestamp = 0f;
        }
    }
}
