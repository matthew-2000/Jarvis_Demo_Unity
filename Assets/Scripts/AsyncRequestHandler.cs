using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class AsyncRequestHandler : MonoBehaviour
{
    [Header("Endpoint Configuration")]
    [SerializeField] private string baseUrl = "https://example.com/api";
    private Dictionary<string, string> endpoints;

    [Header("Response Events")]
    public UnityEvent<string> OnTextResponseReceived;
    public UnityEvent<string> OnAudioResponseReceived;

    private void Awake()
    {
        // Configurazione centralizzata degli endpoint
        endpoints = new Dictionary<string, string>
        {
            { "text", $"{baseUrl}/text" },
            { "audio", $"{baseUrl}/audio" }
        };
    }

    public IEnumerator SendTextAsync(string text)
    {
        Debug.Log($"[AsyncRequestHandler] Sending text: {text}");
        WWWForm form = new WWWForm();
        form.AddField("text", text);

        using (UnityWebRequest request = UnityWebRequest.Post(endpoints["text"], form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[AsyncRequestHandler] Text sent successfully: {request.downloadHandler.text}");
                OnTextResponseReceived?.Invoke(request.downloadHandler.text); // Invoca l'evento
            }
            else
            {
                Debug.LogError($"[AsyncRequestHandler] Failed to send text: {request.error}");
            }
        }
    }

    public IEnumerator SendAudioAsync(byte[] audioData, string fileName)
    {
        Debug.Log($"[AsyncRequestHandler] Sending audio file: {fileName}");
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, fileName, "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(endpoints["audio"], form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[AsyncRequestHandler] Audio sent successfully: {request.downloadHandler.text}");
                OnAudioResponseReceived?.Invoke(request.downloadHandler.text); // Invoca l'evento
            }
            else
            {
                Debug.LogError($"[AsyncRequestHandler] Failed to send audio: {request.error}");
            }
        }
    }
}
