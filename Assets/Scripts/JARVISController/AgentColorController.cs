using UnityEngine;

[System.Serializable]
public class AgentColorController
{
    public void ApplyColor(AgentUIController.AgentState state, MaterialPropertyBlock block)
    {
        // In futuro puoi impostare un dizionario o profili colore per stato
        Color c = Color.white;

        switch (state)
        {
            case AgentUIController.AgentState.Listening:
                c = Color.yellow;
                break;

            case AgentUIController.AgentState.Speaking:
                c = Color.green;
                break;

            case AgentUIController.AgentState.None:
                c = Color.blue;
                break;
        }

        block.SetColor("_OrbColor", c);
    }
}
