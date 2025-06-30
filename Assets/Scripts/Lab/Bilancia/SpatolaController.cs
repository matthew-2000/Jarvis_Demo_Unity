using UnityEngine;

public class SpatolaController : MonoBehaviour
{
    public GameObject polverePrefab; // prefab della piccola sfera
    public Transform puntoPolvere;   // posizione sulla spatola dove far apparire la polvere
    private bool haPolvere = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!haPolvere && other.CompareTag("Polvere"))
        {
            Debug.Log("Spatola ha raccolto la polvere.");
            GameObject polvere = Instantiate(polverePrefab, puntoPolvere.position, Quaternion.identity, transform);
            haPolvere = true;
        }
    }

    public bool HasPolvere()
    {
        Debug.Log($"HasPolvere chiamato: {haPolvere}");
        return haPolvere;
    }

    public void RilasciaPolvere(Transform target)
    {
        if (haPolvere)
        {
            Debug.Log("Spatola rilascia la polvere.");
            Transform childPolvere = transform.GetChild(transform.childCount - 1); // ultima figlia
            childPolvere.SetParent(null);
            childPolvere.position = target.position;
            haPolvere = false;
        }
        else
        {
            Debug.Log("Tentativo di rilasciare polvere, ma la spatola è vuota.");
        }
    }

    public void RimuoviPolvere()
    {
        if (haPolvere)
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag("PolverePiccola"))
                {
                    Destroy(child.gameObject); // oppure SetActive(false) se vuoi riutilizzarla
                    break;
                }
            }

            haPolvere = false;
        }
    }


}
