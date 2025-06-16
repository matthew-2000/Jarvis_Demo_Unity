using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using Meta.WitAi.TTS.Utilities;
using TMPro;

public class ResponseHandler : MonoBehaviour
{
    [SerializeField] private AsyncRequestHandler asyncRequestHandler;   // endpoint I/O
    [SerializeField] private TTSSpeaker          speaker;               // componente TTS
    [SerializeField] private TextMeshPro         responseText;          // UI testuale
    [SerializeField] private AgentUIController   uiOrb;                 // Orb visivo

    /*────────── SINGLETON (per accesso da VoiceManager) ─────────*/
    public static ResponseHandler Instance { get; private set; }

    /*────────── Regex per estrarre "response":"..." dal JSON ────*/
    private static readonly Regex responseRegex =
        new Regex("\"response\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    /*────────── Stato ultima risposta ───────────────────────────*/
    private string lastLLMResponse = "";

    /*───────────────────────── Unity lifecycle ──────────────────*/
    private void Awake()
    {
        Instance = this;

        asyncRequestHandler.OnTextResponseReceived .AddListener(HandleTextResponse);
        asyncRequestHandler.OnAudioResponseReceived.AddListener(_ => { /* non usato ora */ });
    }

    /*─────────────────────── API pubbliche ──────────────────────*/
    public void RepeatLastResponse()
    {
        if (string.IsNullOrEmpty(lastLLMResponse))
            return;

        SpeakImmediate(lastLLMResponse);
    }

    public void StopSpeech()
    {
        if (speaker != null && speaker.IsSpeaking)
            speaker.Stop();

        StopAllCoroutines();                       // ferma eventuale wait-coroutine
        uiOrb?.SetState(AgentUIController.AgentState.None);
    }

    /*────────────────────── Event handlers ─────────────────────*/
    private void HandleTextResponse(string json)
    {
        // Estrai il testo dal JSON di risposta
        string llm = responseRegex.Match(json) is { Success: true } m
            ? m.Groups[1].Value
            : json;                                // fallback se regex fallisce

        lastLLMResponse = llm;
        SpeakImmediate(llm);
    }

    /*─────────────────────── Parla subito ──────────────────────*/
    private void SpeakImmediate(string text)
    {
        responseText.text = text;
        if (speaker == null) return;

        StartCoroutine(SpeechCycle(text));
    }

    private IEnumerator SpeechCycle(string text)
    {
        Debug.Log($"[SpeechCycle] Starting speech cycle for text: {text}");

        // 1. chiedi al TTSSpeaker di generare/queue-are il clip
        speaker.Speak(text);
        Debug.Log("[SpeechCycle] TTSSpeaker.Speak called.");

        // 2. aspetta che inizi davvero
        while (!speaker.IsSpeaking)
            yield return null;

        Debug.Log("[SpeechCycle] TTSSpeaker started speaking.");
        uiOrb?.SetState(AgentUIController.AgentState.Speaking);

        // 3. aspetta la fine
        while (speaker.IsSpeaking)
            yield return null;

        Debug.Log("[SpeechCycle] TTSSpeaker finished speaking.");
        uiOrb?.SetState(AgentUIController.AgentState.None);

        Debug.Log("[SpeechCycle] Speech cycle completed.");
    }

}