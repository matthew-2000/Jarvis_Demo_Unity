using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AgentUIController : MonoBehaviour
{
    public enum AgentState { None, Listening, Speaking }

    /*────────────────── Shader IDs ──────────────────*/
    private static readonly int MOV_SPEED = Shader.PropertyToID("_SurfaceMovementSpeed");
    private static readonly int NOISE     = Shader.PropertyToID("_NoiseScale");
    private static readonly int ORB_COLOR = Shader.PropertyToID("_OrbColor");
    private static readonly int ENERGY    = Shader.PropertyToID("_EnergyLevel");

    /*───────────────── State presets ─────────────────*/
    [Header("State ‣ Visual presets")]
    [SerializeField] private StateVisual noneState      = new(0f,   0f,  Color.white);
    [SerializeField] private StateVisual listeningState = new(0.01f,0.10f,new Color(0.20f,0.90f,1f));
    [SerializeField] private StateVisual speakingState  = new(0.02f,0.40f,new Color(1f,0.55f,0.20f));

    /*──────────────── Emotion presets ────────────────*/
    [Header("Emotion ‣ Visual presets")]
    [SerializeField] private EmotionVisual joyPreset      = new(new Color(1f,0.86f,0.25f), 0.02f, 0.45f);
    [SerializeField] private EmotionVisual sadnessPreset  = new(new Color(0.26f,0.55f,1f), 0.005f,0.15f);
    [SerializeField] private EmotionVisual angerPreset    = new(new Color(1f,0.23f,0.11f), 0.03f, 0.60f);
    [SerializeField] private EmotionVisual fearPreset     = new(new Color(0.65f,0.20f,0.90f),0.015f,0.30f);
    [SerializeField] private EmotionVisual surprisePreset = new(new Color(0.20f,1f,0.95f), 0.025f,0.50f);
    [SerializeField] private EmotionVisual disgustPreset  = new(new Color(0.35f,0.75f,0.20f),0.018f,0.35f);

    /*──────────────── Animation settings ─────────────*/
    [Header("Animation")]
    [SerializeField, Range(0.05f,2f)] private float transitionDuration = 0.25f;
    [SerializeField] private bool  pulseWhileSpeaking = true;
    [SerializeField, Range(0f,1f)] private float pulseAmplitude = 0.25f;
    [SerializeField, Range(0.2f,5f)] private float pulseSpeed   = 2.5f;

    /*──────────────── Inner orb sync (optional) ──────*/
    [Header("Inner Orb (optional)")]
    [SerializeField] private Renderer innerOrbRenderer;
    [SerializeField] private string   innerColorProperty = "_OrbColor";

    /*──────────────── Internals ──────────────────────*/
    private AgentState currentState = AgentState.None;
    private readonly Dictionary<string,EmotionVisual> emotionLUT = new();
    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private MaterialPropertyBlock innerMpb;
    private Coroutine transitionRoutine;

    /*──────────────── Unity life-cycle ───────────────*/
    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb  = new MaterialPropertyBlock();
        if (innerOrbRenderer) innerMpb = new MaterialPropertyBlock();

        // Costruisco la Look-Up-Table emozioni → preset
        emotionLUT["joy"]      = joyPreset;
        emotionLUT["happiness"]= joyPreset;   // sinonimi frequenti
        emotionLUT["sadness"]  = sadnessPreset;
        emotionLUT["anger"]    = angerPreset;
        emotionLUT["fear"]     = fearPreset;
        emotionLUT["surprise"] = surprisePreset;
        emotionLUT["disgust"]  = disgustPreset;
        emotionLUT["neutral"]  = noneState.ToEmotionVisual(); // fallback

        ApplyStateInstant(noneState);
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

    /*──────────────────── API Stati ──────────────────*/
    public void SetState(AgentState newState)
    {
        if (newState == currentState) return;
        currentState = newState;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionTo(GetPreset(newState)));
    }

    /*────────────────── API Emozioni ─────────────────*/
    public void ApplyEmotion(string label, float intensity = 1f)
    {
        if (string.IsNullOrWhiteSpace(label)) return;
        label = label.ToLowerInvariant();

        if (!emotionLUT.TryGetValue(label, out var preset))
            preset = emotionLUT["neutral"];

        // Scala velocità e noise con l’intensità (0–1)
        float targSpeed = preset.speed  * Mathf.Clamp01(intensity * 1.2f);
        float targNoise = preset.noise  * Mathf.Clamp01(intensity * 1.2f);

        mpb.SetColor(ORB_COLOR, preset.color);
        mpb.SetFloat(MOV_SPEED, targSpeed);
        mpb.SetFloat(NOISE,     targNoise);
        mpb.SetFloat(ENERGY,    1f + intensity * 0.5f);
        rend.SetPropertyBlock(mpb);
        SyncInnerOrbColor(preset.color);
    }

    /*────────────────── Helpers interni ──────────────*/
    private IEnumerator TransitionTo(StateVisual target)
    {
        rend.GetPropertyBlock(mpb);
        float startSpeed = mpb.GetFloat(MOV_SPEED);
        float startNoise = mpb.GetFloat(NOISE);
        Color startCol   = mpb.GetColor(ORB_COLOR);

        float t = 0f;
        while (t < transitionDuration)
        {
            float k = t / transitionDuration;
            mpb.SetFloat(MOV_SPEED, Mathf.Lerp(startSpeed, target.speed,  k));
            mpb.SetFloat(NOISE,     Mathf.Lerp(startNoise, target.noise,  k));
            Color c = Color.Lerp(startCol, target.color, k);
            mpb.SetColor(ORB_COLOR, c);
            rend.SetPropertyBlock(mpb);
            SyncInnerOrbColor(c);
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
        if (!innerOrbRenderer) return;
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

    /*───────────── struct di supporto ───────────────*/
    [System.Serializable] private struct StateVisual
    {
        public float speed, noise; public Color color;
        public StateVisual(float s,float n,Color c){speed=s;noise=n;color=c;}
        public EmotionVisual ToEmotionVisual() => new EmotionVisual(color,speed,noise);
    }
    [System.Serializable] private struct EmotionVisual
    {
        public Color color; public float speed, noise;
        public EmotionVisual(Color c,float s,float n){color=c;speed=s;noise=n;}
    }
}
