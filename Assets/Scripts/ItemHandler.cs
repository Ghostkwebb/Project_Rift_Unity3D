using UnityEngine;
using UnityEngine.InputSystem;

public class ItemHandler : MonoBehaviour
{
    public PlayerMovement playerMove;
    public Transform holdPosition;
    public Transform torchHoldPosition;
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
            if (playerMove.anim != null) playerMove.anim.SetTrigger("Interact");
            if (heldItem == null) TryGrab(); else DropItem();
        }

        if (playerMove.controls.Player.Throw.WasPressedThisFrame())
        {
            if (heldItem != null)
            {
                if (playerMove.anim != null) playerMove.anim.SetTrigger("Throw");
                ThrowItem();
            }
        }
    }

    void FixedUpdate()
    {
        if (heldItem != null && heldRb != null)
        {
            Transform targetHold = heldItem.GetComponent<TorchItem>() != null ? torchHoldPosition : holdPosition;

            Vector3 moveDir = targetHold.position - heldItem.transform.position;
            heldRb.linearVelocity = moveDir * holdSpeed;
            heldRb.MoveRotation(Quaternion.Slerp(heldItem.transform.rotation, targetHold.rotation, Time.fixedDeltaTime * 10f));
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

                heldRb.useGravity = false;
                heldRb.linearDamping = 10f;
                heldRb.angularDamping = 10f;
            }
        }
    }

    void DropItem() => ReleaseItem(false);
    void ThrowItem() => ReleaseItem(true);

    void ReleaseItem(bool isThrow)
    {
        heldRb.useGravity = true;
        heldRb.linearDamping = 0f;
        heldRb.angularDamping = 0.05f;

        if (isThrow) heldRb.AddForce(mainCamera.transform.forward * throwForce, ForceMode.Impulse);

        heldItem = null;
        heldRb = null;
        heldCol = null;
    }
}