using UnityEngine;

public class HeatedStoneToggle : MonoBehaviour
{
    [SerializeField] private Color offColor = Color.gray;       // Colore "spento"
    [SerializeField] private Color onColor = Color.red;         // Colore "acceso" (es. rosso incandescente)
    [SerializeField] private Renderer targetRenderer;

    private bool isOn = false;

    private void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        SetStoneColor(offColor);
    }

    public void ToggleStoneState()
    {
        isOn = !isOn;
        SetStoneColor(isOn ? onColor : offColor);
    }

    private void SetStoneColor(Color color)
    {
        if (targetRenderer != null)
        {
            targetRenderer.material.color = color;
        }
    }

    public bool IsOn => isOn;  // Proprieta' per verificare lo stato della pietra
}
