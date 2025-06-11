using UnityEngine;

public class PoseDetectionManager : MonoBehaviour
{

    public GameObject jarvis;

    void Start()
    {
        jarvis.SetActive(false);
    }

    public void ShowJarvis()
    {
        jarvis.SetActive(true);
    }
    
    public void HideJarvis()
    {
        jarvis.SetActive(false);
    }
}
