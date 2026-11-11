using UnityEngine;

public class SnowSound : MonoBehaviour
{
    public AudioClip snowCrunch;
    public float noiseRadius = 8f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponentInParent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerBehavior player =
            other.GetComponentInParent<PlayerBehavior>();

        if (player != null)
        {
            if (audioSource != null && snowCrunch != null)
            {
                audioSource.PlayOneShot(snowCrunch);
            }

            NoiseSystem.MakeNoise(
                transform.position,
                noiseRadius
            );
        }
    }
}
