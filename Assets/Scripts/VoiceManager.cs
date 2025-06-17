using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Voice;
using Meta.WitAi;
using Meta.WitAi.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public class VoiceManager : MonoBehaviour
{
    /*───────────────────────── Inspector ─────────────────────────*/
    [Header("Wit Configuration")]
    [SerializeField] private AppVoiceExperience appVoice;
    [SerializeField] private TextMeshPro        transcriptionText;

    [Header("Events (optional)")]
    [SerializeField] private UnityEvent<string> onCompleteTranscription;
    [SerializeField] private UnityEvent<string> onPartialTranscription;

    [Header("Networking")]
    [SerializeField] private AsyncRequestHandler asyncRequestHandler;

    [Header("Responses / TTS")]
    [SerializeField] private ResponseHandler responseHandler;

    [Header("JARVIS Orb UI")]
    [SerializeField] private AgentUIController uiOrb;
    [SerializeField] private AgentMover agentMover;

    /*───────────────────────── Internals ─────────────────────────*/
    private bool isListening = false;
    private bool                requestInProgress = false;
    private readonly List<byte> pcmBuffer           = new();
    private readonly List<string> transcriptionBuffer = new();

    /*────── Parole-chiave (“intents” locali) ─────*/
    private static readonly string[] ACTIVATE = {
        // forme colloquiali
        "ehi agente", "hey agente", "ciao agente",
        // chiamate “one-word”
        "agente", "jarvis", "assistente",
        // varianti con “ok/hey”
        "ok agente",  "hey jarvis", "ok jarvis"
    };

    private static readonly string[] REPEAT = {
        "ripeti", "ripetilo", "puoi ripetere",
        "ripeti per favore", "ancora", "di nuovo",
        "non ho capito", "me lo ripeti"
    };

    private static readonly string[] STOP = {
        "basta", "stop", "ferma", "zitto",
        "silenzio", "interrompi", "annulla",
        "basta così", "fermati"
    };

    private static readonly string[] MOVE_HERE = {
        "vieni qui", "avvicinati", "qui davanti",
        "vieni davanti a me", "spostati qui",
        "vienimi vicino", "qui vicino"
    };

    private static readonly string[] RESET_CONVERSATION = {
        "resetta", "resetta conversazione", "ricomincia", 
        "nuova conversazione", "cancella tutto", "azzera"
    };

    /*───────────────────────── Unity life-cycle ─────────────────*/
    private void Awake()
    {
        if (responseHandler == null)
            responseHandler = ResponseHandler.Instance;

        uiOrb?.SetState(AgentUIController.AgentState.None);

        var ve = appVoice.VoiceEvents;
        ve.OnPartialTranscription.AddListener(OnPartialTranscription);
        ve.OnFullTranscription.AddListener(OnFullTranscription);
        ve.OnRequestCompleted.AddListener(OnRequestCompleted);

        ve.OnStoppedListening.AddListener(StopListening);
        ve.OnStoppedListeningDueToInactivity.AddListener(StopListening);
        ve.OnStoppedListeningDueToTimeout.AddListener(StopListening);

        if (AudioBuffer.Instance != null)
            AudioBuffer.Instance.Events.OnByteDataReady.AddListener(OnByteDataReady);
        else
            Debug.LogError("[VoiceManager] AudioBuffer.Instance is null!");
    }

    private void OnDestroy()
    {
        var ve = appVoice.VoiceEvents;
        ve.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        ve.OnFullTranscription   .RemoveListener(OnFullTranscription);
        ve.OnRequestCompleted    .RemoveListener(OnRequestCompleted);

        ve.OnStoppedListening                 .RemoveListener(StopListening);
        ve.OnStoppedListeningDueToInactivity  .RemoveListener(StopListening);
        ve.OnStoppedListeningDueToTimeout     .RemoveListener(StopListening);

        if (AudioBuffer.Instance != null)
            AudioBuffer.Instance.Events.OnByteDataReady.RemoveListener(OnByteDataReady);
    }

    /*───────────────────────── Public API ───────────────────────*/
    public void StartListening()
    {
        if (isListening || requestInProgress) return;

        Debug.Log("[VoiceManager] StartListening");
        ResetTextBuffers();                 // non svuotiamo l’audio qui
        isListening       = true;
        requestInProgress = true;

        uiOrb?.SetState(AgentUIController.AgentState.Listening);
        appVoice.Activate();
    }

    public void StopListening()
    {
        if (!isListening) return;

        Debug.Log("[VoiceManager] StopListening");
        isListening = false;
        appVoice.Deactivate();
        uiOrb?.SetState(AgentUIController.AgentState.None);

        /*── Trascrizione finale ──*/
        string full  = transcriptionBuffer.Count > 0
                     ? string.Join(" ", transcriptionBuffer)
                     : transcriptionText.text;
        string lower = full.ToLowerInvariant();

        /*── Intents locali ──*/
        if (ContainsAny(lower, REPEAT))
        { responseHandler?.RepeatLastResponse();  ResetTextBuffers(); return; }

        if (ContainsAny(lower, STOP))
        { responseHandler?.StopSpeech();          ResetTextBuffers(); return; }

        if (ContainsAny(lower, ACTIVATE))
        {                                         ResetTextBuffers(); return; }

        if (ContainsAny(lower, MOVE_HERE))
        {
            agentMover?.MoveInFrontOfUser();   
            ResetTextBuffers();
            return;
        }

        if (ContainsAny(lower, RESET_CONVERSATION))
        {
            StartCoroutine(asyncRequestHandler.ResetConversation());
            ResetTextBuffers();
            return;
        }

        if (!string.IsNullOrWhiteSpace(full))
            StartCoroutine(asyncRequestHandler.SendTextAsync(full));

        ResetTextBuffers();                      // audio buffer rimane intatto
    }

    public void CancelListening()
    {
        if (!isListening) return;

        isListening = false;
        appVoice.DeactivateAndAbortRequest();
        ResetAudioBuffer();
        ResetTextBuffers();
        uiOrb?.SetState(AgentUIController.AgentState.None);
    }

    /*───────────────────────── Wit callbacks ────────────────────*/
    private void OnPartialTranscription(string txt)
    {
        if (!isListening) return;
        transcriptionText.text = txt;
        onPartialTranscription?.Invoke(txt);
    }

    private void OnFullTranscription(string txt)
    {
        if (!isListening) return;
        transcriptionBuffer.Add(txt);
        onCompleteTranscription?.Invoke(txt);
    }

    private void OnRequestCompleted()
    {
        requestInProgress = false;

        if (pcmBuffer.Count > 0)
        {
            int sr = AudioBuffer.Instance.AudioEncoding.samplerate; // es. 48000 su Quest
            byte[] wav  = ConvertPCMToWAV(pcmBuffer.ToArray(), 1, sr);
            string name = $"wit_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
            StartCoroutine(asyncRequestHandler.SendAudioAsync(wav, name));
        }
        ResetAudioBuffer();

        Debug.Log("[VoiceManager] RequestCompleted");
    }

    private void OnByteDataReady(byte[] data, int offset, int length)
    {
        if (!isListening && !requestInProgress) return;
        for (int i = offset; i < offset + length; i++)
            pcmBuffer.Add(data[i]);
    }

    /*───────────────────────── Helpers ──────────────────────────*/
    private static string Normalize(string s)
    {
        s = s.ToLowerInvariant();

        // 1. rimuovi diacritici (è-é-ò …)
        string formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (char c in formD)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        s = sb.ToString().Normalize(NormalizationForm.FormC);

        // 2. spazi & punteggiatura
        s = Regex.Replace(s, @"[^\w\s]", " ");   // rimuove punt. lasciando spazi
        s = Regex.Replace(s, @"\s{2,}", " ").Trim();
        return s;
    }

    /* versione migliorata di ContainsAny */
    private static bool ContainsAny(string raw, string[] dict)
    {
        string src = Normalize(raw);
        foreach (string k in dict)
            if (src.Contains(Normalize(k)))
                return true;
        return false;
    }

    private void ResetTextBuffers()
    {
        transcriptionBuffer.Clear();
        transcriptionText.text = "";
    }

    private void ResetAudioBuffer() => pcmBuffer.Clear();

    /*────────────────── Utility WAV / Clip ─────────────────────*/
    public static byte[] ConvertPCMToWAV(byte[] pcm, int channels, int sampleRate)
    {
        using var m = new MemoryStream();
        using var w = new BinaryWriter(m);
        int byteRate = sampleRate * channels * 2;

        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + pcm.Length);
        w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write((short)channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write((short)(channels * 2));
        w.Write((short)16);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        w.Write(pcm.Length);
        w.Write(pcm);
        w.Flush();
        return m.ToArray();
    }

    /*── facoltativo: clip per debug ─*/
    public AudioClip GetRecordedAudioClip()
    {
        int sampleCount = pcmBuffer.Count / 2;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            short s16  = (short)(pcmBuffer[i * 2] | (pcmBuffer[i * 2 + 1] << 8));
            samples[i] = s16 / 32768f;
        }
        int sr = AudioBuffer.Instance != null
                 ? AudioBuffer.Instance.AudioEncoding.samplerate
                 : 16000;
        var clip = AudioClip.Create("WitClip", sampleCount, 1, sr, false);
        clip.SetData(samples, 0);
        return clip;
    }
}