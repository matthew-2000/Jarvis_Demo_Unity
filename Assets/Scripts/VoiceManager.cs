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

    [SerializeField] private TextMeshPro transcriptionText;

    [Header("Events (optional)")]
    [SerializeField] private UnityEvent<string> onCompleteTranscription;
    [SerializeField] private UnityEvent<string> onPartialTranscription;

    [Header("Networking")]
    [SerializeField] private AsyncRequestHandler asyncRequestHandler;

    /*───────────────────────── Internals ─────────────────────────*/
    private bool isListening = false;   // session flag
    private bool requestInProgress = false;   // evita overlap Wit
    private readonly List<byte> pcmBuffer = new List<byte>(); // byte 16-bit mono 16 kHz
    private readonly List<string> transcriptionBuffer = new List<string>(); // accumula trascrizioni

    /*───────────────────────── Unity life-cycle ─────────────────*/
    private void Awake()
    {
        var ve = appVoice.VoiceEvents;
        ve.OnPartialTranscription.AddListener(OnPartialTranscription);
        ve.OnFullTranscription.AddListener(OnFullTranscription);
        ve.OnRequestCompleted.AddListener(OnRequestCompleted);

        if (AudioBuffer.Instance != null)
        {
            AudioBuffer.Instance.Events.OnByteDataReady.AddListener(OnByteDataReady);
            Debug.Log("[VoiceManager] Subscribed to OnByteDataReady.");
        }
        else
            Debug.LogError("[VoiceManager] AudioBuffer.Instance is null!");
    }

    private void OnDestroy()
    {
        var ve = appVoice.VoiceEvents;
        ve.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        ve.OnFullTranscription.RemoveListener(OnFullTranscription);
        ve.OnRequestCompleted.RemoveListener(OnRequestCompleted);

        if (AudioBuffer.Instance != null)
            AudioBuffer.Instance.Events.OnByteDataReady.RemoveListener(OnByteDataReady);
    }

    /*───────────────────────── Public API ───────────────────────*/
    public void StartListening()
    {
        if (isListening || requestInProgress) return;

        Debug.Log("[VoiceManager] StartListening");
        pcmBuffer.Clear();
        transcriptionBuffer.Clear(); // reset trascrizioni
        isListening = true;
        requestInProgress = true;
        appVoice.Activate();
    }

    public void StopListening()
    {
        if (!isListening) return;

        Debug.Log("[VoiceManager] StopListening");
        isListening = false;
        appVoice.Deactivate();

        // Usa il testo parziale come fallback se il buffer è vuoto
        string fullTranscription = transcriptionBuffer.Count > 0
            ? string.Join(" ", transcriptionBuffer)
            : transcriptionText.text;

        Debug.Log($"[VoiceManager] Final transcription: {fullTranscription}");
        StartCoroutine(asyncRequestHandler.SendTextAsync(fullTranscription));

        // Converti buffer audio in WAV e invia
        if (pcmBuffer.Count > 0)
        {
            byte[] wav = ConvertPCMToWAV(pcmBuffer.ToArray(), 1, 16_000);
            string fname = $"wit_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
            StartCoroutine(asyncRequestHandler.SendAudioAsync(wav, fname));
        }

        pcmBuffer.Clear();    // reset per la prossima sessione
        transcriptionBuffer.Clear(); // reset trascrizioni
    }

    public void CancelListening()
    {
        if (!isListening) return;

        Debug.Log("[VoiceManager] CancelListening");
        isListening = false;
        appVoice.DeactivateAndAbortRequest();
        pcmBuffer.Clear();
    }

    /*───────────────────────── Wit callbacks ────────────────────*/
    private void OnPartialTranscription(string text)
    {
        if (!isListening) return;

        Debug.Log($"[VoiceManager] … {text}");
        transcriptionText.text = text;
        onPartialTranscription?.Invoke(text);
    }

    private void OnFullTranscription(string text)
    {
        if (!isListening) return;   // safety

        Debug.Log($"[VoiceManager] Full transcription received: {text}");
        transcriptionBuffer.Add(text); // Accumula trascrizione
        onCompleteTranscription?.Invoke(text);
    }

    private void OnRequestCompleted()
    {
        requestInProgress = false;
        Debug.Log("[VoiceManager] RequestCompleted (Wit finished)");
    }

    private void OnByteDataReady(byte[] data, int offset, int length)
    {
        if (!isListening && !requestInProgress) return;   // accoda solo in sessione
        for (int i = offset; i < offset + length; i++)
            pcmBuffer.Add(data[i]);
    }

    /*───────────────────────── Utility WAV ─────────────────────*/
    public static byte[] ConvertPCMToWAV(byte[] pcm, int channels, int sampleRate)
    {
        using var m = new MemoryStream();
        using var w = new BinaryWriter(m);
        int byteRate = sampleRate * channels * 2;

        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + pcm.Length);
        w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16);               // PCM header length
        w.Write((short)1);         // PCM
        w.Write((short)channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write((short)(channels * 2));
        w.Write((short)16);        // bits per sample
        w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        w.Write(pcm.Length);
        w.Write(pcm);
        w.Flush();
        return m.ToArray();
    }
    
    /*───────────────────────── Utility: AudioClip ──────────────────*/
    public AudioClip GetRecordedAudioClip()
    {
        // Chiama questo metodo dopo che la richiesta è completa
        // (o in StopListening) quando pcmBuffer contiene l’intero utterance.
        int sampleCount = pcmBuffer.Count / 2;          // 16-bit ⇒ 2 byte per sample
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample16 = (short)(pcmBuffer[i * 2] |
                                    (pcmBuffer[i * 2 + 1] << 8));
            samples[i] = sample16 / 32768f;             // normalizza in [-1,1]
        }

        var clip = AudioClip.Create("WitClip",
                                    sampleCount,
                                    1,          // mono
                                    16_000,
                                    false);
        clip.SetData(samples, 0);
        return clip;
    }

}
