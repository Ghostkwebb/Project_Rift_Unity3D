using UnityEngine;

public class TorchItem : MonoBehaviour
{
    public Light torchLight;
    public bool isOn = true;

    void Start()
    {
        if (torchLight != null) torchLight.enabled = isOn;
    }

    public void TurnOff()
    {
        if (!isOn) return;
        isOn = false;
        if (torchLight != null) torchLight.enabled = false;
    }
}