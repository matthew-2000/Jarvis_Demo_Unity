using UnityEngine;
using TMPro;

public class LiquidDisplayUI : MonoBehaviour
{
    [SerializeField] LiquidContainer container;
    [SerializeField] TextMeshProUGUI textMl;

    void Start()
    {
        if (container == null)
            container = GetComponent<LiquidContainer>();
    }

    void Update()
    {
        if (container != null && textMl != null)
        {
            float current = Mathf.Round(container.CurrentMl);
            float max = Mathf.Round(container.Capacity);
            textMl.text = $"{current}/{max}";
        }
    }
}
