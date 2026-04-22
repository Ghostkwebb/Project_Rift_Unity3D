using UnityEngine;

public class ThrowableItem : MonoBehaviour
{
    public enum Weight { Light, Heavy }
    public Weight weightClass;

    [Header("Impact VFX/SFX")]
    public GameObject dustParticle; // Assign simple dust prefab here
    public AudioClip hitSound;

    // Weight Stats
    public float speedMod => weightClass == Weight.Heavy ? 0.6f : 1f;
    public float throwForce => weightClass == Weight.Heavy ? 5f : 15f;
    public float noiseRadius => weightClass == Weight.Heavy ? 30f : 10f;

    private bool wasThrown = false;

    public void SetThrown()
    {
        wasThrown = true;
    }

    void OnCollisionEnter(Collision col)
    {
        if (wasThrown)
        {
            if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);

            NoiseSystem.MakeNoise(transform.position, noiseRadius);

            if (dustParticle != null) Instantiate(dustParticle, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw Noise Radius when selected in Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
#endif
}