using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AgentUIController : MonoBehaviour
{
    public enum AgentState
    {
        None,
        Listening,
        Speaking
    }

    [Header("Stato corrente dell'agente")]
    private AgentState currentState;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        ApplyState();
    }

    void OnValidate()
    {
        ApplyState();
    }

    public void SetState(AgentState newState)
    {
        currentState = newState;
        ApplyState();
    }

    private void ApplyState()
    {
        // Configura lo stato visivo dell'Orb in base allo stato dell'agente
        Debug.Log($"Orb: Applicando stato {currentState}");

        switch (currentState)
        {
            case AgentState.None:
                _propertyBlock.SetFloat("_SurfaceMovementSpeed", 0.0f);
                _propertyBlock.SetFloat("_NoiseScale", 0.0f);
                break;

            case AgentState.Listening:
                _propertyBlock.SetFloat("_SurfaceMovementSpeed", 0.01f);
                _propertyBlock.SetFloat("_NoiseScale", 0.1f);
                break;

            case AgentState.Speaking:
                _propertyBlock.SetFloat("_SurfaceMovementSpeed", 0.02f);
                _propertyBlock.SetFloat("_NoiseScale", 0.4f);
                break;
        }

        _renderer.SetPropertyBlock(_propertyBlock);
    }

    // Metodo per aggiornare il colore dell'Orb (es. personalizzazione)
    public void UpdateOrbColor(Color color)
    {
        Debug.Log($"Orb: Aggiornamento colore: {color}");
        _propertyBlock.SetColor("_OrbColor", color);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    // Metodo per aggiornare altre informazioni, come l'energia
    public void UpdateOrbEnergy(float energyLevel)
    {
        Debug.Log($"Orb: Aggiornamento livello di energia: {energyLevel}");
        // Potresti gestire visivamente l'energia (ad esempio, un parametro di emissione)
        _propertyBlock.SetFloat("_EnergyLevel", energyLevel);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}