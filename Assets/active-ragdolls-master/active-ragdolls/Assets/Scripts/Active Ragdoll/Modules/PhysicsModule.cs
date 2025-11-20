#pragma warning disable 649
using Brickface.Networking.Physics;
using Unity.Netcode;
using UnityEngine;

    public class PhysicsModule : NetworkBehaviour {
        public enum BALANCE_MODE {
            UPRIGHT_TORQUE,
            MANUAL_TORQUE,
            STABILIZER_JOINT,
            FREEZE_ROTATIONS,
            NONE,
        }

        [Header("--- GENERAL ---")]
        [SerializeField] private BALANCE_MODE _balanceMode = BALANCE_MODE.STABILIZER_JOINT;
        public BALANCE_MODE BalanceMode { get { return _balanceMode; } }
        public float customTorsoAngularDrag = 0.05f;

        [Header("--- UPRIGHT TORQUE ---")]
        public float uprightTorque = 10000;
        [Tooltip("Defines how much torque percent is applied given the inclination angle percent [0, 1]")]
        public AnimationCurve uprightTorqueFunction;
        public float rotationTorque = 500;

        [Header("--- MANUAL TORQUE ---")]
        public float manualTorque = 500;
        public float maxManualRotSpeed = 5;

        private Vector2 _torqueInput;

        [Header("--- STABILIZER JOINT ---")]
        [SerializeField] private JointDriveConfig _stabilizerJointDrive;
        [SerializeField] private PlayerRagdoll _playerRagdoll;

        public JointDriveConfig StabilizerJointDrive {
            get { return _stabilizerJointDrive; }
            set { if (_stabilizerJoint != null)
                    _stabilizerJoint.angularXDrive = _stabilizerJoint.angularXDrive = (JointDrive)value;
                }
        }

        private GameObject _stabilizerGameobject;
        private Rigidbody _stabilizerRigidbody;
        private ConfigurableJoint _stabilizerJoint;

        [Header("--- FREEZE ROTATIONS ---")]
        [SerializeField] private float freezeRotationSpeed = 5;


        public Vector3 TargetDirection { get; set; }
        private Quaternion _targetRotation;



        private void Start() {
            if(!IsServer) return;
            
            UpdateTargetRotation();
            InitializeStabilizerJoint();
            StartBalance();
        }

        
        private void InitializeStabilizerJoint() {
            _stabilizerGameobject = new GameObject("Stabilizer", typeof(Rigidbody), typeof(ConfigurableJoint));
            _stabilizerGameobject.transform.parent = _playerRagdoll.PhysicalTorso.parent;
            _stabilizerGameobject.transform.rotation = _playerRagdoll.PhysicalTorso.rotation;

            _stabilizerJoint = _stabilizerGameobject.GetComponent<ConfigurableJoint>();
            _stabilizerRigidbody = _stabilizerGameobject.GetComponent<Rigidbody>();
            _stabilizerRigidbody.isKinematic = true;

            var joint = _stabilizerGameobject.GetComponent<ConfigurableJoint>();
            joint.connectedBody = _playerRagdoll.GetPhysicalTorsoRigidbody();
        }

        private void FixedUpdate() {
            if(!IsServer) return;
            UpdateTargetRotation();
            ApplyCustomDrag();

            switch (_balanceMode) {
                case BALANCE_MODE.UPRIGHT_TORQUE:
                    var balancePercent = Vector3.Angle(_playerRagdoll.GetPhysicalTorsoRigidbody().transform.up,
                                                         Vector3.up) / 180;
                    balancePercent = uprightTorqueFunction.Evaluate(balancePercent);
                    var rot = Quaternion.FromToRotation(_playerRagdoll.GetPhysicalTorsoRigidbody().transform.up,
                                                         Vector3.up).normalized;

                    _playerRagdoll.GetPhysicalTorsoRigidbody().AddTorque(new Vector3(rot.x, rot.y, rot.z)
                                                                * uprightTorque * balancePercent);

                    var directionAnglePercent = Vector3.SignedAngle(_playerRagdoll.GetPhysicalTorsoRigidbody().transform.forward,
                                        TargetDirection, Vector3.up) / 180;
                    _playerRagdoll.GetPhysicalTorsoRigidbody().AddRelativeTorque(0, directionAnglePercent * rotationTorque, 0);
                    break;

                case BALANCE_MODE.FREEZE_ROTATIONS:
                    var smoothedRot = Quaternion.Lerp(_playerRagdoll.GetPhysicalTorsoRigidbody().rotation,
                                       _targetRotation, Time.fixedDeltaTime * freezeRotationSpeed);
                    _playerRagdoll.GetPhysicalTorsoRigidbody().MoveRotation(smoothedRot);

                    break;

                case BALANCE_MODE.STABILIZER_JOINT:
                    _stabilizerRigidbody.MovePosition(_playerRagdoll.GetPhysicalTorsoRigidbody().position);
                    _stabilizerRigidbody.MoveRotation(_targetRotation);

                    break;

                case BALANCE_MODE.MANUAL_TORQUE:
                    if (_playerRagdoll.GetPhysicalTorsoRigidbody().angularVelocity.magnitude < maxManualRotSpeed) {
                        var force = _torqueInput * manualTorque;
                        _playerRagdoll.GetPhysicalTorsoRigidbody().AddRelativeTorque(force.y, 0, force.x);
                    }

                    break;

                default: break;
            }
        }

        private void UpdateTargetRotation() {
            if(!IsOwner) return;

            if (TargetDirection != Vector3.zero)
                _targetRotation = Quaternion.LookRotation(TargetDirection, Vector3.up);
            else
                _targetRotation = Quaternion.identity;
        }

        private void ApplyCustomDrag() {
            if(!IsOwner) return;

            if (_playerRagdoll.GetPhysicalTorsoRigidbody().isKinematic) {
                Debug.LogError($"[PhysicsModule] PhysicalTorso is KINEMATIC! Forcing to non-kinematic. IsOwner: {IsOwner}");
                _playerRagdoll.GetPhysicalTorsoRigidbody().isKinematic = false;
                return; 
            }

            var angVel = _playerRagdoll.GetPhysicalTorsoRigidbody().angularVelocity;
            angVel -= (Mathf.Pow(angVel.magnitude, 2) * customTorsoAngularDrag) * angVel.normalized;
           _playerRagdoll.GetPhysicalTorsoRigidbody().angularVelocity = angVel;
        }

        public void SetBalanceMode(BALANCE_MODE balanceMode) {
            if(!IsServer) return;

            if (_balanceMode == balanceMode) {
                Debug.LogWarning("SetBalanceMode was called but the mode selected was the same as the current one. No changes made."); ;
                return;
            }

            StopBalance();
            _balanceMode = balanceMode;
            StartBalance();
        }

        private void StartBalance() {
            switch (_balanceMode) {
                case BALANCE_MODE.UPRIGHT_TORQUE:
                    break;

                case BALANCE_MODE.FREEZE_ROTATIONS:
                    _playerRagdoll.GetPhysicalTorsoRigidbody().constraints = RigidbodyConstraints.FreezeRotation;
                    break;

                case BALANCE_MODE.STABILIZER_JOINT:
                    var jointDrive = (JointDrive) _stabilizerJointDrive;
                    _stabilizerJoint.angularXDrive = _stabilizerJoint.angularYZDrive = jointDrive;
                    break;

                case BALANCE_MODE.MANUAL_TORQUE:
                    break;

                default: break;
            }
        }

        private void StopBalance() {
            switch (_balanceMode) {
                case BALANCE_MODE.UPRIGHT_TORQUE:
                    break;

                case BALANCE_MODE.FREEZE_ROTATIONS:
                    _playerRagdoll.GetPhysicalTorsoRigidbody().constraints = 0;
                    break;

                case BALANCE_MODE.STABILIZER_JOINT:
                    var jointDrive = (JointDrive) JointDriveConfig.ZERO;
                    _stabilizerJoint.angularXDrive = _stabilizerJoint.angularYZDrive = jointDrive;
                    break;

                case BALANCE_MODE.MANUAL_TORQUE:
                    break;

                default: break;
            }
        }

        public void ManualTorqueInput(Vector2 torqueInput) {
            if(!IsServer) return;

            _torqueInput = torqueInput;
        }
    }
