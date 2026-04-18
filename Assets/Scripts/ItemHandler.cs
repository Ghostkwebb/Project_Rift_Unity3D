using UnityEngine;
using UnityEngine.InputSystem;

public class ItemHandler : MonoBehaviour
{
    public PlayerMovement playerMove;
    public Transform holdPosition;
    public Camera mainCamera;
    public Collider playerCollider;
    public float throwForce = 15f;
    public float grabDistance = 3f;
    public float grabRadius = 0.5f;
    public float holdSpeed = 15f;

    private GameObject heldItem;
    private Rigidbody heldRb;
    private Collider heldCol;

    void Update()
    {
        if (playerMove.controls.Player.Interact.WasPressedThisFrame())
        {
            if (heldItem == null) TryGrab(); else DropItem();
        }

        if (playerMove.controls.Player.Throw.WasPressedThisFrame())
        {
            if (heldItem != null) ThrowItem();
        }
    }

    void FixedUpdate()
    {
        // Move via physics, not Transform. Stop at walls.
        if (heldItem != null && heldRb != null)
        {
            Vector3 moveDir = holdPosition.position - heldItem.transform.position;
            heldRb.linearVelocity = moveDir * holdSpeed;
            heldRb.MoveRotation(Quaternion.Slerp(heldItem.transform.rotation, holdPosition.rotation, Time.fixedDeltaTime * 10f));
        }
    }

    void TryGrab()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        int layerMask = ~(1 << 2);

        if (Physics.SphereCast(ray, grabRadius, out RaycastHit hit, grabDistance, layerMask))
        {
            if (hit.collider.CompareTag("ScavengeItem"))
            {
                heldItem = hit.collider.gameObject;
                heldRb = heldItem.GetComponent<Rigidbody>();
                heldCol = heldItem.GetComponent<Collider>();

                if (playerCollider != null) Physics.IgnoreCollision(heldCol, playerCollider, true);

                // Physics hold setup
                heldRb.useGravity = false;
                heldRb.linearDamping = 10f; // Stop shake
                heldRb.angularDamping = 10f;
            }
        }
    }

    void DropItem() => ReleaseItem(false);
    void ThrowItem() => ReleaseItem(true);

    void ReleaseItem(bool isThrow)
    {
        // Reset physics
        heldRb.useGravity = true;
        heldRb.linearDamping = 0f;
        heldRb.angularDamping = 0.05f;

        if (isThrow) heldRb.AddForce(mainCamera.transform.forward * throwForce, ForceMode.Impulse);

        heldItem = null;
        heldRb = null;
        heldCol = null;
    }
}