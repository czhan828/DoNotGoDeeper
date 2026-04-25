using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyOpen : MonoBehaviour
{
    public int KeyAmount;

    private GameObject keyInRange;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Key")
        {
            keyInRange = other.gameObject;
            Debug.Log("Key in range!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Key")
        {
            keyInRange = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && keyInRange != null)
        {
            KeyAmount += 1;
            Destroy(keyInRange);
            keyInRange = null;
            Debug.Log("Key picked up! Total: " + KeyAmount);
        }
    }
}