using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Voice;
using Meta.WitAi;
using Meta.WitAi.Data;

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

    /*───────────────────────── Internals ─────────────────────────*/
    private bool                 isListening       = false;
    private bool                 requestInProgress = false;
    private readonly List<byte>  pcmBuffer          = new();
    private readonly List<string>transcriptionBuffer = new();

    /*────── Parole-chiave (“intents” locali) ─────*/
    private static readonly string[] ACTIVATE = { "ehi agente", "ciao agente", "hey agente" };
    private static readonly string[] REPEAT   = { "ripeti", "di nuovo", "ripetilo" };
    private static readonly string[] STOP     = { "basta", "stop", "ferma", "zitto", "silenzio" };

    /*───────────────────────── Unity life-cycle ─────────────────*/
    private void Awake()
    {
        if (responseHandler == null) responseHandler = ResponseHandler.Instance;
        uiOrb?.SetState(AgentUIController.AgentState.None);

        var ve = appVoice.VoiceEvents;
        ve.OnPartialTranscription.AddListener(OnPartialTranscription);
        ve.OnFullTranscription   .AddListener(OnFullTranscription);
        ve.OnRequestCompleted    .AddListener(OnRequestCompleted);

        ve.OnStoppedListening                    .AddListener(StopListening);
        ve.OnStoppedListeningDueToInactivity     .AddListener(StopListening);
        ve.OnStoppedListeningDueToTimeout        .AddListener(StopListening);

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

        if (AudioBuffer.Instance != null)
            AudioBuffer.Instance.Events.OnByteDataReady.RemoveListener(OnByteDataReady);
    }

    /*───────────────────────── Public API ───────────────────────*/
    public void StartListening()
    {
        if (isListening || requestInProgress) return;

        Debug.Log("[VoiceManager] StartListening");
        ResetBuffers();
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

        /*── Trascrizione definitiva ──*/
        string full = transcriptionBuffer.Count > 0 ? string.Join(" ", transcriptionBuffer)
                                                    : transcriptionText.text;
        string lower = full.ToLowerInvariant();

        /*── Intents locali ──*/
        if (ContainsAny(lower, REPEAT))
        { responseHandler?.RepeatLastResponse();  ResetBuffers(); return; }

        if (ContainsAny(lower, STOP))
        { responseHandler?.StopSpeech();          ResetBuffers(); return; }

        if (ContainsAny(lower, ACTIVATE))
        {                                           ResetBuffers(); return; }

        /*── Invio al backend ──*/
        StartCoroutine(asyncRequestHandler.SendTextAsync(full));
        ResetBuffers();
    }

    public void CancelListening()
    {
        if (!isListening) return;
        isListening = false;
        appVoice.DeactivateAndAbortRequest();
        pcmBuffer.Clear();
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
            byte[] wav  = ConvertPCMToWAV(pcmBuffer.ToArray(), 1, 16_000);
            string name = $"wit_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
            StartCoroutine(asyncRequestHandler.SendAudioAsync(wav, name));
        }
        pcmBuffer.Clear();
        Debug.Log("[VoiceManager] RequestCompleted");
    }

    private void OnByteDataReady(byte[] data, int offset, int length)
    {
        if (!isListening && !requestInProgress) return;
        for (int i = offset; i < offset + length; i++) pcmBuffer.Add(data[i]);
    }

    /*───────────────────────── Helpers ──────────────────────────*/
    private static bool ContainsAny(string src, string[] keys)
    { foreach (string k in keys) if (src.Contains(k)) return true; return false; }

    private void ResetBuffers()
    {
        pcmBuffer.Clear();
        transcriptionBuffer.Clear();
        transcriptionText.text = "";
    }

    /*────────────────────── Utility WAV / Clip ─────────────────*/
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

    public AudioClip GetRecordedAudioClip()
    {
        int sampleCount = pcmBuffer.Count / 2;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            short s16  = (short)(pcmBuffer[i * 2] | (pcmBuffer[i * 2 + 1] << 8));
            samples[i] = s16 / 32768f;
        }
        var clip = AudioClip.Create("WitClip", sampleCount, 1, 16_000, false);
        clip.SetData(samples, 0);
        return clip;
    }
}