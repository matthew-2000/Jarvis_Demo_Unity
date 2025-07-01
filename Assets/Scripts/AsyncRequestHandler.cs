using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Text;

public class AsyncRequestHandler : MonoBehaviour
{
    [Header("Endpoint Configuration")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:5001";
    [SerializeField] private string userID  = "unity_client";

    [TextArea(3,10)]
    [SerializeField] private string sessionContext =
        "[CONTEXT] Stiamo iniziando un’esercitazione di realtà mista sulla sintesi chimica del 2,4,6-trinitro-resorcinolo.\n" +
        "L’utente indossa Meta Quest 3 e usa voce + gesture per manipolare pallone, becher, cilindro graduato, piastra HP e reagenti virtuali.\n" +
        "Il tuo compito è guidarlo mentre esegue, in sequenza:\n" +
        "- posizionare il pallone sul supporto;\n" +
        "- pesare 500 mg di resorcinolo;\n" +
        "- trasferire la polvere;\n" +
        "- misurare 50 mL di etanolo;\n" +
        "- aggiungere il solvente nel pallone;\n" +
        "- accendere la piastra a 60 °C;\n" +
        "- versare acido solforico dalla provetta nel becher;\n" +
        "- aggiungere lentamente acido nitrico dalla provetta nel becher;\n" +
        "- versare la miscela nitrante nel pallone;\n" +
        "- osservare il cambio di colore finale. [END CONTEXT]";

    private bool contextSent = false;        // <-- NEW

    private Dictionary<string, string> endpoints;

    [Header("Response Events")]
    public UnityEvent<string> OnTextResponseReceived;
    public UnityEvent<string> OnAudioResponseReceived;

    private void Awake()
    {
        endpoints = new Dictionary<string, string>
        {
            { "audio", $"{baseUrl}/upload_audio" },
            { "chat",  $"{baseUrl}/chat_message" },
            { "reset", $"{baseUrl}/reset_conversation" }
        };
    }

    /*────────────────────────── AUDIO ──────────────────────────*/
    public IEnumerator SendAudioAsync(byte[] audioData, string fileName = "clip.wav")
    {
        Debug.Log($"[AsyncRequestHandler] Uploading audio ({audioData.Length} bytes)");

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, fileName, "audio/wav");
        form.AddField("user_id", userID);

        using (UnityWebRequest req = UnityWebRequest.Post(endpoints["audio"], form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[AsyncRequestHandler] Audio upload OK → {req.downloadHandler.text}");
                OnAudioResponseReceived?.Invoke(req.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[AsyncRequestHandler] Audio upload FAIL: {req.error}");
            }
        }
    }

    /*────────────────────────── TEXT ───────────────────────────*/
    public IEnumerator SendTextAsync(string userText)
    {

        string text = contextSent 
            ? userText 
            : $"{sessionContext}\n\n{userText}";

        contextSent = true; // Set contextSent to true after the first message

        Debug.Log($"[AsyncRequestHandler] Sending text: {text}");

        // Escape eventuali doppi apici / backslash
        string safe = text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
        string json = $"{{\"user_id\":\"{userID}\",\"text\":\"{safe}\"}}";
        Debug.Log($"[AsyncRequestHandler] JSON payload: {json}"); // Log JSON payload
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(endpoints["chat"], "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[AsyncRequestHandler] Chat OK → {req.downloadHandler.text}");
                OnTextResponseReceived?.Invoke(req.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[AsyncRequestHandler] Chat FAIL: {req.error}");
            }
        }
    }

    /*────────────────────────── RESET ──────────────────────────*/
    public IEnumerator ResetConversation()
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userID);
        Debug.Log("[AsyncRequestHandler] Resetting conversation on server...");

        using (UnityWebRequest req = UnityWebRequest.Post(endpoints["reset"], form))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success) {
                Debug.Log("[AsyncRequestHandler] Conversation reset on server.");
                contextSent = false;
            }
            else
                Debug.LogError($"[AsyncRequestHandler] Reset FAIL: {req.error}");
        }
    }
}
