using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class AudioRecorder : MonoBehaviour
{
    public AudioSource audioSource;
    public Button recordButton;
    public Button stopButton;
    public Button playButton;

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

    void Start()
    {
        // Richiesta permesso microfono
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        // Trova primo microfono disponibile
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            Debug.Log("Microfono trovato: " + micDevice);
        }
        else
        {
            Debug.LogError("Nessun microfono trovato!");
        }

        // Collega i pulsanti
        recordButton.onClick.AddListener(StartRecording);
        stopButton.onClick.AddListener(StopRecording);
        playButton.onClick.AddListener(PlayRecording);
    }

    public void StartRecording()
    {
        if (!isRecording && micDevice != null)
        {
            Debug.Log("Inizio registrazione...");
            recordedClip = Microphone.Start(micDevice, false, 10, 44100);
            isRecording = true;
        }
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            Microphone.End(micDevice);
            Debug.Log("Registrazione terminata.");
            isRecording = false;
        }
    }

    public void PlayRecording()
    {
        if (recordedClip != null)
        {
            Debug.Log("Riproduzione...");
            audioSource.clip = recordedClip;
            audioSource.Play();
        }
    }
}
