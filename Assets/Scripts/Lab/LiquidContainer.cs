using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum LiquidType { None, Etanolo, MiscelaNitrante, ProdottoFinale }

public struct LiquidPortion
{
    public float volume;
    public Color topColor;
    public Color sideColor;
    public LiquidType type;

    public LiquidPortion(float v, Color top, Color side, LiquidType t)
    {
        volume    = v;
        topColor  = top;
        sideColor = side;
        type      = t;
    }
}

[RequireComponent(typeof(Renderer))]
public class LiquidContainer : MonoBehaviour
{
    /* ----------------- INSPECTOR ----------------- */
    [Header("Setup")]
    [SerializeField] float       capacityMl      = 250f;
    [SerializeField] float       startMl         = 0f;
    [SerializeField] LiquidType  startType       = LiquidType.None;
    [SerializeField] Color       startTopColor   = Color.clear;
    [SerializeField] Color       startSideColor  = Color.clear;
    [SerializeField] string      shaderFillProp  = "_Fill";
    [SerializeField] string      shaderTopProp   = "_TopColor";
    [SerializeField] string      shaderSideProp  = "_SideColor";
    [SerializeField] Renderer    liquidRenderer;

    /* ----------------- STATO ----------------- */
    float currentMl;
    Color currentTopColor;
    Color currentSideColor;

    readonly Dictionary<LiquidType,float> composition = new();
    public  float CurrentMl                       => currentMl;
    public  float CapacityMl                      => capacityMl;
    public  IReadOnlyDictionary<LiquidType,float> Composition => composition;

    public event System.Action OnContentChanged;

    MaterialPropertyBlock mpb;
    Coroutine transitionRoutine;

    /* ============== API PUBBLICA ============== */

    public void Add(LiquidType type, float ml, Color topCol, Color sideCol)
    {
        if (ml <= 0f) return;

        float spazio  = capacityMl - currentMl;
        float versato = Mathf.Min(spazio, ml);
        if (versato <= 0f) return;

        // ********** miscela colori (media pesata) **********
        float newVol = currentMl + versato;
        if (currentMl < 0.001f)
        {
            currentTopColor  = topCol;
            currentSideColor = sideCol;
        }
        else
        {
            float wOld = currentMl / newVol;
            float wNew = versato   / newVol;
            currentTopColor  = currentTopColor  * wOld + topCol  * wNew;
            currentSideColor = currentSideColor * wOld + sideCol * wNew;
        }

        currentMl = newVol;

        // aggiorna dizionario
        if (!composition.ContainsKey(type))
            composition[type] = 0f;
        composition[type] += versato;

        UpdateShader();
        OnContentChanged?.Invoke();
    }

    public LiquidPortion Draw(float ml)
    {
        float estratto = Mathf.Clamp(ml, 0, currentMl);
        currentMl    -= estratto;

        // tipo predominante (semplicissimo: quello col volume maggiore)
        LiquidType dominante = LiquidType.None;
        float maxVol = 0f;
        foreach (var kv in composition)
        {
            if (kv.Value > maxVol) { dominante = kv.Key; maxVol = kv.Value; }
        }

        if (dominante != LiquidType.None)
        {
            composition[dominante] -= estratto;
            if (composition[dominante] <= 0.001f)
                composition.Remove(dominante);
        }

        UpdateShader();
        OnContentChanged?.Invoke();

        return new LiquidPortion(estratto, currentTopColor, currentSideColor, dominante);
    }

    public void TransitionToColor(Color tgtTop, Color tgtSide, float dur = 3f,
                                  AnimationCurve curve = null)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(LerpRoutine(
            currentTopColor, currentSideColor,
            tgtTop,          tgtSide,
            dur,
            curve ?? AnimationCurve.Linear(0,0,1,1)
        ));
    }

    /* ============== UNITY ============== */

    void Awake()
    {
        if (!liquidRenderer) liquidRenderer = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();

        InitState();          // inizializza volumi + dizionario
        UpdateShader();
    }

    void InitState()
    {
        currentMl        = Mathf.Clamp(startMl, 0, capacityMl);
        currentTopColor  = startTopColor;
        currentSideColor = startSideColor;

        composition.Clear();
        if (currentMl > 0.001f && startType != LiquidType.None)
            composition[startType] = currentMl;
    }

    void UpdateShader()
    {
        float height    = liquidRenderer.bounds.size.y;             // world-space
        float fillWorld = (currentMl / capacityMl) * height;

        liquidRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(shaderFillProp, fillWorld);
        mpb.SetColor(shaderTopProp,  currentTopColor);
        mpb.SetColor(shaderSideProp, currentSideColor);
        liquidRenderer.SetPropertyBlock(mpb);
    }

    IEnumerator LerpRoutine(Color sTop, Color sSide,
                             Color eTop, Color eSide,
                             float d, AnimationCurve c)
    {
        float t = 0f;
        while (t < d)
        {
            float k = c.Evaluate(t / d);
            currentTopColor  = Color.Lerp(sTop,  eTop,  k);
            currentSideColor = Color.Lerp(sSide, eSide, k);
            UpdateShader();
            t += Time.deltaTime;
            yield return null;
        }
        currentTopColor  = eTop;
        currentSideColor = eSide;
        UpdateShader();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!liquidRenderer) liquidRenderer = GetComponent<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        InitState();
        UpdateShader();
    }
#endif
}