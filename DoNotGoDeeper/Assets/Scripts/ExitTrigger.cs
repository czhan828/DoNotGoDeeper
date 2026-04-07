using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.instance.HasEnoughKeys())
            {
                Debug.Log("Level complete!");
                // SceneManager.LoadScene("NextLevel"); // uncomment when ready
            }
            else
            {
                Debug.Log("Need more keys!");
            }
        }
    }
}