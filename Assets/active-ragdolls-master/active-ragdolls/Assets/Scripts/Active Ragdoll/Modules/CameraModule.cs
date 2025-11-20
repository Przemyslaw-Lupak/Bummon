using Brickface.Networking.Physics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraModule : NetworkBehaviour {
    [SerializeField] private PlayerRagdoll  _playerRagdoll;
    [SerializeField] private Animator _animatorFixed;
    [Header("--- GENERAL ---")]
    [Tooltip("Where the camera should be positioned. Head by default.")]
    public Transform _cameraPoint;
    public float lookSensitivity = 2f;
    public bool invertY = false;
    public bool invertX = false;

    [Header("--- LIMITS ---")]
    [Tooltip("How far can the camera look down.")]
    public float minVerticalAngle = -85;
    [Tooltip("How far can the camera look up.")]
    public float maxVerticalAngle = 85;

    [Header("--- SMOOTHING ---")]
    public bool enableSmoothing = false;
    public float rotationSpeed = 15f;

    [Header("--- HEAD BOB ---")]
    public bool enableHeadBob = false;
    public float bobFrequency = 2f;
    public float bobHorizontalAmount = 0.05f;
    public float bobVerticalAmount = 0.05f;

    [Header("--- CAMERA SETTINGS ---")]
    public float fieldOfView = 60f;
    public float nearClipPlane = 0.01f;
    public float farClipPlane = 1000f;

    [Header("--- VISIBILITY ---")]
    public bool hideOwnBody = false;
    [Tooltip("If you want to hide only certain parts (like head) to prevent blocking view")]
    public bool hideHeadOnly = true;

    // Private fields
    private GameObject _cameraGameObject;
    private Camera _camera;
    private Vector2 _inputDelta;
    private float _yaw = 0f;
    private float _pitch = 0f;
    private Vector3 _lastHeadPosition;
    private bool _isInitialized = false;
    private float _bobTimer = 0f;

    private void OnValidate() {
        if (_cameraPoint == null && _playerRagdoll != null)
            _cameraPoint = _playerRagdoll.GetPhysicalBone(HumanBodyBones.Head);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        // Only create camera for the local player
        if (!IsOwner) {
            enabled = false;
            return;
        }

        InitializeFirstPersonCamera();
    }

    private void InitializeFirstPersonCamera() {
        // Create camera GameObject
        _cameraGameObject = new GameObject("First Person Camera");
        _cameraGameObject.transform.position = _cameraPoint.position;
        _cameraGameObject.transform.rotation = Quaternion.identity;

        // Add Camera component
        _camera = _cameraGameObject.AddComponent<Camera>();
        _camera.fieldOfView = fieldOfView;
        _camera.nearClipPlane = nearClipPlane;
        _camera.farClipPlane = farClipPlane;
        
        // Tag as MainCamera
        _cameraGameObject.tag = "MainCamera";
        
        // Add AudioListener
        _cameraGameObject.AddComponent<AudioListener>();

        // Initialize rotation
        _yaw = 0f;
        _pitch = 0f;
        
        // Initialize position tracking
        _lastHeadPosition = _cameraPoint.position;
        _isInitialized = true;
        
        
        
        // Lock and hide cursor for FPS controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    

    private void LateUpdate() {
        if (!IsOwner || !_isInitialized) return;
        
        UpdateCameraPosition();
        UpdateCameraRotation();
        UpdateHeadBob();
    }

    private void UpdateCameraPosition() {
        if (_cameraPoint == null) return;
        
        // Calculate the movement delta of the head
        Vector3 headMovement = _cameraPoint.position - _lastHeadPosition;
        
        // Move camera by the same amount
        _cameraGameObject.transform.position += headMovement;
        
        // Update last position
        _lastHeadPosition = _cameraPoint.position;
    }

    private void UpdateCameraRotation() {
        // Get mouse input
        float mouseX = _inputDelta.x * lookSensitivity * (invertX ? -1 : 1);
        float mouseY = _inputDelta.y * lookSensitivity * (invertY ? -1 : 1);

        // Update yaw and pitch
        _yaw += mouseX;
        _pitch -= mouseY;
        
        // Clamp pitch
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        // Apply rotation to camera only
        Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        
        if (enableSmoothing) {
            _cameraGameObject.transform.rotation = Quaternion.Slerp(
                _cameraGameObject.transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        } else {
            _cameraGameObject.transform.rotation = targetRotation;
        }
        
        // Reset input delta
        _inputDelta = Vector2.zero;
    }

    private void UpdateHeadBob() {
        if (!enableHeadBob) return;

        // Check if player is moving
        bool isMoving = _animatorFixed.GetBool("moving");
        
        if (isMoving) {
            _bobTimer += Time.deltaTime * bobFrequency;
            
            // Calculate bob offset
            float horizontalBob = Mathf.Sin(_bobTimer) * bobHorizontalAmount;
            float verticalBob = Mathf.Abs(Mathf.Sin(_bobTimer * 2)) * bobVerticalAmount;
            
            // Apply bob offset to position
            Vector3 bobOffset = _cameraGameObject.transform.right * horizontalBob + 
                               _cameraGameObject.transform.up * verticalBob;
            _cameraGameObject.transform.position += bobOffset;
        } else {
            _bobTimer = 0;
        }
    }

    // Public accessor methods
    public Vector3 GetCameraForward() {
        if (_cameraGameObject != null) {
            return _cameraGameObject.transform.forward;
        }
        return Vector3.forward;
    }

    public Transform GetCameraTransform() {
        return _cameraGameObject?.transform;
    }

    public Camera GetCamera() {
        return _camera;
    }

    public float GetHorizontalRotation() {
        return _yaw;
    }

    public float GetVerticalRotation() {
        return _pitch;
    }

    public void SetFieldOfView(float fov) {
        if (_camera != null) {
            _camera.fieldOfView = fov;
        }
    }

    // Input handlers
    public void OnLook(InputValue value) {
        if (!IsOwner) return;
        _inputDelta += value.Get<Vector2>();
    }
}