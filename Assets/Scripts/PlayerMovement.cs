using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    public float baseSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float crouchMultiplier = 0.5f;
    public float swapSpeedMultiplier = 1f;

    [Header("Movement")]
    public float jumpForce = 5f;
    public float mouseSensitivity = 0.2f;
    public Transform cameraRoot;
    public Transform yawRoot;
    public Animator anim;
    public float footstepSoundRadius = 15f;

    [Header("Head Bob")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float sprintBobSpeed = 18f;
    public float sprintBobAmount = 0.1f;
    private float bobTimer;

    public PlayerInputActions controls { get; private set; }

    [HideInInspector] public float itemSpeedMultiplier = 1f;

    private Rigidbody rb;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private bool isSprinting;
    [HideInInspector] public bool isCrouching;
    private bool isGrounded;

    private Vector3 originalCamPos;
    public float crouchCameraBump = 0.5f;

    void Awake()
    {
        controls = new PlayerInputActions();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        controls.Player.Sprint.performed += ctx => isSprinting = true;
        controls.Player.Sprint.canceled += ctx => isSprinting = false;

        controls.Player.Crouch.performed += ctx => ToggleCrouch(true);
        controls.Player.Crouch.canceled += ctx => ToggleCrouch(false);

        controls.Player.Jump.performed += ctx => Jump();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        originalCamPos = cameraRoot.localPosition;

        if (anim != null) anim.SetFloat("CrouchTime", 0.4f);
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        yRotation += mouseX;
        xRotation = Mathf.Clamp(xRotation, -90f, 65f);

        if (yawRoot != null) yawRoot.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        if (anim != null)
        {
            float speedScale = 0.5f; // Walk base
            if (isSprinting && !isCrouching) speedScale = 1f; // Sprint
            if (isCrouching) speedScale = 0.25f; // Slow anim if crouching

            anim.SetFloat("VelX", moveInput.x * speedScale, 0.1f, Time.deltaTime);
            anim.SetFloat("VelZ", moveInput.y * speedScale, 0.1f, Time.deltaTime);
            anim.SetBool("IsGrounded", isGrounded);
        }

        // Head Bob & Smooth Crouch Camera 
        float targetHeight = originalCamPos.y;
        if (isCrouching) targetHeight += crouchCameraBump;

        float bobOffset = 0f;
        float flatSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;

        if (isGrounded && flatSpeed > 0.1f)
        {
            float bSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;
            float bAmount = isSprinting ? sprintBobAmount : walkBobAmount;
            if (isCrouching) { bSpeed = 8f; bAmount = 0.02f; }

            bobTimer += Time.deltaTime * bSpeed;
            bobOffset = Mathf.Sin(bobTimer) * bAmount;
        }
        else bobTimer = 0f;

        Vector3 finalCamPos = cameraRoot.localPosition;
        finalCamPos.y = Mathf.Lerp(finalCamPos.y, targetHeight + bobOffset, Time.deltaTime * 15f);
        cameraRoot.localPosition = finalCamPos;
    }

    void FixedUpdate()
    {
        float speedMod = 1f;
        if (isCrouching) speedMod = crouchMultiplier;
        else if (isSprinting) speedMod = sprintMultiplier;

        float currentSpeed = baseSpeed * swapSpeedMultiplier * speedMod * itemSpeedMultiplier;

        Vector3 forward = yawRoot.forward;
        Vector3 right = yawRoot.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 moveDir = (right * moveInput.x + forward * moveInput.y).normalized;
        Vector3 targetVelocity = moveDir * currentSpeed;

        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        if (!isCrouching && isGrounded && targetVelocity.magnitude > 0.5f)
        {
            float currentNoiseRadius = isSprinting ? 15f : 7f;
            NoiseSystem.MakeNoise(transform.position, currentNoiseRadius);
        }
    }

    void Jump()
    {
        if (isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            if (anim != null) anim.SetTrigger("Jump");
        }
    }

    void ToggleCrouch(bool crouchState)
    {
        isCrouching = crouchState;
        transform.localScale = isCrouching ? new Vector3(1, 0.5f, 1) : Vector3.one;
        if (anim != null) anim.SetBool("IsCrouching", isCrouching);

    }
}