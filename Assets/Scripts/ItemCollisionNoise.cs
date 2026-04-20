using UnityEngine;

public class ItemCollisionNoise : MonoBehaviour
{
    public float noiseRadius = 20f;
    public AudioClip clackSound;

    private Rigidbody rb;
    private AudioSource audioSrc;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.spatialBlend = 1f;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (rb != null && rb.linearVelocity.magnitude > 2f)
        {
            NoiseSystem.MakeNoise(transform.position, noiseRadius);

            if (clackSound != null && !audioSrc.isPlaying)
            {
                audioSrc.PlayOneShot(clackSound);
            }
        }
    }
}