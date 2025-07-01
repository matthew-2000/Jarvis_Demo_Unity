using UnityEngine;

public class PourDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] LiquidContainer source;
    [SerializeField] Transform pourPoint;

    [Header("Tuning")]
    [SerializeField] float pourAngle   = 60f;
    [SerializeField] float pourRate    = 50f;   // ml/sec
    [SerializeField] float searchRadius = 0.05f;
    [SerializeField] LayerMask receiverMask = ~0; // opzionale: limita i collider da testare

    void Reset() => source = GetComponent<LiquidContainer>();

    void Update()
    {
        // 1. inclinazione sufficiente?
        if (Vector3.Angle(transform.up, Vector3.up) < pourAngle) return;
        if (source.CurrentMl <= 0f)                             return;

        // 2. cerca un target che abbia sia LiquidContainer che PourDetector
        Collider[] hits = Physics.OverlapSphere(
                            pourPoint.position,
                            searchRadius,
                            receiverMask);

        LiquidContainer target = null;

        foreach (Collider col in hits)
        {
            // deve avere un rigidbody (più performante risalire da lì)
            if (!col.attachedRigidbody) continue;

            // *** filtro: ci serve *sia* LiquidContainer *sia* PourDetector ***
            var lc  = col.attachedRigidbody.GetComponent<LiquidContainer>();
            var pd  = col.attachedRigidbody.GetComponent<PourDetector>();

            if (lc != null && pd != null && lc != source)   // trovato!
            {
                target = lc;
                break;
            }
        }

        if (target == null) return;

        /* 3) Trasferisci volume + colore -------------------------------- */
        float delta = Mathf.Min(pourRate * Time.deltaTime, source.CurrentMl);

        // 3.a preleva dal source
        LiquidPortion portion = source.Draw(delta);

        // 3.b versa nel target
        target.PourIn(portion);
    }
}
