using UnityEngine;
using UnityEngine.Events;

public class AudioRecorder : MonoBehaviour
{
    [Header("Dependencies")]
    public VoiceManager voiceManager;
    public AudioSource audioSource;

    [Header("Recording Events")]
    public UnityEvent OnStartRecordingEvent;
    public UnityEvent OnStopRecordingEvent;
    public UnityEvent OnPlayRecordingEvent;

    private void Start()
    {
        // Collega gli eventi UnityEvent ai metodi interni
        OnStartRecordingEvent.AddListener(StartRecording);
        OnStopRecordingEvent.AddListener(StopRecording);
        OnPlayRecordingEvent.AddListener(PlayRecording);
    }

    public void StartRecording()
    {
        Debug.Log("[AudioRecorder] StartRecording invoked");
        voiceManager.StartListening();
    }

    public void StopRecording()
    {
        Debug.Log("[AudioRecorder] StopRecording invoked");
        voiceManager.StopListening();
    }

    public void PlayRecording()
    {
        Debug.Log("[AudioRecorder] PlayRecording invoked");

        AudioClip clip = voiceManager.GetRecordedAudioClip();
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log("[AudioRecorder] Playing recorded audio");
        }
        else
        {
            Debug.LogWarning("[AudioRecorder] No recorded audio available to play");
        }
    }
}