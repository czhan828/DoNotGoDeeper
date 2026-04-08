using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public static GameOver Instance;
    private int respawnSceneIndex;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }
    public void EndGame(int sceneIndex) {
        respawnSceneIndex = sceneIndex;
        Cursor.lockState = CursorLockMode.None; // Release the mouse
        Cursor.visible = true;
        Debug.Log($"[GameOverManager] Game Over! Scene index to respawn: {respawnSceneIndex}");
    }
    public void RespawnLevel()
    {
        SceneManager.LoadScene(respawnSceneIndex);
    }

    public void QuitToMainMenu()
    {
        SceneManager.LoadScene(2);

    }
}