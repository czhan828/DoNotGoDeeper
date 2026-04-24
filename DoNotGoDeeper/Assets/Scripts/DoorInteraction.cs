using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction : MonoBehaviour
{
    [SerializeField] Transform door;
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 2f;
    [SerializeField] float transitionDelay = 1f;
    [SerializeField] string winSceneName = "Winning Screen";

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Coroutine _currentCoroutine;

    private bool _playerInRange = false;
    private bool isOpen = false;
    private bool _hasWon = false;

    void Start()
    {
        _closedRotation = door.rotation;
        _openRotation = Quaternion.Euler(door.eulerAngles + new Vector3(0, openAngle, 0));
    }

    void Update()
    {
        // allow E to toggle door freely, but block it after winning
        if (_playerInRange && Input.GetKeyDown(KeyCode.E) && !_hasWon)
        {
            if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
            _currentCoroutine = StartCoroutine(ToggleDoor());
        }
    }

    private IEnumerator ToggleDoor()
    {
        Quaternion targetRotation = isOpen ? _closedRotation : _openRotation;
        isOpen = !isOpen;

        while (Quaternion.Angle(door.rotation, targetRotation) > 0.01f)
        {
            door.rotation = Quaternion.Lerp(door.rotation, targetRotation, Time.deltaTime * openSpeed);
            yield return null;
        }

        door.rotation = targetRotation;

        // only trigger win on open, not on close
        if (isOpen && !_hasWon)
        {
            _hasWon = true;
            StartCoroutine(LoadWinScene());
        }
    }

    private IEnumerator LoadWinScene()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(winSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) _playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) _playerInRange = false;
    }
}