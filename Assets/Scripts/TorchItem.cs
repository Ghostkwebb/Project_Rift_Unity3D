using UnityEngine;
using UnityEngine.InputSystem;

public class TorchItem : MonoBehaviour
{
    public Light torchLight;
    public bool isOn = true;

    void Start()
    {
        if (torchLight != null) torchLight.enabled = isOn;
    }

    void Update()
    {
        if (isOn && transform.parent != null)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                isOn = false;
                if (torchLight != null) torchLight.enabled = false;
            }
        }
    }
}