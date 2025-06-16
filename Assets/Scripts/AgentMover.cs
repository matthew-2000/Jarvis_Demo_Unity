using UnityEngine;
using System.Collections;
using Meta.WitAi.TTS.Utilities;   // solo se vuoi il TTSSpeaker, vedi note

public class AgentMover : MonoBehaviour
{
    [Header("Target (utente)")]
    [SerializeField] private Transform userHead;        // di norma CenterEyeAnchor
    [Header("Offset (metri)")]
    [SerializeField] private float forwardDistance = 1.2f;
    [SerializeField] private float heightOffset    = -0.20f;
    [Header("Animazione")]
    [SerializeField] private float moveDuration = 0.4f;

    /*──────────── Facing ────────────*/
    [Header("Facing options")]
    [Tooltip("Se true, alla fine del movimento ruota l’agente verso l’utente.")]
    [SerializeField] private bool faceUser = true;
    [Tooltip("Usa 180 se il modello è posizionato all’indietro.")]
    [SerializeField] private float yawOffset = 180f;     // 0 oppure 180 in base al rig

    /*──────────── Voce ──────────────*/
    [Header("Speech (optional)")]
    [SerializeField] private bool speakOnMove = true;
    [SerializeField] private string arrivalSentence = "Arrivo!";
    [SerializeField] private TTSSpeaker speaker;        // drag-n-drop dal prefab

    /*───────────────────────────────────────────────────────────*/

    public void MoveInFrontOfUser()
    {
        if (userHead == null) userHead = Camera.main.transform;

        Vector3 targetPos = userHead.position +
                            userHead.forward * forwardDistance +
                            Vector3.up       * heightOffset;

        if (speakOnMove && speaker != null)
            speaker.Speak(arrivalSentence);

        StopAllCoroutines();
        StartCoroutine(LerpTo(targetPos));
    }

    /*──────────────────────── helpers ────────────────────────*/
    private IEnumerator LerpTo(Vector3 target)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        while (t < moveDuration)
        {
            float k = t / moveDuration;
            transform.position = Vector3.Lerp(startPos, target, k);
            if (faceUser) FaceUser(k);            // rotazione progressiva
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = target;
        if (faceUser) FaceUser(1f);               // assicurati del facing finale
    }

    private void FaceUser(float blend)
    {
        if (userHead == null) return;

        Vector3 dir = userHead.position - transform.position;
        dir.y = 0f;                               // mantieni orizzontale
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        look *= Quaternion.Euler(0f, yawOffset, 0f);      // correzione 180°, se serve
        transform.rotation = Quaternion.Slerp(transform.rotation, look, blend);
    }
}
