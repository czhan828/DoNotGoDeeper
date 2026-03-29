using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
    public string nextScene = "Level 2";

    private void OnTriggerStay(Collider other)
{
    if (other.CompareTag("Player"))
    {
        SceneManager.LoadScene(nextScene);
    }
}
}