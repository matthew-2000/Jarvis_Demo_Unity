using UnityEngine;

public class ResponseHandler : MonoBehaviour
{
    [SerializeField] private AsyncRequestHandler asyncRequestHandler;

    private void Awake()
    {
        asyncRequestHandler.OnTextResponseReceived.AddListener(HandleTextResponse);
        asyncRequestHandler.OnAudioResponseReceived.AddListener(HandleAudioResponse);
    }

    private void HandleTextResponse(string response)
    {
        Debug.Log($"[ResponseHandler] Text response received: {response}");
        // Logica per gestire la risposta del testo
    }

    private void HandleAudioResponse(string response)
    {
        Debug.Log($"[ResponseHandler] Audio response received: {response}");
        // Logica per gestire la risposta dell'audio
    }
}
