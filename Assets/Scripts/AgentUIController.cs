using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AgentUIController : MonoBehaviour
{
    public enum AgentState { None, Listening, Speaking }

    /*───────────── Shader property IDs ─────────────*/
    private static readonly int MOV_SPEED = Shader.PropertyToID("_SurfaceMovementSpeed");
    private static readonly int NOISE     = Shader.PropertyToID("_NoiseScale");
    private static readonly int ORB_COLOR = Shader.PropertyToID("_OrbColor");
    private static readonly int ENERGY    = Shader.PropertyToID("_EnergyLevel");

    /*───────────── Visual presets ─────────────*/
    [Header("State ‣ Visual Presets")]
    [SerializeField] private StateVisual noneState      = new(0f,   0f,  Color.white);
    [SerializeField] private StateVisual listeningState = new(0.01f,0.10f,new Color(0.2f,0.9f,1f));
    [SerializeField] private StateVisual speakingState  = new(0.02f,0.40f,new Color(1f,0.55f,0.2f));

    /*───────────── Animation settings ─────────────*/
    [Header("Animation")]
    [SerializeField, Range(0.05f,2f)] private float transitionDuration = 0.25f;
    [SerializeField] private bool  pulseWhileSpeaking = true;
    [SerializeField, Range(0f,1f)] private float pulseAmplitude = 0.25f;
    [SerializeField, Range(0.2f,5f)] private float pulseSpeed   = 2.5f;

    /*───────────── Inner orb sync ─────────────*/
    [Header("Inner Orb (optional)")]
    [Tooltip("Renderer del piccolo orb interno che deve avere lo stesso colore.")]
    [SerializeField] private Renderer innerOrbRenderer;
    [Tooltip("Nome property colore nello shader dell’inner orb (default _OrbColor o _BaseColor)")]
    [SerializeField] private string   innerColorProperty = "_OrbColor";

    /*───────────── Internals ─────────────*/
    private AgentState currentState = AgentState.None;
    private Renderer   rend;
    private MaterialPropertyBlock mpb;
    private MaterialPropertyBlock innerMpb;
    private Coroutine transitionRoutine;

    /*────────────────── Unity life-cycle ──────────────────*/
    private void Awake()
    {
        rend     = GetComponent<Renderer>();
        mpb      = new MaterialPropertyBlock();

        if (innerOrbRenderer != null)
            innerMpb = new MaterialPropertyBlock();

        ApplyStateInstant(noneState);      // preset iniziale
    }

    private void Update()
    {
        if (pulseWhileSpeaking && currentState == AgentState.Speaking)
        {
            float osc = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * pulseAmplitude;
            mpb.SetFloat(ENERGY, 1f + osc);
            rend.SetPropertyBlock(mpb);
        }
    }

    /*────────────────── API pubblica ─────────────────────*/
    public void SetState(AgentState newState)
    {
        if (newState == currentState) return;
        currentState = newState;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionTo(GetPreset(newState)));
    }

    public void ForceNone()      => SetState(AgentState.None);
    public void ForceListening() => SetState(AgentState.Listening);
    public void ForceSpeaking()  => SetState(AgentState.Speaking);

    public void UpdateOrbColor(Color c)
    {
        mpb.SetColor(ORB_COLOR, c);
        rend.SetPropertyBlock(mpb);
        SyncInnerOrbColor(c);
    }

    public void UpdateOrbEnergy(float e)
    {
        mpb.SetFloat(ENERGY, e);
        rend.SetPropertyBlock(mpb);
    }

    /*───────────────── Transition helpers ────────────────*/
    private IEnumerator TransitionTo(StateVisual target)
    {
        rend.GetPropertyBlock(mpb);

        float startSpeed = mpb.GetFloat(MOV_SPEED);
        float startNoise = mpb.GetFloat(NOISE);
        Color startColor = mpb.GetColor(ORB_COLOR);

        float t = 0f;
        while (t < transitionDuration)
        {
            float k = t / transitionDuration;
            mpb.SetFloat(MOV_SPEED, Mathf.Lerp(startSpeed, target.speed, k));
            mpb.SetFloat(NOISE,     Mathf.Lerp(startNoise, target.noise, k));
            Color col = Color.Lerp(startColor, target.color, k);
            mpb.SetColor(ORB_COLOR, col);
            rend.SetPropertyBlock(mpb);
            SyncInnerOrbColor(col);                // ⇦ aggiorna anche l’interno
            t += Time.deltaTime;
            yield return null;
        }
        ApplyStateInstant(target);
    }

    private void ApplyStateInstant(StateVisual s)
    {
        mpb.SetFloat(MOV_SPEED, s.speed);
        mpb.SetFloat(NOISE,     s.noise);
        mpb.SetColor(ORB_COLOR, s.color);
        mpb.SetFloat(ENERGY,    1f);
        rend.SetPropertyBlock(mpb);
        SyncInnerOrbColor(s.color);
    }

    private void SyncInnerOrbColor(Color c)
    {
        if (innerOrbRenderer == null) return;
        innerOrbRenderer.GetPropertyBlock(innerMpb);
        innerMpb.SetColor(innerColorProperty, c);
        innerOrbRenderer.SetPropertyBlock(innerMpb);
    }

    private StateVisual GetPreset(AgentState s) => s switch
    {
        AgentState.None      => noneState,
        AgentState.Listening => listeningState,
        AgentState.Speaking  => speakingState,
        _                    => noneState
    };

    /*──────────── struct per preset ────────────*/
    [System.Serializable]
    private struct StateVisual
    {
        public float speed;
        public float noise;
        public Color color;
        public StateVisual(float s,float n,Color c){speed=s;noise=n;color=c;}
    }
}
