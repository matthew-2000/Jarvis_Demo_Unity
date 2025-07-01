using UnityEngine;
using System.Collections;
using System.Linq;

public class ReactionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] LiquidContainer   pallone;
    [SerializeField] HeatedStoneToggle piastra;        // script tuo
    [SerializeField] GameObject[]      polveri;        // granuli di resorcinolo
    [SerializeField] AudioSource       audioSource;    // opzionale, per suoni di reazione
    [SerializeField] AudioClip         reazioneClip;   // opzionale, suono di reazione
    [SerializeField] GameObject        particelle;     // opzionale, per effetti visivi

    [Header("Recipe (requisiti minimi)")]
    [SerializeField] float      sogliaVolume      = 40f;                 // ml
    [SerializeField] LiquidType solventeRichiesto = LiquidType.Etanolo;
    [SerializeField] LiquidType nitranteRichiesto = LiquidType.MiscelaNitrante;

    [Header("Colore finale")]
    [SerializeField] Color rbTop            = new (0.55f, 0.18f, 0.08f);
    [SerializeField] Color rbSide           = new (0.40f, 0.12f, 0.05f);
    [SerializeField] float tempoTransizione = 4f;

    bool reazionePartita = false;

    // ➡️  Conserviamo il delegate per poterlo rimuovere correttamente
    System.Action<bool> plateHandler;

    /* -------------------------------------------------------------- */
    void Awake()
    {
        if (!pallone)
        {
            Debug.LogError("[ReactionController] Pallone mancante!", this);
            enabled = false;
            return;
        }

        pallone.OnContentChanged += CheckReaction;
        Debug.Log("[ReactionController] Listener su pallone registrato.");

        // Registrazione listener sulla piastra (se presente)
        if (piastra)
        {
            plateHandler = _ => CheckReaction();
            piastra.OnPlateStateChanged += plateHandler;
            Debug.Log("[ReactionController] Listener su piastra registrato.");
        }
    }

    void OnDestroy()
    {
        if (pallone)  pallone.OnContentChanged    -= CheckReaction;
        if (piastra && plateHandler != null) piastra.OnPlateStateChanged -= plateHandler;
    }

    /* -------------------------------------------------------------- */
    void Update()
    {
        // piccola safety‑net nel caso qualche update sfugga ai callback
        // if (!reazionePartita) CheckReaction();
    }

    /* -------------------------------------------------------------- */
    void CheckReaction()
    {
        if (reazionePartita) return;

        Debug.Log("[ReactionController] --- Verifica reazione ---");
        Debug.Log($"Volume attuale = {pallone.CurrentMl:F1} ml (soglia {sogliaVolume})");

        /* 1️⃣ Volume minimo */
        if (pallone.CurrentMl < sogliaVolume)
        {
            Debug.Log("[ReactionController] Volume insufficiente. Abort.");
            return;
        }

        /* 2️⃣ Reagenti presenti */
        var comp = pallone.Composition;
        if (!comp.ContainsKey(solventeRichiesto))
        {
            Debug.Log("[ReactionController] Solvente richiesto assente. Abort.");
            return;
        }
        if (!comp.ContainsKey(nitranteRichiesto))
        {
            Debug.Log("[ReactionController] Miscela nitrante assente. Abort.");
            Debug.Log($"Composizione attuale: {string.Join(", ", comp.Select(kv => $"{kv.Key}: {kv.Value:F1} ml"))}");
            return;
        }
        Debug.Log("[ReactionController] Reagenti OK.");

        /* 3️⃣ Piastra accesa */
        if (!piastra || !piastra.IsOn)
        {
            Debug.Log("[ReactionController] Piastra spenta. Abort.");
            return;
        }
        Debug.Log("[ReactionController] Piastra ON.");

        /* 4️⃣ Tutti i granuli devono essere attivi */
        bool tuttiGranuliAttivi = polveri.All(g => g.activeInHierarchy);
        if (!tuttiGranuliAttivi)
        {
            Debug.Log("[ReactionController] Non tutti i granuli sono presenti. Abort.");
            return;
        }
        Debug.Log("[ReactionController] Granuli OK.");

        // ---- Tutte le condizioni rispettate! ----
        Debug.Log("[ReactionController] CONDIZIONI RISPETTATE -> Avvio reazione");
        StartCoroutine(ReactionRoutine());
    }

    /* -------------------------------------------------------------- */
    IEnumerator ReactionRoutine()
    {
        reazionePartita = true;

        Debug.Log("[ReactionController] Reazione avviata – fase di agitazione");
        yield return new WaitForSeconds(2f);

        Debug.Log("[ReactionController] Transizione colore verso rosso‑bruno");
        pallone.TransitionToColor(rbTop, rbSide, tempoTransizione);
        if (audioSource && reazioneClip)
        {
            audioSource.clip = reazioneClip;
            audioSource.loop = false;
            audioSource.Play();
        }
        if (particelle)
        {
            particelle.SetActive(true);
        }

        // eventuale logica post‑reazione…
        // piastra.SetState(false);
        Debug.Log("[ReactionController] Reazione completata");
    }
}
