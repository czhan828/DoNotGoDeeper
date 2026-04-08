using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTrigger : MonoBehaviour
{
    [Tooltip("Exact name of the next scene to load when the player exits.")]
    [SerializeField] string nextSceneName = "Level2";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.instance.HasEnoughKeys())
            {
                Debug.Log("Level complete! Loading: " + nextSceneName);
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.Log("Need more keys! Have " + GameManager.instance.keysCollected + "/" + GameManager.instance.keysRequired);
            }
        }
    }
}