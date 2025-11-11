using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    #region Vars
    public CharacterController characterController;

    public Camera mainCamera;

    private CameraInputActions inputActions;
    CameraInputActions.CameraActions cameraActions;

    private float cameraSpeed = 30f;
    private float cameraAcceleration = 5f;
    private float cameraDeacceleration = .3f;

    private float currentCameraSpeed = 0f;
    private Vector3 currentMovement = Vector3.zero;
    private Vector3 currentMoveInput;

    private float cameraRotationSensitivity = 10f;
    private float minPitchAngle = -90f; // Minimum vertical angle (looking down)
    private float maxPitchAngle = 90f;  // Maximum vertical angle (looking up)

    private float currentYaw = 0f;   // Y-axis rotation (left/right)
    private float currentPitch = 0f; // X-axis rotation (up/down)

    private bool isLookingUnlocked = false;
    #endregion Vars
    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        inputActions = new CameraInputActions();
    }

    #region InputCallbacks
    void OnMove(InputAction.CallbackContext context)
    {
        currentMoveInput = context.ReadValue<Vector3>();
    }

    void OnAccelerate(InputAction.CallbackContext context)
    {
        currentCameraSpeed = cameraSpeed * cameraAcceleration;
    }

    void OnDeaccelerate(InputAction.CallbackContext context)
    {
        currentCameraSpeed = cameraSpeed * cameraDeacceleration;
    }

    void OnLeaveCamera(InputAction.CallbackContext context)
    {
        currentCameraSpeed = cameraSpeed;
    }

    void OnLook(InputAction.CallbackContext context)
    {
        if (!isLookingUnlocked)
        {
            return;
        }

        Vector2 lookInputDelta = context.ReadValue<Vector2>();
        currentYaw += lookInputDelta.x * cameraRotationSensitivity * Time.deltaTime;
        currentPitch -= lookInputDelta.y * cameraRotationSensitivity * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitchAngle, maxPitchAngle);

        characterController.transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    void OnLookingLock(InputAction.CallbackContext context)
    {
        isLookingUnlocked = context.ReadValueAsButton(); 
    }
    #endregion InputCallbacks

    private void Start()
    {
        currentCameraSpeed = cameraSpeed;
        characterController.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        currentMovement = currentCameraSpeed * Time.deltaTime * currentMoveInput;
        characterController.Move(characterController.transform.rotation * currentMovement);
    }

    #region InputSystemHandling
    void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new CameraInputActions();
        }
        cameraActions = inputActions.Camera;

        cameraActions.Move.started += OnMove;
        cameraActions.Move.performed += OnMove;
        cameraActions.Move.canceled += OnMove;

        cameraActions.Accelerate.started += OnAccelerate;
        cameraActions.Deaccelerate.started += OnDeaccelerate;

        cameraActions.Accelerate.canceled += OnLeaveCamera;
        cameraActions.Deaccelerate.canceled += OnLeaveCamera;

        cameraActions.Look.performed += OnLook;

        cameraActions.LookingLock.started += OnLookingLock;
        cameraActions.LookingLock.canceled += OnLookingLock;

        cameraActions.Enable();
    }

    void OnDisable()
    {
        // Unsubscribe from all events to prevent errors and memory leaks.
        if (inputActions != null)
        {
            cameraActions.Move.started -= OnMove;
            cameraActions.Move.performed -= OnMove;
            cameraActions.Move.canceled -= OnMove;

            cameraActions.Accelerate.started -= OnAccelerate;
            cameraActions.Deaccelerate.started -= OnDeaccelerate;

            cameraActions.Accelerate.canceled -= OnLeaveCamera;
            cameraActions.Deaccelerate.canceled -= OnLeaveCamera;

            cameraActions.Look.performed -= OnLook;

            cameraActions.LookingLock.started -= OnLookingLock;
            cameraActions.LookingLock.canceled -= OnLookingLock;

            // Disable the actions
            cameraActions.Disable();
        }
    }
    #endregion InputSystemHandling
}
