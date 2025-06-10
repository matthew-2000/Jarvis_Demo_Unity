using UnityEngine;
using UnityEngine.UI; // Importa il namespace per l'interfaccia utente

public class Prova : MonoBehaviour
{
    public Button myButton; // Riferimento al bottone

    void Start()
    {
        // Verifica che il bottone sia assegnato
        if (myButton != null)
        {
            // Aggiungi un listener all'evento click del bottone
            myButton.onClick.AddListener(OnButtonClick);
        }
    }

    // Metodo da chiamare al clic del bottone
    void OnButtonClick()
    {
        // Azione da fare quando il bottone viene cliccato
        Debug.Log("Bottone cliccato!");

        // Puoi aggiungere altre azioni qui, ad esempio:
        // gameObject.SetActive(false);  // Disattiva l'oggetto
        // scenaManager.LoadScene("NuovaScena"); // Carica una nuova scena
    }
}
