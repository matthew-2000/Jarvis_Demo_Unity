using UnityEngine;
using UnityEngine.Events;
using System.Text.RegularExpressions;
using Meta.WitAi.TTS.Utilities;
using TMPro;

public class ResponseHandler : MonoBehaviour
{
    [SerializeField] private AsyncRequestHandler asyncRequestHandler;
    [SerializeField] private TTSSpeaker speaker;
    [SerializeField] private TextMeshPro responseText;

    /* simple regex to pull `"response":"...text..."` */
    private static readonly Regex responseRegex =
        new Regex("\"response\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    private void Awake()
    {
        asyncRequestHandler.OnTextResponseReceived.AddListener(HandleTextResponse);
        asyncRequestHandler.OnAudioResponseReceived.AddListener(HandleAudioResponse);
    }

    private void HandleTextResponse(string json)
    {
        var match = responseRegex.Match(json);
        string llmText = match.Success ? match.Groups[1].Value : json;
        Debug.Log($"[LLM] {llmText}");
        responseText.text = llmText;
        if (speaker != null)
        {
            speaker.Speak(llmText);
            Debug.Log($"[LLM] Speaking: {llmText}");
        }
        else
        {
            Debug.LogWarning("[LLM] TTSSpeaker is not assigned!");
        }
    }

    private void HandleAudioResponse(string json)
    {
        Debug.Log($"[Server-Audio] {json}");
        // json will be  {"status":"buffering"}  OR
        //               {"status":"inferred","emotions":{...}}
        // Parse & update any UI accordingly
    }
}