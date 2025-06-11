using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Voice;

public class VoiceManager : MonoBehaviour
{
    [Header("Wit Configuration")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    [SerializeField] private TextMeshPro transcriptionText;

    [Header("Events")]
    [SerializeField] private UnityEvent<string> completeTranscription;
    [SerializeField] private UnityEvent<string> partialTranscription;

    private bool isListening = false;

    private void Awake()
    {
        appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(OnRequestCompleted);
    }

    private void OnDestroy()
    {
        appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(OnRequestCompleted);
    }

    public void StartListening()
    {
        if (!isListening)
        {
            Debug.Log("[VoiceManager] Start Listening");
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

        // Optionally stop listening automatically after full transcription
        isListening = false;
    }

    private void OnRequestCompleted()
    {
        Debug.Log("[VoiceManager] Request completed");
        isListening = false;
    }
}
