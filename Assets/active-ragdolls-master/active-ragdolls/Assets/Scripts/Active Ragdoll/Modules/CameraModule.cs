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
    public float keyboardRotationSpeed = 90f; // Degrees per second for A/D rotation
    public bool invertY = false;
    public bool invertX = false;

    [Header("--- LIMITS ---")]
    [Tooltip("How far can the camera look down.")]
    public float minVerticalAngle = -85;
    [Tooltip("How far can the camera look up.")]
    public float maxVerticalAngle = 85;
    [Tooltip("How far can the camera look left/right before body turns.")]
    public float maxHorizontalAngle = 90f; // Can look 90 degrees left/right
    [Tooltip("When clamped, how fast does body turn to recenter camera.")]
    public float bodyTurnSpeed = 180f; // Degrees per second

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
    private float _rotationInput; // A/D keyboard rotation
    private float _yaw = 0f;
    private float _pitch = 0f;
    private float _bodyYaw = 0f; // Track the body's rotation separately
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
        DontDestroyOnLoad(_cameraGameObject);

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

        // Add keyboard rotation (A/D)
        float keyboardRotation = _rotationInput * keyboardRotationSpeed * Time.deltaTime;

        // Update relative yaw (camera rotation relative to body)
        _yaw += mouseX + keyboardRotation;
        _pitch -= mouseY;
        
        // Clamp pitch (vertical)
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        // Handle horizontal (yaw) clamping and body rotation
        if (_yaw > maxHorizontalAngle) {
            // Looking too far right - turn body right
            float turnAmount = _yaw - maxHorizontalAngle;
            _bodyYaw += turnAmount; // Body turns right
            _yaw = maxHorizontalAngle; // Camera stays at limit
        } 
        else if (_yaw < -maxHorizontalAngle) {
            // Looking too far left - turn body left
            float turnAmount = _yaw + maxHorizontalAngle; // Negative value
            _bodyYaw += turnAmount; // Body turns left
            _yaw = -maxHorizontalAngle; // Camera stays at limit
        }

        // Camera rotation is body rotation + relative camera rotation
        float totalYaw = _bodyYaw + _yaw;
        Quaternion targetRotation = Quaternion.Euler(_pitch, totalYaw, 0f);
        
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
        _rotationInput = 0f;
    }

    private void RotateBody(float yawDelta) {
        // Body rotation happens automatically through PhysicsModule
        // It uses GetCameraForward() which includes the body yaw
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
            // Return the actual camera forward direction (includes body yaw)
            return _cameraGameObject.transform.forward;
        }
        return Vector3.forward;
    }

    public void UpdateRotationInput(float horizontalInput) {
        // Called by PlayerMovement to pass A/D input for rotation
        _rotationInput = horizontalInput;
    }

    public float GetBodyYaw() {
        return _bodyYaw;
    }

    public float GetRelativeYaw() {
        return _yaw;
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