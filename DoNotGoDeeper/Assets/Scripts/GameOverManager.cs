using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public void RespawnLevel()
    {
        SceneManager.LoadSceneAsync(0);

    }

    public void QuitToMainMenu()
    {
        SceneManager.LoadSceneAsync(2);

    }
}