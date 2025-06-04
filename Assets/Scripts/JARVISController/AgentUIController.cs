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
    public AgentState currentState;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    // Gestore separato della logica dello stato
    private IAgentStateLogic _stateLogic;

    // Futuro gestore dei colori
    [SerializeField]
    private AgentColorController colorController;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();

        // Puoi implementare una fabbrica o iniezione se servono comportamenti dinamici
        _stateLogic = new DefaultAgentStateLogic();
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
        if (_stateLogic == null || _renderer == null) return;

        _stateLogic.Configure(currentState, _propertyBlock);

        // Gestione colore futura (non attiva ora)
        if (colorController != null)
            colorController.ApplyColor(currentState, _propertyBlock);

        _renderer.SetPropertyBlock(_propertyBlock);
    }
}

public interface IAgentStateLogic
{
    void Configure(AgentUIController.AgentState state, MaterialPropertyBlock propertyBlock);
}

public class DefaultAgentStateLogic : IAgentStateLogic
{
    public void Configure(AgentUIController.AgentState state, MaterialPropertyBlock block)
    {
        switch (state)
        {
            case AgentUIController.AgentState.None:
                block.SetFloat("_SurfaceMovementSpeed", 0.0f);
                block.SetFloat("_NoiseScale", 0.0f);
                break;

            case AgentUIController.AgentState.Listening:
                block.SetFloat("_SurfaceMovementSpeed", 0.01f);
                block.SetFloat("_NoiseScale", 0.1f);
                break;

            case AgentUIController.AgentState.Speaking:
                block.SetFloat("_SurfaceMovementSpeed", 0.02f);
                block.SetFloat("_NoiseScale", 0.4f);
                break;
        }
    }
}
