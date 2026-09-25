using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions InputAction;

    [Header("MOVEMENT PARAMETERS")]
    public float walkSpeed;
    public float sprintSpeed;
    public float jumpForce;
    public Rigidbody rb;
    public bool isSprinting;
    private Vector2 moveDirection;

    [Header("CAMERA PARAMETERS")]
    [SerializeField] private Camera playerCamera;
    public float mouseSensitivity;
    public float lookLimitX;
    private float cameraAngle = 0f;
    private Vector2 lookInput;

    [Header("CROUCHING")]
    public CapsuleCollider hitbox;
    [SerializeField] private bool isCrouching;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float cameraHeight = 1.5f;

    [Header("GROUND DETECTION")]
    [SerializeField] private bool isGrounded = true;
    public float groundCheckDistance;
    public LayerMask groundLayer;

    private Pickup currentPickupInRange = null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        InputAction = new PlayerInputActions();
    }

    private void OnEnable()
    {
        InputAction.Player.Enable();

        InputAction.Player.Move.performed += context => moveDirection = context.ReadValue<Vector2>();
        InputAction.Player.Move.canceled += context => moveDirection = Vector2.zero;

        InputAction.Player.Look.performed += context => lookInput = context.ReadValue<Vector2>();
        InputAction.Player.Look.canceled += context => lookInput = Vector2.zero;

        InputAction.Player.Jump.performed += OnJump;
        InputAction.Player.Crouch.performed += OnCrouch;
        InputAction.Player.Sprint.performed += OnSprint;
        InputAction.Player.Sprint.canceled += OnSprint;
    }

    private void OnDisable()
    {
        InputAction.Player.Move.performed -= context => moveDirection = context.ReadValue<Vector2>();
        InputAction.Player.Move.canceled -= context => moveDirection = Vector2.zero;

        InputAction.Player.Look.performed -= context => lookInput = context.ReadValue<Vector2>();
        InputAction.Player.Look.canceled -= context => lookInput = Vector2.zero;

        InputAction.Player.Jump.performed -= OnJump;
        InputAction.Player.Crouch.performed -= OnCrouch;
        InputAction.Player.Sprint.performed -= OnSprint;
        InputAction.Player.Sprint.canceled -= OnSprint;
        InputAction.Player.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        LookHandler();

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[MANUAL INPUT] Physical 'E' key pressed down!");
            ExecuteManualInteraction();
        }
    }

    private void FixedUpdate()
    {
        GroundDetection();
        MoveHandler();
    }

    private void ExecuteManualInteraction()
    {
        if (currentPickupInRange != null)
        {
            Debug.Log("[DEBUG 2] Forwarding collect command to pickup: " + currentPickupInRange.gameObject.name);
            currentPickupInRange.Collect();
        }
        else
        {
            Debug.LogWarning("[DEBUG 3] 'E' pressed, but no pickup is currently detected near the player.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Pickup pickup = other.GetComponent<Pickup>();
        if (pickup != null)
        {
            currentPickupInRange = pickup;
            Debug.Log("[DEBUG 4] Player entered trigger of pickup: " + other.gameObject.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Pickup pickup = other.GetComponent<Pickup>();
        if (pickup != null && currentPickupInRange == pickup)
        {
            currentPickupInRange = null;
            Debug.Log("[DEBUG 5] Player exited trigger of pickup: " + other.gameObject.name);
        }
    }

    public void MoveHandler()
    {
        Vector3 moveDir = transform.right * moveDirection.x + transform.forward * moveDirection.y;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 targetVelocity = moveDir * currentSpeed;

        currentVelocity.x = targetVelocity.x;
        currentVelocity.z = targetVelocity.z;

        rb.linearVelocity = currentVelocity;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = !isSprinting;
    }

    public void LookHandler()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        cameraAngle -= mouseY;
        cameraAngle = Mathf.Clamp(cameraAngle, -lookLimitX, lookLimitX);

        playerCamera.transform.localRotation = Quaternion.Euler(cameraAngle, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    public void Crouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        hitbox.height = Mathf.Lerp(hitbox.height, targetHeight, 0.2f);

        Vector3 camLocalPos = playerCamera.transform.localPosition;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetHeight - 0.25f, Time.deltaTime * .2f);
        hitbox.height = targetHeight;
        playerCamera.transform.localPosition = camLocalPos;
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        isCrouching = !isCrouching;
        Crouch();
    }

    public void GroundDetection()
    {
        float currentScaleY = transform.localScale.y;
        Vector3 start = transform.position + (Vector3.up * 0.1f * currentScaleY);
        float dynamicCheckDistance = (groundCheckDistance + 0.1f) * currentScaleY;
        isGrounded = Physics.Raycast(start, Vector3.down, dynamicCheckDistance, groundLayer);
    }
}
