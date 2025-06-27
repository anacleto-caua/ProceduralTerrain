using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    #region Vars
    public CharacterController characterController;

    public Camera mainCamera;

    CameraInputActions.CameraActions cameraActions;

    private float cameraSpeed = 5f;
    private float cameraAcceleration = 2.5f;
    private float cameraDeacceleration = .3f;

    private float currentCameraSpeed = 0f;
    private Vector3 currentMovement = Vector3.zero;
    private Vector3 currentMoveInput;

    private float cameraRotationSensitivity = 5f;
    private float minPitchAngle = -90f; // Minimum vertical angle (looking down)
    private float maxPitchAngle = 90f;  // Maximum vertical angle (looking up)

    private float currentYaw = 0f;   // Y-axis rotation (left/right)
    private float currentPitch = 0f; // X-axis rotation (up/down)

    private bool isLookingUnlocked = false;
    #endregion Vars

    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        #region InputSetup
        cameraActions = new CameraInputActions().Camera;

        cameraActions.Move.started += context => OnMove(context);
        cameraActions.Move.performed += context => OnMove(context);
        cameraActions.Move.canceled += context => OnMove(context);

        cameraActions.Accelerate.started += context => OnAccelerate(context);
        cameraActions.Deaccelerate.started += context => OnDeaccelerate(context);

        cameraActions.Accelerate.canceled += context => OnLeaveCamera(context);
        cameraActions.Deaccelerate.canceled += context => OnLeaveCamera(context);

        cameraActions.Look.performed += context => OnLook(context);
        
        cameraActions.LookingLock.started += context => OnLookingLock(context);
        cameraActions.LookingLock.canceled += context => OnLookingLock(context);
        #endregion InputSetup
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
        cameraActions.Enable();
    }

    void OnDisable()
    {
        cameraActions.Disable();
    }
    #endregion InputSystemHandling
}
