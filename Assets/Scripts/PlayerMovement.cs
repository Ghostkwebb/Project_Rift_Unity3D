using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float baseSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float swapSpeedMultiplier = 1f;
    public float jumpForce = 5f;
    public float mouseSensitivity = 0.2f;
    public Transform playerCamera;
    public Animator anim; // ASSIGN POLY PIZZA MODEL HERE

    public PlayerInputActions controls { get; private set; }

    private Rigidbody rb;
    private float xRotation = 0f;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private bool isSprinting;
    private bool isGrounded;

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

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        if (anim != null)
        {
            float speedScale = isSprinting ? 1f : 0.5f;

            anim.SetFloat("VelX", moveInput.x * speedScale, 0.1f, Time.deltaTime);
            anim.SetFloat("VelZ", moveInput.y * speedScale, 0.1f, Time.deltaTime);
        }

    }

    void FixedUpdate()
    {
        float currentSpeed = baseSpeed * swapSpeedMultiplier * (isSprinting ? sprintMultiplier : 1f);
        Vector3 moveDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        Vector3 targetVelocity = moveDir * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    void Jump()
    {
        if (isGrounded) rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    void ToggleCrouch(bool isCrouching)
    {
        transform.localScale = isCrouching ? new Vector3(1, 0.5f, 1) : new Vector3(1, 1f, 1);
    }
}