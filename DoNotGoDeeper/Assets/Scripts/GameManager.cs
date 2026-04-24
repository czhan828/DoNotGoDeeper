using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager — Singleton for key collection, level progression, and catch sequence.
///
/// OnPlayerCaught() is called by EnemyAI when the player enters catch radius.
/// It disables player movement, plays a jumpscare sound, fades to black,
/// then shows the game-over panel.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   Catch/Game Over section requires:
///     - playerMovement: drag the player's PlayerMovement component
///     - jumpscareAudioSource: an AudioSource on the GameManager (or Camera)
///     - jumpscareClip: a loud burst SFX
///     - gameOverPanel: the game-over UI panel (starts inactive)
///     - fadeCanvasGroup: a full-screen black Image's CanvasGroup (alpha starts at 0)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Key Collection")]
    public int keysCollected = 0;
    public int keysRequired;

    [Header("Catch / Game Over")]
    [Tooltip("The PlayerMovement component on the player — disabled when caught.")]
    [SerializeField] PlayerMovement playerMovement;

    [Tooltip("AudioSource to play the jumpscare clip through.")]
    [SerializeField] AudioSource jumpscareAudioSource;

    [Tooltip("Loud burst sound that plays when the player is caught.")]
    [SerializeField] AudioClip jumpscareClip;

    [Tooltip("The game-over UI panel. Should be inactive at game start.")]
    [SerializeField] GameObject gameOverPanel;

    [Tooltip("CanvasGroup on a full-screen black Image for fading. Alpha starts at 0.")]
    [SerializeField] CanvasGroup fadeCanvasGroup;

    [Tooltip("Seconds between being caught and the fade starting.")]
    [SerializeField] float catchDelay = 0.4f;

    [Tooltip("Duration of the fade to black.")]
    [SerializeField] float fadeDuration = 1.2f;

    private bool _caught = false;


    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        instance = this;
    }

    // ─── Key collection ───────────────────────────────────────────────────────

    public void CollectKey()
    {
        keysRequired = GetKeysRequired();
        keysCollected++;
        Debug.Log("Keys: " + keysCollected + "/" + keysRequired);
    }


    public bool HasEnoughKeys()
    {
        return keysCollected >= GetKeysRequired();
    }

    public int GetKeysRequired()
    {
        string scenename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scenename == "Level1")
        {
            return 4;
        }
        else if (scenename == "Level2")
        {
            return 6;
        }
        else if (scenename == "Level3")
        {
            return 8;
        }
        else
        {
            Debug.LogWarning("Unknown scene name: " + scenename + ". Defaulting keysRequired to 1.");
            return 1;
        }
    }

    // ─── Catch sequence ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by EnemyAI.TriggerCaught(). Runs once — subsequent calls are ignored.
    /// Sequence: disable movement → jumpscare SFX → delay → fade to black → game-over panel.
    /// </summary>
    public void OnPlayerCaught()
    {
        if (_caught) return;
        _caught = true;

        // Freeze the player immediately
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Jumpscare audio
        if (jumpscareAudioSource != null && jumpscareClip != null)
            jumpscareAudioSource.PlayOneShot(jumpscareClip);

        StartCoroutine(CatchSequence());
    }

    IEnumerator CatchSequence()
    {
        yield return new WaitForSeconds(catchDelay);

        // Fade to black
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // Show game-over UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}
