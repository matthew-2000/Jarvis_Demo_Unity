using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;
using TMPro;

public class ResponseHandler : MonoBehaviour
{
    [SerializeField] private AsyncRequestHandler asyncRequestHandler;
    [SerializeField] private TTSSpeaker speaker;
    [SerializeField] private TextMeshPro responseText;
    [SerializeField] private AgentUIController uiOrb;

    public static ResponseHandler Instance { get; private set; }

    private static readonly Regex responseRegex =
        new Regex("\"response\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    /*──────── NEW: regex per coppie emozione-valore ───────*/
    private static readonly Regex emoPairRx =
        new Regex("\"(?<key>[^\"]+)\"\\s*:\\s*\"(?<val>[0-9\\.]+)\"", RegexOptions.Compiled);

    private string lastLLMResponse = "";

    private void Awake()
    {
        Instance = this;
        asyncRequestHandler.OnTextResponseReceived.AddListener(HandleTextResponse);
        asyncRequestHandler.OnAudioResponseReceived.AddListener(HandleEmotionResponse);
    }

    /*────────────────────── Chat LLM ─────────────────────*/
    private void HandleTextResponse(string json)
    {
        string raw = responseRegex.Match(json) is { Success: true } m ? m.Groups[1].Value : json;
        string llm = DecodeUnicodeEscapes(raw);
        lastLLMResponse = llm;
        SpeakImmediate(llm);
    }

    /*───────────── Emozioni da /upload_audio ─────────────*/
    private void HandleEmotionResponse(string json)
    {
        if (!json.Contains("\"status\":\"inferred\"")) return; // ignoriamo “buffering”

        // Cerco tutte le coppie chiave-valore nell’oggetto "emotions"
        var matches = emoPairRx.Matches(json);
        string topE = null; float topVal = 0f;

        foreach (Match m in matches)
        {
            string k = m.Groups["key"].Value.ToLowerInvariant();
            if (!float.TryParse(m.Groups["val"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                continue;
            if (v > topVal) { topVal = v; topE = k; }
        }

        if (topE != null)
        {
            Debug.Log($"[Emotion] Top emotion = {topE} ({topVal:P0})");
            uiOrb?.ApplyEmotion(topE, topVal);
        }
    }

    /*────────────────── Helpers ─────────────────────────*/
    private static string DecodeUnicodeEscapes(string src) =>
        Regex.Replace(src, @"\\u(?<val>[0-9a-fA-F]{4})",
            m => ((char)System.Convert.ToInt32(m.Groups["val"].Value, 16)).ToString());

    public void RepeatLastResponse()
    {
        if (!string.IsNullOrEmpty(lastLLMResponse)) SpeakImmediate(lastLLMResponse);
    }
    public void StopSpeech()
    {
        if (speaker && speaker.IsSpeaking) speaker.Stop();
        StopAllCoroutines();
        uiOrb?.SetState(AgentUIController.AgentState.None);
    }

    private void SpeakImmediate(string text)
    {
        responseText.text = text;
        if (!speaker) return;
        StartCoroutine(SpeechCycle(text));
    }

    private IEnumerator SpeechCycle(string text)
    {
        speaker.Speak(text);
        while (!speaker.IsSpeaking) yield return null;
        uiOrb?.SetState(AgentUIController.AgentState.Speaking);
        while (speaker.IsSpeaking) yield return null;
        uiOrb?.SetState(AgentUIController.AgentState.None);
    }
}