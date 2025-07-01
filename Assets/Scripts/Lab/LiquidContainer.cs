using UnityEngine;
using System.Collections;

public struct LiquidPortion
{
    public float  volume;      // ml
    public Color  topColor;    // colore parte alta (riflessi)
    public Color  sideColor;   // colore laterale / bulk

    public LiquidPortion(float v, Color top, Color side)
    {
        volume    = v;
        topColor  = top;
        sideColor = side;
    }
}

[RequireComponent(typeof(MeshRenderer))]
public class LiquidContainer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] float  capacityMl          = 250f;
    [SerializeField] float  startMl             = 0f;
    [SerializeField] Color  startTopColor       = Color.clear;          // etanolo → quasi trasparente
    [SerializeField] Color  startSideColor      = Color.clear;
    [SerializeField] string shaderFillProperty  = "_Fill";
    [SerializeField] string shaderTopProperty   = "_TopColor";
    [SerializeField] string shaderSideProperty  = "_SideColor";
    [SerializeField] Renderer liquidRenderer;                           

    /// Stato corrente
    float currentMl;
    Color currentTopColor;
    Color currentSideColor;

    MaterialPropertyBlock mpb;

    #region API pubblica
    public float CurrentMl      => currentMl;
    public float Capacity       => capacityMl;
    public Color CurrentTopCol  => currentTopColor;
    public Color CurrentSideCol => currentSideColor;

    /// Rimuove fino a "ml" e restituisce la parte prelevata (volume/colori)
    public LiquidPortion Draw(float ml)
    {
        float delta = Mathf.Clamp(ml, 0f, currentMl);
        currentMl  -= delta;

        // restituiamo il liquido con lo stesso colore del bulk
        LiquidPortion portion = new LiquidPortion(delta, currentTopColor, currentSideColor); 
        UpdateShader();
        return portion;
    }

    /// Aggiunge liquido; se il container era vuoto prende direttamente i colori del versato,
    /// altrimenti fa una media pesata (mix)
    public void PourIn(LiquidPortion p)
    {
        if (p.volume <= 0f) return;

        float newVol = Mathf.Min(currentMl + p.volume, capacityMl);
        float incoming = newVol - currentMl;          // nel caso di overflow scartiamo l'eccedenza

        // *** MIX COLORE  = media pesata ***
        if (currentMl <= 0.0001f)            // recipiente inizialmente vuoto
        {
            currentTopColor  = p.topColor;
            currentSideColor = p.sideColor;
        }
        else
        {
            float wOld = currentMl / newVol;
            float wNew = incoming   / newVol;

            currentTopColor  = currentTopColor  * wOld + p.topColor  * wNew;
            currentSideColor = currentSideColor * wOld + p.sideColor * wNew;
        }

        currentMl = newVol;
        UpdateShader();
    }
    #endregion

    public void TransitionToColor(Color targetTop,
                               Color targetSide,
                               float duration = 3f,
                               AnimationCurve curve = null)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(LerpColorsRoutine(
            currentTopColor, currentSideColor,
            targetTop,       targetSide,
            duration,
            curve ?? AnimationCurve.Linear(0,0,1,1)
        ));
    }

    Coroutine transitionRoutine;   // campo privato

    IEnumerator LerpColorsRoutine(Color startTop, Color startSide,
                                Color endTop,   Color endSide,
                                float duration, AnimationCurve curve)
    {
        float t = 0f;
        while (t < duration)
        {
            float k = curve.Evaluate(t / duration);   // 0→1 secondo curva

            currentTopColor  = Color.Lerp(startTop,  endTop,  k);
            currentSideColor = Color.Lerp(startSide, endSide, k);

            UpdateShader();           // già presente nello script
            t += Time.deltaTime;
            yield return null;
        }

        // assicura valori finali precisi
        currentTopColor  = endTop;
        currentSideColor = endSide;
        UpdateShader();
    }


    #region Interno
    void Awake()
    {
        if (!liquidRenderer) liquidRenderer = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();

        currentMl        = Mathf.Clamp(startMl, 0, capacityMl);
        currentTopColor  = startTopColor;
        currentSideColor = startSideColor;

        UpdateShader();
    }

    void UpdateShader()
    {
        // 1) livello di riempimento (world-units)
        float height = liquidRenderer.bounds.size.y;
        float fillWorld = (currentMl / capacityMl) * height;

        // 2) scrivi blocco
        liquidRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(shaderFillProperty,  fillWorld);
        mpb.SetColor(shaderTopProperty,   currentTopColor);
        mpb.SetColor(shaderSideProperty,  currentSideColor);
        liquidRenderer.SetPropertyBlock(mpb);
    }
#if UNITY_EDITOR
    void OnValidate()
    {
        if (!liquidRenderer) liquidRenderer = GetComponentInChildren<Renderer>();
        if (mpb == null)     mpb = new MaterialPropertyBlock();
        currentMl = Mathf.Clamp(startMl, 0, capacityMl);
        currentTopColor  = startTopColor;
        currentSideColor = startSideColor;
        UpdateShader();
    }
#endif
    #endregion
}