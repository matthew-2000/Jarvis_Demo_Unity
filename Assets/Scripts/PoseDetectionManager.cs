using UnityEngine;

public class PoseDetectionManager : MonoBehaviour
{

    public GameObject jarvis;
    public AgentMover agentMover; // Assicurati di assegnare questo nel tuo Inspector
    public AsyncRequestHandler requestHandler; // Assicurati di assegnare questo nel tuo Inspector

    void Start()
    {
        jarvis.SetActive(false);
    }

    public void ShowJarvis()
    {
        agentMover?.MoveInFrontOfUser();
        jarvis.SetActive(true);
    }

    public void HideJarvis()
    {
        jarvis.SetActive(false);
    }

    public void ResetConversation()
    {
        // Logica per resettare la conversazione
        requestHandler?.ResetConversation();
        Debug.Log("[PoseDetectionManager] Conversation reset.");
    }
}
