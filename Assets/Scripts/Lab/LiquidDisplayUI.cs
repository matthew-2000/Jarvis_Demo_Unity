using UnityEngine;
using TMPro;

public class LiquidDisplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiquidContainer container;          // Contenitore da monitorare
    [SerializeField] private TextMeshProUGUI textMl;             // Label di testo

    void Awake()
    {
        // Se non impostati via Inspector li recuperiamo dallo stesso GameObject
        if (!textMl)      textMl = GetComponent<TextMeshProUGUI>();
        if (!container)   container = GetComponentInParent<LiquidContainer>(); // o GetComponent<>

        // Aggiorna subito il display
        RefreshLabel();

        // Registrati, se lo script del contenitore espone l’evento
        if (container != null)
            container.OnContentChanged += RefreshLabel;
    }

    void OnDestroy()
    {
        if (container != null)
            container.OnContentChanged -= RefreshLabel;
    }

    /// <summary>
    /// Aggiorna la label “xx/yy ml”.
    /// </summary>
    void RefreshLabel()
    {
        if (!textMl || !container) return;

        float current = Mathf.Round(container.CurrentMl);
        float max     = Mathf.Round(container.CapacityMl);
        textMl.text   = $"{current}/{max}";
    }

#if UNITY_EDITOR
    // Così lo vedi aggiornato anche mentre modifichi valori in Editor
    void OnValidate() => RefreshLabel();
#endif
}