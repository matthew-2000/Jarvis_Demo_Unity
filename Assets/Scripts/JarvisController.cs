using UnityEngine;

public class JarvisController : MonoBehaviour
{
    [Header("Riferimento all'Orb (UI dell'agente)")]
    [SerializeField] private AgentUIController orbController; // Orb che rappresenta la UI dell'agente

    // Stato dell'agente che vogliamo mostrare
    public AgentUIController.AgentState currentState = AgentUIController.AgentState.None;
    public Color orbColor = Color.blue; // Colore dell'Orb

    // Metodo per ricevere informazioni e aggiornare lo stato dell'Orb
    public void UpdateOrbState(AgentUIController.AgentState newState)
    {
        // Puoi aggiungere logiche per modificare lo stato a seconda delle informazioni ricevute
        Debug.Log($"Jarvis: Ricevuto stato per l'Orb: {newState}");

        currentState = newState;

        // Passiamo lo stato ad Orb per l'aggiornamento
        orbController.SetState(newState);
    }

    // Metodo per aggiornare altre informazioni, come il colore o effetti specifici
    public void UpdateOrbColor(Color newColor)
    {
        Debug.Log($"Jarvis: Aggiornato colore dell'Orb: {newColor}");
        orbController.UpdateOrbColor(newColor);
    }

    // Metodo per altri tipi di informazioni (es. energia, salute)
    public void UpdateOrbEnergy(float energyLevel)
    {
        Debug.Log($"Jarvis: Aggiornato livello di energia dell'Orb: {energyLevel}");
        orbController.UpdateOrbEnergy(energyLevel);
    }

    // Utilizziamo OnValidate per applicare automaticamente i cambiamenti quando vengono fatti nell'Inspector
    void OnValidate()
    {
        // Aggiorna lo stato ogni volta che l'Inspector cambia il valore
        Debug.Log($"Jarvis: Applicazione stato {currentState} all'Orb");
        Debug.Log($"Jarvis: Applicazione colore {orbColor} all'Orb");
        orbController.SetState(currentState);
        orbController.UpdateOrbColor(orbColor);
    }
}

