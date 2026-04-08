using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    [Tooltip("The index of your Main Menu scene.")]

    // This runs when something enters the collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing that touched the model is the Player
        // Make sure your Player object has the Tag "Player" set in the Inspector!
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached the goal! Returning to Main Menu...");
            
            // Re-enable the cursor so you can actually click buttons on the menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene(2);
        }
    }
}