using UnityEngine;
using UnityEngine.Android;

public class AudioRecorder : MonoBehaviour
{
    public AudioSource audioSource;

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
    }

    // Funzione per avviare la registrazione, può essere chiamata da qualsiasi altro evento
    public void StartRecording()
    {
        if (!isRecording && micDevice != null)
        {
            Debug.Log("Inizio registrazione...");
            recordedClip = Microphone.Start(micDevice, false, 10, 44100); // durata 10 secondi, frequenza di campionamento 44100 Hz
            isRecording = true;
        }
    }

    // Funzione per fermare la registrazione
    public void StopRecording()
    {
        if (isRecording)
        {
            Microphone.End(micDevice);
            Debug.Log("Registrazione terminata.");
            isRecording = false;
            PlayRecording(); // Riproduce la registrazione appena terminata
            Debug.Log("Registrazione salvata: " + recordedClip.name);
        }
    }

    // Funzione per riprodurre la registrazione
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
