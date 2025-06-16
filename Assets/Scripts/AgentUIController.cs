using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AgentUIController : MonoBehaviour
{
    public enum AgentState { None, Listening, Speaking }

    /*───────────────────── Settings per stato ───────────────────*/
    [Header("State ‣ Visual Presets")]
    [SerializeField] private StateVisual noneState      = new(0f,   0f,  Color.white);
    [SerializeField] private StateVisual listeningState = new(0.01f,0.1f,new Color(0.2f,0.9f,1f));   // azzurro tenue
    [SerializeField] private StateVisual speakingState  = new(0.02f,0.4f,new Color(1f,0.55f,0.2f)); // arancio

    /*───────────────────── Animazione & transizioni ─────────────*/
    [Header("Animation")]
    [SerializeField, Range(0.05f,2f)] private float transitionDuration = 0.25f;
    [SerializeField] private bool  pulseWhileSpeaking = true;
    [SerializeField, Range(0f,1f)] private float pulseAmplitude = 0.25f;
    [SerializeField, Range(0.2f,5f)] private float pulseSpeed   = 2.5f;

    /*───────────────────── Shader property IDs (cache) ──────────*/
    private static readonly int MOV_SPEED  = Shader.PropertyToID("_SurfaceMovementSpeed");
    private static readonly int NOISE      = Shader.PropertyToID("_NoiseScale");
    private static readonly int ORB_COLOR  = Shader.PropertyToID("_OrbColor");
    private static readonly int ENERGY     = Shader.PropertyToID("_EnergyLevel");

    /*───────────────────── Internals ────────────────────────────*/
    private AgentState currentState = AgentState.None;
    private Renderer   rend;
    private MaterialPropertyBlock mpb;
    private Coroutine transitionRoutine;

    /*──────────────────────── Unity life-cycle ──────────────────*/
    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb  = new MaterialPropertyBlock();
        ApplyStateInstant(noneState);              // preset di avvio
    }

    private void Update()
    {
        // Effetto "pulse" mentre l'agente sta parlando
        if (pulseWhileSpeaking && currentState == AgentState.Speaking)
        {
            float osc = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * pulseAmplitude;
            mpb.SetFloat(ENERGY, 1f + osc);        // oscilla tra 1 e 1+ampiezza
            rend.SetPropertyBlock(mpb);
        }
    }

    /*───────────────────── API pubblica ─────────────────────────*/
    public void SetState(AgentState newState)
    {
        if (newState == currentState) return;

        currentState = newState;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionTo(GetPreset(newState)));
    }

    public void ForceNone()    => SetState(AgentState.None);
    public void ForceListening()=> SetState(AgentState.Listening);
    public void ForceSpeaking() => SetState(AgentState.Speaking);

    public void UpdateOrbColor(Color c)
    {
        mpb.SetColor(ORB_COLOR, c);
        rend.SetPropertyBlock(mpb);
    }

    public void UpdateOrbEnergy(float energy)
    {
        mpb.SetFloat(ENERGY, energy);
        rend.SetPropertyBlock(mpb);
    }

    /*───────────────────── Transition helpers ───────────────────*/
    private IEnumerator TransitionTo(StateVisual target)
    {
        rend.GetPropertyBlock(mpb);

        float startSpeed  = mpb.GetFloat(MOV_SPEED);
        float startNoise  = mpb.GetFloat(NOISE);
        Color startColor  = mpb.GetColor(ORB_COLOR);

        float t = 0f;
        while (t < transitionDuration)
        {
            float k = t / transitionDuration;
            mpb.SetFloat(MOV_SPEED, Mathf.Lerp(startSpeed, target.speed, k));
            mpb.SetFloat(NOISE,     Mathf.Lerp(startNoise, target.noise, k));
            mpb.SetColor(ORB_COLOR, Color.Lerp(startColor, target.color, k));

            rend.SetPropertyBlock(mpb);
            t += Time.deltaTime;
            yield return null;
        }
        ApplyStateInstant(target);                  // assicura valori finali esatti
    }

    private void ApplyStateInstant(StateVisual s)
    {
        mpb.SetFloat(MOV_SPEED, s.speed);
        mpb.SetFloat(NOISE,     s.noise);
        mpb.SetColor(ORB_COLOR, s.color);
        mpb.SetFloat(ENERGY,    1f);               // reset energia
        rend.SetPropertyBlock(mpb);
    }

    private StateVisual GetPreset(AgentState s) => s switch
    {
        AgentState.None      => noneState,
        AgentState.Listening => listeningState,
        AgentState.Speaking  => speakingState,
        _                    => noneState
    };

    /*───────────────────── Struct dati stato ───────────────────*/
    [System.Serializable]
    private struct StateVisual
    {
        public float speed;
        public float noise;
        public Color color;
        public StateVisual(float s, float n, Color c)
        {
            speed = s; noise = n; color = c;
        }
    }
}