using UnityEngine;

public class LiquidContainer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] float capacityMl = 250f;
    [SerializeField] float startMl    = 0f;
    [SerializeField] string shaderFillProperty = "_Fill";   // deve coincidere con lo shader
    [SerializeField] Renderer liquidRenderer;                // cilindro liquido, NON il vetro

    float currentMl;
    MaterialPropertyBlock mpb;

    public float CurrentMl => currentMl;
    public float Capacity   => capacityMl;

    void Awake()
    {
        if (liquidRenderer == null)
            liquidRenderer = GetComponentInChildren<Renderer>();   // trova “Liquido Becher”

        mpb = new MaterialPropertyBlock();

        currentMl = Mathf.Clamp(startMl, 0, capacityMl);
        UpdateShader();
    }

    public void Add   (float ml) { currentMl = Mathf.Min(currentMl + ml, capacityMl); UpdateShader(); }
    public void Remove(float ml) { currentMl = Mathf.Max(currentMl - ml, 0f);         UpdateShader(); }

    void UpdateShader()
    {
        // 1. altezza reale del cilindro liquido
        float height = liquidRenderer.bounds.size.y;              // <-- riga corretta
        // 2. quota in metri che lo shader deve “tagliare”
        float fillWorld = (currentMl / capacityMl) * height;

        // 3. scrivi la variabile al materiale via PropertyBlock
        liquidRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(shaderFillProperty, fillWorld);
        liquidRenderer.SetPropertyBlock(mpb);
    }

#if UNITY_EDITOR
    void OnValidate()        // aggiorna anche in Edit-mode
    {
        if (liquidRenderer == null)
            liquidRenderer = GetComponentInChildren<Renderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        currentMl = Mathf.Clamp(startMl, 0, capacityMl);
        UpdateShader();
    }
#endif
}