using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions InputAction;

    [Header("ANIMATION PARAMETERS")]
    private Animator anim;
    private ShootHandler shootHandler; // Cached link to your existing ShootHandler component

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
        anim = GetComponent<Animator>();
        shootHandler = GetComponent<ShootHandler>(); // Automatically pair with your weapon script

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

        if (shootHandler == null)
        {
            Debug.LogError($"<b>[{gameObject.name}]</b> PlayerController failed to find the ShootHandler script on this GameObject!");
        }
    }

    void Update()
    {
        LookHandler();

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[MANUAL INPUT] Physical 'E' key pressed down!");
            ExecuteManualInteraction();
        }

        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        GroundDetection();
        MoveHandler();
    }

    /// <summary>
    /// Reads coordinates and handles stance booleans, keeping your 2D Locomotion Blend tree synced.
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        if (anim == null) return;

        // 1. Send movement magnitude values to drive walking/running loops
        float flatSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        anim.SetFloat("Speed", flatSpeed);

        // 2. Drive the 2D Blend tree stance slider (0f = standing, 1f = crouching)
        float crouchWeight = isCrouching ? 1f : 0f;
        anim.SetFloat("isCrouching", crouchWeight);

        // 3. Environment trackers
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);

        // 4. --- UPDATED AMMO LINK SYNC (FLOAT VERSION) ---
        // Reads from your exact ShootHandler script data to determine if a bow stance is allowed!
        if (shootHandler != null)
        {
            // If arrows > 0, weight is 1.0f (Alert Bow Idle). If 0, weight is 0.0f (Lowered Civilian Idle).
            float ammoWeight = (shootHandler.GetCurrentAmmoCount() > 0) ? 1f : 0f;
            anim.SetFloat("hasAmmo", ammoWeight);
        }
    }


    // Call this function when picking up quiver items to route data safely to your core inventory handler
    public void AddAmmunition(int amount)
    {
        if (shootHandler != null)
        {
            shootHandler.AddAmmo(amount);
        }
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
            if (anim != null) anim.SetTrigger("JumpTrigger");
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
