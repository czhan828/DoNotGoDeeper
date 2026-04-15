using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Door : MonoBehaviour
{
    public GameObject door;
    public Vector3 openRotation = new Vector3(0, 120, 0);
    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
            Open();
    }

    void Open()
    {
        door.transform.localEulerAngles = openRotation;
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Player")
            Close();
    }

    void Close()
    {
        door.transform.localEulerAngles = Vector3.zero;
    }
}