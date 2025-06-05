using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Voice;
using System.Reflection;
using Meta.WitAi.CallbackHandlers;

public class VoiceManager : MonoBehaviour
{
    [Header("Wit Configuration")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    [SerializeField] private WitResponseMatcher responseMatcher;
    [SerializeField] private TextMeshProUGUI transcriptionText;

    [Header("Events")]
    [SerializeField] private UnityEvent wakeWordDetected;
    [SerializeField] private UnityEvent<string> completeTranscription;

    private bool _voiceCommandReady = false;

    private void Awake()
    {
        appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(ReactivateVoice);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);

        var eventField = typeof(WitResponseMatcher).GetField("onMultiValueEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (eventField != null && eventField.GetValue(responseMatcher) is MultiValueEvent onMultiValueEvent)
        {
            onMultiValueEvent.AddListener(WakeWordDetected);
        }

        appVoiceExperience.Activate();
    }

    private void OnDestroy()
    {
        appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(ReactivateVoice);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);

        var eventField = typeof(WitResponseMatcher).GetField("onMultiValueEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (eventField != null && eventField.GetValue(responseMatcher) is MultiValueEvent onMultiValueEvent)
        {
            onMultiValueEvent.RemoveListener(WakeWordDetected);
        }

    }

    private void ReactivateVoice() => appVoiceExperience.Activate();

    private void WakeWordDetected(string[] arg0)
    {
        Debug.Log("[VoiceManager] Wake word detected: " + string.Join(", ", arg0));
        _voiceCommandReady = true;
        Debug.Log($"[VoiceManager] Voice command ready: {_voiceCommandReady}");
        wakeWordDetected?.Invoke();
    }

    private void OnPartialTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;
        Debug.Log($"[VoiceManager] Partial transcription: {transcription}");
        Debug.Log($"[VoiceManager] Voice command ready: {_voiceCommandReady}");
        transcriptionText.text = transcription;
    }

    private void OnFullTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;
        Debug.Log($"[VoiceManager] Full transcription: {transcription}");
        Debug.Log($"[VoiceManager] Voice command ready: {_voiceCommandReady}");
        _voiceCommandReady = false;
        Debug.Log($"[VoiceManager] Voice command ready (should be false): {_voiceCommandReady}");
        completeTranscription?.Invoke(transcription);
    }

}