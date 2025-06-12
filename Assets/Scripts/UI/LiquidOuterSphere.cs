using UnityEngine;

public class LiquidOuterSphere : MonoBehaviour
{
    public Transform innerSphere;
    public float followSpeed = 5f;
    public float damping = 0.9f;

    public float maxStretch = 0.2f;
    public float stretchLerpSpeed = 5f;

    private Vector3 velocity = Vector3.zero;

    void Update()
    {
        // Calcola differenza tra le posizioni
        Vector3 delta = innerSphere.position - transform.position;

        // Applica forza tipo molla
        velocity += delta * followSpeed * Time.deltaTime;

        // Applica damping per non farla oscillare all'infinito
        velocity *= damping;

        // Muove la sfera esterna
        transform.position += velocity * Time.deltaTime;

        // Calcola quanto "stretchare" la sfera esterna in base alla velocità
        Vector3 stretch = new Vector3(
            1f + Mathf.Abs(velocity.x) * maxStretch,
            1f + Mathf.Abs(velocity.y) * maxStretch,
            1f + Mathf.Abs(velocity.z) * maxStretch
        );

        // Applica interpolazione morbida della scala
        transform.localScale = Vector3.Lerp(transform.localScale, stretch, Time.deltaTime * stretchLerpSpeed);
    }
}
