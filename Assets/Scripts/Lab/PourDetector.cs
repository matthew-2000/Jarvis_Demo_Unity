using UnityEngine;

[RequireComponent(typeof(LiquidContainer))]
public class PourDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] LiquidContainer source;
    [SerializeField] Transform       pourPoint;

    [Header("Audio")]
    [SerializeField] AudioSource pourAudioSource;
    [SerializeField] AudioClip  pourClip;

    [Header("Tuning")]
    [SerializeField] float     pourAngle    = 60f;    // ° rispetto alla verticale
    private float     pourRate     = 10f;    // ml / sec
    [SerializeField] float     searchRadius = 0.05f;
    [SerializeField] LayerMask receiverMask = ~0;

    bool isPouring = false;

    void Reset()
    {
        source = GetComponent<LiquidContainer>();
        pourAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        bool pouringNow = false;

        if (Vector3.Angle(transform.up, Vector3.up) >= pourAngle && source.CurrentMl > 0.001f)
        {
            Collider[] hits = Physics.OverlapSphere(
                pourPoint.position, searchRadius, receiverMask);

            LiquidContainer target = null;
            foreach (var col in hits)
            {
                var lc = col.attachedRigidbody ?
                         col.attachedRigidbody.GetComponent<LiquidContainer>() : null;
                if (lc != null && lc != source) { target = lc; break; }
            }

            if (target != null)
            {
                float delta = Mathf.Min(pourRate * Time.deltaTime, source.CurrentMl);
                LiquidPortion portion = source.Draw(delta);
                target.Add(portion.type, portion.volume, portion.topColor, portion.sideColor);
                pouringNow = true;
            }
        }

        // Suono di versamento
        if (pouringNow && !isPouring)
        {
            StartPourSound();
            isPouring = true;
        }
        else if (!pouringNow && isPouring)
        {
            StopPourSound();
            isPouring = false;
        }
    }

    void StartPourSound()
    {
        if (pourAudioSource && pourClip)
        {
            pourAudioSource.clip = pourClip;
            pourAudioSource.loop = true;
            pourAudioSource.Play();
        }
    }

    void StopPourSound()
    {
        if (pourAudioSource && pourAudioSource.isPlaying)
        {
            pourAudioSource.Stop();
        }
    }
}
