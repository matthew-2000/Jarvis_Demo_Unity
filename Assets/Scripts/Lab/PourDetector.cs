using UnityEngine;

public class PourDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] LiquidContainer source;    // il tuo stesso bicchiere
    [SerializeField] Transform pourPoint;       // empty sul bordo superiore

    [Header("Tuning")]
    [SerializeField] float pourAngle = 60f;     // ° oltre i quali si versa
    [SerializeField] float pourRate  = 50f;     // ml / secondo
    [SerializeField] float searchRadius = 0.25f;// quanto lontano cercare

    void Reset()
    {
        source = GetComponent<LiquidContainer>();
    }

    void Update()
    {
        // 1. abbastanza inclinato?
        if (Vector3.Angle(transform.up, Vector3.up) < pourAngle || source.CurrentMl <= 0f)
            return;

        // 2. determina quanto liquido versare questo frame
        float delta = Mathf.Min(pourRate * Time.deltaTime, source.CurrentMl);
        source.Remove(delta);

        // 3. cerca un altro bicchiere vicino al punto di versamento
        Collider[] hits = Physics.OverlapSphere(
            pourPoint.position,
            searchRadius);

        foreach (Collider col in hits)
        {
            if (col.attachedRigidbody == null) continue;          // niente rigidbody? salta
            LiquidContainer target = col.attachedRigidbody.GetComponent<LiquidContainer>();
            if (target != null && target != source)
            {
                target.Add(delta);
                break;  // trovato il primo, basta così
            }
        }
    }
}
