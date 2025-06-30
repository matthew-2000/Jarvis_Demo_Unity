using UnityEngine;

public class LabTaskManager : MonoBehaviour
{
    private enum Step
    {
        PlaceFlask     = 0,  // F1
        WeighSolid     = 1,  // F2
        TransferSolid  = 2,  // F3
        MeasureSolvent = 3,  // F4
        AddSolvent     = 4,  // F5
        HeatTo60       = 5,  // F6
        AddH2SO4       = 6,  // F7
        AddHNO3        = 7,  // F8
        PourMixture    = 8,  // F9
        ObserveColor   = 9,  // F10
        Completed      = 10
    }

    [Header("ID oggetti principali")]
    [SerializeField] private string flaskId   = "SnapInteractionPallone";
    [SerializeField] private string supportId = "SupportoSnap";

    private Step currentStep = Step.PlaceFlask;

    private void Awake()
    {
        LabEvents.ObjectAttached += OnObjectAttached;
    }

    private void OnDestroy()
    {
        LabEvents.ObjectAttached -= OnObjectAttached;
    }

    private void OnObjectAttached(string child, string parent)
    {
        if (currentStep != Step.PlaceFlask) return;

        if (child == flaskId && parent == supportId)
        {
            Debug.Log("[LabTask] F1 completata: pallone agganciato.</color>");
            AdvanceStep();
        }
    }

    private void AdvanceStep()
    {
        currentStep++;

        if (currentStep == Step.Completed)
        {
            Debug.Log("[LabTask] Tutte le fasi completate ✅</color>");
        }
    }
}
