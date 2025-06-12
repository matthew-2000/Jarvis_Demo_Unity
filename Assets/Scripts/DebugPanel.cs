using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DebugPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshPro debugText; // Riferimento al componente TextMeshPro
    [SerializeField] private int maxLines = 15; // Numero massimo di righe da mostrare

    private Queue<string> logQueue = new Queue<string>();

    private void Awake()
    {
        // Sottoscrivi il metodo HandleLog agli eventi di log
        Application.logMessageReceived += HandleLog;
    }

    private void OnDestroy()
    {
        // Rimuovi la sottoscrizione quando l'oggetto viene distrutto
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Aggiungi il nuovo log alla coda
        if (logQueue.Count >= maxLines)
        {
            logQueue.Dequeue(); // Rimuovi il log più vecchio
        }

        logQueue.Enqueue(logString); // Aggiungi il nuovo log
        debugText.text = string.Join("\n", logQueue.ToArray()); // Aggiorna il testo del pannello
    }
}
