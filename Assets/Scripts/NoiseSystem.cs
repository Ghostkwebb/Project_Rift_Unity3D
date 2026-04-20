using UnityEngine;

public static class NoiseSystem
{
    public static System.Action<Vector3, float> OnNoise;

    public static void MakeNoise(Vector3 position, float volumeRadius)
    {
        OnNoise?.Invoke(position, volumeRadius);
    }
}