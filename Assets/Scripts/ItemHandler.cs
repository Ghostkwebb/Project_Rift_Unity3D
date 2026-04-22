using UnityEngine;
using UnityEngine.InputSystem;

public class ItemHandler : MonoBehaviour
{
    public PlayerMovement playerMove;
    public Transform holdPosition;
    public Transform torchHoldPosition;
    public Camera mainCamera;
    public Collider playerCollider;
    public float grabDistance = 3f;
    public float grabRadius = 0.5f;

    private GameObject heldItem;
    private Rigidbody heldRb;
    private ThrowableItem heldThrowable; // NEW

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
            heldRb.linearVelocity = moveDir * 50f;
            heldRb.MoveRotation(Quaternion.Slerp(heldItem.transform.rotation, targetHold.rotation, Time.fixedDeltaTime * 25f));
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
                heldThrowable = heldItem.GetComponent<ThrowableItem>();

                if (playerCollider != null) Physics.IgnoreCollision(heldItem.GetComponent<Collider>(), playerCollider, true);

                heldRb.useGravity = false;
                heldRb.linearDamping = 10f;
                heldRb.angularDamping = 10f;
                heldRb.interpolation = RigidbodyInterpolation.Interpolate;

                // Apply movement penalty if heavy
                if (heldThrowable != null) playerMove.itemSpeedMultiplier = heldThrowable.speedMod;
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
        heldRb.interpolation = RigidbodyInterpolation.None;

        // Restore player speed
        playerMove.itemSpeedMultiplier = 1f;

        if (isThrow)
        {
            float force = heldThrowable != null ? heldThrowable.throwForce : 15f;
            if (heldThrowable != null) heldThrowable.SetThrown(); // Mark for destruction

            heldRb.AddForce(mainCamera.transform.forward * force, ForceMode.Impulse);
        }

        heldItem = null;
        heldRb = null;
        heldThrowable = null;
    }

    public TorchItem GetHeldTorch()
    {
        if (heldItem == null) return null;
        return heldItem.GetComponent<TorchItem>();
    }
}