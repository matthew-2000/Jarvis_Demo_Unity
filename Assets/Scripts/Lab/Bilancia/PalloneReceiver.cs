using UnityEngine;

public class PalloneReceiver : MonoBehaviour
{
    public GameObject cartina; // assegna da Inspector

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Pallone ha toccato: {other.gameObject.name}");
        if (other.gameObject == cartina)
        {
            Debug.Log("Pallone ha toccato la cartina. Trasferimento polvere...");

            Transform cartinaTransform = cartina.transform;
            Transform palloneTransform = transform;

            // Trova la PRIMA polvere attiva nella cartina
            Transform polvereDaTrasferire = null;
            foreach (Transform child in cartinaTransform)
            {
                if (child.CompareTag("PolverePiccola") && child.gameObject.activeSelf)
                {
                    polvereDaTrasferire = child;
                    break;
                }
            }

            if (polvereDaTrasferire == null)
            {
                Debug.Log("Nessuna polvere attiva sulla cartina.");
                return;
            }

            // Trova il PRIMO slot libero nel pallone
            foreach (Transform slot in palloneTransform)
            {
                if (slot.CompareTag("PolverePiccola") && !slot.gameObject.activeSelf)
                {
                    slot.gameObject.SetActive(true);
                    polvereDaTrasferire.gameObject.SetActive(false);
                    Debug.Log("Polvere trasferita dalla cartina al pallone.");
                    return;
                }
            }

            Debug.Log("Tutti gli slot del pallone sono già pieni.");
        }
    }
}
