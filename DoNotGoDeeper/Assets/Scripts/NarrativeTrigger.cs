using UnityEngine;
using TMPro;
using System.Collections;

public class NarrativeTrigger : MonoBehaviour
{
    public TextMeshProUGUI narrativeText;
    public string message = "Something terrible\nhappened here...";
    public AudioClip narrativeSound;
    public float displayDuration = 4f;

    private AudioSource audioSource;
    private bool triggered = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (narrativeText != null)
            narrativeText.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(ShowNarrative());
        }
    }

    IEnumerator ShowNarrative()
    {
        if (narrativeText != null)
        {
            narrativeText.text = message;
            narrativeText.gameObject.SetActive(true);
        }
        if (audioSource != null && narrativeSound != null)
            audioSource.PlayOneShot(narrativeSound);
        yield return new WaitForSeconds(displayDuration);
        if (narrativeText != null)
            narrativeText.gameObject.SetActive(false);
    }
}
