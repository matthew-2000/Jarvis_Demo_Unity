using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Supporto S1 che, quando il pallone P1 viene snappato,
/// notifica LabEvents.ObjectAttached("P1","S1").
/// Rimpiazza il componente SnapInteractable originale sul supporto.
/// </summary>
public class FlaskSupportSnap : SnapInteractable
{
    [Tooltip("ID del pallone che deve essere riconosciuto")]
    [SerializeField] private string flaskId = "SnapInteractionPallone";

    // viene invocato dall'Interaction SDK appena un nuovo interactor
    // entra nello stato *Select* con questa snap-zone
    protected override void SelectingInteractorAdded(SnapInteractor interactor)
    {
        Debug.Log($"[FlaskSupportSnap] SelectingInteractorAdded called on {name} with interactor: {(interactor != null ? interactor.gameObject.name : "null")}");
        base.SelectingInteractorAdded(interactor);

        if (interactor != null &&
            interactor.gameObject != null &&
            interactor.gameObject.name == flaskId)
        {
            Debug.Log($"[F1] {flaskId} agganciato a {name}");
            LabEvents.RaiseObjectAttached(flaskId, name); // P1 → S1
        }
        else
        {
            Debug.Log($"[FlaskSupportSnap] Interactor non valido o nome non corrispondente: {(interactor != null ? interactor.gameObject.name : "null")}, richiesto: {flaskId}");
        }
    }

    // facoltativo – se ti serve sapere quando viene staccato
    /*
    protected override void SelectingInteractorRemoved(SnapInteractor interactor)
    {
        base.SelectingInteractorRemoved(interactor);

        if (interactor != null &&
            interactor.gameObject != null &&
            interactor.gameObject.name == flaskId)
        {
            Debug.Log($"[F1] {flaskId} rimosso da {name}");
        }
    }
    */
}