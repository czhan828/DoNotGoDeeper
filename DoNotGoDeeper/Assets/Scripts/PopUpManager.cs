using UnityEngine;

public class PopUpManager : MonoBehaviour
{
    // Drag your pop-up Panel GameObject here in the Inspector
    public GameObject popUpWindow;

    public static object Instance { get; internal set; }

    // Method to show the pop-up
    public void ShowPopUp()
    {
        if (popUpWindow != null)
        {
            popUpWindow.SetActive(true);
            // Optional: Pause the game when the pop-up appears
            // Time.timeScale = 0f; 
        }
    }

    // Method to hide the pop-up
    public void HidePopUp()
    {
        if (popUpWindow != null)
        {
            popUpWindow.SetActive(false);
            // Optional: Resume the game when the pop-up closes
            // Time.timeScale = 1f; 
        }
    }
}