using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Voice;
using Meta.WitAi;
using Meta.WitAi.Data;
using System.Collections.Generic;
using System.IO;

public class VoiceManager : MonoBehaviour
{
    [Header("Wit Configuration")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    [SerializeField] private TextMeshPro transcriptionText;

    [Header("Events")]
    [SerializeField] private UnityEvent<string> completeTranscription;
    [SerializeField] private UnityEvent<string> partialTranscription;

    private bool isListening = false;

    private List<byte> recordedAudioData = new List<byte>();

    private void Awake()
    {
        appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(OnRequestCompleted);

        if (AudioBuffer.Instance != null)
        {
            AudioBuffer.Instance.Events.OnByteDataReady.AddListener(OnByteDataReady);
            Debug.Log("[VoiceManager] Subscribed to OnByteDataReady.");
        }
        else
        {
            Debug.LogError("[VoiceManager] AudioBuffer.Instance is null!");
        }
    }

    private void OnDestroy()
    {
        appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(OnRequestCompleted);

        if (AudioBuffer.Instance != null)
        {
            AudioBuffer.Instance.Events.OnByteDataReady.RemoveListener(OnByteDataReady);
            Debug.Log("[VoiceManager] Unsubscribed from OnByteDataReady.");
        }
    }

    public void StartListening()
    {
        if (!isListening)
        {
            Debug.Log("[VoiceManager] Start Listening");

            recordedAudioData.Clear(); // Pulizia buffer audio

            isListening = true;
            appVoiceExperience.Activate();
        }
    }

    public void StopListening()
    {
        if (isListening)
        {
            Debug.Log("[VoiceManager] Stop Listening");
            isListening = false;
            appVoiceExperience.Deactivate();
        }
    }

    public void CancelListening()
    {
        if (isListening)
        {
            Debug.Log("[VoiceManager] Cancel Listening");
            isListening = false;
            appVoiceExperience.DeactivateAndAbortRequest();
        }
    }

    private void OnPartialTranscription(string transcription)
    {
        if (!isListening) return;

        Debug.Log($"[VoiceManager] Partial transcription: {transcription}");
        transcriptionText.text = transcription;
        partialTranscription?.Invoke(transcription);
    }

    private void OnFullTranscription(string transcription)
    {
        if (!isListening) return;

        Debug.Log($"[VoiceManager] Full transcription: {transcription}");
        transcriptionText.text = transcription;
        completeTranscription?.Invoke(transcription);

        isListening = false;
    }

    private void OnRequestCompleted()
    {
        Debug.Log("[VoiceManager] Request completed");
        isListening = false;

        // Salvataggio automatico
        string filename = $"wit_recording_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
        string fullPath = Path.Combine(Application.persistentDataPath, filename);

        SaveRecordedAudioToFile(fullPath);
        Debug.Log($"[VoiceManager] Audio salvato in: {fullPath}");
    }

    private void OnByteDataReady(byte[] data, int offset, int length)
    {
        // Qui ricevi i byte PCM dal microfono
        for (int i = offset; i < offset + length; i++)
        {
            recordedAudioData.Add(data[i]);
        }
    }

    public void SaveRecordedAudioToFile(string path)
    {
        Debug.Log($"[VoiceManager] Salvo audio registrato in {path}");

        byte[] wavData = ConvertPCMToWAV(recordedAudioData.ToArray(), 1, 16000); // mono, 16kHz
        File.WriteAllBytes(path, wavData);

        Debug.Log("[VoiceManager] Audio salvato.");
    }

    public static byte[] ConvertPCMToWAV(byte[] pcmData, int channels, int sampleRate)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int byteRate = sampleRate * channels * 2; // 16 bit

        // WAV header
        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + pcmData.Length);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * 2)); // block align
        writer.Write((short)16); // bits per sample

        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(pcmData.Length);
        writer.Write(pcmData);

        writer.Flush();
        return stream.ToArray();
    }

    public AudioClip GetRecordedAudioClip()
    {
        float[] samples = new float[recordedAudioData.Count / 2]; // 16 bit = 2 byte per sample
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(recordedAudioData[i * 2] | (recordedAudioData[i * 2 + 1] << 8));
            samples[i] = sample / 32768.0f;
        }

        AudioClip clip = AudioClip.Create("WitRecording", samples.Length, 1, 16000, false);
        clip.SetData(samples, 0);
        return clip;
    }

}
