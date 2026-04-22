using System.Collections;
using UnityEngine;

public class DoorInteration : MonoBehaviour
{
    [SerializeField] Transform door; // 👈 assign your Door child here
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 2f;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Coroutine _currentCoroutine;

    private bool _playerInRange = false;
    private bool isOpen = false;

    void Start()
    {
        _closedRotation = door.rotation;
        _openRotation = Quaternion.Euler(door.eulerAngles + new Vector3(0, openAngle, 0));
    }

    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
        }
    }
}