using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Brickface.Networking.Physics    
{
    public class PlayerRagdoll : NetworkBehaviour
    {
        public InputModule Input;
        public AnimatorHelper AnimatorHelper;
        public ConfigurableJoint[] Joints;
        public Transform PhysicalTorso;
        public Transform FixedTorso;
        public Transform[] AnimatedBones;
        
        [SerializeField] private int updateRate = 15;
        [SerializeField] private int _solverIterations = 12;
        [SerializeField] private int _solverVelocityIterations = 4;
        [SerializeField] private float _maxAngularVelocity = 50;

        [SerializeField] private RagdollBody _ragdollBody;
        [SerializeField] private Animator _animatorFixed;
        [SerializeField] private Animator _animatorPhysics;
        
        private Transform[] _syncedBones;
        private Rigidbody[] _allRigidbodies;
        private const int SNAPSHOT_BUFFER_SIZE = 3;
        private RagdollSnapshot[] _snapshotBuffer;
        private int _snapshotIndex = 0;
        private float _updateTimer = 0f;
        List<BodyPart> _bodyParts = new List<BodyPart>();

        void OnValidate()
        {
            if(_bodyParts.Count == 0)
                GetDefaultBodyParts();
        }

        private void Awake()
        {
            _syncedBones = _ragdollBody.GetBones();
            _allRigidbodies = GetComponentsInChildren<Rigidbody>();
            _snapshotBuffer = new RagdollSnapshot[SNAPSHOT_BUFFER_SIZE];

            Joints = PhysicalTorso?.GetComponentsInChildren<ConfigurableJoint>();
            AnimatedBones = FixedTorso?.GetComponentsInChildren<Transform>();

            for (int i = 0; i < SNAPSHOT_BUFFER_SIZE; i++)
            {
                _snapshotBuffer[i] = new RagdollSnapshot(_syncedBones.Length);
            }
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[PlayerRagdoll] OnNetworkSpawn - IsServer: {IsServer}, IsOwner: {IsOwner}, NetworkObjectId: {NetworkObjectId}");
            
            // Configure rigidbodies based on authority
            foreach(Rigidbody rigidbody in _allRigidbodies)
            {
                if (IsServer)
                {
                    // Server: Active physics
                    rigidbody.isKinematic = false;
                    rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    rigidbody.solverIterations = _solverIterations;
                    rigidbody.solverVelocityIterations = _solverVelocityIterations;
                    rigidbody.maxAngularVelocity = _maxAngularVelocity;
                    Debug.Log($"[PlayerRagdoll] Server: Set {rigidbody.name} to non-kinematic");
                }
                else
                {
                    // Clients: Kinematic (receive snapshots)
                    rigidbody.isKinematic = true;
                    rigidbody.detectCollisions = false;
                    Debug.Log($"[PlayerRagdoll] Client: Set {rigidbody.name} to kinematic");
                }
            }
            
            // Initialize body parts on server
            foreach(BodyPart bodyPart in _bodyParts)
            {
                if(IsServer)
                    bodyPart.Init();
            }
        }

        void FixedUpdate()
        {
            if(!IsSpawned) return;
            
            if(IsServer)
            {
                // Server: Sync animated body and send snapshots
                _updateTimer += Time.fixedDeltaTime;
                SyncAnimatedBody();
                
                if(_updateTimer >= 1f / updateRate)
                {
                    _updateTimer = 0f;
                    SendSnapshot();
                }
            }
            else
            {
                // Clients: Apply received snapshots
                ApplySnapshot();
            }
        }

        private void SendSnapshot()
        {
            Vector3[] positions = new Vector3[_syncedBones.Length];
            Quaternion[] rotations = new Quaternion[_syncedBones.Length];

            for(int i = 0; i < _syncedBones.Length; i++)
            {
                positions[i] = _syncedBones[i].position;
                rotations[i] = _syncedBones[i].rotation;
            }

            ReceiveSnapshotClientRpc(positions, rotations);
        }

        [ClientRpc]
        private void ReceiveSnapshotClientRpc(Vector3[] positions, Quaternion[] rotations)
        {
            if (IsServer) return; 
            
            _snapshotIndex = (_snapshotIndex + 1) % SNAPSHOT_BUFFER_SIZE;
            _snapshotBuffer[_snapshotIndex].BonePositions = positions;
            _snapshotBuffer[_snapshotIndex].BoneRotations = rotations;
            _snapshotBuffer[_snapshotIndex].Timestamp = Time.time;
        }

        private void ApplySnapshot()
        {
            var snapshot = _snapshotBuffer[_snapshotIndex];
            if (snapshot.BonePositions == null) return;
            
            float lerpSpeed = updateRate * 0.5f;
            
            for (int i = 0; i < _syncedBones.Length; i++)
            {
                _syncedBones[i].position = Vector3.Lerp(
                    _syncedBones[i].position,
                    snapshot.BonePositions[i],
                    Time.fixedDeltaTime * lerpSpeed
                );
                
                _syncedBones[i].rotation = Quaternion.Slerp(
                    _syncedBones[i].rotation,
                    snapshot.BoneRotations[i],
                    Time.fixedDeltaTime * lerpSpeed
                );
            }
            
            // Also sync the animated body on clients
            SyncAnimatedBody();
        }

        private void SyncAnimatedBody() {
            Transform torso = _ragdollBody.GetTorso();
            _animatorFixed.transform.position = torso.position + (_animatorFixed.transform.position - torso.position);
            _animatorFixed.transform.rotation = torso.rotation;
        }

        private void GetDefaultBodyParts() 
        {
            _bodyParts.Add(new BodyPart("Head Neck",
                TryGetJoints(HumanBodyBones.Head, HumanBodyBones.Neck)));
            _bodyParts.Add(new BodyPart("Torso",
                TryGetJoints(HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest)));
            _bodyParts.Add(new BodyPart("Left Arm",
                TryGetJoints(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand)));
            _bodyParts.Add(new BodyPart("Right Arm",
                TryGetJoints(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand)));
            _bodyParts.Add(new BodyPart("Left Leg",
                TryGetJoints(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot)));
            _bodyParts.Add(new BodyPart("Right Leg",
                TryGetJoints(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot)));
        }

        private List<ConfigurableJoint> TryGetJoints(params HumanBodyBones[] bones) 
        {
            List<ConfigurableJoint> jointList = new List<ConfigurableJoint>();
            foreach (HumanBodyBones bone in bones) {
                Transform boneTransform = _animatorPhysics.GetBoneTransform(bone);
                if (boneTransform != null && (boneTransform.TryGetComponent(out ConfigurableJoint joint)))
                    jointList.Add(joint);
            }
            return jointList;
        }

        public Transform GetAnimatedBone(HumanBodyBones bone) {
            return _animatorFixed.GetBoneTransform(bone);
        }

        public Transform GetPhysicalBone(HumanBodyBones bone) {
            return _animatorPhysics.GetBoneTransform(bone);
        }

        public BodyPart GetBodyPart(string name) {
            foreach (BodyPart bodyPart in _bodyParts)
                if (bodyPart.bodyPartName == name) return bodyPart;
            return null;
        }

        public void SetStrengthScaleForAllBodyParts(float scale) {
            foreach (BodyPart bodyPart in _bodyParts)
                bodyPart.SetStrengthScale(scale);
        }

        public Rigidbody GetPhysicalTorsoRigidbody()
        {
            return PhysicalTorso.GetComponent<Rigidbody>();
        }
    }
}