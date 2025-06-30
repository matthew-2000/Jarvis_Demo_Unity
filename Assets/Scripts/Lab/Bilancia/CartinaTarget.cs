using UnityEngine;
using TMPro;

public class CartinaTarget : MonoBehaviour
{

    public TextMeshProUGUI testoMg; // assegna da Inspector
    private int quantitaRaccoltaMg = 0;
    private const int MG_PER_POLVERE = 100;
    private const int MG_TOTALE = 500;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter chiamato da: {other.gameObject.name}");

        if (other.CompareTag("Spatola"))
        {
            Debug.Log("Oggetto con tag 'Spatola' rilevato.");

            SpatolaController spatola = other.GetComponent<SpatolaController>();
            if (spatola != null && spatola.HasPolvere())
            {
                Debug.Log("La spatola ha polvere. Attivo un oggetto PolverePiccola...");

                foreach (Transform child in transform)
                {
                    if (child.CompareTag("PolverePiccola") && !child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(true);
                        spatola.RimuoviPolvere();

                        quantitaRaccoltaMg += MG_PER_POLVERE;
                        quantitaRaccoltaMg = Mathf.Min(quantitaRaccoltaMg, MG_TOTALE); // Clamp

                        AggiornaTestoMg();

                        return;
                    }
                }

                Debug.Log("Tutti gli oggetti PolverePiccola sono già attivi.");
            }
            else
            {
                Debug.Log("La spatola NON ha polvere oppure SpatolaController non trovato.");
            }
        }
    }
    
    private void AggiornaTestoMg()
    {
        if (testoMg != null)
        {
            testoMg.text = quantitaRaccoltaMg.ToString();
        }
    }

}
