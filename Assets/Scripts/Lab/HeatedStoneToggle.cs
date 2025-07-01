using UnityEngine;
using System;

public class HeatedStoneToggle : MonoBehaviour
{
    [SerializeField] Color     offColor     = Color.gray;      // colore spento
    [SerializeField] Color     onColor      = Color.red;       // colore acceso
    [SerializeField] Renderer  targetRenderer;

    bool isOn = false;
    public  bool IsOn => isOn;

    public event Action<bool> OnPlateStateChanged;             // <true>=acceso, <false>=spento

    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        SetStoneColor(offColor);
    }

    public void ToggleStoneState() => SetState(!isOn);

    public void SetState(bool on)
    {
        isOn = on;
        SetStoneColor(isOn ? onColor : offColor);
        OnPlateStateChanged?.Invoke(isOn);
    }

    void SetStoneColor(Color c)
    {
        if (targetRenderer != null)
            targetRenderer.material.color = c;
    }
}